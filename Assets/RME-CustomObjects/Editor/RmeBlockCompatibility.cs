using System.Reflection;

namespace RazisRealm.RmeCustomObjects.Editor
{
    internal static class RmeBlockCompatibility
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public;

        internal static bool HasCurrentSchema =>
            typeof(RmeObjectBlock).GetField("PrimitiveVisible", Fields) != null &&
            typeof(RmeObjectBlock).GetField("PrimitiveCollidable", Fields) != null &&
            typeof(RmeObjectBlock).GetField("CameraLabel", Fields) != null;

        internal static bool PrimitiveVisible(RmeObjectBlock block) => Get(block, "PrimitiveVisible", true);
        internal static bool PrimitiveCollidable(RmeObjectBlock block) => Get(block, "PrimitiveCollidable", true);
        internal static string CameraLabel(RmeObjectBlock block) => Get(block, "CameraLabel", "CustomCamera");
        internal static void SetPrimitiveVisible(RmeObjectBlock block, bool value) => Set(block, "PrimitiveVisible", value);
        internal static void SetPrimitiveCollidable(RmeObjectBlock block, bool value) => Set(block, "PrimitiveCollidable", value);
        internal static void SetCameraLabel(RmeObjectBlock block, string value) => Set(block, "CameraLabel", value);

        private static T Get<T>(RmeObjectBlock block, string name, T fallback)
        {
            object value = block == null ? null : typeof(RmeObjectBlock).GetField(name, Fields)?.GetValue(block);
            return value is T typed ? typed : fallback;
        }

        private static void Set<T>(RmeObjectBlock block, string name, T value) =>
            typeof(RmeObjectBlock).GetField(name, Fields)?.SetValue(block, value);
    }
}
