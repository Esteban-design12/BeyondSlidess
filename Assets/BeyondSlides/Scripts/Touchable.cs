using UnityEngine;
using UnityEngine.Events;

namespace BeyondSlides
{
    /// <summary>
    /// Objeto que se puede "tocar" y "seleccionar".
    ///  - En XR: tocar = apuntar/acercar el control (hover); seleccionar = gatillo/grip.
    ///    (lee el XRSimpleInteractable que pone el constructor de escena).
    ///  - En escritorio: tocar = apuntar con la mira; seleccionar = clic izquierdo.
    /// </summary>
    public class Touchable : MonoBehaviour
    {
        public UnityEvent onTouchEnter = new UnityEvent();
        public UnityEvent onTouchExit = new UnityEvent();
        public UnityEvent onSelect = new UnityEvent();

        public bool IsTouched { get; private set; }

        XRReflection.BoolReader xrHovered;
        XRReflection.BoolReader xrSelected;
        bool desktopHover;
        bool wasSelected;

        void Awake()
        {
            var xr = GetComponent("XRSimpleInteractable");
            if (xr != null)
            {
                xrHovered = new XRReflection.BoolReader(xr, "isHovered");
                xrSelected = new XRReflection.BoolReader(xr, "isSelected");
            }
        }

        void Update()
        {
            bool touched = desktopHover || (xrHovered != null && xrHovered.Value);
            if (touched != IsTouched)
            {
                IsTouched = touched;
                if (touched) onTouchEnter.Invoke(); else onTouchExit.Invoke();
            }

            bool selected = xrSelected != null && xrSelected.Value;
            if (selected && !wasSelected) onSelect.Invoke();
            wasSelected = selected;
        }

        void OnDisable()
        {
            desktopHover = false;
        }

        // Llamados por DesktopPlayer
        public void SetDesktopHover(bool value) { desktopHover = value; }
        public void DesktopSelect() { onSelect.Invoke(); }
    }
}
