#!/usr/bin/env python3
import json, math, pathlib, sys

PROJECT_ROOT = pathlib.Path(__file__).resolve().parents[1]
EXPECTED_SCRIPT_GUIDS = {
    "Assets/RME-CustomObjects/Editor/RmeCustomObjectBuilder.cs": "2fc47c2687764cb0bb40d38c29b7af26",
    "Assets/RME-CustomObjects/Editor/RmeBlockCompatibility.cs": "02c36d27ccf74066afd4790c50c4a129",
    "Assets/RME-CustomObjects/Editor/RmeJsonExporter.cs": "5e56c96a4f0847b6988eaee725ea3297",
    "Assets/RME-CustomObjects/Editor/RmeJsonImporter.cs": "b7a332db6af5445ba7f37e39958b733b",
    "Assets/RME-CustomObjects/Editor/RmeObjectBlockEditor.cs": "709756e344844ea085b9bb82a7588b7e",
    "Assets/RME-CustomObjects/Editor/RmePreviewFactory.cs": "d5652e886772462d8783db39b66dc89e",
    "Assets/RME-CustomObjects/Runtime/RmeCustomObjectRoot.cs": "a68b9d0c693147f2b9608f21bc8b4ac1",
    "Assets/RME-CustomObjects/Runtime/RmeObjectBlock.cs": "4c1d2e9ab8f34d65a028bf0fe7e55c92",
}

def validate_script_metadata():
    scripts = PROJECT_ROOT / "Assets" / "RME-CustomObjects"
    guids = {}
    for script in scripts.rglob("*.cs"):
        meta = pathlib.Path(str(script) + ".meta")
        if not meta.is_file(): fail(f"Unity metadata is missing for {script.relative_to(PROJECT_ROOT)}")
        guid_line = next((line for line in meta.read_text(encoding="utf-8").splitlines()
                          if line.startswith("guid: ")), None)
        if not guid_line: fail(f"Unity metadata has no GUID for {script.relative_to(PROJECT_ROOT)}")
        guid = guid_line.removeprefix("guid: ").strip()
        if guid in guids: fail(f"duplicate Unity script GUID in {script.relative_to(PROJECT_ROOT)} and {guids[guid]}")
        relative = script.relative_to(PROJECT_ROOT)
        expected = EXPECTED_SCRIPT_GUIDS.get(relative.as_posix())
        if expected is None: fail(f"Unity script identity is not pinned for {relative}")
        if guid != expected: fail(f"Unity script GUID changed for {relative}: expected {expected}, found {guid}")
        guids[guid] = relative
    missing = set(EXPECTED_SCRIPT_GUIDS) - {path.as_posix() for path in guids.values()}
    if missing: fail(f"pinned Unity scripts are missing: {', '.join(sorted(missing))}")
    block_source = (scripts / "Runtime" / "RmeObjectBlock.cs").read_text(encoding="utf-8-sig")
    for required in ("EditorSchemaVersion = 4", "[SelectionBase]", "PrimitiveVisible", "PrimitiveCollidable", "UseCustomRgb", "CustomRed", "CustomGreen", "CustomBlue", "CustomAlpha", "CameraLabel", "AnimatorName", "MotionOffset", "MotionRotation", "MotionDuration"):
        if required not in block_source:
            fail(f"RmeObjectBlock runtime/editor schema mismatch: missing {required}")
    compatibility_source = (scripts / "Editor" / "RmeBlockCompatibility.cs").read_text(encoding="utf-8-sig")
    for required in ("CollidableFlag = 1", "VisibleFlag = 2"):
        if required not in compatibility_source:
            fail(f"primitive flag encoding does not match SCP:SL: missing {required}")
    invalid_hdrp_drawer = "UnityEditor.Rendering.HighDefinition"
    for shader in (PROJECT_ROOT / "Assets").rglob("*.shader"):
        if invalid_hdrp_drawer in shader.read_text(encoding="utf-8-sig", errors="ignore"):
            fail(f"HDRP-only material drawer remains in {shader.relative_to(PROJECT_ROOT)}")

def fail(message):
    raise ValueError(message)

def vector(value, label):
    values = list(value.values()) if isinstance(value, dict) else value
    if not isinstance(values, list) or len(values) != 3 or not all(isinstance(x, (int, float)) and math.isfinite(x) for x in values):
        fail(f"{label} must contain three finite numbers")

def validate(path):
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    root, blocks = data.get("RootObjectId"), data.get("Blocks")
    if not isinstance(root, int) or not isinstance(blocks, list): fail("RootObjectId and Blocks are required")
    if len(blocks) > 50000: fail("more than 50,000 blocks")
    ids, parents = set(), {}
    for index, block in enumerate(blocks):
        if not isinstance(block, dict): fail(f"block {index} is not an object")
        object_id, parent_id = block.get("ObjectId"), block.get("ParentId")
        if not isinstance(object_id, int) or object_id in ids: fail(f"block {index} has a missing or duplicate ObjectId")
        ids.add(object_id); parents[object_id] = parent_id
        if block.get("BlockType") not in range(13) or block.get("BlockType") == 6: fail(f"block {object_id} has unsupported inline BlockType; teleports belong in the sidecar")
        for key in ("Position", "Rotation", "Scale"): vector(block.get(key), f"block {object_id} {key}")
        animator = block.get("AnimatorName")
        if animator is not None and (not isinstance(animator, str) or len(animator) > 100 or not animator or ".." in animator or "/" in animator or "\\" in animator):
            fail(f"block {object_id} has an unsafe AnimatorName")
        has_motion = "MotionDuration" in block
        if has_motion:
            if block.get("BlockType") != 1: fail(f"block {object_id} procedural motion is only supported for primitives")
            if animator: fail(f"block {object_id} cannot combine AnimatorName with procedural motion")
            duration = block.get("MotionDuration")
            if not isinstance(duration, (int, float)) or not math.isfinite(duration) or duration <= 0: fail(f"block {object_id} MotionDuration must be a positive finite number")
            for key in ("MotionOffset", "MotionRotation"): vector(block.get(key), f"block {object_id} {key}")
            if block.get("MotionLoopMode", 2) not in (1, 2): fail(f"block {object_id} has unsupported MotionLoopMode")
        if block.get("BlockType") == 12 and not (block.get("Properties") or {}).get("PrefabName"): fail(f"block {object_id} needs Properties.PrefabName")
    for object_id, parent_id in parents.items():
        if object_id != root and parent_id != root and parent_id not in ids: fail(f"block {object_id} has missing parent {parent_id}")
        seen, current = set(), object_id
        while current != root and current in parents:
            if current in seen: fail(f"parent cycle at block {current}")
            seen.add(current); current = parents[current]
    print(f"OK: {path} ({len(blocks)} blocks)")

if __name__ == "__main__":
    if len(sys.argv) == 2 and sys.argv[1] == "--project":
        try:
            validate_script_metadata()
            print("OK: Unity script metadata is stable")
        except Exception as error:
            print(f"ERROR: {error}", file=sys.stderr); sys.exit(1)
        sys.exit(0)
    if len(sys.argv) != 2: print("usage: validate.py OBJECT.json | --project", file=sys.stderr); sys.exit(2)
    try:
        validate_script_metadata()
        validate(pathlib.Path(sys.argv[1]))
    except Exception as error: print(f"ERROR: {error}", file=sys.stderr); sys.exit(1)
