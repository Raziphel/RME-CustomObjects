using System.Linq;
using TMPro;
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
                RmeBlockKind.Primitive => Shape(block.PrimitiveType, "Primitive preview", Vector3.one, block.Color),
                RmeBlockKind.Light => LightPreview(block),
                RmeBlockKind.Pickup => Box("Pickup preview", new Vector3(.3f, .12f, .5f), new Color(.25f, .7f, 1f)),
                RmeBlockKind.Workstation => Box("Workstation preview", new Vector3(1.2f, 1.5f, .7f), new Color(.25f, .45f, .65f)),
                RmeBlockKind.Locker => Box("Locker preview", new Vector3(1.8f, 2.1f, .65f), new Color(.35f, .4f, .45f)),
                RmeBlockKind.Door => Box("Door preview", new Vector3(2.3f, 2.8f, .18f), new Color(.25f, .3f, .35f)),
                RmeBlockKind.Interactable => Box("Interactable preview", Vector3.one, new Color(.8f, .35f, .85f, .45f)),
                RmeBlockKind.Text => TextPreview(block),
                RmeBlockKind.Waypoint => Sphere("Waypoint preview", new Vector3(.3f, .3f, .3f), new Color(.3f, .85f, 1f, .55f)),
                RmeBlockKind.Prefab => ImportedPrefab(block.PrefabName) ?? PrefabProxy(block.PrefabName),
                _ => Box("Block preview", Vector3.one, Color.gray)
            };
            preview.name = PreviewName;
            preview.transform.SetParent(block.transform, false);
            preview.hideFlags = HideFlags.NotEditable;
            if (block.Kind == RmeBlockKind.Primitive && !block.PrimitiveVisible)
                foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            foreach (Collider collider in preview.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            EditorUtility.SetDirty(block.gameObject);
        }

        private static GameObject LightPreview(RmeObjectBlock block)
        {
            var value = new GameObject("Light preview");
            Light light = value.AddComponent<Light>();
            int serializedType = (int)block.LightType;
            if (serializedType < (int)RmeLightType.Spot || serializedType > (int)RmeLightType.Disc)
            {
                block.LightType = RmeLightType.Point;
                block.LightShape = RmeLightShape.Cone;
                EditorUtility.SetDirty(block);
            }
            light.type = (LightType)(int)block.LightType;
            light.color = block.Color;
            light.intensity = Mathf.Max(0f, block.LightIntensity);
            light.range = Mathf.Max(0f, block.LightRange);
            light.spotAngle = Mathf.Clamp(block.SpotAngle, 0f, 179f);
            light.innerSpotAngle = Mathf.Clamp(block.InnerSpotAngle, 0f, light.spotAngle);
            light.shadows = block.LightShadows;
            light.shadowStrength = Mathf.Clamp01(block.LightShadowStrength);
            light.areaSize = Vector2.one;
            return value;
        }

        private static GameObject TextPreview(RmeObjectBlock block)
        {
            var value = new GameObject("Text Toy preview");
            TextMeshPro text = value.AddComponent<TextMeshPro>();
            text.text = string.IsNullOrEmpty(block.Text) ? "Custom Text" : block.Text;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 2f;
            text.enableAutoSizing = true;
            text.fontSizeMin = .1f;
            text.fontSizeMax = 2f;
            text.rectTransform.sizeDelta = new Vector2(
                Mathf.Max(.05f, block.TextDisplaySize.x), Mathf.Max(.05f, block.TextDisplaySize.y));
            return value;
        }

        private static GameObject ImportedPrefab(string name)
        {
            GameObject asset = FindImportedAsset(name);
            if (asset == null) return null;
            GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (instance == null) return null;
            bool hasGeometry = instance.GetComponentsInChildren<MeshFilter>(true).Any(value => value.sharedMesh != null) ||
                instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(value => value.sharedMesh != null);
            if (hasGeometry) return instance;
            Object.DestroyImmediate(instance);
            return null;
        }

        internal static GameObject FindImportedAsset(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string[] matches = AssetDatabase.FindAssets($"{name} t:Prefab",
                new[] { "Assets/RME-CustomObjects/Prefabs/RRP" });
            foreach (string match in matches)
            {
                string path = AssetDatabase.GUIDToAssetPath(match);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset != null && asset.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return asset;
            }
            return null;
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
                if (color.a < 1f)
                {
                    material.SetFloat("_Mode", 3f);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
                renderer.sharedMaterial = material;
            }
            return value;
        }
    }
}
