# RME Custom Objects

A standalone authoring kit for reusable Realm Map Editor objects. It uses the familiar ProjectMER/SL-CustomObjects block format, while adding strict validation and generic SCP:SL network-prefab placement.

## Open in Unity

This repository is also a Unity project. Install **Unity 2021.3.17f1** in Unity Hub, choose **Add project from disk**, and select this `RME-CustomObjects` folder—the folder containing `Assets`, `Packages`, and `ProjectSettings`. Do not select the `Assets` folder itself.

If Unity Hub displays “Editor version not installed,” use its **Install Editor** button for `2021.3.17f1`. No platform build-support module is required just to author/export objects. Unity creates `Library`, `Logs`, and other ignored local folders during the first import.

After scripts compile, the **RME Builder** window opens automatically and creates a `NewCustomObject` root. Reopen it any time from **RME Custom Objects → Open Builder**. Select the root or one of its children, add primitives or SCP:SL prefabs, arrange and nest them normally in the Scene, edit block properties in the Inspector, and click **Export Selected Custom Object**.

Use **Import JSON for Editing** in the builder—or **RME Custom Objects → Import JSON for Editing**—to reconstruct an exported custom-object JSON as a new editable Unity hierarchy. The importer restores parent relationships, local transforms, block types, supported properties, primitive flags, text, lights, custom pickups, and prefab previews. Sidecar rigidbody, teleport, and animator files remain separate and are not imported.

Always update the complete `Assets/RME-CustomObjects` folder, including both `Runtime` and `Editor`. Copying only the Editor folder causes compiler errors because the importer and preview tools share the versioned `RmeObjectBlock` runtime schema. If Unity was open during an external update, use **Assets → Refresh** after all files finish copying.

If Unity still reports missing `UnityEditor.Rendering.HighDefinition` culling enums after an update, run **RME Custom Objects → Refresh Imported Material Shaders**. The included compatibility shader uses built-in culling values; the command forces Unity to discard an older cached HDRP material drawer.

The project tracks and validation-pins a permanent Unity `.meta` GUID for every RME script. Keep those files beside their matching scripts when copying or updating the project; replacing scripts without their metadata breaks the GUID references stored in existing scenes. Run `python3 tools/validate.py --project` to verify the exact identities before distributing an update. Add fields without renaming/removing existing serialized fields; use Unity's `FormerlySerializedAs` attribute if a rename is unavoidable.

Doors have one workflow: search `door` in the SCP:SL prefab browser and select the exact visual you want. Do not add an Interaction Trigger for a door—the real network door supplies its own interaction. Interaction Trigger is only for invisible custom click/hold volumes.

Lights have dedicated one-click presets for Point, Spot Cone, Spot Pyramid, Spot Box, Directional, Rectangle/Area, Disc, and Tube. Each light uses Unity's native `Light` component for a MER-style scene preview. The Inspector exposes type, shape, color, intensity, range, spot angles, shadow mode, and shadow strength; those same values are exported to RME.

Primitive blocks expose independent **Visible** and **Collidable** toggles using MER's `PrimitiveFlags` values. This permits invisible barriers, visible decoration without collision, both behaviours, or neither. Enable **Custom RGB** to enter raw MER red, green, and blue channels outside the normal 0-255 range, including negative values; alpha remains a floating-point value normally between 0 and 1. These colors export and import as MER's `R:G:B:A` format, while standard colors continue to use hexadecimal. Primitive color alpha is rendered transparently in the Scene view; primitives with Visible disabled or zero alpha remain editable through a cyan wireframe gizmo. Internal scene previews are hidden from the hierarchy and are never exported as blocks. Camera prefabs whose names end in `CameraToy` also expose an **SCP-079 Camera Name** field that is applied to the network camera in-game.

Visible preview geometry is selectable across its complete surface and resolves to the owning RME block. Invisible primitives remain selectable through their cyan wireframe rather than only through the transform pivot.

SCP:SL encodes `Collidable` as bit 1 and `Visible` as bit 2. Exported values therefore use `0` for neither, `1` for collision only, `2` for visibility only, and `3` for both.

Prefab blocks use editor-safe visual proxies because SCP:SL's original meshes and scripts are not redistributable as a complete Unity package. The exported block stores the exact network-prefab name; RealmPlugin replaces that proxy with the genuine interactive server prefab when the RME map loads.

The `Assets/RME-CustomObjects/Prefabs/RRP` folder contains all 26 prefab descriptors from the supplied `RRP.zip`. The builder searches and instantiates those real assets first. If Unity cannot resolve a prefab's external game mesh/material GUIDs, it automatically shows a clearly shaped fallback preview instead of creating an invisible or broken scene object. Either preview exports the same exact interactive server-prefab name.

The required visual dependencies from `SCPSL 14.1 - Rooms.unitypackage` are included selectively under `Assets/14.1` and `Assets/Room Reference`: 236 of the RRP files' 322 direct GUID references plus 153 transitive mesh/material/texture/shader dependencies. The unrelated multi-gigabyte room library is intentionally excluded. Most remaining references are game-only scripts, audio, controllers, built-in resources, and some secondary meshes that were not present in the supplied package. The builder therefore checks whether usable geometry imported and keeps its safe-preview fallback. RealmPlugin supplies the real behavior in-game.

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
| 3 | Pickup | `ItemType`, optional `CustomItemName`, `Chance`, `Locked` |
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
