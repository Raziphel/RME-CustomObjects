using UnityEngine;

namespace RazisRealm.RmeCustomObjects
{
    public enum RmeBlockKind
    {
        Empty = 0, Primitive = 1, Light = 2, Pickup = 3, Workstation = 4,
        NestedObject = 5, Locker = 7, Text = 8, Interactable = 9,
        Waypoint = 10, Door = 11, Prefab = 12
    }

    [DisallowMultipleComponent]
    public sealed class RmeObjectBlock : MonoBehaviour
    {
        [HideInInspector] public int ObjectId;
        public RmeBlockKind Kind = RmeBlockKind.Primitive;
        public string PrefabName;
        public PrimitiveType PrimitiveType = PrimitiveType.Cube;
        public Color Color = Color.white;
        public bool IsOpen;
        public bool IsLocked;
        public bool IsInteractable = true;
        public int ItemType;
        [Range(0f, 100f)] public float Chance = 100f;
        public int LockerType;
        public int RequiredPermissions;
        public bool RequireAll = true;
        public float LightIntensity = 1f;
        public float LightRange = 10f;
        [TextArea] public string Text;
        public string NestedObjectName;
    }
}
