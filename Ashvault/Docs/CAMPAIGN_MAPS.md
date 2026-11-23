# Nine-Level Campaign / Map Authoring Guide

## Episodes

| Map ID | Episode | Title | Boss finale? |
| --- | --- | --- | --- |
| E01_L01 | Cinder Keep | Ashen Threshold | No |
| E01_L02 | Cinder Keep | The Iron Reliquary | No |
| E01_L03 | Cinder Keep | Throne of the Ember King | **The Ember King** |
| E02_L01 | Weeping Fen | Mirewatch Crossing | No |
| E02_L02 | Weeping Fen | Rotwater Sanctuary | No |
| E02_L03 | Weeping Fen | Heart of the Mire | **Mother of the Mire** |
| E03_L01 | Astral Citadel | Broken Observatory | No |
| E03_L02 | Astral Citadel | The Starless Chapel | No |
| E03_L03 | Astral Citadel | Crown of the Void | **The Starless Lich** |

Each is a 48×34 grid serialized to JSON. The current maps reuse a common main progression spine but have episode-specific side halls and different mirrored/expanded room configurations. To get commercial-quality pacing, design and test their identities individually rather than simply duplicating the template.

## Data schema

Level JSON is consumed by `JsonUtility.FromJson<LevelBlueprint>` and includes:

- `id`, `episode`, `level`, `title`, `episodeTitle`, `intro` — progression and presentation.
- `width`, `height`, `rows` — ASCII tile map, **first row is highest Z**. Each cell has a width of 3 Unity units.
- `sectors[]` — named rectangular `minX/maxX/minZ/maxZ` regions, `floor` elevation (0.0–0.30) and `ceiling` underside height (4.45–5.6). Sector geometry gets floor collision and lowered ceiling slabs.
- `enemies[]` — authored enemy `kind`, `mode`, `(x,z)` spawn anchor.
- `classDrops[]` — one `(x,z)` secondary-weapon cache for each `classId` 0–7.

### Tile legend

| Code | Meaning |
| --- | --- |
| `#` / `.` | solid stone / open corridor or room |
| `P` / `X` | player entrance / portal exit |
| `b`, `a`, `c` | Bronze / Azure / Crimson sigil pickups |
| `B`, `A`, `C` | corresponding key-locked door, opened by `E` |
| `Z` | all-three-sigils exit lock |
| `1`–`8` | Knight, Ranger, Arcanist, Shadowblade, Cleric, Berserker, Druid, Warlock gated passages |
| `s` | hidden wall: looks like ordinary masonry, opens with `E`, counts towards secrets |
| `+` / `w` | healing elixir / class-specific secondary weapon pickup |

The core path uses all three sigils; class gates are *optional alternatives* or caches, never essential for progression. Every map has three secret masonry blocks. The three finale maps prevent portal completion while the living boss remains.

## Navigation and geometry rules

- Both the Unity dungeon builder and the AI read exactly the same JSON. `DungeonMap.IsOpen` treats unopened gates and secrets as blocked.
- Grid path searches are four-directional; there is no diagonal cutting through walls. Routes must accommodate character radius and low-ceiling player height.
- A sector-to-sector elevation difference above 0.43 Unity units makes an edge impassable to the AI. The current maps use 0–0.30 m so AI and players can step across all required transitions.
- Class gates and secrets physically lift vertically over 0.74 and 0.90 seconds; only after the transition does the navigation cell become open.
- All maps use floor slabs, ceiling volumes, pillars, decorative arch lintels and torches. These **are currently block/cell-authored geometry**, not Doom's original binary sector/linedef representation. Mesh merging/occlusion are essential production tasks.

## Rebuilding and verifying

```sh
python3 build_campaign.py      # rewrites the nine JSONs and preview PNGs
python3 validate_campaign.py   # validates the saved JSON data
```

The validator checks **all eight classes** in **all nine levels**, three-key victory paths, all three secrets, and every class weapon cache. It also validates sprite references and ensures the bosses only appear at episode finales. `Docs/campaign_overview.png` is a 3×3 tile-layout overview, with detailed maps `E01_L01_overview.png` etc.

Unity also has **Tools > Ashvault > Validate All Nine Campaign Maps** for validation against the runtime C# pathfinder, without requiring a play session. Neither script proves that an encounter is fun or that CharacterController collision behaves correctly; those require actual playtesting.
