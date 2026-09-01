# RME-CustomObjects agent guide

RME-CustomObjects is a Unity 2021.3.17f1 authoring kit for reusable Realm Map Editor objects and compatible JSON exports. C# editor/runtime tooling previews, imports, edits, and exports ProjectMER-style blocks; Python utilities validate project metadata and object files.

## Map and conventions

- `Assets/RME-CustomObjects/Editor/`: builder window, inspectors, previews, JSON import/export.
- `Assets/RME-CustomObjects/Runtime/`: serialized component/schema types shared by editor code.
- `schema/`: JSON schema; `catalog/`: supported SCP:SL prefab names; `examples/`: sample exports.
- `tools/validate.py`: project/object validation; `tools/new_prefab.py`: generic-prefab JSON helper.
- `Packages/manifest.json` and `ProjectSettings/ProjectVersion.txt`: Unity dependencies/version.
- `Assets/RME-CustomObjects/Prefabs/RRP/` and imported reference assets: large Unity descriptors/assets; inspect only for prefab/asset tasks.

Preserve every Unity `.meta` file and pinned GUID. Keep Runtime and Editor schemas synchronized; add serialized fields compatibly and use `FormerlySerializedAs` for unavoidable renames. Exported JSON and catalog/schema formats are contracts with sibling `RealmPlugin`, which performs server-side placement. There is no database or deployable server code here.

## Validation and runtime

- Project metadata/schema: `python3 tools/validate.py --project`
- One object: `python3 tools/validate.py path/to/object.json`
- Finish with: `git diff --check`

Unity editor/compiler behavior requires opening the project in Unity 2021.3.17f1 and refreshing/recompiling; Python validation alone does not prove inspector, preview, import, or export behavior.

## Token discipline

Read this file first. Use targeted `rg` in the relevant Editor/Runtime/tool/schema path and inspect only direct dependencies; never inventory `Assets/` or explore sibling repositories unless changing an export/runtime contract. Do not reread unchanged files. Prefer existing compatibility/export helpers, preserve behavior unless explicitly changing it, and avoid unrelated abstractions, refactors, formatting, cleanup, or planning/documentation files. Normally skip `Library/`, `Temp/`, `Logs/`, `Obj/`, `Build*/`, `UserSettings/`, `MemoryCaptures/`, generated IDE projects, binaries, package caches/lockfiles, and unrelated imported assets. Run the smallest validator before Unity-wide checks. Keep the handoff to changes, validation, and required follow-up; do not restate the task or explain obvious code.
