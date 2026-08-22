using UnityEditor;
using UnityEngine;

namespace RazisRealm.RmeCustomObjects.Editor
{
    internal static class RmePreviewFactory
    {
        internal const string PreviewName = "__RME_PREVIEW__";

        internal static void Rebuild(RmeObjectBlock block)
        {
            if (block == null) return;
            Transform existing = block.transform.Find(PreviewName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            GameObject preview = block.Kind switch
            {
                RmeBlockKind.Light => Sphere("Light preview", new Vector3(.25f, .25f, .25f), new Color(1f, .9f, .25f)),
                RmeBlockKind.Pickup => Box("Pickup preview", new Vector3(.3f, .12f, .5f), new Color(.25f, .7f, 1f)),
                RmeBlockKind.Workstation => Box("Workstation preview", new Vector3(1.2f, 1.5f, .7f), new Color(.25f, .45f, .65f)),
                RmeBlockKind.Locker => Box("Locker preview", new Vector3(1.8f, 2.1f, .65f), new Color(.35f, .4f, .45f)),
                RmeBlockKind.Door => Box("Door preview", new Vector3(2.3f, 2.8f, .18f), new Color(.25f, .3f, .35f)),
                RmeBlockKind.Interactable => Box("Interactable preview", Vector3.one, new Color(.8f, .35f, .85f, .45f)),
                RmeBlockKind.Prefab => PrefabProxy(block.PrefabName),
                _ => Box("Block preview", Vector3.one, Color.gray)
            };
            preview.name = PreviewName;
            preview.transform.SetParent(block.transform, false);
            preview.hideFlags = HideFlags.NotEditable;
            foreach (Collider collider in preview.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            EditorUtility.SetDirty(block.gameObject);
        }

        private static GameObject PrefabProxy(string name)
        {
            string lower = (name ?? "").ToLowerInvariant();
            if (lower.Contains("door")) return Box(name, new Vector3(2.3f, 2.8f, .18f), new Color(.22f, .3f, .38f));
            if (lower.Contains("locker") || lower.Contains("rack") || lower.Contains("medkit") || lower.Contains("pedestal"))
                return Box(name, new Vector3(1.5f, 2f, .65f), new Color(.36f, .42f, .48f));
            if (lower.Contains("camera")) return Box(name, new Vector3(.45f, .35f, .7f), new Color(.2f, .25f, .3f));
            if (lower.Contains("capybara")) return Box(name, new Vector3(1.1f, .65f, .55f), new Color(.55f, .35f, .2f));
            if (lower.Contains("work") || lower.Contains("generator")) return Box(name, new Vector3(1.4f, 1.6f, .8f), new Color(.25f, .45f, .6f));
            if (lower.Contains("target")) return Box(name, new Vector3(.7f, 1.7f, .12f), new Color(.7f, .7f, .65f));
            return Box(name, Vector3.one, new Color(.35f, .65f, .5f));
        }

        private static GameObject Box(string name, Vector3 scale, Color color) => Shape(PrimitiveType.Cube, name, scale, color);
        private static GameObject Sphere(string name, Vector3 scale, Color color) => Shape(PrimitiveType.Sphere, name, scale, color);
        private static GameObject Shape(PrimitiveType type, string name, Vector3 scale, Color color)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.localScale = scale;
            Renderer renderer = value.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
            }
            return value;
        }
    }
}
