#!/usr/bin/env python3
import argparse, json, pathlib, sys

ROOT = pathlib.Path(__file__).resolve().parents[1]

parser = argparse.ArgumentParser(description="Create an RME block for a known SCP:SL prefab")
parser.add_argument("name")
parser.add_argument("--id", type=int, required=True)
parser.add_argument("--parent", type=int, default=0)
args = parser.parse_args()

catalog = json.loads((ROOT / "catalog" / "prefabs.json").read_text())
entry = next((item for item in catalog if item["name"].casefold() == args.name.casefold()), None)
if entry is None:
    print("Unknown catalog name. Confirm it with the server's 'rme prefabs' command.", file=sys.stderr)
    sys.exit(1)

block_type = entry["recommendedBlockType"]
properties = {}
if block_type == 7:
    properties = {"LockerType": entry["lockerType"], "Chance": 100, "OpenedChambers": 0, "KeycardPermissions": 0}
elif block_type == 4:
    properties = {"IsInteractable": True}
else:
    properties = {"PrefabName": entry["name"]}

print(json.dumps({
    "Name": entry["name"], "ObjectId": args.id, "ParentId": args.parent,
    "Position": {"x": 0, "y": 0, "z": 0}, "Rotation": {"x": 0, "y": 0, "z": 0},
    "Scale": {"x": 1, "y": 1, "z": 1}, "BlockType": block_type, "Properties": properties
}, indent=2))
