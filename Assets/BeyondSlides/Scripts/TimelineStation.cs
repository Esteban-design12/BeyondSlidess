using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// ESTACIÓN 3 — Línea de tiempo.
    /// Al tocar cada hito se muestra su texto. Al visitar todos, la estación se completa.
    /// </summary>
    public class TimelineStation : MonoBehaviour
    {
        public Station station;
        public Touchable[] milestones;
        [TextArea] public string[] descriptions;
        public TextMesh infoText;
        public Color visitedColor = new Color(1f, 0.8f, 0.2f);

        bool[] visited;

        void Start()
        {
            if (milestones == null) return;
            visited = new bool[milestones.Length];
            for (int i = 0; i < milestones.Length; i++)
            {
                if (milestones[i] == null) continue;
                int index = i; // copia para el closure
                milestones[i].onTouchEnter.AddListener(() => Visit(index));
            }
        }

        void Visit(int i)
        {
            if (infoText != null && descriptions != null && i < descriptions.Length)
                infoText.text = descriptions[i];

            HapticsManager.Play(HapticPattern.Light);

            if (visited[i]) return;
            visited[i] = true;

            var r = milestones[i].GetComponent<Renderer>();
            if (r != null) r.material.color = visitedColor;

            for (int k = 0; k < visited.Length; k++)
                if (!visited[k]) return;

            if (station != null) station.Complete();
        }
    }
}
