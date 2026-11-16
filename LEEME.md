# Beyond Slides — prototipo XR (IEEE VR 2026 3DUI Contest)

Prototipo de su propuesta: un espacio navegable con **hub central + 4 estaciones**, donde el
presentador manipula contenido 3D y la audiencia participa desde el celular, sin visor.

| Lo que dice la presentación | Dónde está en el prototipo |
|---|---|
| Hub central, punto de inicio claro | `Hub` en el origen, con letreros hacia cada estación |
| Navegación por caminar o teletransporte | Escritorio: WASD + teclas 0–4. XR: joystick + teletransporte sobre el suelo |
| Estación 1 · Objeto manipulable | Molécula que se agarra, rota y escala |
| Estación 2 · Gráfico + háptica | Barras que vibran según su valor; la más alta vibra fuerte |
| Estación 3 · Línea de tiempo | 5 hitos que muestran su texto al tocarlos |
| Estación 4 · Cierre + audio | Botón de cierre, melodía y resumen de la audiencia |
| Ambiente sonoro distinto por estación (audio espacial) | Un acorde 3D por estación, generado por código |
| Háptica: jerarquía de datos / interés de audiencia / cierre narrativo | Vibración fuerte / pulso doble / patrón "check" |
| Unity ↔ WebSocket ↔ Interfaz web | `AudienceClient.cs` ↔ `Server/server.js` ↔ `Server/public/index.html` |

No se necesita ningún asset externo: la escena, los materiales y los sonidos se generan solos.

---

## Parte A — Verlo funcionando en el PC (sin visor)

1. Abre **Unity Hub → New project**. Usa **Unity 2022.3 LTS o Unity 6** con la plantilla
   **3D (Built-in)** o **Universal 3D**. Ponle un nombre, por ejemplo `BeyondSlides`.
2. Copia la carpeta `Unity/Assets/BeyondSlides` de este paquete dentro de la carpeta `Assets`
   de tu proyecto (arrástrala a la ventana *Project* de Unity).
3. Espera a que compile. Arriba aparecerá el menú **Tools → Beyond Slides**.
4. Elige **Tools → Beyond Slides → 1. Construir escena - Escritorio (sin visor)**.
5. Guarda la escena (**Ctrl+S**) y pulsa **Play**. Haz clic dentro de la ventana *Game*.

Controles:

| Tecla | Acción |
|---|---|
| W A S D / mouse | Caminar / mirar (Shift corre) |
| 0, 1, 2, 3, 4 | Teletransporte al hub y a cada estación |
| Apuntar con la mira `+` | "Tocar" (barras, hitos) |
| Clic izquierdo | Pulsar botón / agarrar el objeto (mantén pulsado) |
| Q / E, rueda | Rotar y escalar el objeto agarrado |
| P | Simular un ping de la audiencia (3 seguidos = pulso doble) |
| H / Esc | Ocultar ayuda / soltar el cursor |

Sin visor no hay vibración real, así que cada patrón háptico se muestra con un texto amarillo
y un golpe grave de sonido. Eso también sirve para que se entienda en el video.

## Parte B — Audiencia en tiempo real (servidor + web)

1. Instala **Node.js LTS** desde nodejs.org.
2. Abre una terminal en la carpeta `Server` y ejecuta:
   ```
   npm install
   npm start
   ```
3. La consola muestra una dirección como `http://192.168.x.x:8080`. Ábrela en uno o varios
   celulares conectados a la **misma WiFi** que el PC (o en el mismo PC: `http://localhost:8080`).
4. Pulsa Play en Unity. En el hub aparece la dirección y cuántos asistentes hay conectados.
5. La web muestra los botones de la estación donde esté el presentador. Cada toque aparece en el
   panel "AUDIENCIA EN VIVO" de la estación; en la Estación 2 también se puede reaccionar a cada barra.
   Tres pings al mismo punto en 4 segundos disparan el **pulso doble**.

Si el celular no abre la página: acepta el aviso del Firewall de Windows al iniciar el servidor.
La WiFi de la universidad suele bloquear la comunicación entre dispositivos; en ese caso
comparte internet desde un celular y conecta el PC a esa red.

## Parte C — Pasarlo a XR (visor o simulador)

Hazlo cuando A y B ya funcionen.

1. **Window → Package Manager → Unity Registry**: instala **XR Plugin Management**,
   **OpenXR Plugin** y **XR Interaction Toolkit**. Si pide reiniciar por el Input System, acepta.
2. En el Package Manager, dentro de *XR Interaction Toolkit → Samples*, importa
   **Starter Assets** y **XR Device Simulator** (en versiones nuevas se llama *XR Interaction Simulator*).
   Si aparece la ventana para corregir capas de interacción ("Teleport"), pulsa **Fix**.
3. **Edit → Project Settings → XR Plug-in Management**: marca **OpenXR**. En *OpenXR* añade el
   perfil de control de tu visor (para Quest: *Oculus Touch Controller Profile*).
4. Ejecuta **Tools → Beyond Slides → 2. Construir escena - XR (visor o simulador)**.
   Esto reconstruye la escena para añadir agarre XR, interacción por rayo y teletransporte,
   y coloca el `XR Origin (XR Rig)` y el simulador.
5. Play:
   - **Sin visor:** el simulador mueve cabeza y controles con teclado y mouse (sus teclas salen en pantalla).
   - **Meta Quest con cable (Link / Air Link):** activa Link, **desactiva el objeto del simulador** en la
     jerarquía (`BeyondSlides/Jugador/XR ... Simulator`) y pulsa Play. Es la forma más rápida de
     grabar el demo con háptica real, y el servidor sigue siendo `ws://localhost:8080`.
   - **Quest standalone (APK):** instala el módulo Android, cambia la plataforma a Android, en
     `BeyondSlides/Managers → Audience Client → Server Url` escribe `ws://IP-DEL-PC:8080`, y en
     *Player Settings → Internet Access* pon **Require**.

Para volver al modo escritorio sin reconstruir: **Tools → Beyond Slides → Cambiar jugador a Escritorio**.

## Guion sugerido para el video del demo (2–3 min)

1. **Hub:** se ve la dirección web y el contador de asistentes; alguien se conecta con el celular.
2. **Estación 1:** agarrar, girar y escalar el objeto → suena el "check" y el marcador se pone verde.
3. **Estación 2:** pasar por las barras de menor a mayor (la vibración crece) y llegar a la más alta
   (vibración fuerte). La audiencia hace ping a una barra → pulso doble.
4. **Estación 3:** tocar los 5 hitos en orden; al último, "check".
5. **Estación 4:** pulsar el botón → melodía, resumen de reacciones y pantalla de "Gracias" en los celulares.

Graben en pantalla dividida: Unity a un lado y un celular al otro.

## Qué archivo corresponde a cada rol

| Rol | Archivos |
|---|---|
| Persona 1 · Backend / networking | `Server/server.js`, `Server/public/index.html`, `AudienceClient.cs`, `PingTarget.cs` |
| Persona 2 · Interacciones XR | `Touchable.cs`, `ManipulableObject.cs`, `DesktopPlayer.cs`, `XRReflection.cs`, `InputCompat.cs` |
| Persona 3 · Háptica y mapeo narrativo | `HapticsManager.cs`, `DataBar.cs`, `BarChart.cs`, `Station.cs` (patrón de cierre) |
| Persona 4 · Contenido, UX, entorno | `Editor/BeyondSlidesBuilder.cs`, `TimelineStation.cs`, `ClosingStation.cs`, `StationAmbience.cs`, `ProceduralAudio.cs` |

## Cómo personalizarlo

- **Datos del gráfico y textos de la línea de tiempo:** son contenido de ejemplo. Cámbienlos en
  `BeyondSlidesBuilder.cs` (`BuildStation2` y `BuildStation3`) y reconstruyan, o directamente en el
  Inspector (`DataBar → Value`, `TimelineStation → Descriptions`). Verifiquen las fechas antes de presentarlas.
- **Objeto de la Estación 1:** pongan su propio modelo como hijo de `ObjetoManipulable` y borren las esferas.
  Debe tener al menos un collider.
- **Sonidos:** en cada `AmbienteSonoro` pueden cambiar las notas (`Frequencies`) o asignar un clip propio en `Custom Clip`.
- **Fuerza y duración de la háptica:** en `HapticsManager.cs`, método `Run`.
- **Sensibilidad del pulso doble:** en cada `PingTarget` (`Burst Threshold`, `Burst Window`).

Ojo: "Construir escena" borra y vuelve a crear el objeto `BeyondSlides`. Lo que decoren a mano
pónganlo fuera de ese objeto para no perderlo.

## Problemas comunes

| Síntoma | Solución |
|---|---|
| No aparece el menú Tools → Beyond Slides | Hay un error rojo en la consola; cópialo completo y pásamelo |
| Todo se ve rosado | Cambiaste de render pipeline; vuelve a ejecutar "Construir escena" |
| En XR no puedo teletransportarme | Selecciona `Suelo` y en *Teleportation Area → Interaction Layer Mask* elige **Teleport** |
| En XR las manos no agarran ni tocan | Construiste la escena antes de instalar el XR Interaction Toolkit; ejecuta la opción 2 otra vez |
| El hub dice "servidor desconectado" | El servidor no está corriendo o `Server Url` no coincide |
| El visor no vibra | Revisa el perfil de control en OpenXR y desactiva el simulador |

## Estado de las pruebas

- El servidor y el protocolo de mensajes se probaron con un presentador y dos asistentes simulados.
- Los scripts de Unity pasaron revisión de sintaxis, pero **no se pudieron compilar ni ejecutar dentro
  de Unity** al prepararlos. Si la consola muestra algún error al importarlos, envíenme el texto
  del error y se corrige.
