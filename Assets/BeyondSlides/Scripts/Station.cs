using System.Collections.Generic;
using UnityEngine;

namespace BeyondSlides
{
    /// <summary>Una estación del recorrido (el hub es la estación 0).</summary>
    public class Station : MonoBehaviour
    {
        public int index;
        public string title = "Estación";
        [Tooltip("Radio (m) dentro del cual se considera que el presentador está en la estación.")]
        public float radius = 4f;
        public Renderer marker;
        public TextMesh statusText;
        public Color completedColor = new Color(0.2f, 0.9f, 0.4f);
        [Tooltip("Objetivos a los que la audiencia puede enviar pings mientras se presenta esta estación.")]
        public List<PingTarget> pingTargets = new List<PingTarget>();

        public bool Completed { get; private set; }

        AudioSource chime;

        /// <summary>Punto donde se para el presentador (centro de la plataforma).</summary>
        public Vector3 StandPoint { get { return transform.position; } }
        public Quaternion StandRotation { get { return transform.rotation; } }

        /// <summary>Marca la estación como terminada: check háptico + campanita + marcador verde.</summary>
        public void Complete()
        {
            if (Completed) return;
            Completed = true;

            if (marker != null) marker.material.color = completedColor;
            if (statusText != null)
            {
                statusText.text = "COMPLETADA";
                statusText.color = completedColor;
            }

            HapticsManager.Play(HapticPattern.Check);

            if (chime == null)
            {
                chime = gameObject.AddComponent<AudioSource>();
                chime.playOnAwake = false;
                chime.spatialBlend = 1f;
                chime.minDistance = 2f;
                chime.maxDistance = 15f;
                chime.clip = ProceduralAudio.Chime("Check", new float[] { 880f, 1318.5f }, 0.12f);
            }
            chime.Play();
        }
    }
}
