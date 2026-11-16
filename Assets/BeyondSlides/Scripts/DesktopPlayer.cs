using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// Jugador de escritorio (sin visor): sirve para desarrollar, probar y grabar el demo.
    ///   WASD mover · Mouse mirar · Shift correr · 0-4 teletransporte a hub/estaciones
    ///   Apuntar = tocar · Clic = seleccionar / agarrar · Q/E rotar · Rueda escalar
    ///   P = ping simulado · H = ayuda · Esc = soltar el cursor
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopPlayer : MonoBehaviour
    {
        public Camera cam;
        public float walkSpeed = 3.5f;
        public float runSpeed = 7f;
        public float lookSensitivity = 2f;
        public float reach = 5f;

        CharacterController cc;
        float pitch;
        float verticalSpeed;
        bool showHelp = true;

        Touchable hovered;
        ManipulableObject held;
        float holdDistance;
        GUIStyle crossStyle;
        GUIStyle helpStyle;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (cam == null) cam = GetComponentInChildren<Camera>();
        }

        void Start()
        {
            LockCursor(true);
        }

        void OnDisable()
        {
            LockCursor(false);
            SetHovered(null);
            Release();
        }

        void Update()
        {
            if (InputCompat.EscapeDown()) LockCursor(false);
            if (InputCompat.HelpDown()) showHelp = !showHelp;

            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (!locked)
            {
                if (InputCompat.ClickDown()) LockCursor(true);
                return;
            }

            Look();
            Move();
            Teleports();
            Interact();
        }

        void Look()
        {
            Vector2 d = InputCompat.Look() * lookSensitivity;
            transform.Rotate(0f, d.x, 0f, Space.World);
            pitch = Mathf.Clamp(pitch - d.y, -85f, 85f);
            if (cam != null) cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Move()
        {
            Vector2 m = InputCompat.Move();
            Vector3 dir = transform.right * m.x + transform.forward * m.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            float speed = InputCompat.Sprint() ? runSpeed : walkSpeed;

            if (cc.isGrounded) verticalSpeed = -1f;
            else verticalSpeed -= 9.81f * Time.deltaTime;

            cc.Move((dir * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        void Teleports()
        {
            int digit = InputCompat.DigitDown();
            if (digit < 0 || StationManager.Instance == null) return;
            var s = StationManager.Instance.Get(digit);
            if (s == null) return;

            cc.enabled = false;
            transform.position = s.StandPoint + Vector3.up * 0.05f;
            transform.rotation = Quaternion.Euler(0f, s.StandRotation.eulerAngles.y, 0f);
            cc.enabled = true;
            pitch = 0f;
        }

        void Interact()
        {
            if (cam == null) return;

            // --- Objeto agarrado ---
            if (held != null)
            {
                if (!InputCompat.ClickHeld())
                {
                    Release();
                }
                else
                {
                    Vector3 target = cam.transform.position + cam.transform.forward * holdDistance;
                    held.transform.position = Vector3.Lerp(held.transform.position, target, 15f * Time.deltaTime);
                    float rot = InputCompat.Rotate();
                    if (rot != 0f) held.transform.Rotate(Vector3.up, rot * 120f * Time.deltaTime, Space.World);
                    float scroll = InputCompat.Scroll();
                    if (scroll != 0f) held.ScaleBy(1f + scroll * 0.1f);
                }
                return;
            }

            // --- Rayo desde la mira ---
            Touchable touch = null;
            ManipulableObject manip = null;
            RaycastHit hit;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out hit, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                touch = hit.collider.GetComponentInParent<Touchable>();
                manip = hit.collider.GetComponentInParent<ManipulableObject>();
            }

            SetHovered(touch);

            if (InputCompat.ClickDown())
            {
                if (manip != null)
                {
                    held = manip;
                    held.HeldByDesktop = true;
                    holdDistance = Mathf.Clamp(Vector3.Distance(cam.transform.position, manip.transform.position), 1f, 3f);
                }
                else if (touch != null)
                {
                    touch.DesktopSelect();
                }
            }
        }

        void SetHovered(Touchable t)
        {
            if (t == hovered) return;
            if (hovered != null) hovered.SetDesktopHover(false);
            hovered = t;
            if (hovered != null) hovered.SetDesktopHover(true);
        }

        void Release()
        {
            if (held != null) held.HeldByDesktop = false;
            held = null;
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void OnGUI()
        {
            if (crossStyle == null)
            {
                crossStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
                helpStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13 };
                helpStyle.normal.textColor = Color.white;
            }

            GUI.color = hovered != null ? Color.yellow : Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 20f, Screen.height / 2f - 20f, 40f, 40f), "+", crossStyle);
            GUI.color = Color.white;

            if (!showHelp) return;
            string station = StationManager.Instance != null && StationManager.Instance.Current != null
                ? StationManager.Instance.Current.title : "-";
            string net = AudienceClient.Instance != null && AudienceClient.Instance.Connected
                ? "conectado (" + AudienceClient.Instance.AudienceCount + " asistentes)" : "desconectado";
            GUI.Box(new Rect(10f, 10f, 330f, 150f),
                "BEYOND SLIDES — modo escritorio\n" +
                "WASD mover · Mouse mirar · Shift correr\n" +
                "0 hub · 1-4 estaciones\n" +
                "Apuntar = tocar · Clic = pulsar / agarrar\n" +
                "Q/E rotar · Rueda escalar (agarrando)\n" +
                "P ping simulado · H ocultar ayuda · Esc cursor\n" +
                "Estación: " + station + "\n" +
                "Servidor: " + net, helpStyle);
        }
    }
}
