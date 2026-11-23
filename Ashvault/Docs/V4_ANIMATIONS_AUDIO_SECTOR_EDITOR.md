# Ashvault v4 — Directional art, audio, doors, dynamic environment, decals, polygon-sector editor

## Status and scope

This is an **integration prototype**, not a tested Unity build. The original v3 nine-map campaign, class logic, keys, enemy AI, sector machinery and level progression have been retained. No Unity Editor, Player build, Xcode or supported GPU renderer was available during assembly; the repository contents must be compiled and exercised in Unity.

## Importing

1. Create a **Universal 3D (URP)** project in the Unity 6.6 Editor, as described in `README.md`.
2. Copy `Assets/Ashvault/` from the full v4 archive into the project's `Assets/` folder, **replacing** the earlier version; do not merge the two ZIPs on top of one another without replacement.
3. Ensure the Input System package is installed and Active Input Handling is `Input System` or `Both`.
4. Run **Tools → Ashvault → Validate All Nine Campaign Maps**, **Validate Visuals, Lighting and Machinery**, then **Validate V4 Art, Sectors and Audio**.
5. Run **Tools → Ashvault → Create or Rebuild Prototype Scene** and press Play.
6. In Episode I, choose a class, approach the three seal doors, locate the extra lever switches and inspect the optional raised dais and pit sectors. Walk near enemies to observe their directional, attack, hurt and death states.

## Skeleton Reaver art

The new `Textures/Directional/enemy_skeleton_directions.png` is **768×1584**, divided into 8 directions by 11 pose rows at 96×144 pixels/cell. Eight genuinely different view angles originate from the new original source turnaround art. Attack, hurt, walking and collapse frames are **programmatically derived** from the posed artwork, rather than individually illustrated frame-by-frame. The revised animation has a timed windup / strike / recovery, stagger and four-stage death playback.

Direction columns: `front`, `front-right`, `right`, `rear-right`, `rear`, `rear-left`, `left`, `front-left`.

Rows **from top to bottom**: `idle`, `walk A`, `walk B`, `windup`, `strike`, `recover`, `hurt`, `death start`, `fall`, `collapse`, `corpse`.

The other fourteen enemy archetypes retain v3 **8×6 placeholder atlases**; the runtime supports both. The poster-style concept illustration is not included in runtime game art. Preview the usable sprite poses in `Docs/skeleton_reaver_animation_preview.png`.

### Timing

- Normal strikes: start at time `t`, apply damage/projectile at `t+0.20 s` when still alive. Boss impact occurs at `t+0.34 s`.
- Visual windup: 0–0.11 s; strike: 0.11–0.24 s; recovery: 0.24–0.38 s.
- Hurt animation: 0.24 s, interrupted by death.
- Death: phases at 0.14, 0.36 and 0.69 s, then corpse hold until the actor is cleaned up.
- Further sprite production should hand-draw attack/death frames individually and add unique movement phases per archetype. The present system is an animation *framework + Reaver showcase*.

## Original audio

Twenty self-generated mono WAV sound effects (`22.05 kHz / 16-bit`) are included under `Resources/Audio/`. These are synthesised sound effects, not ripped game audio, commercial sound packs or third-party recordings. `AshAudio.OneShot()` spatializes enemy attacks, hurt/death, player strikes/hurt, door opening, lever use, trap damage, projectile impacts and hidden wall discovery. Each episode also starts its own low-level looping ambient drone. Unity's normal AudioClip importer handles the WAV files; no optional audio package is required.

**Sound design caveat:** Procedurally synthesised effects are *functional placeholders*, not professionally recorded/mixed production SFX. Balance spatial rolloff, sample loudness, compression, accessibility and parental hearing volume in physical playtesting. Add audio mixer groups before release.

## Animated doors / switches

The three sigil gates keep their lock conditions. Bronze rises, Azure splits into two visual leaves (with a full-cell collider until fully opened), and Crimson retracts sideways; class seals and the final gate rise. The gate opens over 1.05 s and only then calls `DungeonMap.Unseal`, so AI pathing and collision don't prematurely expose a gap. All nine maps contain a switch adjacent to each of their three coloured key gates (27 total). Switches can open the linked gate but **cannot bypass the sigil or class requirement**. The levers physically tilt and change indicator colour. Sound accompanies lock rejection, door motion and lever click.

## Animated environments

Four prop animation strips (`Animated/*_frames.png`, 4×128×128) cover the brazier, mushrooms, crystal and hanging star-lantern. Existing flicker lights continue to illuminate world geometry. Each map adds three low-cost scrolling floor patches (27 total) using `env_lava`, `env_mire`, or `env_aether` materials. They are **visual overlays only**; rune traps and crushers remain the authoritative damage sources. The floating water/lava meshes are opaque in this prototype; translucent refraction, foam and post effects are not implemented.

## Decals

`ImpactDecal` places short-lived, surface-aligned alpha blended quads for `blood`, `scorch`, `crack` and `rune_impact`, all original transparent PNGs. It caps visible decals at 64 for desktop and 20 for mobile, then expires them after 28/12 seconds. It deliberately does **not** require URP's `Decal Renderer Feature` / `Decal Projector`, which would need explicit configuration in each project's renderer asset. These are surface-aligned quads with depth offset: they do not bend around geometry or wrap continuously over corners, and blending/lighting must be tested under the selected renderer. Mobile impact counts, overdraw and GPU memory need profiling.

## Polygon-sector editing — Unity Editor menu

Open **Tools → Ashvault → Open Polygon Sector Editor**. Select any episode/map at the top. The window loads, displays and saves that map's existing JSON resource rather than creating an isolated demo asset.

- **Polygon sector:** create a shape and left-click on the map to add 3+ points; drag an existing point to move it, right-click a point to delete it. Change `kind` between `Platform`, `Pit` and `Solid`; edit floor and ceiling heights. Polygons may be convex or simple concave outlines, with diagonal edges.
- **Wall / secret:** click once for a start point and again for an end point; set wall thickness, lower/upper height and `Secret`. A secret wall slides down when used, updates `DungeonMap` wall-passability and counts as a secret.
- **Stairs:** click to create staircase origin, then edit direction, width, tread depth, rise and number of steps. Geometry is generated from the staircase data.
- **Save:** run polygon validity checks and write all data to `Resources/Maps/E??_L??.json`, then `AssetDatabase.ImportAsset` refreshes the asset.

Map data fields: `polygonSectors`, `wallSegments`, `stairs`, `switches`, `animatedSurfaces`. See JSON for complete examples. The nine shipped maps each include **two polygons (one concave raised dais, one shallow pit), one 3-step staircase, and one extra secret line segment**.

### Current geometry limits

The existing campaign still uses legacy tile walls for most corridors. Polygon floors are ear-clipped and rendered as MeshFilter + MeshCollider geometry, arbitrary wall segments become angled collidable wall strips, and the grid-based AI uses polygon floor heights plus line-segment crossing checks. Pit openings in legacy tile floors are **approximated to cell-size cutouts**, not boolean-subtracted at arbitrary polygon boundaries, and are only 0.38 m deep in the sample maps. These are NOT full Doom sectors with linedefs, visplane portals, 3D floors, sloped ramps, BSP visibility, auto floor welding, terrain clipper or dynamic NavMesh rebaking.

Enemy navigation remains grid-based, so arbitrary polygon geometry is a **hybrid editing layer**. Designers should use the Editor validator, manually inspect collisions and confirm enemy pursuit for every new layout; a future navigation mesh or polygon navgraph should replace the coarse overlay for unrestricted geometry.

## Testing checklist

- [ ] Import into Unity 6.6 Universal 3D without compile errors (run all three Editor validators).
- [ ] Test Reaver all 8 angles while walking around it, observing sprite pivot/scale.
- [ ] Confirm windup and impact timing matches hit detection and plays the appropriate sound.
- [ ] Confirm death ends in a grounded corpse before cleanup; no enemies attack after death.
- [ ] Verify levers work with and without sigils; door colliders are impassable until opened.
- [ ] Test Azure two-leaf visual seal, Bronze vertical lift and Crimson lateral slide.
- [ ] Enter and leave sample pits without becoming trapped; ensure stairs don't obstruct progression.
- [ ] Find four secrets (3 original + 1 polygon segment) on each map and verify completion stats.
- [ ] Ensure enemies see barriers and update their route when secrets/gates open.
- [ ] Check all three animated surface types, animated prop lights and shadow behaviour.
- [ ] Hit stone and enemies with projectiles; verify decal orientation, alpha and expiration.
- [ ] Test Windows/macOS high quality and iPhone/iPad low quality separately; reduce live lights/shadows and decals if necessary.
- [ ] Run memory/CPU/GPU profiling; source remains a prototype, not a performance-certified iOS or Steam build.

## Offline QA

```bash
python3 validate_campaign.py
python3 validate_visual_expansion.py
python3 validate_v4.py
python3 Tools/validate_machinery_routes.py
```

`validate_v4.py` adds checks for all new authored fields, 72 route combinations (with game gate symbols eligible for opening), actual polygon validity and separation, twenty sound files, four animated prop strips and four decals.

Run `Tools/author_v4_campaign.py` only when intentionally **regenerating** all v4 map attachments: it will overwrite manual v4 polygon edits. `Tools/build_v4_assets.py` is a reproducible local asset generator but references the original source image path used during production; its generated files are already shipped in the ZIP, so it is **not needed** to import/play the game.
