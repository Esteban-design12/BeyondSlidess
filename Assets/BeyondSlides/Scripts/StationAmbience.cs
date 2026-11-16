using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// Ambiente sonoro sutil y distinto para cada estación, con audio espacial 3D:
    /// se oye desde la dirección de la estación y sube al acercarse.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class StationAmbience : MonoBehaviour
    {
        [Tooltip("Notas (Hz) que forman el acorde de esta estación.")]
        public float[] frequencies = { 220f, 277.18f, 329.63f };
        [Range(0f, 1f)] public float volume = 0.35f;
        public float minDistance = 1.5f;
        public float maxDistance = 9f;
        [Tooltip("Opcional: si asignas un clip propio se usa en lugar del sonido generado.")]
        public AudioClip customClip;

        void Start()
        {
            var src = GetComponent<AudioSource>();
            src.clip = customClip != null ? customClip : ProceduralAudio.Drone(name + "_Drone", frequencies, 8f);
            src.loop = true;
            src.spatialBlend = 1f;               // 100% 3D
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.dopplerLevel = 0f;
            src.volume = volume;
            src.Play();
        }
    }
}
