# ASHVAULT — Fantasy Retro Shooter Game Design (Expansion)

**Working title:** Ashvault  
**Studio:** SquishieInc  
**Genre:** Single-player fast-paced fantasy boomer shooter / class-based dungeon FPS  
**Target platforms:** Windows, macOS, Steam distribution, iOS  
**Commercial model:** One-time premium purchase. No ads, IAP, live-service systems or data harvesting.

## One-sentence pitch

Descend through a cursed fortress, a drowned wilderness and a ruined astral kingdom, clearing sprite-filled 3D dungeon mazes with swords, bolts and spells. Choose a class at the start of a run; find its signature weapon and secret routes; collect three sigils; and defeat the keepers guarding the final gates.

## Design pillars

1. **Classic fast shooter:** responsive movement, readable enemy silhouettes, short attack tells, mixed melee and ranged combat.
2. **Real replay value through classes:** eight starting archetypes with differentiated health, speed, melee/ranged strengths, weapon upgrades and optional routes.
3. **Exploration first:** interconnected halls, class-sealed passages, hidden masonry, keyed doors, non-linear loopbacks and vertical space.
4. **Original visual identity:** low-resolution handcrafted pixel graphics, 2D sprite actors over 3D sector-like architecture, own world/music/names.
5. **Premium and offline:** a complete purchasable campaign without monetisation loops, ads or external services.

## Playable classes

| Archetype | Base HP | Move speed | Primary | Secondary | Passive / identity |
| --- | ---: | ---: | --- | --- | --- |
| Iron Knight | 150 | 7.4 | Oathbreaker Sword | Warhammer | 15% less incoming damage |
| Wildranger | 105 | 9.0 | Hunter Crossbow | Storm Repeater | Fast projectile pressure |
| Arcanist | 85 | 8.0 | Runewand | Ember Sceptre | Infinite basic spells, area damage |
| Shadowblade | 90 | 10.3 | Dual Fangs | Venom Throwers | Fast melee, 3 HP heal on kill |
| Dawn Cleric | 125 | 7.8 | Dawnmace | Judgement Orb | 9 HP heal on kill |
| Berserker | 170 | 8.3 | Rage Cleaver | Siege Axe | +30% damage below half HP |
| Thorn Druid | 110 | 8.8 | Thorn Staff | Wild Bloom | Area-control magic |
| Void Warlock | 95 | 8.1 | Hex Grimoire | Soul Nova | 6 HP heal on kill |

The prototype provides 8 separate class-sealed passages per map; no mandatory key requires a class door. Secondary weapons are obtained through class-compatible pickups. Health/weapon state carries between maps. Design follow-ups: active class skills, stamina/resources, differentiated mobility animations and class-specific objectives.

## Campaign flow

`Choose episode` → `Choose class` → `Map 1` → `Map 2` → `Map 3 / boss` → `Next episode` → `Campaign complete`. A player can begin with any unlocked episode in this prototype; formal progression unlock/save rules are a future release feature.

### Episode I — The Cinder Keep

**Art direction:** ember-lit stone fortifications, chains, ironwork, pale skeletal enemies, orange torchlight.

- **E01_L01 Ashen Threshold** — introduce Bronze/Azure/Crimson locks, classes, movement and secret walls.
- **E01_L02 The Iron Reliquary** — increase ambush pressure and heavy enemies.
- **E01_L03 Throne of the Ember King** — boss, adds and warning zones.

### Episode II — The Weeping Fen

**Art direction:** drowned shrines, mossy stone, green light, twisted fauna, spectral figures.

- **E02_L01 Mirewatch Crossing** — rapid skirmishers and wisp volleys.
- **E02_L02 Rotwater Sanctuary** — summoners and wider ambush groups.
- **E02_L03 Heart of the Mire** — Mother of the Mire finale.

### Episode III — The Astral Citadel

**Art direction:** cracked violet-blue masonry, ruins under a starless sky, floating crystalline motifs.

- **E03_L01 Broken Observatory** — swift stalkers and long-range adept bolts.
- **E03_L02 The Starless Chapel** — dense sentinels and rift summons.
- **E03_L03 Crown of the Void** — the Starless Lich finale.

The current 9 level files are functional prototypes built from a shared designed progression spine with multiple themed variations; full artistic differentiation is a future content pass.

## Core map loop

Find a Bronze sigil → unlock a bronze passage → descend toward Azure → open the Azure passage → find Crimson → open the final two seals → defeat the boss when present → enter the exit portal. Optional class routes allow shortcuts and class loot; three secrets per map reward close exploration. Enemy types and attack patterns vary per map and episode.

## Enemy classification and bosses

See `ENEMY_ROSTER.md` for the detailed 15-archetype HP/speed/range/damage matrix. Planned roster includes melee grunts, skirmishers, ranged casters, telegraphed charging brutes, hovering ranged creatures, minion summoners and bosses with three phase changes.

Enemies use physics sight raycasts, sound alerts, a memory of the last visible or heard player position and door-aware 4-direction grid pathing. Ambush spawners create enemies when a player reaches a planned nearby point. Every boss has a health bar, faster attacks in later phases and a danger-circle ground attack with a visible tell.

## Geometry / visual systems

- Actual 3D collision walls, stepped sector floors, lowered vault ceilings, interior pillars, lintels and sliding key doors.
- Camera-facing 2D sprite actors with a low-resolution world render and full-resolution HUD.
- Asset/geometry streaming and combat loops are currently prototype implementations; replace cube-per-cell meshes with sector meshes and palette/atlas-aware materials for production.
- Intention for future map authoring: sector/linedef-like visual editor, moving lifts, descending stairs, changing-height platforms, damaging liquids, locked switches, moving crushers and per-episode geometry palettes.

## Unimplemented production systems

Commercial-grade AI animation, individual monster sound identities, map event scripting, authoring/editor tools, difficulty modes, saves, controllers, localisations, accessibility, platform certification, achievements, Steamworks, store metadata, parental/accessibility design review and optimization across iPhone generations remain outstanding.

## Intellectual property

Reference the **feel of early 1990s FPS design**, not exact maps, audio, level art, monster likenesses or branded lore. Fantasy class labels are generic and aren't tied to any licensed D&D campaign setting. Review the final game name and all production art for commercial rights before publishing.
