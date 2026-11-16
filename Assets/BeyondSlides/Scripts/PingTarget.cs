using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// Punto del mundo al que la audiencia puede enviar "pings" desde la web.
    /// Lleva la cuenta, parpadea al recibir uno y, si llegan muchos seguidos,
    /// dispara el pulso doble háptico para el presentador.
    /// </summary>
    public class PingTarget : MonoBehaviour
    {
        static readonly Dictionary<string, PingTarget> registry = new Dictionary<string, PingTarget>();
        public static IEnumerable<PingTarget> All { get { return registry.Values; } }

        [Tooltip("Identificador único. Debe coincidir con el que envía la web.")]
        public string id = "target";
        [Tooltip("Texto del botón en la web de la audiencia.")]
        public string label = "Reacción";
        public TextMesh countText;
        [Tooltip("Si es true el texto muestra 'etiqueta: n'; si no, solo el número.")]
        public bool showLabelInText = true;
        public Renderer indicator;
        public Color flashColor = new Color(1f, 0.9f, 0.2f);

        [Header("Ráfaga = muchos asistentes reaccionan al mismo punto")]
        public int burstThreshold = 3;
        public float burstWindow = 4f;

        public int Count { get; private set; }

        readonly List<float> recent = new List<float>();
        float lastBurst = -100f;
        Color baseColor = Color.gray;
        Vector3 baseScale = Vector3.one;
        Coroutine flash;

        void OnEnable() { registry[id] = this; }

        void OnDisable()
        {
            PingTarget current;
            if (registry.TryGetValue(id, out current) && current == this) registry.Remove(id);
        }

        void Start()
        {
            if (indicator != null)
            {
                baseColor = indicator.material.color;
                baseScale = indicator.transform.localScale;
            }
            Refresh();
        }

        public static bool Dispatch(string targetId)
        {
            PingTarget t;
            if (targetId != null && registry.TryGetValue(targetId, out t))
            {
                t.Ping();
                return true;
            }
            return false;
        }

        public void Ping()
        {
            Count++;
            Refresh();

            float now = Time.time;
            recent.Add(now);
            recent.RemoveAll(t => now - t > burstWindow);

            bool burst = recent.Count >= burstThreshold && now - lastBurst > 1.5f;
            if (burst)
            {
                lastBurst = now;
                recent.Clear();
                HapticsManager.Play(HapticPattern.DoublePulse);
            }

            if (indicator != null && isActiveAndEnabled)
            {
                if (flash != null) StopCoroutine(flash);
                flash = StartCoroutine(Flash(burst ? 2.2f : 1.5f, burst ? 0.9f : 0.45f));
            }
        }

        public void ResetCount()
        {
            Count = 0;
            recent.Clear();
            Refresh();
        }

        void Refresh()
        {
            if (countText == null) return;
            countText.text = showLabelInText ? label + ": " + Count : (Count > 0 ? Count.ToString() : "");
        }

        IEnumerator Flash(float scale, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / seconds);
                indicator.material.color = Color.Lerp(baseColor, flashColor, k);
                indicator.transform.localScale = baseScale * Mathf.Lerp(1f, scale, k);
                yield return null;
            }
            indicator.material.color = baseColor;
            indicator.transform.localScale = baseScale;
            flash = null;
        }
    }
}
