using UnityEngine;

namespace Ashvault
{
    // Runtime geometry is generated from the same JSON files used by GridPathfinder.
    // Sector elevations, low ceilings, support columns and arch beams create 2.5D relief.
    public static class DungeonBuilder
    {
        private const float Tile = DungeonMap.Tile;
        private const float RoofHeight = 5.65f;
        private static readonly Vector2Int[] Torches = {
            new Vector2Int(7, 6), new Vector2Int(20, 7),
            new Vector2Int(24, 23), new Vector2Int(38, 9) };

        public static void Build(AshGame game, Transform root, DungeonMap map)
        {
            var theme = GetTheme(map.Level.episode);
            var stone = VisualFactory.StoneMaterial("wall_bricks", theme.wall);
            var accent = VisualFactory.StoneMaterial("wall_bricks", theme.accent);
            var floor = VisualFactory.StoneMaterial("floor_tiles", theme.floor,
                new Vector2(map.Width, map.Height));
            var roofMaterial = VisualFactory.StoneMaterial("ceiling_tiles", theme.ceiling,
                new Vector2(map.Width, map.Height));
            var doors = VisualFactory.StoneMaterial("door_runes", Color.white);
            AshAudio.Ambient(root, map.Level.episode);
            var world = new GameObject("ARCHITECTURE / SECTORS");
            world.transform.SetParent(root, false);
            Transform geometry = world.transform;

            // Continuous ground with distinct collidable raised-sector platforms.
            if (map.Level.polygonSectors != null &&
                System.Array.Exists(map.Level.polygonSectors, p => p.kind == "Pit"))
                BuildTileFloorWithPitCutouts(geometry, map, floor);
            else
                Cube(geometry, "Base stone bed", new Vector3((map.Width - 1) * Tile * 0.5f,
                    -0.18f, (map.Height - 1) * Tile * 0.5f),
                    new Vector3(map.Width * Tile, 0.36f, map.Height * Tile), floor);
            Cube(geometry, "Upper vaulted roof", new Vector3((map.Width - 1) * Tile * 0.5f,
                RoofHeight + 0.18f, (map.Height - 1) * Tile * 0.5f),
                new Vector3(map.Width * Tile, 0.36f, map.Height * Tile), roofMaterial);

            if (map.Level.sectors != null)
            {
                foreach (var sector in map.Level.sectors)
                {
                    Vector3 mid = new Vector3((sector.minX + sector.maxX) * Tile * 0.5f,
                        0f, (sector.minZ + sector.maxZ) * Tile * 0.5f);
                    Vector3 area = new Vector3((sector.maxX - sector.minX + 1) * Tile,
                        0f, (sector.maxZ - sector.minZ + 1) * Tile);
                    if (sector.floor > 0f)
                    {
                        Material sectorFloor = VisualFactory.StoneMaterial("floor_tiles", theme.accent,
                            new Vector2(sector.maxX - sector.minX + 1,
                                sector.maxZ - sector.minZ + 1));
                        bool containsPit = false;
                        for(int sx=sector.minX;sx<=sector.maxX && !containsPit;sx++)
                            for(int sz=sector.minZ;sz<=sector.maxZ;sz++)
                                if (map.IsPit(sx,sz)) {containsPit=true;break;}
                        if (containsPit) BuildTileFloorWithPitCutouts(geometry,map,sectorFloor,sector);
                        else Cube(geometry, sector.name + " - raised sector floor",
                            mid + Vector3.up * (sector.floor * 0.5f),
                            new Vector3(area.x, sector.floor, area.z), sectorFloor);
                    }
                    if (sector.ceiling < RoofHeight - 0.05f)
                        Cube(geometry, sector.name + " - lowered vaulted ceiling",
                            mid + Vector3.up * (sector.ceiling + 0.14f),
                            new Vector3(area.x, 0.28f, area.z),
                            VisualFactory.StoneMaterial("ceiling_tiles", theme.ceiling,
                                new Vector2(sector.maxX - sector.minX + 1,
                                    sector.maxZ - sector.minZ + 1)));
                }
            }

            for (int x = 0; x < map.Width; x++)
            for (int z = 0; z < map.Height; z++)
            {
                char c = map.Cell(x, z);
                Vector3 pos = new Vector3(x * Tile, 0, z * Tile);
                if (c == '#')
                {
                    // Only boundary/inner-island walls need geometry. Fully hidden blocks do not.
                    if (map.Cell(x - 1, z) == '#' && map.Cell(x + 1, z) == '#' &&
                        map.Cell(x, z - 1) == '#' && map.Cell(x, z + 1) == '#') continue;
                    Cube(geometry, "Solid wall " + x + "," + z,
                        pos + Vector3.up * (RoofHeight * 0.5f),
                        new Vector3(Tile, RoofHeight, Tile), (x + z) % 5 == 0 ? accent : stone);
                    // Shallow decorative cornices add texture to larger rooms.
                    if ((x * 7 + z) % 9 == 0)
                        Decoration(geometry, "Carved wall cornice", pos + Vector3.up * 4.6f,
                            new Vector3(Tile * 0.98f, 0.20f, Tile * 0.98f), accent);
                }
                else if (c == 's') BuildSecret(game, map, geometry, x, z, stone);
                else if (c == 'B' || c == 'A' || c == 'C' || c == 'Z' ||
                         (c >= '1' && c <= '8')) BuildDoor(game, map, geometry, x, z, c, doors);
            }

            // Arched lintels over structural gates (visual only, no invisible collision).
            foreach (var gate in new[] { 'B', 'A', 'C', 'Z' })
            {
                var cell = map.Locate(gate);
                Decoration(geometry, "Arched masonry lintel", map.World(cell.x, cell.y) + Vector3.up * 4.65f,
                    new Vector3(Tile * 1.25f, 0.6f, Tile * 1.25f), accent);
            }

            // Independent polygon-sector layer: supports concave floors, angled walls,
            // ramps of discrete stairs, pits and hidden passages.
            PolygonWorldBuilder.Build(game, geometry, map, stone, floor);
            // One-click animated switch control for each of the authored key-seal doors.
            if (map.Level.switches != null)
            {
                foreach (var spec in map.Level.switches)
                {
                    var target = geometry.GetComponentInChildren<LockedDoor>();
                    // Prefer exact target coordinate instead of reliance on hierarchy order.
                    foreach (var door in geometry.GetComponentsInChildren<LockedDoor>())
                        if (door.Matches(spec.targetX, spec.targetZ)) { target = door; break; }
                    if (target != null && target.Matches(spec.targetX, spec.targetZ))
                        AshSwitch.Create(geometry, map, spec, target);
                }
            }
            if (map.Level.animatedSurfaces != null)
                foreach(var flow in map.Level.animatedSurfaces)
                    FlowingSurface.Spawn(geometry,map,flow);
            // Authored hazards, moving sectors and non-colliding, theme-specific 2D props.
            // Keep these on optional cells: they can change height and are not progression gates.
            if (map.Level.mechanisms != null)
                foreach (var machine in map.Level.mechanisms)
                    SectorMachinery.Spawn(game, geometry, map, machine, accent, doors);

            if (map.Level.decorations != null)
                foreach (var prop in map.Level.decorations)
                    if (map.IsOpen(prop.x, prop.z)) EnvironmentDecor.Spawn(geometry, map, prop);

            for (int x = 0; x < map.Width; x++)
            for (int z = 0; z < map.Height; z++)
            {
                char c = map.Cell(x, z);
                switch (c)
                {
                    case 'b': Key(map, root, x, z, SigilKey.Bronze); break;
                    case 'a': Key(map, root, x, z, SigilKey.Azure); break;
                    case 'c': Key(map, root, x, z, SigilKey.Crimson); break;
                    case '+': Health(map, root, x, z, 35); break;
                    case 'w': Weapon(map, root, x, z, game.SelectedClass); break;
                    case 'X': Exit(map, root, x, z); break;
                }
            }
            if (map.Level.classDrops != null)
                foreach (var drop in map.Level.classDrops)
                    if (drop.classId == (int)game.SelectedClass)
                        Weapon(map, root, drop.x, drop.z, game.SelectedClass);
            if (map.Level.enemies != null)
                foreach (var encounter in map.Level.enemies)
                {
                    if (!System.Enum.TryParse(encounter.kind, out EnemyArchetype kind))
                    {
                        Debug.LogError("Unknown enemy: " + encounter.kind); continue;
                    }
                    game.RegisterEnemy();
                    if (encounter.mode == "Ambush")
                    {
                        var anchor = new GameObject("AMBUSH / " + encounter.kind);
                        anchor.transform.SetParent(root, false);
                        anchor.transform.position = map.World(encounter.x, encounter.z);
                        anchor.AddComponent<EnemySpawnAnchor>().Initialize(game, kind,
                            new Vector2Int(encounter.x, encounter.z), 14f);
                    }
                    else EnemyFactory.Spawn(game, kind, new Vector2Int(encounter.x, encounter.z),
                        encounter.mode == "Patrol", false);
                }
            if (map.Level.fixtures != null && map.Level.fixtures.Length > 0)
            {
                foreach (var fixture in map.Level.fixtures)
                    if (map.IsOpen(fixture.x, fixture.z))
                        Brazier(map, geometry, fixture.x, fixture.z, theme.fire,
                            fixture.shadows, fixture.kind);
            }
            else
            {
                for (int i = 0; i < Torches.Length; i++)
                {
                    var t = Torches[i];
                    int x = map.Level.level == 2 ? map.Width - 1 - t.x : t.x;
                    int z = map.Level.level == 3 ? map.Height - 1 - t.y : t.y;
                    if (map.IsOpen(x, z)) Brazier(map, geometry, x, z, theme.fire, i == 0, "Torch");
                }
            }
        }

        private static void BuildTileFloorWithPitCutouts(Transform parent,DungeonMap map,Material material,
            SectorData limit = null)
        {
            // Tile-aligned cutouts are intentional for compatibility with legacy 1993-style
            // square maps. A future boolean polygon mesh is required for exact rim cutouts.
            var vertices=new System.Collections.Generic.List<Vector3>();
            var uv=new System.Collections.Generic.List<Vector2>();
            var triangles=new System.Collections.Generic.List<int>();
            for(int x=0;x<map.Width;x++)for(int z=0;z<map.Height;z++)
            {
                if (map.Cell(x,z)=='#' || map.IsPit(x,z) ||
                    (limit != null && !limit.Contains(x,z)))continue;
                float ax=(x-.5f)*Tile,bx=(x+.5f)*Tile;
                float az=(z-.5f)*Tile,bz=(z+.5f)*Tile;
                int n=vertices.Count;
                float top = limit == null ? 0f : limit.floor;
                vertices.Add(new Vector3(ax,top,az));vertices.Add(new Vector3(bx,top,az));
                vertices.Add(new Vector3(ax,top,bz));vertices.Add(new Vector3(bx,top,bz));
                uv.Add(new Vector2(x,z));uv.Add(new Vector2(x+1,z));
                uv.Add(new Vector2(x,z+1));uv.Add(new Vector2(x+1,z+1));
                triangles.Add(n);triangles.Add(n+2);triangles.Add(n+1);
                triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n+3);
            }
            var mesh=new Mesh{ name="Tile floor with pit cutouts" };
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            var floor=new GameObject("FLOOR / PIT-CUT TILE MESH / " +
                (limit==null?"BASE":limit.name));floor.transform.SetParent(parent,false);
            floor.AddComponent<MeshFilter>().sharedMesh=mesh;
            floor.AddComponent<MeshRenderer>().sharedMaterial=material;
            floor.AddComponent<MeshCollider>().sharedMesh=mesh;
        }

        private static (Color wall, Color accent, Color floor, Color ceiling, Color fire) GetTheme(int episode)
        {
            if (episode == 2) return (new Color(.49f, .61f, .48f), new Color(.30f, .48f, .37f),
                new Color(.65f, .64f, .48f), new Color(.36f, .50f, .39f), new Color(.4f, .95f, .5f));
            if (episode == 3) return (new Color(.58f, .57f, .78f), new Color(.38f, .35f, .68f),
                new Color(.60f, .60f, .77f), new Color(.29f, .28f, .52f), new Color(.6f, .45f, 1f));
            return (new Color(.79f, .74f, .72f), new Color(.54f, .40f, .45f),
                new Color(.82f, .76f, .66f), new Color(.65f, .53f, .62f), new Color(1f, .5f, .19f));
        }

        private static GameObject Cube(Transform root, string title, Vector3 position,
            Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = title;
            go.transform.SetParent(root, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static void Decoration(Transform root, string title, Vector3 point, Vector3 scale, Material material)
        {
            var go = Cube(root, title, point, scale, material);
            Object.Destroy(go.GetComponent<Collider>());
        }

        private static void BuildDoor(AshGame game, DungeonMap map, Transform root,
            int x, int z, char symbol, Material baseMaterial)
        {
            // Full-cell collision prevents corner-cutting around gates in turning passages.
            // A slimmer visual panel with a separate thick collider is future polish.
            Vector3 scale = new Vector3(Tile, 4.6f, Tile);
            Color color = symbol == 'B' ? new Color(1f, .64f, .28f) :
                symbol == 'A' ? new Color(.3f, .7f, 1f) :
                symbol == 'C' || symbol == 'Z' ? new Color(1f, .28f, .25f) :
                new Color(.8f, .5f, 1f);
            var slab = Cube(root, "SEALED / " + symbol + " (" + x + "," + z + ")",
                map.World(x, z) + Vector3.up * 2.3f, scale,
                VisualFactory.StoneMaterial("door_runes", color));
            var gate = slab.AddComponent<LockedDoor>();
            gate.Configure(game, map, x, z, symbol);
            if (symbol == 'A') SplitDoorFacade.Create(slab, gate);
        }

        private static void BuildSecret(AshGame game, DungeonMap map, Transform root,
            int x, int z, Material stone)
        {
            var block = Cube(root, "UNMARKED SECRET WALL", map.World(x, z) + Vector3.up * 2.8f,
                new Vector3(Tile, 5.6f, Tile), stone);
            block.AddComponent<SecretWall>().Configure(map, x, z);
            game.RegisterSecret();
        }

        private static GameObject Pickup(DungeonMap map, Transform root, int x, int z,
            string title, string art, float size = 1.35f)
        {
            var go = new GameObject(title);
            go.transform.SetParent(root, false);
            go.transform.position = map.World(x, z) + Vector3.up * .8f;
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = .9f;
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var picture = VisualFactory.Billboard(art, Vector3.zero,
                new Vector2(size, size), "2D pickup sprite");
            picture.transform.SetParent(go.transform, false);
            go.AddComponent<PickupSpin>();
            return go;
        }
        private static void Key(DungeonMap map, Transform root, int x, int z, SigilKey key)
        {
            string name = key == SigilKey.Bronze ? "key_bronze" :
                key == SigilKey.Azure ? "key_azure" : "key_crimson";
            Pickup(map, root, x, z, "SIGIL / " + key, name).AddComponent<KeyPickup>().Kind = key;
        }
        private static void Weapon(DungeonMap map, Transform root, int x, int z, HeroClass hero)
        {
            var pickup = Pickup(map, root, x, z, "CLASS ARSENAL / " + hero,
                "weapon_pickup", 1.55f);
            pickup.AddComponent<WeaponPickup>().ClassRequired = hero;
        }
        private static void Health(DungeonMap map, Transform root, int x, int z, int amount)
        {
            Pickup(map, root, x, z, "HEALTH ELIXIR", "health_potion")
                .AddComponent<HealthPickup>().Amount = amount;
        }
        private static void Exit(DungeonMap map, Transform root, int x, int z)
        {
            var finish = Pickup(map, root, x, z, "EXIT PORTAL", "exit_portal", 2.2f);
            finish.GetComponent<SphereCollider>().radius = 1f;
            finish.AddComponent<ExitSeal>();
        }
        private static void Brazier(DungeonMap map, Transform root, int x, int z,
            Color color, bool castShadows, string kind)
        {
            string texture = map.Level.episode == 1 ? "deco_brazier" :
                map.Level.episode == 2 ? "deco_mushrooms" : "deco_crystal";
            var art = VisualFactory.Billboard(texture,
                map.World(x, z) + Vector3.up * 1.12f,
                new Vector2(1.3f, 2.15f), "LIGHT / " + kind + " / " + x + "," + z);
            art.transform.SetParent(root, true);
            art.AddComponent<EnvironmentSpriteAnimator>().Initialize(map.Level.episode == 1 ? "brazier" :
                map.Level.episode == 2 ? "mushrooms" : "crystal", 5f);
            var lantern = new GameObject("WARM / POINTLIGHT / FLICKER");
            lantern.transform.SetParent(root, false);
            lantern.transform.position = map.World(x, z) + Vector3.up * 1.75f;
            var light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 2.5f;
            light.range = map.Level.episode == 3 ? 12f : 10.2f;
            // Desktop: two shadowed fires per level; mobile: at most one if quality is Medium+.
            bool allowShadow = castShadows && (!Application.isMobilePlatform ||
                (QualitySettings.GetQualityLevel() > 1 && kind == "Brazier"));
            light.shadows = allowShadow ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = .77f;
            light.shadowBias = .04f;
            light.shadowNormalBias = .25f;
            var flutter = lantern.AddComponent<TorchFlicker>();
            flutter.BaseIntensity = light.intensity;
            flutter.FlickerAmount = map.Level.episode == 1 ? .27f : .16f;
        }
    }
}
