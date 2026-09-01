using System.Reflection;

using UnityEngine;

namespace RazisRealm.RmeCustomObjects.Editor
{
    internal static class RmeBlockCompatibility
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public;
        internal const int CollidableFlag = 1;
        internal const int VisibleFlag = 2;

        internal static bool HasCurrentSchema =>
            typeof(RmeObjectBlock).GetField("PrimitiveVisible", Fields) != null &&
            typeof(RmeObjectBlock).GetField("PrimitiveCollidable", Fields) != null &&
            typeof(RmeObjectBlock).GetField("UseCustomRgb", Fields) != null &&
            typeof(RmeObjectBlock).GetField("CameraLabel", Fields) != null;

        internal static bool PrimitiveVisible(RmeObjectBlock block) => Get(block, "PrimitiveVisible", true);
        internal static bool PrimitiveCollidable(RmeObjectBlock block) => Get(block, "PrimitiveCollidable", true);
        internal static string CameraLabel(RmeObjectBlock block) => Get(block, "CameraLabel", "CustomCamera");
        internal static void SetPrimitiveVisible(RmeObjectBlock block, bool value) => Set(block, "PrimitiveVisible", value);
        internal static void SetPrimitiveCollidable(RmeObjectBlock block, bool value) => Set(block, "PrimitiveCollidable", value);
        internal static void SetCameraLabel(RmeObjectBlock block, string value) => Set(block, "CameraLabel", value);
        internal static bool UsesCustomRgb(RmeObjectBlock block) => Get(block, "UseCustomRgb", false);
        internal static Color PreviewColor(RmeObjectBlock block) => UsesCustomRgb(block)
            ? new Color(Get(block, "CustomRed", 255) / 255f, Get(block, "CustomGreen", 255) / 255f,
                Get(block, "CustomBlue", 255) / 255f, Get(block, "CustomAlpha", 1f))
            : block.Color;
        internal static string CustomRgb(RmeObjectBlock block)
        {
            float alpha = Get(block, "CustomAlpha", 1f);
            if (float.IsNaN(alpha) || float.IsInfinity(alpha))
                throw new System.InvalidOperationException($"Primitive '{block.name}' has an invalid Custom RGB alpha value.");
            return $"{Get(block, "CustomRed", 255)}:{Get(block, "CustomGreen", 255)}:{Get(block, "CustomBlue", 255)}:{alpha.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}";
        }
        internal static void SetCustomRgb(RmeObjectBlock block, int red, int green, int blue, float alpha)
        {
            Set(block, "UseCustomRgb", true);
            Set(block, "CustomRed", red);
            Set(block, "CustomGreen", green);
            Set(block, "CustomBlue", blue);
            Set(block, "CustomAlpha", alpha);
        }

        private static T Get<T>(RmeObjectBlock block, string name, T fallback)
        {
            object value = block == null ? null : typeof(RmeObjectBlock).GetField(name, Fields)?.GetValue(block);
            return value is T typed ? typed : fallback;
        }

        private static void Set<T>(RmeObjectBlock block, string name, T value) =>
            typeof(RmeObjectBlock).GetField(name, Fields)?.SetValue(block, value);
    }
}
