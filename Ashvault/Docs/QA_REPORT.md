# ASHVAULT Campaign Expansion — Verification Report

**Build:** Campaign Expansion v2 — 23 November 2026  
**Editor target:** Unity 6.6 (6000.6.10f1); fresh Universal 3D (URP) project recommended

## Checks completed in this environment

| Check | Result |
| --- | --- |
| Distinct campaign JSON maps | PASS — 9 |
| Episodes | PASS — 3 × 3 maps |
| Class/map progression checks | PASS — 8 classes on every map (72 combinations) |
| Key and exit progression | PASS — all 3 sigils and final seal reachable for each class |
| Class gates and weapon caches | PASS — present and reachable on every map |
| Secret rooms | PASS — 27 (three per level) |
| Planned encounter anchors | PASS — 135 coordinates on valid map cells |
| Enemy types | PASS — 15 catalogued archetypes |
| Episode bosses | PASS — 3 finale encounters |
| Asset references | PASS — 39 verified sprite/texture links |
| C# files included | PASS — 15 source files with balanced brace delimiters |
| C# compilation with Unity APIs | **NOT RUN** (Unity / C# compiler unavailable) |
| Runtime physics, collision, nav and enemy behaviour | **NOT RUN** (requires Unity scene playtest) |
| Windows, macOS, Steam, iOS export & performance | **NOT RUN** |

This verification establishes internal consistency of data and static structure, **not** a playable or shippable game. A compile test in Unity, in-Editor encounter tests, physical-device touch testing and release QA are mandatory next steps.

## Unity follow-up

1. Open a new Universal 3D Unity 6.6 project, import the `Assets/Ashvault` folder and install Input System.
2. Use **Tools → Ashvault → Validate All Nine Campaign Maps**.
3. Use **Tools → Ashvault → Create or Rebuild Prototype Scene** and inspect Console warnings/errors.
4. Playtest all eight classes, progression, unlock seals and three bosses.
5. Inspect enemy route changes when doors open; test corners, raised sectors, summons and navigation under load.
6. Profile gameplay and controls on Windows/macOS and iOS before attempting release.
