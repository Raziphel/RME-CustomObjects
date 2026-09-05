# RME-CustomObjects

Unity 2021.3.17f1 authoring kit for reusable Realm Map Editor objects and JSON exports. Editor tooling is in `Assets/RME-CustomObjects/Editor/`; serialized types in `Runtime/`; formats in `schema/` and `catalog/`; validators in `tools/`. Preserve `.meta` files/GUIDs and compatibility (`FormerlySerializedAs` for unavoidable renames).

| Request | Start here |
|---|---|
| Editor/import/export | `Editor/RmeCustomObjectBuilder.cs`, `RmeJsonImporter.cs`, `RmeJsonExporter.cs` |
| Block/runtime schema | `Runtime/RmeObjectBlock.cs`, `RmeCustomObjectRoot.cs` |
| Preview/inspector | `Editor/RmePreviewFactory.cs`, `RmeObjectBlockEditor.cs` |
| JSON/prefab validity | `schema/`, `catalog/`, `tools/validate.py` |

Exports are consumed by `../RealmPlugin/`; keep Runtime, Editor, schema, and catalog compatible. Use `flowseeker-rme` before targeted `rg`; never inventory `Assets/`, and skip `Library/`, `Temp/`, `Logs/`, builds, generated IDE projects, caches, and imported assets unless necessary. Run `python3 tools/validate.py --project` or the relevant object validator, then `git diff --check`. Unity editor behavior still needs Unity refresh/recompile.
