# Visual / Mechanics v3 QA — 23 November 2026

## Static checks completed

- All 9 campaign JSON maps remain distinct, with 3 episodes × 3 maps.
- All 15 enemy types have valid 768 x 864 RGBA directional atlases with 8 facing columns × 6 motion rows.
- All 12 original environment sprites are present and transparent (144 x 160 RGBA crops).
- **36 dynamic sector fixtures**: 9 lifts, 9 horizontal shuttles, 9 crushers, 9 rune traps.
- **72 episode-themed principal lights** (8 per map), 162 contextual sprites (18 per map).
- Machinery is situated on an open cell with four cardinal open neighbors; mandatory gates, keys and exits remain unoccupied.
- **72 class/map worst-case passage checks** pass with each map's moving lift cell treated as impassable. Mandatory keys, class weapon cache and exit remain reachable when relevant sigil and class gates have been unlocked.
- Existing `validate_campaign.py`: 9 maps, 135 encounter anchors, 27 secrets, 15 enemy species, 3 boss encounters pass.
- Python source scripts can be compiled by Python's `compileall`; source archive is Git-ready.

## Not verified here

- C# compilation against Unity 6000.6.10f1 and URP 17.6 (Unity Editor unavailable).
- 3D Lit material + SpriteRenderer actual shadow-caster behaviour across Vulkan/Metal/Direct3D.
- Per-frame rider movement, hazard hitboxes, collision at sector/ceiling boundaries, texture UVs, visual orientation, CPU allocations under sustained combat.
- Audio effects are still placeholders; not yet polished/implemented.
- Steam build, Apple signing, native iOS touch-controls, App Store compliance, performance and accessibility.

## Acceptance targets for the first Unity playtest

No Console errors; character can complete E01_L01; enemy frames swap correctly; each hazard behaves without repeated instant kills; torch light visibly affects textured wall faces; an opaque stone column casts shadow on nearby geometry; open secrets and collect all sigils; runs on Windows/macOS/iOS with appropriate per-platform light budgets.
