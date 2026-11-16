using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// Genera todos los sonidos por código, así el proyecto no necesita archivos de audio.
    /// Si luego quieren usar sonidos propios, basta con asignar clips en los AudioSource.
    /// </summary>
    public static class ProceduralAudio
    {
        const int Rate = 44100;

        /// <summary>Ambiente continuo y suave (se repite sin cortes).</summary>
        public static AudioClip Drone(string name, float[] freqs, float seconds)
        {
            int n = Mathf.RoundToInt(Rate * seconds);
            var data = new float[n];
            if (freqs == null || freqs.Length == 0) freqs = new float[] { 220f };

            for (int f = 0; f < freqs.Length; f++)
            {
                // Se ajusta cada frecuencia para que complete ciclos enteros y el loop no haga "clic".
                float cycles = Mathf.Round(freqs[f] * seconds);
                float tremCycles = 1f + f; // vaivén lento distinto por voz
                float gain = 0.5f / freqs.Length;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / n;
                    float trem = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * tremCycles * t + f);
                    float s = Mathf.Sin(2f * Mathf.PI * cycles * t);
                    s += 0.25f * Mathf.Sin(2f * Mathf.PI * cycles * 2f * t);
                    data[i] += s * trem * gain;
                }
            }
            return Make(name, data);
        }

        /// <summary>Secuencia de notas tipo campanita (confirmaciones, cierre).</summary>
        public static AudioClip Chime(string name, float[] notes, float noteSeconds)
        {
            if (notes == null || notes.Length == 0) notes = new float[] { 880f };
            int perNote = Mathf.RoundToInt(Rate * noteSeconds);
            int tail = Mathf.RoundToInt(Rate * 0.8f);
            var data = new float[perNote * notes.Length + tail];

            for (int k = 0; k < notes.Length; k++)
            {
                int start = k * perNote;
                int len = perNote + tail;
                for (int i = 0; i < len && start + i < data.Length; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Exp(-4.5f * t) * Mathf.Clamp01(i / 200f);
                    float s = Mathf.Sin(2f * Mathf.PI * notes[k] * t)
                              + 0.3f * Mathf.Sin(2f * Mathf.PI * notes[k] * 2f * t);
                    data[start + i] += s * env * 0.35f;
                }
            }
            return Make(name, data);
        }

        /// <summary>Golpe grave y corto: representa la vibración cuando no hay control háptico.</summary>
        public static AudioClip Thump(string name, float seconds)
        {
            int n = Mathf.RoundToInt(Rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Exp(-18f * t) * Mathf.Clamp01(i / 60f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 70f * t) * env * 0.9f;
            }
            return Make(name, data);
        }

        static AudioClip Make(string name, float[] data)
        {
            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
