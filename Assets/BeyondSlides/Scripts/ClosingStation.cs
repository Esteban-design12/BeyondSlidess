using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// ESTACIÓN 4 — Cierre + audio.
    /// Al pulsar el botón suena la melodía de cierre, se muestra el resumen de
    /// participación de la audiencia y la web pasa a la pantalla de "Gracias".
    /// </summary>
    public class ClosingStation : MonoBehaviour
    {
        public Station station;
        public Touchable button;
        public TextMesh summaryText;
        public Light stationLight;

        AudioSource source;
        bool closed;
        float closedAt;

        void Start()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 3f;
            source.maxDistance = 20f;
            // Arpegio de Do mayor ascendente
            source.clip = ProceduralAudio.Chime("Cierre",
                new float[] { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f, 1318.5f }, 0.22f);

            if (button != null) button.onSelect.AddListener(Close);
        }

        void Update()
        {
            if (closed && stationLight != null)
            {
                float t = Time.time - closedAt;
                stationLight.intensity = 2f + Mathf.Sin(t * 3f) * 1f;
                stationLight.color = Color.HSVToRGB(Mathf.Repeat(t * 0.15f, 1f), 0.5f, 1f);
            }
        }

        public void Close()
        {
            if (closed) return;
            closed = true;
            closedAt = Time.time;

            source.Play();

            int total = 0;
            PingTarget top = null;
            foreach (var t in PingTarget.All)
            {
                total += t.Count;
                if (top == null || t.Count > top.Count) top = t;
            }

            if (summaryText != null)
            {
                string s = "¡Gracias por su atención!\n\nReacciones de la audiencia: " + total;
                if (top != null && top.Count > 0)
                    s += "\nLo que más interesó: " + top.label + " (" + top.Count + ")";
                summaryText.text = s;
            }

            if (station != null) station.Complete();
            if (AudienceClient.Instance != null) AudienceClient.Instance.SendEnd();
        }
    }
}
