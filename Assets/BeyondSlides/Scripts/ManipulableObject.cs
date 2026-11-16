using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// ESTACIÓN 1 — Objeto manipulable.
    /// En XR se agarra con el XRGrabInteractable que añade el constructor de escena.
    /// En escritorio lo mueve DesktopPlayer (clic sostenido, Q/E rota, rueda escala).
    /// Cuando el presentador lo mueve o lo gira lo suficiente, la estación se completa.
    /// </summary>
    public class ManipulableObject : MonoBehaviour
    {
        public Station station;
        [Tooltip("Si se aleja más que esto de su pedestal y nadie lo sostiene, vuelve a su sitio.")]
        public float returnDistance = 4f;
        public float minScale = 0.4f;
        public float maxScale = 2.5f;

        /// <summary>True mientras DesktopPlayer lo tiene agarrado.</summary>
        public bool HeldByDesktop { get; set; }

        Vector3 homePos;
        Quaternion homeRot;
        Vector3 homeScale;
        XRReflection.BoolReader xrSelected;
        bool wasHeld;

        void Start()
        {
            homePos = transform.position;
            homeRot = transform.rotation;
            homeScale = transform.localScale;

            var grab = GetComponent("XRGrabInteractable");
            if (grab != null) xrSelected = new XRReflection.BoolReader(grab, "isSelected");
        }

        bool IsHeld { get { return HeldByDesktop || (xrSelected != null && xrSelected.Value); } }

        void Update()
        {
            bool held = IsHeld;
            if (held && !wasHeld) HapticsManager.Play(HapticPattern.Light);
            wasHeld = held;

            if (station != null && !station.Completed)
            {
                bool moved = Vector3.Distance(transform.position, homePos) > 0.25f;
                bool rotated = Quaternion.Angle(transform.rotation, homeRot) > 40f;
                if (moved || rotated) station.Complete();
            }

            if (!held && Vector3.Distance(transform.position, homePos) > returnDistance)
                ResetPose();
        }

        public void ScaleBy(float factor)
        {
            float current = transform.localScale.x / Mathf.Max(0.0001f, homeScale.x);
            float next = Mathf.Clamp(current * factor, minScale, maxScale);
            transform.localScale = homeScale * next;
        }

        public void ResetPose()
        {
            transform.position = homePos;
            transform.rotation = homeRot;
            transform.localScale = homeScale;
        }
    }
}
