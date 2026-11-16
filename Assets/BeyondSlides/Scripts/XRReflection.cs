using System;
using System.Reflection;
using UnityEngine;

namespace BeyondSlides
{
    /// <summary>
    /// Acceso al XR Interaction Toolkit por reflexión. Así los scripts compilan
    /// igual con XRI 2.x, XRI 3.x o incluso sin el paquete instalado (modo escritorio).
    /// </summary>
    public static class XRReflection
    {
        /// <summary>Busca un componente del XR Interaction Toolkit por nombre de clase.</summary>
        public static Type FindType(string className)
        {
            Type fallback = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch (Exception) { continue; }
                if (types == null) continue;

                foreach (var t in types)
                {
                    if (t == null || t.Name != className || t.IsAbstract) continue;
                    if (!typeof(Component).IsAssignableFrom(t)) continue;
                    if (t.Namespace != null && t.Namespace.Contains("Interaction.Toolkit")) return t;
                    if (fallback == null) fallback = t;
                }
            }
            return fallback;
        }

        /// <summary>Añade el componente si existe el paquete; devuelve null si no.</summary>
        public static Component AddIfAvailable(GameObject go, string className)
        {
            var type = FindType(className);
            if (type == null) return null;
            var existing = go.GetComponent(type);
            return existing != null ? existing : go.AddComponent(type);
        }

        public static void TrySet(Component c, string property, object value)
        {
            if (c == null) return;
            try
            {
                var p = c.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
                if (p == null || !p.CanWrite) return;
                if (p.PropertyType.IsEnum && value is string)
                    value = Enum.Parse(p.PropertyType, (string)value);
                p.SetValue(c, value, null);
            }
            catch (Exception) { /* la propiedad no existe en esta versión: se ignora */ }
        }

        /// <summary>Lector de una propiedad bool (isHovered / isSelected) de un interactable.</summary>
        public class BoolReader
        {
            readonly Component target;
            readonly PropertyInfo prop;

            public BoolReader(Component target, string property)
            {
                this.target = target;
                if (target != null)
                    prop = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            }

            public bool Value
            {
                get
                {
                    if (target == null || prop == null) return false;
                    try { return (bool)prop.GetValue(target, null); }
                    catch (Exception) { return false; }
                }
            }
        }
    }
}
