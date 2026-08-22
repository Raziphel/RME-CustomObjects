using UnityEditor;
using UnityEngine;

namespace RazisRealm.RmeCustomObjects.Editor
{
    [CustomEditor(typeof(RmeObjectBlock))]
    public sealed class RmeObjectBlockEditor : UnityEditor.Editor
    {
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
            }
            if (kind == RmeBlockKind.Pickup)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ItemType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Chance"));
            }
            if (kind == RmeBlockKind.Locker)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LockerType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Chance"));
            }
            if (kind == RmeBlockKind.Light)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LightIntensity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LightRange"));
            }
            if (kind == RmeBlockKind.Door || kind == RmeBlockKind.Prefab)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IsOpen"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IsLocked"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RequiredPermissions"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RequireAll"));
            }
            if (kind == RmeBlockKind.Workstation || kind == RmeBlockKind.Interactable)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IsInteractable"));
            if (kind == RmeBlockKind.Interactable)
                EditorGUILayout.HelpBox("An Interaction Trigger is an invisible clickable volume for scripted map interactions. It does not create a door.", MessageType.None);
            if (kind == RmeBlockKind.Text) EditorGUILayout.PropertyField(serializedObject.FindProperty("Text"));
            if (kind == RmeBlockKind.NestedObject) EditorGUILayout.PropertyField(serializedObject.FindProperty("NestedObjectName"));
            if (serializedObject.ApplyModifiedProperties()) RmePreviewFactory.Rebuild((RmeObjectBlock)target);
            if (GUILayout.Button("Rebuild Scene Preview")) RmePreviewFactory.Rebuild((RmeObjectBlock)target);
        }
    }
}
