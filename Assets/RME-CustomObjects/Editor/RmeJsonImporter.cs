using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RazisRealm.RmeCustomObjects.Editor
{
    internal static class RmeJsonImporter
    {
        internal static RmeCustomObjectRoot Import(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new InvalidOperationException("Select an existing RME custom-object JSON file.");
            ImportObject data = JsonUtility.FromJson<ImportObject>(File.ReadAllText(path));
            if (data == null || data.Blocks == null)
                throw new InvalidOperationException("The JSON has no Blocks array.");

            var definitions = new Dictionary<int, ImportBlock>();
            foreach (ImportBlock definition in data.Blocks)
            {
                if (definition == null || definition.ObjectId == data.RootObjectId)
                    throw new InvalidOperationException("Every block must have an ObjectId distinct from RootObjectId.");
                if (!definitions.TryAdd(definition.ObjectId, definition))
                    throw new InvalidOperationException($"Duplicate ObjectId {definition.ObjectId}.");
                if (!Enum.IsDefined(typeof(RmeBlockKind), definition.BlockType) || definition.BlockType == 6)
                    throw new InvalidOperationException($"Block {definition.ObjectId} has unsupported BlockType {definition.BlockType}.");
            }
            foreach (ImportBlock definition in data.Blocks)
            {
                var visited = new HashSet<int>();
                ImportBlock current = definition;
                while (current.ParentId != data.RootObjectId)
                {
                    if (!visited.Add(current.ObjectId))
                        throw new InvalidOperationException($"Parent cycle detected at block {current.ObjectId}.");
                    int parentId = current.ParentId;
                    if (!definitions.TryGetValue(parentId, out current))
                        throw new InvalidOperationException($"Block {definition.ObjectId} references missing parent {parentId}.");
                }
            }

            string fallbackName = Path.GetFileNameWithoutExtension(path);
            var rootObject = new GameObject(string.IsNullOrWhiteSpace(data.Id) ? fallbackName : data.Id);
            Undo.RegisterCreatedObjectUndo(rootObject, "Import RME custom object");
            RmeCustomObjectRoot root = rootObject.AddComponent<RmeCustomObjectRoot>();
            root.ObjectName = string.IsNullOrWhiteSpace(data.Id) ? fallbackName : data.Id;
            root.Description = data.Description ?? "Imported RME custom object";
            var objects = new Dictionary<int, GameObject>();

            try
            {
                foreach (ImportBlock definition in data.Blocks)
                {
                    var value = new GameObject(string.IsNullOrWhiteSpace(definition.Name)
                        ? $"Block {definition.ObjectId}" : definition.Name);
                    RmeObjectBlock block = value.AddComponent<RmeObjectBlock>();
                    Apply(block, definition);
                    value.transform.localPosition = definition.Position;
                    value.transform.localEulerAngles = definition.Rotation;
                    value.transform.localScale = definition.Scale;
                    objects.Add(definition.ObjectId, value);
                }

                foreach (ImportBlock definition in data.Blocks)
                {
                    Transform parent;
                    if (definition.ParentId == data.RootObjectId) parent = rootObject.transform;
                    else if (objects.TryGetValue(definition.ParentId, out GameObject parentObject)) parent = parentObject.transform;
                    else throw new InvalidOperationException($"Block {definition.ObjectId} references missing parent {definition.ParentId}.");
                    GameObject value = objects[definition.ObjectId];
                    value.transform.SetParent(parent, false);
                    value.transform.localPosition = definition.Position;
                    value.transform.localEulerAngles = definition.Rotation;
                    value.transform.localScale = definition.Scale;
                    RmePreviewFactory.Rebuild(value.GetComponent<RmeObjectBlock>());
                }
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                throw;
            }

            Selection.activeGameObject = rootObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorUtility.SetDirty(rootObject);
            return root;
        }

        private static void Apply(RmeObjectBlock block, ImportBlock definition)
        {
            ImportProperties properties = definition.Properties ?? new ImportProperties();
            block.ObjectId = definition.ObjectId;
            block.Kind = (RmeBlockKind)definition.BlockType;
            block.PrimitiveType = (PrimitiveType)properties.PrimitiveType;
            RmeBlockCompatibility.SetPrimitiveVisible(block,
                (properties.PrimitiveFlags & RmeBlockCompatibility.VisibleFlag) != 0);
            RmeBlockCompatibility.SetPrimitiveCollidable(block,
                (properties.PrimitiveFlags & RmeBlockCompatibility.CollidableFlag) != 0);
            if (!string.IsNullOrWhiteSpace(properties.Color))
            {
                string[] channels = properties.Color.Split(':');
                if (channels.Length == 4 &&
                    int.TryParse(channels[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int red) &&
                    int.TryParse(channels[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int green) &&
                    int.TryParse(channels[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int blue) &&
                    float.TryParse(channels[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float alpha) &&
                    !float.IsNaN(alpha) && !float.IsInfinity(alpha))
                    RmeBlockCompatibility.SetCustomRgb(block, red, green, blue, alpha);
                else if (ColorUtility.TryParseHtmlString(properties.Color, out Color color)) block.Color = color;
            }
            block.PrefabName = properties.PrefabName;
            RmeBlockCompatibility.SetCameraLabel(block,
                string.IsNullOrWhiteSpace(properties.CameraLabel) ? "CustomCamera" : properties.CameraLabel);
            block.IsOpen = properties.IsOpen;
            block.IsLocked = properties.IsLocked || properties.Locked;
            block.IsInteractable = properties.IsInteractable;
            block.ItemType = properties.ItemType;
            block.CustomItemName = properties.CustomItemName;
            block.Chance = properties.Chance;
            block.LockerType = properties.LockerType;
            block.RequiredPermissions = properties.RequiredPermissions != 0
                ? properties.RequiredPermissions : properties.KeycardPermissions;
            block.RequireAll = properties.RequireAll;
            block.LightIntensity = properties.Intensity;
            block.LightRange = properties.Range;
            block.LightType = (RmeLightType)properties.LightType;
            block.LightShape = (RmeLightShape)properties.Shape;
            block.LightShadows = (LightShadows)properties.ShadowType;
            block.LightShadowStrength = properties.ShadowStrength;
            block.SpotAngle = properties.SpotAngle;
            block.InnerSpotAngle = properties.InnerSpotAngle;
            block.Text = properties.Text;
            block.TextDisplaySize = properties.DisplaySize == Vector2.zero ? Vector2.one : properties.DisplaySize;
            block.NestedObjectName = string.IsNullOrWhiteSpace(properties.Prefab)
                ? properties.SchematicName : properties.Prefab;
        }

        [Serializable] private sealed class ImportObject
        {
            public string Id;
            public string Description;
            public int RootObjectId;
            public ImportBlock[] Blocks;
        }

        [Serializable] private sealed class ImportBlock
        {
            public string Name;
            public int ObjectId;
            public int ParentId;
            public Vector3 Position;
            public Vector3 Rotation;
            public Vector3 Scale = Vector3.one;
            public int BlockType;
            public ImportProperties Properties;
        }

        [Serializable] private sealed class ImportProperties
        {
            public int PrimitiveType;
            public string Color;
            public int PrimitiveFlags = 3;
            public string PrefabName;
            public string CameraLabel;
            public bool IsOpen;
            public bool IsLocked;
            public bool Locked;
            public bool IsInteractable = true;
            public int ItemType;
            public string CustomItemName;
            public float Chance = 100f;
            public int LockerType;
            public int RequiredPermissions;
            public int KeycardPermissions;
            public bool RequireAll = true;
            public float Intensity = 1f;
            public float Range = 10f;
            public int LightType = 2;
            public int ShadowType;
            public int Shape;
            public float ShadowStrength = 1f;
            public float SpotAngle = 30f;
            public float InnerSpotAngle = 21.80208f;
            public string Text;
            public Vector2 DisplaySize = Vector2.one;
            public string Prefab;
            public string SchematicName;
        }
    }
}
