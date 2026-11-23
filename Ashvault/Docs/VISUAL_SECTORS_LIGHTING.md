# Ashvault v3 — 3D lighting, directional sprites, moving sectors and world decoration

## Scope

**Delivered source + assets** in this iteration: eight-view enemy sprite animation framework, hit/attack/death feedback, dynamic point lights and torch flicker, URP 3D-lit world textures and sprite cutouts, authored moving machinery/traps, and 12 original decorative sprite sources distributed throughout all nine campaign maps.

**Not Editor-playtested:** Unity is not installed here. Import, shader correctness, CharacterController platform riding, shadow compatibility, and device framerates MUST be tested before describing this as a playable release. No Steam or iOS builds are included.

## Directional enemies

- Each existing enemy and boss has a replacement-friendly resource atlas at `Assets/Ashvault/Resources/Textures/Directional/<baseSprite>_directions.png`.
- Exact dimensions **768 x 864**: eight 96x144 facing columns and six animation rows.
- Columns in **left to right** order: front, front-right, right, rear-right, back, rear-left, left, front-left. Direction is chosen by angle from enemy forward to camera.
- Rows in **top to bottom** order: idle, walk A, walk B, attack, hurt, death.
- `DirectionalSpriteAnimator` automatically chooses the facing from enemy orientation and selects the action based on `EnemyAI` state. Walk is a 6.5 fps two-frame loop, attack .31s, hurt .22s; death holds until cleanup.
- **Art caveat:** shipped atlases are procedurally manipulated prototypes derived from the earlier original front-facing enemy images, including compressed facings, shading, bobbing, red hurt and collapsed death poses. They are **not** professionally hand-drawn true side/back poses. Replace each atlas with original eight-angle art without changing the runtime code.

## Combat feel

- Hurt and attack states trigger the correct motion rows.
- Red tint flash on impact; instant area spark; floating damage numbers and a death pose retained for 1.35 seconds.
- Bosses get larger impact bursts and numbers.
- Player damage briefly washes the retro framebuffer red.
- Area attacks and traps apply damage; a trapped enemy can die without counting as a player kill source.
- TODO for production: add authored attack/death sound effects, enemy stagger tuning, gib variations, screen-shake option, impact decals and configurable damage text.

## Sector machinery

Each of the nine maps contains four records in its `mechanisms` JSON list. The subsystem uses 3D box meshes/colliders to make the moving pieces interact with Unity physics:

| Kind | Runtime behavior | Navigation / combat |
|---|---|---|
| `Lift` | Vertical one-cell floor moves up and down with eased travel and dwell time | Updates `DungeonMap.Surface` dynamically; pathfinder avoids excessive step heights; carries feet-on-platform CharacterControllers |
| `MovingPlatform` | Small horizontal shuttle in a room | Carries CharacterControllers; no dynamic nav links yet |
| `Crusher` | Periodically descending overhead slab with red light telegraph | Damages occupants when slab reaches head level (once/sec per victim) |
| `RuneTrap` | Timed glowing floor seal | Has a warm warning glow; deals periodic damage when activated |

All machinery cycles automatically. Authored sites are intentionally **optional and away from keys/exit gates**; no switch-trigger puzzle logic or arbitrary 3D sectors yet. This preserves the campaign's keycard-style progression and ensures the prototype remains navigable even while lift cells are raised.

### Sector limitations / next step

This is an upgraded **grid-and-sector / 2.5D** prototype, not a WAD-compatible Doom BSP/visplane engine. Each machine occupies one tile. There is not yet an arbitrary linedef/sector editor, swinging doors, conveyor physics, sector-synchronized sound, machinery switches, cover-aware navmesh links or lethal crush based on true physics contact. The `MovingPlatform` currently rides over an existing stone floor: for pits and jumps, author a map cell gap and add traversability links.

## Lighting, wall shading and shadows

Ashvault deliberately uses a **Universal 3D Renderer**, not URP's separate 2D Renderer. 3D walls, floors and ceilings now use **Universal Render Pipeline/Lit** materials instead of Unlit, so existing point lights can affect their pixel art textures. The low-res framebuffer preserves sharp, retro-looking edges.

Enemy / prop sprites are rendered with lit alpha-clipped materials (`_ALPHATEST_ON`, `AlphaClip`, `_Cutoff`), with shadow casting/receiving requested on `SpriteRenderer`. This approach is meant to make sprites sit in the same light field as the 3D geometry. **Inspect each shader in Unity**: platform-specific URP/SpriteRenderer behaviour and depth/shadow support must be verified in the Editor and player builds. For production, a custom double-sided URP Lit billboard shader/Shader Graph with an explicit shadow caster pass is recommended.

`Atmosphere.Configure` controls a dark ambient tint and episode-specific exponential fog. Maps carry 8 authored light fixtures and use flickering 3D point lights; only two per map are configured to cast soft realtime shadows on desktop. On iOS, the current logic limits realtime shadows further based on quality level. Decorative glowing mushrooms/crystals and active hazards have cheaper no-shadow point lights.

- Cinder Keep: warm orange-red light, soot haze.
- Weeping Fen: acidic green luminescence, mist.
- Astral Citadel: violet-blue magic, cosmic haze.

**Mobile caveat:** ~8 main and up to 6 decorative lights can appear in a scene, and how many actually influence an object depends on URP quality/renderer settings. Profile per-object additional light limits, shadows, overdraw and RenderTexture resolution. Disable decorative lights or use an emissive visual shader on older iPhones. Runtime-generated maps are not lightmapped.

For realistic wall highlights and shadows, verify the selected Universal Renderer has *Additional Lights* and *Additional Light Shadows* enabled. Avoid a 2D Renderer asset; it uses a different 2D lighting path.

## Contextual decor

`EnvironmentDecor` loads lit pixel-art billboards from `Resources/Textures/deco_*.png` and positions them using each map's `decorations` JSON array. There are **18 placed sprites per map**, 162 across the campaign, with no colliders so they don't disrupt navigation.

| Episode | Four art types |
|---|---|
| I — Cinder Keep | Brazier, rune banner, sarcophagus, hanging iron cage |
| II — Weeping Fen | Bioluminescent mushrooms, roots, moss idol, skull totem |
| III — Astral Citadel | Violet crystal, armillary, star lantern, runic monolith |

`Docs/decorations_source_sheet.png` stores the original transparent sprite sheet; game imports use individually cropped 144 x 160 PNGs. At least one prop of each theme category repeats on each map. Some glowing art types add a small fill light. The billboard uses the same painterly pixel vocabulary as the earlier enemy and pickup placeholders.

## Editor checklist

1. In **Unity 6.6 Universal 3D (URP)**, copy `Assets/Ashvault` into your project. Ensure `com.unity.render-pipelines.universal` and the Input System are installed.
2. Select **Tools → Ashvault → Validate All Nine Campaign Maps**.
3. Select **Tools → Ashvault → Validate Visuals, Lighting and Machinery**.
4. Use **Tools → Ashvault → Create or Rebuild Prototype Scene**, then Play.
5. Turn an enemy through 360° and verify all eight directions; verify hurt, attack, walk and death frames on ordinary enemies and bosses.
6. Stand on a moving lift and shuttle, confirm carried movement and safe dismount; inspect collision on raised/low ceiling sectors, and route changes around a raised lift.
7. Trigger a crusher and rune trap: test player/enemy damage cooldowns and whether walls block the right projectiles.
8. Observe a torch beside a stone wall, verify pixel-art texture receives its warm light, and verify wall geometry casts a visible soft shadow across floor/props.
9. Test E01, E02 and E03 light palettes; validate URP Forward vs Forward+ shadow limits, HDR disabled and retro texture point filtering.
10. Build a Windows or macOS standalone and an **iOS device build**, profile FPS/battery/memory, and tune realtime shadow/light budgets.

Offline map and asset QA scripts:

```sh
python validate_campaign.py
python validate_visual_expansion.py
python Tools/validate_machinery_routes.py
```

To regenerate atlas placeholders: `python Tools/make_visual_assets.py`. To re-author environment records after regenerating the campaign: `python Tools/author_environment.py`. Avoid running either generator over manually painted final art.
