using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RazisRealm.RmeCustomObjects.Editor
{
    internal static class RmeJsonExporter
    {
        internal static string Export(RmeCustomObjectRoot root)
        {
            RmeObjectBlock[] blocks = root.GetComponentsInChildren<RmeObjectBlock>(true)
                .Where(value => value.gameObject.name != RmePreviewFactory.PreviewName).ToArray();
            if (blocks.Length == 0)
                throw new InvalidOperationException("The custom object is empty. Add at least one primitive or SCP:SL prefab before exporting.");
            if (root.transform.localScale != Vector3.one)
                throw new InvalidOperationException("The custom-object root must keep scale 1,1,1. Scale its child blocks instead.");
            var ids = new Dictionary<Transform, int> { [root.transform] = 0 };
            int nextId = 1;
            foreach (RmeObjectBlock block in blocks) ids[block.transform] = nextId++;
            var output = new StringBuilder();
            output.Append("{\n  \"FormatVersion\": 1,\n  \"Id\": ").Append(Q(root.ObjectName));
            output.Append(",\n  \"Description\": ").Append(Q(root.Description));
            output.Append(",\n  \"RootObjectId\": 0,\n  \"Blocks\": [");
            for (int i = 0; i < blocks.Length; i++)
            {
                RmeObjectBlock block = blocks[i];
                Vector3 scale = block.transform.localScale;
                if (!Finite(block.transform.localPosition) || !Finite(block.transform.localEulerAngles) || !Finite(scale))
                    throw new InvalidOperationException($"Block '{block.name}' has an invalid transform.");
                Transform parent = block.transform.parent;
                while (parent != null && !ids.ContainsKey(parent)) parent = parent.parent;
                if (parent == null) throw new InvalidOperationException($"Block '{block.name}' is outside the custom-object hierarchy.");
                output.Append(i == 0 ? "\n" : ",\n").Append("    {");
                output.Append("\"Name\":").Append(Q(block.name));
                output.Append(",\"ObjectId\":").Append(ids[block.transform]);
                output.Append(",\"ParentId\":").Append(ids[parent]);
                output.Append(",\"AnimatorName\":null");
                output.Append(",\"Position\":").Append(V(block.transform.localPosition));
                output.Append(",\"Rotation\":").Append(V(block.transform.localEulerAngles));
                output.Append(",\"Scale\":").Append(V(block.transform.localScale));
                output.Append(",\"BlockType\":").Append((int)block.Kind);
                output.Append(",\"Properties\":").Append(Properties(block)).Append('}');
            }
            output.Append("\n  ]\n}\n");
            return output.ToString();
        }

        private static string Properties(RmeObjectBlock block)
        {
            var values = new List<string>();
            void Add(string key, string value) => values.Add(Q(key) + ":" + value);
            switch (block.Kind)
            {
                case RmeBlockKind.Primitive:
                    int flags = (RmeBlockCompatibility.PrimitiveVisible(block) ? RmeBlockCompatibility.VisibleFlag : 0) |
                                (RmeBlockCompatibility.PrimitiveCollidable(block) ? RmeBlockCompatibility.CollidableFlag : 0);
                    Add("PrimitiveType", ((int)block.PrimitiveType).ToString()); Add("Color", Q("#" + ColorUtility.ToHtmlStringRGBA(block.Color))); Add("PrimitiveFlags", flags.ToString()); break;
                case RmeBlockKind.Light:
                    RmePreviewFactory.CaptureLight(block, false);
                    Add("Color", Q("#" + ColorUtility.ToHtmlStringRGBA(block.Color)));
                    Add("Intensity", F(Mathf.Clamp(block.LightIntensity, 0f, 100f)));
                    Add("Range", F(Mathf.Clamp(block.LightRange, 0f, 500f)));
                    Add("LightType", ((int)block.LightType).ToString());
                    Add("ShadowType", ((int)block.LightShadows).ToString());
                    Add("Shape", ((int)block.LightShape).ToString());
                    Add("SpotAngle", F(Mathf.Clamp(block.SpotAngle, 0f, 179f)));
                    Add("InnerSpotAngle", F(Mathf.Clamp(block.InnerSpotAngle, 0f, block.SpotAngle)));
                    Add("ShadowStrength", F(Mathf.Clamp01(block.LightShadowStrength))); break;
                case RmeBlockKind.Pickup:
                    Add("ItemType", block.ItemType.ToString());
                    if (!string.IsNullOrWhiteSpace(block.CustomItemName)) Add("CustomItemName", Q(block.CustomItemName.Trim()));
                    Add("Chance", F(block.Chance)); Add("Locked", B(block.IsLocked)); break;
                case RmeBlockKind.Workstation: Add("IsInteractable", B(block.IsInteractable)); break;
                case RmeBlockKind.NestedObject: Add("Prefab", Q(block.NestedObjectName)); break;
                case RmeBlockKind.Locker:
                    Add("LockerType", block.LockerType.ToString()); Add("Chance", F(block.Chance)); Add("OpenedChambers", "0"); Add("KeycardPermissions", block.RequiredPermissions.ToString()); break;
                case RmeBlockKind.Text:
                    Add("Text", Q(block.Text)); Add("DisplaySize", V(block.TextDisplaySize)); break;
                case RmeBlockKind.Interactable: Add("IsLocked", B(block.IsLocked)); Add("InteractionDuration", "0"); Add("Shape", "0"); break;
                case RmeBlockKind.Door:
                    Add("DoorType", "1"); DoorProperties(block, Add); break;
                case RmeBlockKind.Prefab:
                    if (string.IsNullOrWhiteSpace(block.PrefabName)) throw new InvalidOperationException($"Prefab block '{block.name}' has no PrefabName.");
                    Add("PrefabName", Q(block.PrefabName));
                    if (block.PrefabName.IndexOf("CameraToy", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string label = RmeBlockCompatibility.CameraLabel(block);
                        Add("CameraLabel", Q(string.IsNullOrWhiteSpace(label) ? "CustomCamera" : label.Trim()));
                    }
                    DoorProperties(block, Add); break;
            }
            return "{" + string.Join(",", values) + "}";
        }

        private static void DoorProperties(RmeObjectBlock block, Action<string, string> add)
        {
            add("IsOpen", B(block.IsOpen)); add("IsLocked", B(block.IsLocked));
            add("RequiredPermissions", block.RequiredPermissions.ToString()); add("RequireAll", B(block.RequireAll));
        }

        private static string V(Vector3 value) => $"{{\"x\":{F(value.x)},\"y\":{F(value.y)},\"z\":{F(value.z)}}}";
        private static string V(Vector2 value) => $"{{\"x\":{F(value.x)},\"y\":{F(value.y)}}}";
        private static bool Finite(Vector3 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string B(bool value) => value ? "true" : "false";
        private static string Q(string value) => "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
    }
}
