using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RazisRealm.RmeCustomObjects.Editor
{
    public sealed class RmeCustomObjectBuilder : EditorWindow
    {
        private static readonly string[] KnownPrefabs = {
            "AdrenalineMedkitStructure", "Angled Fences Open Connector", "binaryTargetPrefab",
            "Boxes Ladder Open Connector", "Broken Electrical Box Open Connector", "CapybaraToy",
            "dboyTargetPrefab", "Experimental Weapon Locker", "EZ BreakableDoor", "EzArmCameraToy",
            "EzCameraToy", "GeneratorStructure", "HCZ BreakableDoor", "HCZ BulkDoor", "LczCameraToy",
            "MiscLocker", "Pipes Long Open Connector", "Pipes Short Open Connector", "RegularMedkitStructure",
            "RifleRackStructure", "Scp500PedestalStructure Variant", "Simple Boxes Open Connector",
            "Spawnable Work Station Structure", "sportTargetPrefab", "SzCameraToy",
            "Tank-Supported Shelf Open Connector"
        };

        private int _prefabIndex;
        private PrimitiveType _primitive = PrimitiveType.Cube;
        private string _search = "";
        private Vector2 _prefabScroll;

        [MenuItem("RME Custom Objects/Open Builder %#r")]
        public static void Open() => GetWindow<RmeCustomObjectBuilder>("RME Builder");

        [MenuItem("RME Custom Objects/Create Custom Object Root")]
        public static void CreateRoot()
        {
            var root = new GameObject("NewCustomObject");
            root.AddComponent<RmeCustomObjectRoot>();
            Undo.RegisterCreatedObjectUndo(root, "Create RME custom object");
            Selection.activeGameObject = root;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Razi's Realm Custom Objects", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create a root, add blocks beneath it, arrange them in the Scene, then export.", MessageType.Info);
            if (GUILayout.Button("Create Custom Object Root")) CreateRoot();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Primitive", EditorStyles.boldLabel);
            _primitive = (PrimitiveType)EditorGUILayout.EnumPopup("Shape", _primitive);
            using (new EditorGUI.DisabledScope(FindRoot() == null))
                if (GUILayout.Button("Add Primitive")) AddPrimitive(_primitive);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SCP:SL Prefab", EditorStyles.boldLabel);
            _search = EditorGUILayout.TextField("Search", _search);
            string[] visible = KnownPrefabs.Where(value => string.IsNullOrWhiteSpace(_search) ||
                value.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            _prefabScroll = EditorGUILayout.BeginScrollView(_prefabScroll, GUILayout.Height(150));
            using (new EditorGUI.DisabledScope(FindRoot() == null))
                foreach (string prefab in visible)
                    if (GUILayout.Button($"{Category(prefab)}  {prefab}", EditorStyles.miniButton)) AddPrefab(prefab);
            EditorGUILayout.EndScrollView();
            if (visible.Length == 0) EditorGUILayout.HelpBox("No prefab names match that search.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Interactive blocks", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(FindRoot() == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Light")) AddSimple("Light", RmeBlockKind.Light);
                if (GUILayout.Button("Pickup")) AddSimple("Pickup", RmeBlockKind.Pickup);
                if (GUILayout.Button("Workstation")) AddSimple("Workstation", RmeBlockKind.Workstation);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Locker")) AddSimple("Locker", RmeBlockKind.Locker);
                if (GUILayout.Button("Door")) AddSimple("Door", RmeBlockKind.Door);
                if (GUILayout.Button("Interactable")) AddSimple("Interactable", RmeBlockKind.Interactable);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(FindRoot() == null))
            {
                if (GUILayout.Button("Validate Selected Custom Object")) ValidateSelected();
                if (GUILayout.Button("Export Selected Custom Object", GUILayout.Height(32))) ExportSelected();
            }
        }

        private static RmeCustomObjectRoot FindRoot()
        {
            if (Selection.activeGameObject != null)
            {
                RmeCustomObjectRoot selected = Selection.activeGameObject.GetComponentInParent<RmeCustomObjectRoot>();
                if (selected != null) return selected;
            }
            return FindObjectOfType<RmeCustomObjectRoot>();
        }

        private static Transform PlacementParent()
        {
            RmeCustomObjectRoot root = FindRoot();
            if (root == null) return null;
            return Selection.activeTransform != null && Selection.activeTransform.GetComponentInParent<RmeCustomObjectRoot>() == root
                ? Selection.activeTransform : root.transform;
        }

        private static GameObject NewBlock(string name, RmeBlockKind kind)
        {
            Transform parent = PlacementParent();
            if (parent == null) return null;
            var value = new GameObject(name);
            value.transform.SetParent(parent, false);
            value.AddComponent<RmeObjectBlock>().Kind = kind;
            Undo.RegisterCreatedObjectUndo(value, "Add RME block");
            Selection.activeGameObject = value;
            SceneView.lastActiveSceneView?.FrameSelected();
            return value;
        }

        private static void AddPrimitive(PrimitiveType type)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = type.ToString();
            value.transform.SetParent(PlacementParent(), false);
            RmeObjectBlock block = value.AddComponent<RmeObjectBlock>();
            block.Kind = RmeBlockKind.Primitive;
            block.PrimitiveType = type;
            Undo.RegisterCreatedObjectUndo(value, "Add RME primitive");
            Selection.activeGameObject = value;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private static void AddPrefab(string prefabName)
        {
            GameObject value = NewBlock(prefabName, RmeBlockKind.Prefab);
            if (value == null) return;
            value.GetComponent<RmeObjectBlock>().PrefabName = prefabName;
            RmePreviewFactory.Rebuild(value.GetComponent<RmeObjectBlock>());
        }

        private static void AddSimple(string name, RmeBlockKind kind)
        {
            GameObject value = NewBlock(name, kind);
            if (value != null) RmePreviewFactory.Rebuild(value.GetComponent<RmeObjectBlock>());
        }

        private static void ExportSelected()
        {
            RmeCustomObjectRoot root = FindRoot();
            if (root == null) return;
            string safeName = string.Concat((root.ObjectName ?? "CustomObject").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            if (string.IsNullOrEmpty(safeName)) safeName = "CustomObject";
            string path = EditorUtility.SaveFilePanel("Export RME Custom Object", "", safeName + ".json", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string json = RmeJsonExporter.Export(root);
                File.WriteAllText(path, json, new UTF8Encoding(false));
                EditorUtility.RevealInFinder(path);
                EditorUtility.DisplayDialog("RME Custom Objects", $"Exported {root.ObjectName}\n\n{path}", "Done");
                Debug.Log($"[RME Custom Objects] Exported {root.ObjectName} to {path}");
            }
            catch (Exception exception) { Debug.LogError("[RME Custom Objects] Export failed: " + exception.Message); }
        }

        private static void ValidateSelected()
        {
            RmeCustomObjectRoot root = FindRoot();
            try
            {
                string json = RmeJsonExporter.Export(root);
                int blocks = root.GetComponentsInChildren<RmeObjectBlock>(true).Length;
                EditorUtility.DisplayDialog("RME validation passed", $"{root.ObjectName}\n{blocks} exportable blocks\n{json.Length:N0} JSON characters", "OK");
            }
            catch (Exception exception) { EditorUtility.DisplayDialog("RME validation failed", exception.Message, "Fix it"); }
        }

        private static string Category(string name)
        {
            string value = name.ToLowerInvariant();
            if (value.Contains("locker") || value.Contains("rack") || value.Contains("medkit") || value.Contains("pedestal")) return "LOCKER";
            if (value.Contains("door")) return "DOOR";
            if (value.Contains("camera")) return "CAMERA";
            if (value.Contains("target")) return "TARGET";
            if (value.Contains("capybara")) return "TOY";
            if (value.Contains("work") || value.Contains("generator")) return "INTERACTIVE";
            return "FACILITY";
        }
    }

    [InitializeOnLoad]
    internal static class RmeWelcome
    {
        static RmeWelcome()
        {
            EditorApplication.delayCall += () => {
                if (!SessionState.GetBool("RmeWelcomeShown", false))
                {
                    SessionState.SetBool("RmeWelcomeShown", true);
                    if (UnityEngine.Object.FindObjectOfType<RmeCustomObjectRoot>() == null)
                        RmeCustomObjectBuilder.CreateRoot();
                    RmeCustomObjectBuilder.Open();
                }
            };
        }
    }
}
