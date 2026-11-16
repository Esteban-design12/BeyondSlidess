using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BeyondSlides
{
    /// <summary>
    /// Lee teclado y mouse funcionando tanto con el Input System nuevo
    /// como con el Input Manager clásico (según lo que tenga activo el proyecto).
    /// </summary>
    public static class InputCompat
    {
#if ENABLE_INPUT_SYSTEM
        static Keyboard K { get { return Keyboard.current; } }
        static Mouse M { get { return Mouse.current; } }

        public static Vector2 Move()
        {
            if (K == null) return Vector2.zero;
            float x = (K.dKey.isPressed ? 1f : 0f) - (K.aKey.isPressed ? 1f : 0f);
            float y = (K.wKey.isPressed ? 1f : 0f) - (K.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        public static Vector2 Look()
        {
            return M != null ? M.delta.ReadValue() * 0.05f : Vector2.zero;
        }

        public static bool ClickDown() { return M != null && M.leftButton.wasPressedThisFrame; }
        public static bool ClickHeld() { return M != null && M.leftButton.isPressed; }

        public static float Scroll()
        {
            if (M == null) return 0f;
            return Mathf.Clamp(M.scroll.ReadValue().y, -1f, 1f);
        }

        public static bool Sprint() { return K != null && K.leftShiftKey.isPressed; }
        public static bool EscapeDown() { return K != null && K.escapeKey.wasPressedThisFrame; }
        public static bool PingDown() { return K != null && K.pKey.wasPressedThisFrame; }
        public static bool HelpDown() { return K != null && K.hKey.wasPressedThisFrame; }

        /// <summary>-1 con Q, +1 con E.</summary>
        public static float Rotate()
        {
            if (K == null) return 0f;
            return (K.eKey.isPressed ? 1f : 0f) - (K.qKey.isPressed ? 1f : 0f);
        }

        /// <summary>Devuelve 0..4 si se pulsó esa tecla numérica en este frame, o -1.</summary>
        public static int DigitDown()
        {
            if (K == null) return -1;
            if (K.digit0Key.wasPressedThisFrame) return 0;
            if (K.digit1Key.wasPressedThisFrame) return 1;
            if (K.digit2Key.wasPressedThisFrame) return 2;
            if (K.digit3Key.wasPressedThisFrame) return 3;
            if (K.digit4Key.wasPressedThisFrame) return 4;
            return -1;
        }
#else
        public static Vector2 Move()
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            return new Vector2(x, y);
        }

        public static Vector2 Look()
        {
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        }

        public static bool ClickDown() { return Input.GetMouseButtonDown(0); }
        public static bool ClickHeld() { return Input.GetMouseButton(0); }
        public static float Scroll() { return Mathf.Clamp(Input.mouseScrollDelta.y, -1f, 1f); }
        public static bool Sprint() { return Input.GetKey(KeyCode.LeftShift); }
        public static bool EscapeDown() { return Input.GetKeyDown(KeyCode.Escape); }
        public static bool PingDown() { return Input.GetKeyDown(KeyCode.P); }
        public static bool HelpDown() { return Input.GetKeyDown(KeyCode.H); }

        public static float Rotate()
        {
            return (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
        }

        public static int DigitDown()
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 3;
            if (Input.GetKeyDown(KeyCode.Alpha4)) return 4;
            return -1;
        }
#endif
    }
}
