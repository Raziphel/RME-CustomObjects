#!/usr/bin/env python3
import json, math, pathlib, sys

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
        if block.get("BlockType") == 12 and not (block.get("Properties") or {}).get("PrefabName"): fail(f"block {object_id} needs Properties.PrefabName")
    for object_id, parent_id in parents.items():
        if object_id != root and parent_id != root and parent_id not in ids: fail(f"block {object_id} has missing parent {parent_id}")
        seen, current = set(), object_id
        while current != root and current in parents:
            if current in seen: fail(f"parent cycle at block {current}")
            seen.add(current); current = parents[current]
    print(f"OK: {path} ({len(blocks)} blocks)")

if __name__ == "__main__":
    if len(sys.argv) != 2: print("usage: validate.py OBJECT.json", file=sys.stderr); sys.exit(2)
    try: validate(pathlib.Path(sys.argv[1]))
    except Exception as error: print(f"ERROR: {error}", file=sys.stderr); sys.exit(1)
