# RME Custom Objects

A standalone authoring kit for reusable Realm Map Editor objects. It uses the familiar ProjectMER/SL-CustomObjects block format, while adding strict validation and generic SCP:SL network-prefab placement.

## Install and use

1. Copy a completed `<name>.json` (plus optional `<name>-Rigidbodies.json`, `<name>-Teleports.json`, and animator bundles) into the server's RME `CustomObjects/<name>/` directory.
2. In an RME map, add a custom-object reference whose `Prefab` is `<name>`.
3. Run `python3 tools/validate.py path/to/<name>.json` before handing the object to a server owner.
4. Use `rme prefabs` in-game to obtain exact names for block type `12`.

The included `catalog/prefabs.json` lists the 26 SCP:SL prefab names recovered from the RRP Unity reference set. Generate a correctly formed generic-prefab block with:

```bash
python3 tools/new_prefab.py "GeneratorStructure" --id 12 --parent 0
```

The supplied `.prefab` files are Unity YAML descriptors rather than portable models. They reference external SCP:SL scripts, meshes, materials, and animations, so importing those files into an unrelated Unity project produces missing-script/missing-asset objects. Use them only inside the matching SCP:SL Unity reference project. RME itself uses the exact catalog name to spawn the server's registered network prefab and does not load Unity `.prefab` files.

Existing ProjectMER block types remain compatible. RME extensions are generic prefab (`12`), text (`8`), interactable (`9`), waypoint (`10`), and typed door (`11`). Teleports use the established `<name>-Teleports.json` sidecar.

## Block types

| ID | Kind | Important properties |
|---:|---|---|
| 0 | Empty hierarchy node | none |
| 1 | Primitive | `PrimitiveType`, `Color`, `PrimitiveFlags` |
| 2 | Light | `Color`, `Intensity`, `Range`, `LightType`, `ShadowType`, `Shape`, angles |
| 3 | Pickup | `ItemType`, `Chance`, `Locked` |
| 4 | Workstation | `IsInteractable` |
| 5 | Nested custom object | `Prefab` or `SchematicName` |
| 7 | Locker | `LockerType`, `Chance`, `OpenedChambers`, `KeycardPermissions` |
| 8 | Text | `Text`, `DisplaySize` |
| 9 | Interactable | `Shape`, `InteractionDuration`, `IsLocked` |
| 10 | Waypoint | scale is its bounds |
| 11 | Door | `DoorType`, `IsOpen`, `IsLocked`, permissions |
| 12 | Any registered network prefab | `PrefabName`, plus door properties when applicable |

Positions, Euler rotations, scales, parent IDs, rigidbodies, and animator names are serialized unchanged. The root ID is a virtual map-placement anchor and must not also be used as a visible block.

See `examples/supply-locker.json`. The schema is in `schema/rme-custom-object.schema.json`.
