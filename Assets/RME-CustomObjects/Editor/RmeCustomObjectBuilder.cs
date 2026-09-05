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
            "Tank-Supported Shelf Open Connector", "ElevatorChamber", "ElevatorChamber Gates",
            "ElevatorChamberCargo", "ElevatorChamberNuke"
        };

        private PrimitiveType _primitive = PrimitiveType.Cube;
        private BuildTarget _animationBuildTarget = BuildTarget.StandaloneLinux64;
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

        [MenuItem("RME Custom Objects/Import JSON for Editing")]
        public static void ImportJson()
        {
            string path = EditorUtility.OpenFilePanel("Import RME Custom Object", "", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                RmeCustomObjectRoot root = RmeJsonImporter.Import(path);
                EditorUtility.DisplayDialog("RME Custom Object imported",
                    $"Imported {root.ObjectName}\n\nThe hierarchy and editable RME properties have been reconstructed in the current scene.", "Edit");
            }
            catch (Exception exception)
            {
                Debug.LogError("[RME Custom Objects] Import failed: " + exception);
                EditorUtility.DisplayDialog("RME import failed", exception.Message, "Close");
            }
        }

        [MenuItem("RME Custom Objects/Refresh Imported Material Shaders")]
        public static void RefreshImportedMaterialShaders()
        {
            const string shader = "Assets/Room Reference/DON'T TOUCH/Materials/HDRP_Lit.shader";
            AssetDatabase.ImportAsset(shader, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("[RME Custom Objects] Refreshed the compatibility shader and imported materials.");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Razi's Realm Custom Objects", EditorStyles.boldLabel);
            RmeCustomObjectRoot activeRoot = FindRoot();
            EditorGUILayout.HelpBox(activeRoot == null
                ? "Start by creating a custom-object root."
                : $"Editing: {activeRoot.ObjectName}  •  {activeRoot.GetComponentsInChildren<RmeObjectBlock>(true).Length} blocks",
                activeRoot == null ? MessageType.Warning : MessageType.Info);
            if (GUILayout.Button("Create Custom Object Root")) CreateRoot();
            if (GUILayout.Button("Import JSON for Editing", GUILayout.Height(28))) ImportJson();

            if (activeRoot != null)
            {
                int animated = activeRoot.GetComponentsInChildren<RmeObjectBlock>(true)
                    .Count(block => !string.IsNullOrWhiteSpace(block.AnimatorName));
                if (animated > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Animation Export", EditorStyles.boldLabel);
                    _animationBuildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Server Platform", _animationBuildTarget);
                    EditorGUILayout.HelpBox($"Export will build {animated} configured Animator Bundle(s) beside the JSON for {_animationBuildTarget}. Each animated primitive needs both an Animator Bundle name and an Animator Controller asset.", MessageType.Info);
                }
            }

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
                {
                    GameObject asset = RmePreviewFactory.FindImportedAsset(prefab);
                    Texture image = asset == null ? null : AssetPreview.GetAssetPreview(asset) ?? AssetPreview.GetMiniThumbnail(asset);
                    var content = new GUIContent($"  {prefab}\n  {Category(prefab)}", image,
                        "Click to place this SCP:SL prefab under the selected object");
                    if (GUILayout.Button(content, GUILayout.Height(48))) AddPrefab(prefab);
                }
            EditorGUILayout.EndScrollView();
            if (visible.Length == 0) EditorGUILayout.HelpBox("No prefab names match that search.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lights", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(FindRoot() == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Point")) AddLight("Point Light", RmeLightType.Point, RmeLightShape.Cone);
                if (GUILayout.Button("Spot")) AddLight("Spot Light", RmeLightType.Spot, RmeLightShape.Cone);
                if (GUILayout.Button("Directional")) AddLight("Directional Light", RmeLightType.Directional, RmeLightShape.Cone);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Rectangle")) AddLight("Rectangle Light", RmeLightType.Rectangle, RmeLightShape.Cone);
                if (GUILayout.Button("Disc")) AddLight("Disc Light", RmeLightType.Disc, RmeLightShape.Cone);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Toys", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(FindRoot() == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Text Toy")) AddSimple("Text Toy", RmeBlockKind.Text);
                if (GUILayout.Button("Waypoint Toy")) AddSimple("Waypoint Toy", RmeBlockKind.Waypoint);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Interactive blocks", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(FindRoot() == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Pickup")) AddSimple("Pickup", RmeBlockKind.Pickup);
                if (GUILayout.Button("Workstation")) AddSimple("Workstation", RmeBlockKind.Workstation);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Locker")) AddSimple("Locker", RmeBlockKind.Locker);
                if (GUILayout.Button("Interaction Trigger")) AddSimple("Interaction Trigger", RmeBlockKind.Interactable);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.HelpBox("Doors are placed from the searchable SCP:SL Prefab list above. Search for 'door' and choose the exact LCZ, HCZ, EZ, or Bulk Door visual. Interaction Trigger is an invisible click/hold region—not a door.", MessageType.None);

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
            var value = new GameObject(type.ToString());
            value.name = type.ToString();
            value.transform.SetParent(PlacementParent(), false);
            RmeObjectBlock block = value.AddComponent<RmeObjectBlock>();
            block.Kind = RmeBlockKind.Primitive;
            block.PrimitiveType = type;
            RmePreviewFactory.Rebuild(block);
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

        private static void AddLight(string name, RmeLightType type, RmeLightShape shape)
        {
            GameObject value = NewBlock(name, RmeBlockKind.Light);
            if (value == null) return;
            RmeObjectBlock block = value.GetComponent<RmeObjectBlock>();
            block.LightType = type;
            block.LightShape = shape;
            RmePreviewFactory.Rebuild(block);
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
                int bundles = ExportAnimatorBundles(root, Path.GetDirectoryName(path));
                File.WriteAllText(path, json, new UTF8Encoding(false));
                EditorUtility.RevealInFinder(path);
                EditorUtility.DisplayDialog("RME Custom Object exported",
                    $"Exported {root.ObjectName}\n\n{path}\n\nAnimator bundles: {bundles}\n\nCopy the JSON and any listed bundles into the same server custom-object folder, run 'rme custom reload', then place/reference '{safeName}' in the RME map.", "Done");
                Debug.Log($"[RME Custom Objects] Exported {root.ObjectName} to {path}");
            }
            catch (Exception exception) { Debug.LogError("[RME Custom Objects] Export failed: " + exception.Message); }
        }

        private int ExportAnimatorBundles(RmeCustomObjectRoot root, string outputDirectory)
        {
            RmeObjectBlock[] animated = root.GetComponentsInChildren<RmeObjectBlock>(true)
                .Where(block => !string.IsNullOrWhiteSpace(block.AnimatorName)).ToArray();
            if (animated.Length == 0) return 0;
            var builds = new List<AssetBundleBuild>();
            foreach (IGrouping<string, RmeObjectBlock> group in animated.GroupBy(block => block.AnimatorName.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                if (group.Select(block => block.AnimatorName.Trim()).Distinct(StringComparer.Ordinal).Count() != 1)
                    throw new InvalidOperationException($"Animator Bundle '{group.Key}' uses inconsistent letter casing. Use one exact filename on every primitive.");
                RuntimeAnimatorController[] controllers = group.Select(block => block.AnimatorController)
                    .Where(controller => controller != null).Distinct().ToArray();
                if (controllers.Length != 1 || group.Any(block => block.AnimatorController == null))
                    throw new InvalidOperationException($"Animator Bundle '{group.Key}' must use one assigned Animator Controller.");
                string controllerPath = AssetDatabase.GetAssetPath(controllers[0]);
                if (string.IsNullOrWhiteSpace(controllerPath))
                    throw new InvalidOperationException($"Animator Bundle '{group.Key}' references a controller outside this Unity project.");
                builds.Add(new AssetBundleBuild { assetBundleName = group.Key, assetNames = new[] { controllerPath } });
            }
            if (BuildPipeline.BuildAssetBundles(outputDirectory, builds.ToArray(), BuildAssetBundleOptions.StrictMode,
                    _animationBuildTarget) == null)
                throw new InvalidOperationException("Unity could not build the Animator AssetBundles. Check the Console for the specific import or controller error.");
            return builds.Count;
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
