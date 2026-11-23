# Ashvault v4 — offline static verification

**Date:** 23 November 2026. These checks were run in the container that generated the source. Unity Editor and Player runtime were unavailable.

## Passed

- Nine JSON maps parsed and maintained distinct grid topologies, three sigil keys, eight class gates, exit seals and all original enemy placements.
- 72 class/map combinations remained connected to required key, exit and class-drop objectives when optional new walls, stair heights, polygon floor heights and their crossing tests were applied with gate access permitted.
- Nine maps each have two non-self-intersecting, mutually separated polygon sectors (one raised concave platform and one shallow pit), a three-step staircase and an optional secret segment.
- 27 added lever switches target their map's Bronze, Azure and Crimson doors, with an adjacent open activation cell.
- 27 animated floor overlays are in valid open cells; the correct theme texture is assigned per episode.
- Eight-direction, 11-pose skeleton atlas has alpha and expected 768x1584 dimensions; fourteen legacy atlases remain 768x864.
- Four animated decoration strips (4 frames, 512x128), four transparent damage decals, and twenty 22,050Hz mono WAV files exist and passed format checks.
- V3 campaign, environmental placement, machinery-route and separate v4 data QA scripts pass.

## NOT tested

- Compilation in Unity 6.6 / URP / Input System or whether shaders render as intended.
- Mesh winding, MeshCollider interactions, floor cutout seams, CharacterController stepping, platform collision, crusher contacts, interactions and raycasts.
- URP point-light performance/shadows, alpha-clipped billboard rendering or transparent decal blending.
- Actual directional animation and hit timing, audibility/spatialization of WAV clips, Unity asset imports.
- Windows/macOS standalone, Steam distribution or iOS device performance, battery or thermal impact.

## Commands

```sh
python3 validate_campaign.py
python3 validate_visual_expansion.py
python3 validate_v4.py
python3 Tools/validate_machinery_routes.py
```

The recommended next action is an actual Unity import/compile/test session. Do not call the project production-ready based on the offline checks alone.
