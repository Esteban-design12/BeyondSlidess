using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// ESTACIÓN 2 — Gráfico + háptica.
    /// Cada barra vibra con una fuerza proporcional a su valor; la barra más alta
    /// (el dato más relevante) dispara la vibración fuerte y completa la estación.
    /// </summary>
    public class BarChart : MonoBehaviour
    {
        public Station station;
        public DataBar[] bars;

        void Start()
        {
            if (bars == null || bars.Length == 0) return;

            float max = 0f;
            for (int i = 0; i < bars.Length; i++)
                if (bars[i] != null) max = Mathf.Max(max, bars[i].value);

            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] == null) continue;
                bars[i].Setup(this, max);
            }
        }

        public void OnTopBarTouched()
        {
            if (station != null) station.Complete();
        }
    }
}
