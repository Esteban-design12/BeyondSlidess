using UnityEngine;

namespace BeyondSlides
{
    /// <summary>Crea textos 3D (TextMesh) sin depender de TextMeshPro.</summary>
    public static class TextUtil
    {
        static Font cachedFont;

        public static Font DefaultFont()
        {
            if (cachedFont != null) return cachedFont;
            string[] names = { "LegacyRuntime.ttf", "Arial.ttf" };
            for (int i = 0; i < names.Length && cachedFont == null; i++)
            {
                try { cachedFont = Resources.GetBuiltinResource<Font>(names[i]); }
                catch (System.Exception) { cachedFont = null; }
            }
            return cachedFont;
        }

        /// <param name="height">Altura aproximada de una línea, en metros.</param>
        public static TextMesh Create(Transform parent, string name, string text, Vector3 localPos,
            float height, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            var tm = go.AddComponent<TextMesh>();
            var font = DefaultFont();
            if (font != null)
            {
                tm.font = font;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = font.material;
            }
            tm.text = text;
            tm.fontSize = 80;
            tm.characterSize = height / 8f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            return tm;
        }
    }
}
