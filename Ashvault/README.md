# ASHVAULT — Class-Based Fantasy Retro FPS (v4 polygon sectors / combat & audio)

**Original premium single-player game prototype by SquishieInc.** Classic 1990s fast-paced corridor-shooter design patterns, reinterpreted with a fantasy setting. **Not a Doom/Heretic derivative asset pack:** all included textures, sprites, names and source code are authored for this prototype.

> **IMPORTANT STATUS:** The C# source, map data and original placeholder sprites are included and Python validation has passed for every map and class. **Unity Editor is not available in this environment; the code is not yet compiled or playtested in Unity**, and Windows/macOS/Steam/iOS builds are **not** provided. This is a substantial integration prototype, not a commercial release candidate.

## NEW IN V4 — ENEMY ART, SOUNDS, INTERACTIVE GEOMETRY

**Read [Docs/V4_ANIMATIONS_AUDIO_SECTOR_EDITOR.md](Docs/V4_ANIMATIONS_AUDIO_SECTOR_EDITOR.md) before importing.** v4 adds a new eight-angle Skeleton Reaver turnaround and 11-row animation timeline, 20 original synthesised WAV effects, keyed seal door animations, 27 lever switches, 27 scrolling floor overlays, four animated environment prop strips, transient damage decals, and a polygon-sector Editor window capable of authoring concave floor outlines, arbitrary wall segments, pits, stairs and extra secret walls. Each map contains two authored polygon areas, one staircase and one new secret segment. **The other fourteen enemy atlases still use v3 procedural animation stand-ins.** This is a static-data-tested source expansion; Unity compilation, rendering and gameplay are not verified.

Run **Tools > Ashvault > Validate V4 Art, Sectors and Audio** after importing. Open **Tools > Ashvault > Open Polygon Sector Editor** to edit the nine JSON maps. See [Docs/v4_polygon_sector_atlas.png](Docs/v4_polygon_sector_atlas.png) and [Docs/skeleton_reaver_animation_preview.png](Docs/skeleton_reaver_animation_preview.png).

## Target environment

- **Unity:** 6.6, Editor `6000.6.10f1` (target). Recommended: create a fresh **Universal 3D (URP)** template project and copy `Assets/Ashvault` into it.
- **Rendering:** actual 3D collision geometry, raised floor sectors, lower ceilings, pillars, arched lintels, moving door seals and secret walls; low-resolution 426×240 framebuffer; independently drawn 2D creature/weapon/projectile sprites.
- **Input:** Input System 1.20.1 on Windows, macOS and provisional touch input on iOS; 60 FPS target (not benchmarked).
- **Sale model:** one-time purchase only; no in-app purchasing, ads, third-party analytics, accounts, multiplayer or network access.
- **Steam:** desktop builds are intended for Steam distribution later; **no Steamworks integration is present**.

## Getting started in Unity

1. In Unity Hub, install **Unity Editor 6000.6.10f1** and desired Windows/macOS/iOS build modules.
2. Create a **Universal 3D** project (`Ashvault`), preferably with the corresponding Unity 6.6 Editor version.
3. Copy **`Assets/Ashvault/` from this archive** into the new project's `Assets/` directory. Optionally copy the root docs and `.gitignore`/`.gitattributes`.
4. Install the **Input System** package if not present. In Project Settings > Player > Active Input Handling choose **Input System Package (New)** or **Both**.
5. In Unity select **Tools > Ashvault > Validate All Nine Campaign Maps**. The validator checks the actual JSON assets and the same C# grid pathfinder used by enemies.
6. In Unity select **Tools > Ashvault > Create or Rebuild Prototype Scene**. Open the saved `Assets/Ashvault/Scenes/Prototype.unity` and press **Play**.
7. Select an episode (I, II or III), then pick one of eight classes. Complete three maps per episode and advance from the victory screen. If you continue beyond Episode III, the campaign-complete screen appears.
8. After confirming compilation, commit the generated **`.meta` files**, scenes, scripts and maps to Git. Do not commit Unity's `Library/`, `Temp/`, `Obj/` or build products.

### Alternate route: open supplied folder as a Unity project

`Packages/manifest.json` and `ProjectSettings/ProjectVersion.txt` are included, but the full URP pipeline assets and complete Unity template settings are not bundled. Unity may generate these on first import. Using a **fresh URP project** is the less error-prone route. World geometry and 3D billboards prefer URP Lit materials; some effects use URP Unlit. A Universal 3D Renderer is required for intended lighting.

## Expanded prototype features

### Eight playable classes

1. **Iron Knight** — 150 HP, sword/warhammer, 15% incoming damage resistance.
2. **Wildranger** — 105 HP, crossbow/repeater, fast long-range sustained damage.
3. **Arcanist** — 85 HP, unlimited runewand/area-of-effect sceptre.
4. **Shadowblade** — 90 HP, rapid blades/venom throwing weapons, heal on eliminations.
5. **Dawn Cleric** — 125 HP, radiant mace/holy projectile, stronger heal on eliminations.
6. **Berserker** — 170 HP, heavy axe/siege axe, 30% bonus damage below half health.
7. **Thorn Druid** — 110 HP, thorn spells/wild-bloom area spell.
8. **Void Warlock** — 95 HP, cursed bolts/soul nova, health restored on eliminations.

Each class has a different speed, upgrade weapon, and gated passage/cache in **every map**. Class selection happens at the start of a run and persists as you progress. Health, ammo and unlocked upgrades carry forward between maps. Restarting a level begins it with a fresh loadout; this is a deliberate prototype limitation.

### Three episodes / nine maps

- **Episode I — The Cinder Keep:** Ashen Threshold → The Iron Reliquary → Throne of the Ember King; boss **The Ember King**.
- **Episode II — The Weeping Fen:** Mirewatch Crossing → Rotwater Sanctuary → Heart of the Mire; boss **Mother of the Mire**.
- **Episode III — The Astral Citadel:** Broken Observatory → The Starless Chapel → Crown of the Void; boss **The Starless Lich**.

These are **nine distinct JSON-authored map layouts built from three shared architectural patterns**, with variations in topology, theme, encounter composition and mirrored map orientation. They are not nine individually hand-sculpted, launch-quality levels. Each map contains **3 sigils, 3 discoverable secret walls, 8 class seals, a class-specific secondary weapon cache, and planned encounter anchors**. All maps have a Bronze → Azure → Crimson → All-Sigils exit gate progression. The three finales additionally require defeating their bosses.

### 15 enemy archetypes + boss phases

- Classification: **Grunt, Skirmisher, Ranged, Brute, Flying, Summoner, Boss**.
- Awareness: dormant/patrolling → visible target or heard weapon → investigation of last-known position → grid-route pursuit; chase times out, returning to patrol/idle.
- Attack types: contact melee, projectile bolts, multi-projectile volleys, telegraphed charge, minion summoning, telegraphed boss ground burst.
- Movement: grid-based corridor/door-aware 4-way breadth-first path search, periodic repaths, simple patrols, strafing/retreat for ranged units, and visual hover motion for fliers.
- Bosses have **three health phases** (100–67%, 66–35%, 34–0%) with faster attacks, additional projectiles, ground warnings and spawned helpers.
- Enemies have **per-classification HP, movement speed, detection range, attack range, damage and cooldown**. Encounter spawn positions and ambush flags live in JSON, not scattered throughout C#.

See [`Docs/ENEMY_ROSTER.md`](Docs/ENEMY_ROSTER.md) for the full design/balance matrix, and [`Docs/ENEMY_SPAWNS.csv`](Docs/ENEMY_SPAWNS.csv) for all **135** authored encounter anchor positions.

## Controls

| Action | Windows/macOS | iOS prototype |
| --- | --- | --- |
| Walk / strafe | WASD or arrow keys | left-side drag |
| Aim | mouse | right-side drag |
| Fire / attack | hold left mouse | FIRE |
| Interact (locks and secrets) | E | USE |
| Toggle weapons | Q, 1 or 2 | SWAP |
| Pause | Esc | PAUSE |

**iOS touch controls are provisional.** Small displays, notches, accessibility and dynamic safe areas are untested; the user interface is based on Unity immediate-mode GUI and will be replaced before shipping. Unity iOS exports require a Mac with compatible Xcode for signing.

## Validate without Unity

Python 3 and Pillow are required to create/re-render preview maps and placeholder art. Run:

```sh
python3 validate_campaign.py
python3 build_campaign.py
python3 generate_art.py
```

The validation command reads the *shipped* nine JSON files and confirms 9 distinct topologies, 8 classes, 135 planned encounters, 27 secrets, 15 enemy kinds and three bosses. It also verifies key-state reachability for each class, class-drop accessibility, known asset references and safe floor height differences. A generated reference overview is at [`Docs/campaign_overview.png`](Docs/campaign_overview.png).

**This is static validation, not proof of gameplay correctness**: movement and AI need full Unity playtesting, particularly animation of doors/colliders, enemy targeting, boss phase behaviour, level loading, sound and mobile performance.

## Current engineering limitations

- No Unity compiler/runtime testing has been possible here.
- Runtime cube-per-cell geometry and repeated path searches need mesh merging, batching, caching, occlusion and profiling before a production/mobile release.
- Vertical relief is small stepped sector height changes (0–0.30 m) rather than full changing-height lifts, crushers, polygonal room layout or true portal renderer. Distinct floor/ceiling elevations and sliding seals are first-stage implementations.
- All 15 enemies have directional playback (14 still use placeholder art); the Skeleton Reaver has a new artist-directed eight-view turnaround with derived multi-stage poses. Original placeholder audio and impact effects exist. Ragdolls, production audio mixing and finalized enemy sprite sheets remain future work.
- No difficulty settings, save/checkpoints, loading screen, campaign persistence, player options, achievements or Steamworks SDK integration yet.
- Weapon ammo, boss difficulty, iOS frame pacing, enemy navigation and combat all need balance testing.
- The working title **Ashvault** has not been trademark-cleared. Legal/licensing review remains necessary before commercial publication.

## Project documents

- [`Docs/GAME_DESIGN.md`](Docs/GAME_DESIGN.md) — pitch, progression, class identity and future systems.
- [`Docs/TECHNICAL_ARCHITECTURE.md`](Docs/TECHNICAL_ARCHITECTURE.md) — C# modules, architecture and test plan.
- [`Docs/ENEMY_ROSTER.md`](Docs/ENEMY_ROSTER.md) — enemy/boss taxonomy, statistics, AI states, attack rules.
- [`Docs/CAMPAIGN_MAPS.md`](Docs/CAMPAIGN_MAPS.md) — data authoring schema and topology rules.
- [`Docs/GITHUB_SETUP.md`](Docs/GITHUB_SETUP.md) — repository setup and project hygiene.

## GitHub connection

The connected GitHub identity is **SquishieInc**, but no repository was accessible at packaging time. Therefore **this archive has not been pushed or linked to a GitHub repository**. Create a private `SquishieInc/Ashvault` repository, grant the ChatGPT GitHub connection access, then commit the Unity project according to [`Docs/GITHUB_SETUP.md`](Docs/GITHUB_SETUP.md).

---

## v3 upgrade — sprite animation, combat, moving geometry, lighting & decor

This source archive adds 15 **8-facing x 6-state directional animation atlases** and the `DirectionalSpriteAnimator` component; attack/hurt/death impact feedback; dynamic one-cell lifts, shuttles, crushers and rune-floor traps; flickering point lights and lit wall/sprite materials; and 12 original theme-specific prop sprites (18 placed per map). Every level has 4 mechanisms, 8 light fixtures and 18 decor positions.

**Read `Docs/VISUAL_SECTORS_LIGHTING.md` before testing.** Run **Tools → Ashvault → Validate Visuals, Lighting and Machinery** after import. Preview the placements in `Docs/environment_map_atlas.png` and the sprites in `Docs/decorations_source_sheet.png`.

The 3D Lit rendering assumes a **Universal 3D Renderer**. URP's 2D Renderer uses a different 2D-light path. Shadowed billboards, physics contacts, moving platform riders and iOS lighting budgets must be verified in Unity. The 8-direction art is procedural placeholder art, not fully bespoke enemy turnarounds.

Offline validation (does not compile Unity C#):

```sh
python validate_campaign.py
python validate_visual_expansion.py
python Tools/validate_machinery_routes.py
```
