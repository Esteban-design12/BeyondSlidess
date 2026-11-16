using System.Collections.Generic;
using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// Sabe en qué estación está el presentador (por cercanía) y avisa a la web
    /// para que la audiencia vea los botones de esa estación.
    /// </summary>
    public class StationManager : MonoBehaviour
    {
        public static StationManager Instance { get; private set; }

        public List<Station> stations = new List<Station>();
        public Station Current { get; private set; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            SetCurrent(FindNearest(), true);
        }

        void Update()
        {
            var nearest = FindNearest();
            if (nearest != null && nearest != Current) SetCurrent(nearest, false);

            // Tecla P: simula un ping de la audiencia (para probar sin servidor ni celular).
            if (InputCompat.PingDown() && Current != null && Current.pingTargets.Count > 0)
            {
                var t = Current.pingTargets[Random.Range(0, Current.pingTargets.Count)];
                if (t != null) t.Ping();
            }
        }

        public Station Get(int index)
        {
            for (int i = 0; i < stations.Count; i++)
                if (stations[i] != null && stations[i].index == index) return stations[i];
            return null;
        }

        Station FindNearest()
        {
            var cam = Camera.main;
            if (cam == null) return Current;

            Vector3 p = cam.transform.position;
            Station best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < stations.Count; i++)
            {
                var s = stations[i];
                if (s == null) continue;
                Vector3 d = s.transform.position - p;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist <= s.radius && dist < bestDist)
                {
                    best = s;
                    bestDist = dist;
                }
            }
            return best; // null = va caminando entre estaciones: se mantiene la actual
        }

        void SetCurrent(Station s, bool force)
        {
            if (s == null && !force) return;
            Current = s;
            if (AudienceClient.Instance != null) AudienceClient.Instance.SendState();
        }
    }
}
