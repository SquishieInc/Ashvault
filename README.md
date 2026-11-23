# Ashvault

Original class-based fantasy retro FPS prototype for Windows, macOS, Steam and iOS.
**Development status: Unity integration and gameplay validation in progress.**

## Open the actual Unity project

**The Unity project lives in the root of this repository.**
Open the folder containing `Assets/`, `Packages/`, and
`ProjectSettings/` using Unity Hub.

The checked-in project records **Unity 6000.6.7f1**. Install that version
through Unity Hub when possible; use a newer Unity 6.6 patch only after
committing your work and checking for API/serialization changes.

1. In GitHub Desktop, clone `SquishieInc/Ashvault`.
2. In Unity Hub, **Add → Add project from disk** and select the cloned
   repository root (not the nested `Ashvault/` directory).
3. Open `Assets/Ashvault/Scenes/Prototype.unity`.
4. Open the Console and resolve any errors before pressing Play.
5. Use **Tools → Ashvault → Validate V4 Art, Sectors and Audio** for
   in-Editor asset/map checks, then test the first level in Play mode.

The saved prototype scene is already included and registered in
`ProjectSettings/EditorBuildSettings.asset`. Rebuilding the scene via
**Tools → Ashvault → Create or Rebuild Prototype Scene** is optional and
replaces that scene; only use it after committing any scene edits.

## Check the campaign without launching Unity

Requires only Python 3:

```bash
python3 Tools/check_unity_project.py
```

The GitHub Actions workflow also runs these checks on pushes and PRs.
It checks project structure, 9 maps, keys, 72 class progression routes,
enemy spawn cells, and obsolete `GetInstanceID()` use.

**Important:** Static checks cannot compile C#, render URP materials,
simulate moving-sector collisions, or prove gameplay works on a device.

## Source of truth

Use `Assets/Ashvault/` for all new code, art, levels and changes.

The separate `Ashvault/Assets/Ashvault/` tree is a redundant archive
from a previous delivery. It includes outdated copies (including the
pre-`GetEntityId()` fix). It is **not** compiled by the root Unity
project. Do not copy files from it over the active source.

A separate cleanup PR should preserve any useful documentation/tools
before the nested archive is removed.

## Contributions and GitHub workflow

- Pull the latest `main` before starting work.
- Use short-lived feature branches for fixes and additions.
- Commit Unity `.meta` files alongside asset changes.
- Do **not** commit `Library/`, `Temp/` or builds.
- Verify Editor compilation and Play mode before merging gameplay PRs.

See `TECHNICAL_AUDIT_2026-11-23.md` for the latest audit findings and
first-playable test checklist.

All code and assets should be kept original or appropriately licensed
for future commercial release; no advertisements or IAP are planned.
