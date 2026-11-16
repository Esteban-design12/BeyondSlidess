using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BeyondSlides.EditorTools
{
    /// <summary>
    /// Construye toda la escena del proyecto desde el menú
    ///     Tools > Beyond Slides
    /// Crea el hub, las 4 estaciones, los managers y el jugador (escritorio o XR).
    /// Todo queda como GameObjects normales, así que luego se puede editar a mano.
    /// </summary>
    public static class BeyondSlidesBuilder
    {
        const string RootName = "BeyondSlides";
        const string PlayerName = "Jugador";
        const string MatFolder = "Assets/BeyondSlides/Materials";
        const float StationDistance = 10f;

        static readonly Color White = new Color(0.95f, 0.95f, 0.95f);

        // ------------------------------------------------------------------ menú

        [MenuItem("Tools/Beyond Slides/1. Construir escena - Escritorio (sin visor)", false, 1)]
        static void BuildDesktop() { Build(false); }

        [MenuItem("Tools/Beyond Slides/2. Construir escena - XR (visor o simulador)", false, 2)]
        static void BuildXR() { Build(true); }

        [MenuItem("Tools/Beyond Slides/Cambiar jugador a Escritorio", false, 20)]
        static void SwitchDesktop() { SwitchPlayer(false); }

        [MenuItem("Tools/Beyond Slides/Cambiar jugador a XR", false, 21)]
        static void SwitchXR() { SwitchPlayer(true); }

        static void SwitchPlayer(bool xr)
        {
            var root = GameObject.Find(RootName);
            if (root == null)
            {
                EditorUtility.DisplayDialog("Beyond Slides", "Primero construye la escena desde Tools > Beyond Slides.", "OK");
                return;
            }
            CreatePlayer(root.transform, xr);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------------ construcción

        static void Build(bool xr)
        {
            var old = GameObject.Find(RootName);
            if (old != null)
            {
                if (!EditorUtility.DisplayDialog("Beyond Slides",
                        "Ya existe una escena construida. ¿Reemplazarla? (Se pierden los cambios hechos a mano dentro de 'BeyondSlides').",
                        "Reemplazar", "Cancelar"))
                    return;
                Object.DestroyImmediate(old);
            }

            EnsureFolders();
            var root = new GameObject(RootName).transform;

            // --- Managers ---
            var managers = new GameObject("Managers");
            managers.transform.SetParent(root, false);
            managers.AddComponent<HapticsManager>();
            var stationManager = managers.AddComponent<StationManager>();
            var audience = managers.AddComponent<AudienceClient>();

            // --- Suelo ---
            var floor = Prim(PrimitiveType.Cube, "Suelo", root, new Vector3(0f, -0.1f, 0f),
                new Vector3(40f, 0.2f, 40f), Mat("Suelo", new Color(0.16f, 0.17f, 0.2f)), true);
            var teleportArea = XRReflection.AddIfAvailable(floor, "TeleportationArea");
            SetTeleportLayer(teleportArea);

            // --- Hub + estaciones ---
            var stations = new List<Station>();

            var hub = CreateStation(root, 0, "Hub central", Vector3.zero, Vector3.forward,
                new Color(0.75f, 0.75f, 0.8f), new float[] { 110f, 164.81f, 220f },
                "Punto de inicio.\nCamina o teletranspórtate a cada estación.");
            stations.Add(hub);
            audience.infoText = TextUtil.Create(hub.transform, "TextoAudiencia",
                "Audiencia: servidor desconectado", new Vector3(0f, 4.3f, 3.2f), 0.16f, new Color(0.6f, 0.9f, 1f));

            var dirs = new Vector3[] { Vector3.forward, Vector3.left, Vector3.right, Vector3.back };
            var titles = new string[]
            {
                "Estación 1 · Objeto manipulable",
                "Estación 2 · Gráfico + háptica",
                "Estación 3 · Línea de tiempo",
                "Estación 4 · Cierre + audio"
            };
            var colors = new Color[]
            {
                new Color(0.25f, 0.6f, 1f),
                new Color(1f, 0.55f, 0.2f),
                new Color(0.65f, 0.4f, 1f),
                new Color(0.25f, 0.85f, 0.55f)
            };
            var chords = new float[][]
            {
                new float[] { 261.63f, 329.63f, 392f },     // Do mayor: abierto, curioso
                new float[] { 146.83f, 220f, 293.66f },     // Re en quintas: firme, "datos"
                new float[] { 220f, 261.63f, 329.63f },     // La menor: memoria, historia
                new float[] { 196f, 246.94f, 293.66f, 392f } // Sol mayor: cierre luminoso
            };
            var hints = new string[]
            {
                "Agarra el objeto, gíralo y cámbiale el tamaño.",
                "Toca las barras: la vibración crece con el valor.\nEl dato más alto vibra más fuerte.",
                "Toca cada hito para ver su historia.",
                "Pulsa el botón para cerrar la presentación."
            };

            for (int i = 0; i < 4; i++)
            {
                var st = CreateStation(root, i + 1, titles[i], dirs[i] * StationDistance, dirs[i],
                    colors[i], chords[i], hints[i]);
                stations.Add(st);
                CreatePath(root, dirs[i], colors[i], i + 1);

                // Letrero en el hub que señala hacia la estación
                var sign = TextUtil.Create(hub.transform, "Letrero_" + (i + 1), titles[i],
                    dirs[i] * 3.4f + Vector3.up * 2.3f, 0.14f, colors[i]);
                sign.transform.localRotation = Quaternion.LookRotation(dirs[i]);
            }

            BuildStation1(stations[1], colors[0]);
            BuildStation2(stations[2], colors[1]);
            BuildStation3(stations[3], colors[2]);
            BuildStation4(stations[4], colors[3]);

            stationManager.stations = stations;

            // --- Jugador ---
            CreatePlayer(root, xr);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root.gameObject;
            AssetDatabase.SaveAssets();
            Debug.Log("[BeyondSlides] Escena construida. Guarda la escena (Ctrl+S) y pulsa Play.");
        }

        // ------------------------------------------------------------------ estación genérica

        static Station CreateStation(Transform root, int index, string title, Vector3 pos, Vector3 dir,
            Color color, float[] chord, string hint)
        {
            var go = new GameObject(index == 0 ? "Hub" : "Estacion" + index);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.LookRotation(dir);
            var t = go.transform;

            var st = go.AddComponent<Station>();
            st.index = index;
            st.title = title;
            st.radius = 4f;

            // Plataforma (sin collider: el suelo es quien recibe pasos y teletransporte)
            Prim(PrimitiveType.Cylinder, "Plataforma", t, new Vector3(0f, 0.02f, 0f),
                new Vector3(7f, 0.02f, 7f), Mat("Plataforma" + index, Color.Lerp(color, Color.black, 0.55f)), false);

            // Letrero
            TextUtil.Create(t, "Titulo", title, new Vector3(0f, 3.5f, 3.2f), 0.3f, color);
            TextUtil.Create(t, "Ayuda", hint, new Vector3(0f, 2.75f, 3.2f), 0.12f, White);
            if (index != 0)
            {
                st.statusText = TextUtil.Create(t, "Estado", "PENDIENTE", new Vector3(0f, 3.15f, 3.2f), 0.13f,
                    new Color(0.7f, 0.7f, 0.7f));
                var marker = Prim(PrimitiveType.Sphere, "Marcador", t, new Vector3(0f, 4.05f, 3.2f),
                    Vector3.one * 0.3f, Mat("Marcador" + index, new Color(0.5f, 0.5f, 0.5f)), false);
                st.marker = marker.GetComponent<Renderer>();
            }

            // Luz de color
            var lightGo = new GameObject("Luz");
            lightGo.transform.SetParent(t, false);
            lightGo.transform.localPosition = new Vector3(0f, 3f, 0.5f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 9f;
            light.intensity = 2f;

            // Ambiente sonoro espacial
            var amb = new GameObject("AmbienteSonoro");
            amb.transform.SetParent(t, false);
            amb.transform.localPosition = new Vector3(0f, 1.5f, 1.5f);
            amb.AddComponent<AudioSource>().playOnAwake = false;
            var ambience = amb.AddComponent<StationAmbience>();
            ambience.frequencies = chord;

            // Panel de reacciones de la audiencia
            CreateAudiencePanel(st, color);
            return st;
        }

        static void CreateAudiencePanel(Station st, Color color)
        {
            var panel = new GameObject("PanelAudiencia");
            panel.transform.SetParent(st.transform, false);
            var local = new Vector3(2.7f, 1.45f, 1.3f);
            panel.transform.localPosition = local;
            panel.transform.localRotation = Quaternion.LookRotation(new Vector3(local.x, 0f, local.z));

            Prim(PrimitiveType.Cube, "Tablero", panel.transform, new Vector3(0f, 0f, 0.04f),
                new Vector3(1.5f, 1f, 0.04f), Mat("Tablero", new Color(0.08f, 0.09f, 0.12f)), false);
            TextUtil.Create(panel.transform, "Titulo", "AUDIENCIA EN VIVO", new Vector3(0f, 0.36f, 0f), 0.09f, color);

            string[] ids = { "like", "duda", "aplauso" };
            string[] labels = { "Me interesa", "Tengo una duda", "Aplausos" };
            for (int i = 0; i < ids.Length; i++)
            {
                float y = 0.14f - i * 0.22f;
                var dot = Prim(PrimitiveType.Sphere, "Indicador_" + ids[i], panel.transform,
                    new Vector3(-0.6f, y, 0f), Vector3.one * 0.08f, Mat("Indicador", new Color(0.45f, 0.45f, 0.5f)), false);
                var text = TextUtil.Create(panel.transform, "Texto_" + ids[i], labels[i] + ": 0",
                    new Vector3(0.08f, y, 0f), 0.085f, White);

                var pt = dot.AddComponent<PingTarget>();
                pt.id = "s" + st.index + "_" + ids[i];
                pt.label = labels[i];
                pt.countText = text;
                pt.showLabelInText = true;
                pt.indicator = dot.GetComponent<Renderer>();
                st.pingTargets.Add(pt);
            }
        }

        static void CreatePath(Transform root, Vector3 dir, Color color, int index)
        {
            var path = Prim(PrimitiveType.Cube, "Camino" + index, root, dir * (StationDistance * 0.5f) + Vector3.up * 0.005f,
                new Vector3(1.2f, 0.01f, StationDistance - 6.5f), Mat("Camino" + index, Color.Lerp(color, Color.black, 0.3f)), false);
            path.transform.localRotation = Quaternion.LookRotation(dir);
        }

        // ------------------------------------------------------------------ Estación 1

        static void BuildStation1(Station st, Color color)
        {
            var t = st.transform;
            Prim(PrimitiveType.Cylinder, "Pedestal", t, new Vector3(0f, 0.45f, 1.6f),
                new Vector3(0.6f, 0.45f, 0.6f), Mat("Pedestal", new Color(0.3f, 0.3f, 0.35f)), true);

            var obj = new GameObject("ObjetoManipulable");
            obj.transform.SetParent(t, false);
            obj.transform.localPosition = new Vector3(0f, 1.35f, 1.6f);

            var core = Mat("Molecula_Nucleo", color);
            var atom = Mat("Molecula_Atomo", new Color(1f, 0.35f, 0.35f));
            var bond = Mat("Molecula_Enlace", new Color(0.85f, 0.85f, 0.85f));

            Prim(PrimitiveType.Sphere, "Nucleo", obj.transform, Vector3.zero, Vector3.one * 0.3f, core, true);
            var offsets = new Vector3[]
            {
                new Vector3(0.28f, 0.2f, 0f), new Vector3(-0.28f, 0.2f, 0f),
                new Vector3(0f, -0.2f, 0.28f), new Vector3(0f, -0.2f, -0.28f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                Prim(PrimitiveType.Sphere, "Atomo" + i, obj.transform, offsets[i], Vector3.one * 0.16f, atom, true);
                var b = Prim(PrimitiveType.Cylinder, "Enlace" + i, obj.transform, offsets[i] * 0.5f,
                    new Vector3(0.04f, offsets[i].magnitude * 0.5f, 0.04f), bond, false);
                b.transform.localRotation = Quaternion.FromToRotation(Vector3.up, offsets[i].normalized);
            }

            var rb = obj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            var manip = obj.AddComponent<ManipulableObject>();
            manip.station = st;

            var grab = XRReflection.AddIfAvailable(obj, "XRGrabInteractable");
            XRReflection.TrySet(grab, "movementType", "Instantaneous");
            XRReflection.TrySet(grab, "throwOnDetach", false);
            XRReflection.TrySet(grab, "useDynamicAttach", true);
        }

        // ------------------------------------------------------------------ Estación 2

        static void BuildStation2(Station st, Color color)
        {
            var t = st.transform;
            const float topY = 0.9f;
            const float z = 1.8f;

            Prim(PrimitiveType.Cube, "Mesa", t, new Vector3(0f, topY * 0.5f, z),
                new Vector3(3.4f, topY, 0.7f), Mat("Mesa", new Color(0.22f, 0.22f, 0.26f)), true);
            TextUtil.Create(t, "TituloGrafico", "Atención de la audiencia según el formato (datos de ejemplo)",
                new Vector3(0f, 0.55f, z - 0.36f), 0.07f, White);

            // DATOS DE EJEMPLO: cámbienlos por los suyos aquí o en el Inspector (DataBar.value).
            string[] names = { "Lectura", "Slides 2D", "Video", "Demo en vivo", "XR" };
            float[] values = { 20f, 35f, 50f, 70f, 90f };

            var chartGo = new GameObject("Grafico");
            chartGo.transform.SetParent(t, false);
            var chart = chartGo.AddComponent<BarChart>();
            chart.station = st;
            var bars = new DataBar[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                float x = -1.2f + i * 0.6f;
                float h = values[i] / 100f * 1.1f;
                var barColor = Color.Lerp(new Color(0.35f, 0.4f, 0.5f), color, values[i] / 100f);

                var bar = Prim(PrimitiveType.Cube, "Barra_" + names[i], chartGo.transform,
                    new Vector3(x, topY + h * 0.5f, z), new Vector3(0.38f, h, 0.38f), Mat("Barra" + i, barColor), true);
                bar.AddComponent<Touchable>();
                XRReflection.AddIfAvailable(bar, "XRSimpleInteractable");
                var db = bar.AddComponent<DataBar>();
                db.value = values[i];
                bars[i] = db;

                TextUtil.Create(chartGo.transform, "Valor_" + i, values[i].ToString("0") + "%",
                    new Vector3(x, topY + h + 0.1f, z), 0.1f, White);
                TextUtil.Create(chartGo.transform, "Nombre_" + i, names[i],
                    new Vector3(x, topY - 0.12f, z - 0.36f), 0.065f, White);

                // Ping de la audiencia sobre esta barra
                var dot = Prim(PrimitiveType.Sphere, "Ping_" + i, chartGo.transform,
                    new Vector3(x, topY + h + 0.3f, z), Vector3.one * 0.1f, Mat("Indicador", new Color(0.45f, 0.45f, 0.5f)), false);
                var count = TextUtil.Create(chartGo.transform, "Conteo_" + i, "",
                    new Vector3(x, topY + h + 0.45f, z), 0.08f, new Color(1f, 0.9f, 0.2f));
                var pt = dot.AddComponent<PingTarget>();
                pt.id = "s2_barra" + i;
                pt.label = names[i];
                pt.countText = count;
                pt.showLabelInText = false;
                pt.indicator = dot.GetComponent<Renderer>();
                st.pingTargets.Insert(i, pt);
            }
            chart.bars = bars;
        }

        // ------------------------------------------------------------------ Estación 3

        static void BuildStation3(Station st, Color color)
        {
            var t = st.transform;
            const float y = 1.25f;
            const float z = 1.8f;

            Prim(PrimitiveType.Cube, "Riel", t, new Vector3(0f, y, z), new Vector3(4.2f, 0.04f, 0.04f),
                Mat("Riel", new Color(0.8f, 0.8f, 0.85f)), false);

            // CONTENIDO DE EJEMPLO: verifiquen fechas y cámbienlas por las de su narrativa.
            string[] years = { "1987", "2009", "2013", "2020", "2026" };
            string[] texts =
            {
                "1987 · Nace PowerPoint:\nlas diapositivas pasan al computador.",
                "2009 · Prezi propone un lienzo con zoom,\npero el contenido sigue siendo 2D.",
                "2013 · Zoom se abre al público:\nlas reuniones empiezan a ser remotas.",
                "2020 · La pandemia lleva las presentaciones\na videollamadas: slides 2D + video pasivo.",
                "2026 · Beyond Slides: un espacio XR donde\nse manipula contenido 3D y la audiencia participa."
            };

            var tl = t.gameObject.AddComponent<TimelineStation>();
            tl.station = st;
            tl.descriptions = texts;
            tl.infoText = TextUtil.Create(t, "TextoHito", "Toca un hito de la línea de tiempo",
                new Vector3(0f, 2.15f, z), 0.12f, new Color(1f, 0.9f, 0.6f));
            tl.milestones = new Touchable[years.Length];

            var mat = Mat("Hito", Color.Lerp(color, Color.white, 0.2f));
            for (int i = 0; i < years.Length; i++)
            {
                float x = -1.8f + i * 0.9f;
                var m = Prim(PrimitiveType.Sphere, "Hito_" + years[i], t, new Vector3(x, y, z), Vector3.one * 0.26f, mat, true);
                tl.milestones[i] = m.AddComponent<Touchable>();
                XRReflection.AddIfAvailable(m, "XRSimpleInteractable");
                TextUtil.Create(t, "Anio_" + years[i], years[i], new Vector3(x, y - 0.28f, z), 0.11f, White);
            }
        }

        // ------------------------------------------------------------------ Estación 4

        static void BuildStation4(Station st, Color color)
        {
            var t = st.transform;
            Prim(PrimitiveType.Cylinder, "Pedestal", t, new Vector3(0f, 0.5f, 1.5f),
                new Vector3(0.5f, 0.5f, 0.5f), Mat("Pedestal", new Color(0.3f, 0.3f, 0.35f)), true);

            var button = Prim(PrimitiveType.Cylinder, "BotonCierre", t, new Vector3(0f, 1.05f, 1.5f),
                new Vector3(0.36f, 0.05f, 0.36f), Mat("BotonCierre", new Color(0.9f, 0.2f, 0.25f)), false);
            var box = button.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 2f, 1f);
            var touch = button.AddComponent<Touchable>();
            XRReflection.AddIfAvailable(button, "XRSimpleInteractable");

            TextUtil.Create(t, "TextoBoton", "CERRAR PRESENTACIÓN\n(clic / gatillo)", new Vector3(0f, 1.45f, 1.5f), 0.08f, White);

            var closing = t.gameObject.AddComponent<ClosingStation>();
            closing.station = st;
            closing.button = touch;
            closing.summaryText = TextUtil.Create(t, "Resumen", "", new Vector3(0f, 2.1f, 2.6f), 0.14f, color);
            closing.stationLight = t.Find("Luz") != null ? t.Find("Luz").GetComponent<Light>() : null;
        }

        // ------------------------------------------------------------------ jugador

        static void CreatePlayer(Transform root, bool xr)
        {
            var old = root.Find(PlayerName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var holder = new GameObject(PlayerName).transform;
            holder.SetParent(root, false);

            bool xrCreated = false;
            if (xr)
            {
                var rigPrefab = FindPrefab("XR Origin (XR Rig)", "XR Origin");
                if (rigPrefab == null)
                {
                    EditorUtility.DisplayDialog("Beyond Slides",
                        "No encontré el prefab 'XR Origin (XR Rig)'.\n\n" +
                        "Instala el paquete XR Interaction Toolkit e importa el sample 'Starter Assets' " +
                        "(Window > Package Manager > XR Interaction Toolkit > Samples) y vuelve a ejecutar " +
                        "'Tools > Beyond Slides > Cambiar jugador a XR'.\n\nPor ahora dejo el jugador de escritorio.",
                        "OK");
                }
                else
                {
                    var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                    rig.transform.SetParent(holder, false);
                    xrCreated = true;

                    var simPrefab = FindPrefab("XR Interaction Simulator", "XR Device Simulator");
                    if (simPrefab != null)
                    {
                        var sim = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
                        sim.transform.SetParent(holder, false);
                    }
                    else
                    {
                        Debug.Log("[BeyondSlides] No encontré el simulador XR. Para probar sin visor importa el sample " +
                                  "'XR Device Simulator' del XR Interaction Toolkit y arrastra su prefab a la escena.");
                    }

                    if (XRReflection.FindType("TeleportationArea") != null && GameObject.Find("Suelo") != null)
                    {
                        var area = XRReflection.AddIfAvailable(GameObject.Find("Suelo"), "TeleportationArea");
                        SetTeleportLayer(area);
                    }
                }
            }

            if (!xrCreated)
            {
                var player = new GameObject("DesktopPlayer");
                player.transform.SetParent(holder, false);
                var cc = player.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.3f;
                cc.center = new Vector3(0f, 0.9f, 0f);

                var camGo = new GameObject("Camara");
                camGo.transform.SetParent(player.transform, false);
                camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.nearClipPlane = 0.05f;
                camGo.AddComponent<AudioListener>();

                var dp = player.AddComponent<DesktopPlayer>();
                dp.cam = cam;
            }

            RemoveOtherCameras(holder);
        }

        /// <summary>Quita las cámaras sueltas (la "Main Camera" por defecto) para que solo quede la del jugador.</summary>
        static void RemoveOtherCameras(Transform keepUnder)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var cam in go.GetComponentsInChildren<Camera>(true))
                {
                    if (cam == null || cam.transform.IsChildOf(keepUnder)) continue;
                    Debug.Log("[BeyondSlides] Se eliminó la cámara '" + cam.name + "' (ahora se usa la del jugador).");
                    Object.DestroyImmediate(cam.gameObject);
                }
            }
        }

        static GameObject FindPrefab(params string[] names)
        {
            foreach (var wanted in names)
            {
                GameObject candidate = null;
                foreach (var guid in AssetDatabase.FindAssets(wanted + " t:Prefab"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;
                    if (prefab.name == wanted) return prefab;
                    if (candidate == null && prefab.name.StartsWith(wanted)) candidate = prefab;
                }
                if (candidate != null) return candidate;
            }
            return null;
        }

        /// <summary>
        /// El rig de Starter Assets teletransporta solo sobre la Interaction Layer "Teleport" (capa 31).
        /// </summary>
        static void SetTeleportLayer(Component teleportArea)
        {
            if (teleportArea == null) return;
            var so = new SerializedObject(teleportArea);
            var bits = so.FindProperty("m_InteractionLayers.m_Bits");
            if (bits != null)
            {
                bits.longValue = 1L << 31;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[BeyondSlides] No pude asignar la Interaction Layer 'Teleport' al suelo. " +
                                 "Selecciona 'Suelo' y en Teleportation Area > Interaction Layer Mask elige 'Teleport'.");
            }
        }

        // ------------------------------------------------------------------ utilidades

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos,
            Vector3 localScale, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            return go;
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/BeyondSlides"))
                AssetDatabase.CreateFolder("Assets", "BeyondSlides");
            if (!AssetDatabase.IsValidFolder(MatFolder))
                AssetDatabase.CreateFolder("Assets/BeyondSlides", "Materials");
        }

        static Shader LitShader()
        {
            Shader s = null;
            if (GraphicsSettings.currentRenderPipeline != null)
                s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            return s;
        }

        static Material Mat(string name, Color color)
        {
            EnsureFolders();
            string path = MatFolder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(LitShader());
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.shader = LitShader();
            }
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
