# Ashvault technical audit — 23 November 2026

## Scope and authority

Audited the connected repository `SquishieInc/Ashvault` (main branch)
with source inspection and source-data checks only. This audit does **not**
claim Unity Editor compilation, device testing, or runtime rendering success.

## Confirmed project structure

- **Authoritative Unity project:** repository root (`Assets/`, `Packages/`, `ProjectSettings/`).
- **Saved game scene:** `Assets/Ashvault/Scenes/Prototype.unity`.
- `ProjectSettings/EditorBuildSettings.asset` enables that scene, and its
  GUID links match `Prototype.unity.meta` and `AshGame.cs.meta`.
- `ProjectSettings/ProjectVersion.txt` records **6000.6.7f1**.
  Upgrade to a newer patch only after creating a backup/commit and testing.
- `Packages/manifest.json` declares **URP 17.6.0** and **Input System 1.20.1**.
- Project Settings records `activeInputHandler: 2` (Both).
- The root `.gitignore` excludes generated Unity files.

## Repository duplication — cleanup deferred

The repository also contains `Ashvault/Assets/Ashvault/`, a shadow copy of
the source under a nested project archive. The root `Assets/Ashvault`
is the directory Unity compiles. A SHA comparison found 133 identical
paired assets and one differing active C# source file:
`Assets/Ashvault/Scripts/SectorMachinery.cs`.

The root script contains the `GetEntityId()` compatibility fix.
The nested copy may contain the obsolete `GetInstanceID()` call and is
**not a safe source for copying back into the active Unity project**.

**Do not remove the nested archive yet.** Its `Tools/` scripts and Docs may
still depend on the nested paths. Refactor those references, preserve the
important design references, and remove the duplicate in a separate PR.

## Source-data check

A Python-stdlib-only check is included in
`Tools/check_unity_project.py` and executed on push/PR using
`.github/workflows/ashvault-source-checks.yml`.

It checks:
1. Manifest packages, project version and saved-scene GUID relationships.
2. All nine JSON map dimensions and required marker uniqueness.
3. Enemy spawn kinds/locations, class-drop locations.
4. Eight class route simulations across nine maps, collecting all three keys
   before reaching the exit (72 total simulations).
5. Obsolete `GetInstanceID()` use in the authoritative runtime scripts.
6. A warning if the nested source archive still exists.

The path check is **topological**. It does not simulate real movement,
polygon walls, dynamic doors, platforms, geometry, collisions, boss AI, or
Unity rendering. A passing GitHub check is not equivalent to a Unity test.

## Code fix on this branch

`EnemyAI.ApplyAttack` previously checked a melee range value captured
at attack windup, then checked only whether the player was still visible.
A retreating player could therefore be hit outside melee range.
The claw strike now uses the distance measured **at the impact moment**.

## Remaining items to verify in Unity

### P0 — blocking for first playable build
- Open `Assets/Ashvault/Scenes/Prototype.unity` in Unity **6000.6.7f1** or a
  deliberately upgraded version, and resolve any Console compile errors.
- Press Play, select a class and confirm the world appears without missing
  materials, exceptions, black render texture or input lock issues.
- Test the root `Assets/Ashvault/Scripts/SectorMachinery.cs` rather than the
  old copy in `Ashvault/Assets/Ashvault/Scripts`.
- Verify that world geometry colliders, keys and doors allow finishing map 1.

### P1 — gameplay correctness
- Test a melee enemy, then retreat during its windup to verify the new distance check.
- Kill enemies, open all three seals, reveal secrets and complete the level.
- Validate enemies finding paths around doors, pits and polygon secret walls.
- Ride lifts/moving platforms, trigger crushers/rune hazards, and check
  CharacterController behaviour.
- Visit the boss map; verify the boss cannot be skipped and its death
  allows the exit.

### P2 — graphics and platforms
- Confirm URP point-light flicker, lit sprites, shadows and low-resolution
  fullscreen blit actually render on macOS/Windows.
- Test editor and build framerate; measure lighting, collision and texture costs.
- Replace provisional IMGUI mobile controls and test device safe areas and touch.
- Before release, update `PlayerSettings.companyName` (currently
  `DefaultCompany`) and platform identifiers.

## Testing instructions

Locally, from repository root:

```sh
python3 Tools/check_unity_project.py
```

Then in Unity Editor:

1. Open the `Prototype` scene; clear Console; verify no C# errors.
2. Select **Tools → Ashvault → Validate V4 Art, Sectors and Audio**.
3. Enter Play mode and test a complete Episode I / Map 1 run.
4. Save and commit Unity-generated .meta/scene changes.
5. Record actual Editor errors separately; do not claim readiness from static checks alone.

## Change control

This audit and fixes live on the `audit/unity-66-stabilization` branch.
Review the pull request before merging; `main` remains unchanged until then.
