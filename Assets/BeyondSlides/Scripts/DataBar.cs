using UnityEngine;

namespace BeyondSlides
{
    /// <summary>Una barra del gráfico. "Se siente" más fuerte cuanto mayor es su valor.</summary>
    [RequireComponent(typeof(Touchable))]
    public class DataBar : MonoBehaviour
    {
        public float value = 50f;
        public Color highlightColor = Color.white;
        [Tooltip("Cada cuántos segundos se repite el pulso mientras se mantiene el toque.")]
        public float repeatEvery = 0.3f;

        BarChart chart;
        Touchable touchable;
        Renderer rend;
        Color baseColor;
        float max = 100f;
        bool isTop;
        float nextPulse;

        void Awake()
        {
            touchable = GetComponent<Touchable>();
            rend = GetComponent<Renderer>();
            if (rend != null) baseColor = rend.material.color;
            touchable.onTouchEnter.AddListener(OnEnter);
            touchable.onTouchExit.AddListener(OnExit);
        }

        public void Setup(BarChart owner, float maxValue)
        {
            chart = owner;
            max = Mathf.Max(0.0001f, maxValue);
            isTop = Mathf.Approximately(value, maxValue);
        }

        float Strength { get { return Mathf.Lerp(0.12f, 0.75f, Mathf.Clamp01(value / max)); } }

        void OnEnter()
        {
            if (rend != null) rend.material.color = Color.Lerp(baseColor, highlightColor, 0.55f);

            if (isTop)
            {
                HapticsManager.Play(HapticPattern.Strong);
                if (chart != null) chart.OnTopBarTouched();
                nextPulse = Time.time + 0.8f;
            }
            else
            {
                HapticsManager.Pulse(Strength, 0.12f);
                nextPulse = Time.time + repeatEvery;
            }
        }

        void OnExit()
        {
            if (rend != null) rend.material.color = baseColor;
        }

        void Update()
        {
            if (touchable.IsTouched && Time.time >= nextPulse)
            {
                HapticsManager.Pulse(isTop ? 1f : Strength, 0.1f);
                nextPulse = Time.time + repeatEvery;
            }
        }
    }
}
