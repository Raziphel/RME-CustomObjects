using UnityEditor;
using UnityEngine;

namespace RazisRealm.RmeCustomObjects.Editor
{
    [CustomEditor(typeof(RmeObjectBlock))]
    [CanEditMultipleObjects]
    public sealed class RmeObjectBlockEditor : UnityEditor.Editor
    {
        private static readonly string[] ItemNames =
        {
            "Keycard Janitor", "Keycard Scientist", "Keycard Research Coordinator", "Keycard Zone Manager",
            "Keycard Guard", "Keycard MTF Private", "Keycard Containment Engineer", "Keycard MTF Operative",
            "Keycard MTF Captain", "Keycard Facility Manager", "Keycard Chaos Insurgency", "Keycard O5",
            "Radio", "COM-15", "Medkit", "Flashlight", "Micro H.I.D.", "SCP-500", "SCP-207",
            "12 Gauge Ammo", "E-11-SR", "Crossvec", "5.56x45 Ammo", "FSP-9", "Logicer",
            "High-Explosive Grenade", "Flashbang", ".44 Cal Ammo", "7.62x39 Ammo", "9x19 Ammo",
            "COM-18", "SCP-018", "SCP-268", "Adrenaline", "Painkillers", "Coin", "Light Armor",
            "Combat Armor", "Heavy Armor", "Revolver", "AK", "Shotgun", "SCP-330", "SCP-2176",
            "SCP-244-A", "SCP-244-B", "SCP-1853", "Particle Disruptor", "COM-45", "SCP-1576",
            "Jailbird", "Anti-SCP-207", "FR-MG-0", "A7", "Lantern", "SCP-1344", "Snowball", "Coal",
            "Special Coal", "SCP-1507 Tape", "Debug Ragdoll Mover", "Surface Access Pass", "SCP-127",
            "Custom Task Force Keycard", "Custom Site-02 Keycard", "Custom Management Keycard",
            "Custom Metal Case Keycard", "Marshmallow", "SCP-1509", "SCP-021-J"
        };

        private static readonly int[] ItemValues = BuildItemValues();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Kind"));
            RmeBlockKind kind = (RmeBlockKind)serializedObject.FindProperty("Kind").intValue;
            if (kind == RmeBlockKind.Prefab)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("PrefabName"));
                string prefabName = serializedObject.FindProperty("PrefabName").stringValue;
                if ((prefabName ?? string.Empty).IndexOf("door", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    EditorGUILayout.HelpBox("This is a real SCP:SL network door. Configure its initial state and permissions below; do not add a separate interaction trigger.", MessageType.Info);
            }
            if (kind == RmeBlockKind.Primitive)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("PrimitiveType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Color"));
                SerializedProperty visible = serializedObject.FindProperty("PrimitiveVisible");
                SerializedProperty collidable = serializedObject.FindProperty("PrimitiveCollidable");
                if (visible != null && collidable != null)
                {
                    EditorGUILayout.PropertyField(visible, new GUIContent("Visible"));
                    EditorGUILayout.PropertyField(collidable, new GUIContent("Collidable"));
                }
                else EditorGUILayout.HelpBox("RME Runtime scripts are out of date. Update the complete Runtime folder to edit visibility and collision.", MessageType.Error);
                EditorGUILayout.HelpBox("Visible and Collidable are independent MER-compatible primitive flags. Invisible colliders remain selectable through their hierarchy entry.", MessageType.Info);
            }
            if (kind == RmeBlockKind.Pickup)
            {
                SerializedProperty itemType = serializedObject.FindProperty("ItemType");
                EditorGUI.showMixedValue = itemType.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                int selectedItem = EditorGUILayout.IntPopup("Base Game Item", itemType.intValue,
                    ItemNames, ItemValues);
                if (EditorGUI.EndChangeCheck()) itemType.intValue = selectedItem;
                EditorGUI.showMixedValue = false;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CustomItemName"),
                    new GUIContent("Custom Item Name"));
                EditorGUILayout.HelpBox("Leave Custom Item Name empty to spawn the selected base-game item. A custom item name overrides the dropdown and must exactly match a RealmPlugin registered custom item.", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Chance"));
            }
            if (kind == RmeBlockKind.Locker)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LockerType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Chance"));
            }
            if (kind == RmeBlockKind.Light)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LightType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LightIntensity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LightRange"));
                RmeLightType type = (RmeLightType)serializedObject.FindProperty("LightType").intValue;
                if (type == RmeLightType.Spot)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("SpotAngle"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("InnerSpotAngle"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LightShadows"));
                if ((LightShadows)serializedObject.FindProperty("LightShadows").intValue != LightShadows.None)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("LightShadowStrength"));
                EditorGUILayout.HelpBox("This uses Unity's real Light component, so its color, cone/shape, range, intensity, and shadows preview like MER. RME exports the same values used in-game.", MessageType.Info);
            }
            if (kind == RmeBlockKind.Door || kind == RmeBlockKind.Prefab)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IsOpen"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IsLocked"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RequiredPermissions"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RequireAll"));
            }
            if (kind == RmeBlockKind.Prefab &&
                (serializedObject.FindProperty("PrefabName").stringValue ?? string.Empty)
                    .IndexOf("CameraToy", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SerializedProperty cameraLabel = serializedObject.FindProperty("CameraLabel");
                if (cameraLabel != null) EditorGUILayout.PropertyField(cameraLabel, new GUIContent("SCP-079 Camera Name"));
                else EditorGUILayout.HelpBox("RME Runtime scripts are out of date. Update the complete Runtime folder to name cameras.", MessageType.Error);
            }
            if (kind == RmeBlockKind.Workstation || kind == RmeBlockKind.Interactable)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IsInteractable"));
            if (kind == RmeBlockKind.Interactable)
                EditorGUILayout.HelpBox("An Interaction Trigger is an invisible clickable volume for scripted map interactions. It does not create a door.", MessageType.None);
            if (kind == RmeBlockKind.Text)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Text"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("TextDisplaySize"),
                    new GUIContent("Display Size"));
            }
            if (kind == RmeBlockKind.NestedObject) EditorGUILayout.PropertyField(serializedObject.FindProperty("NestedObjectName"));
            bool changed = serializedObject.ApplyModifiedProperties();
            if (changed) RebuildTargets();
            else
                foreach (Object value in targets)
                    RmePreviewFactory.RefreshLight(value as RmeObjectBlock);
            if (GUILayout.Button("Rebuild Scene Preview")) RebuildTargets();
        }

        private void RebuildTargets()
        {
            foreach (Object value in targets) RmePreviewFactory.Rebuild((RmeObjectBlock)value);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawInvisiblePrimitive(RmeObjectBlock block, GizmoType gizmoType)
        {
            if (block == null || block.Kind != RmeBlockKind.Primitive ||
                RmeBlockCompatibility.PrimitiveVisible(block) && block.Color.a > .001f) return;
            Transform preview = block.transform.Find(RmePreviewFactory.PreviewName);
            MeshFilter filter = preview == null
                ? block.GetComponent<MeshFilter>()
                : preview.GetComponentInChildren<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;
            Gizmos.color = new Color(.2f, .85f, 1f, .9f);
            Gizmos.matrix = filter.transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(filter.sharedMesh);
        }

        private static int[] BuildItemValues()
        {
            var values = new int[ItemNames.Length];
            for (int index = 0; index < values.Length; index++) values[index] = index;
            return values;
        }
    }
}
