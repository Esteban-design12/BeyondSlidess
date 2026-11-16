using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace BeyondSlides
{
    public enum HapticPattern
    {
        Light,        // toque suave genérico
        Strong,       // JERARQUÍA DE DATOS: vibración fuerte en el dato más relevante
        DoublePulse,  // INTERÉS DE AUDIENCIA: pulso doble cuando muchos reaccionan a lo mismo
        Check         // CIERRE NARRATIVO: patrón suave tipo "check" al terminar una estación
    }

    /// <summary>
    /// Punto único para toda la háptica. Envía vibración a los controles XR
    /// (Quest, etc.) y, además, muestra un aviso visual y sonoro para que el
    /// patrón también se note en modo escritorio y en el video del demo.
    /// </summary>
    public class HapticsManager : MonoBehaviour
    {
        public static HapticsManager Instance { get; private set; }

        [Tooltip("Muestra un texto frente a la cámara cuando se dispara un patrón.")]
        public bool visualFeedback = true;
        [Tooltip("Reproduce un golpe grave que imita la vibración.")]
        public bool audioFeedback = true;
        [Range(0f, 1f)] public float intensity = 1f;

        readonly List<InputDevice> devices = new List<InputDevice>();
        AudioSource source;
        AudioClip thump;
        TextMesh hud;
        float hudUntil;
        Coroutine running;

        void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            thump = ProceduralAudio.Thump("Thump", 0.18f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (hud != null && hud.gameObject.activeSelf && Time.unscaledTime > hudUntil)
                hud.gameObject.SetActive(false);
        }

        // ---------- API pública ----------

        public static void Play(HapticPattern pattern)
        {
            if (Instance != null) Instance.PlayPattern(pattern);
        }

        /// <summary>Un solo pulso con amplitud 0..1 (se usa para "sentir" el valor de cada barra).</summary>
        public static void Pulse(float amplitude, float duration)
        {
            if (Instance != null) Instance.SendPulse(amplitude, duration, true);
        }

        public void PlayPattern(HapticPattern pattern)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Run(pattern));
        }

        // ---------- Patrones ----------

        IEnumerator Run(HapticPattern pattern)
        {
            switch (pattern)
            {
                case HapticPattern.Light:
                    SendPulse(0.3f, 0.08f, true);
                    break;

                case HapticPattern.Strong:
                    Show("HÁPTICA: vibración fuerte (dato más relevante)");
                    SendPulse(1f, 0.45f, true);
                    break;

                case HapticPattern.DoublePulse:
                    Show("HÁPTICA: pulso doble (interés de la audiencia)");
                    SendPulse(0.8f, 0.12f, true);
                    yield return new WaitForSeconds(0.22f);
                    SendPulse(0.8f, 0.12f, true);
                    break;

                case HapticPattern.Check:
                    Show("HÁPTICA: check (estación completada)");
                    SendPulse(0.25f, 0.07f, true);
                    yield return new WaitForSeconds(0.12f);
                    SendPulse(0.55f, 0.18f, true);
                    break;
            }
            running = null;
        }

        void SendPulse(float amplitude, float duration, bool withSound)
        {
            amplitude = Mathf.Clamp01(amplitude * intensity);
            SendToNode(XRNode.LeftHand, amplitude, duration);
            SendToNode(XRNode.RightHand, amplitude, duration);
            if (withSound && audioFeedback && source != null)
                source.PlayOneShot(thump, Mathf.Lerp(0.15f, 1f, amplitude));
        }

        void SendToNode(XRNode node, float amplitude, float duration)
        {
            devices.Clear();
            InputDevices.GetDevicesAtXRNode(node, devices);
            for (int i = 0; i < devices.Count; i++)
            {
                HapticCapabilities caps;
                if (devices[i].TryGetHapticCapabilities(out caps) && caps.supportsImpulse)
                    devices[i].SendHapticImpulse(0u, amplitude, duration);
            }
        }

        // ---------- Aviso visual ----------

        void Show(string message)
        {
            if (!visualFeedback) return;
            var cam = Camera.main;
            if (cam == null) return;

            if (hud == null || hud.transform.parent != cam.transform)
            {
                if (hud != null) Destroy(hud.gameObject);
                hud = TextUtil.Create(cam.transform, "HapticHUD", "", new Vector3(0f, -0.28f, 1f), 0.035f,
                    new Color(1f, 0.85f, 0.2f));
            }
            hud.text = message;
            hud.gameObject.SetActive(true);
            hudUntil = Time.unscaledTime + 1.6f;
        }
    }
}
