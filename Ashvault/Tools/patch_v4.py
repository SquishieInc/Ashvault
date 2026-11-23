from pathlib import Path
root=Path(__file__).resolve().parents[1]
scripts=root/'Assets/Ashvault/Scripts'
def patch(name, old,new):
 p=scripts/name; s=p.read_text(); n=s.count(old)
 if n!=1: raise RuntimeError(f'{name}: expected 1 target, got {n}: {old[:75]}')
 p.write_text(s.replace(old,new))

# JSON schemas for polygonal geometry and switches.
patch('CampaignMap.cs', '    [Serializable]\n    public sealed class LevelBlueprint', '''    [Serializable]
    public sealed class PolygonSectorData
    {
        public string id = "sector", kind = "Platform";  // Platform, Solid or Pit
        public Vector2[] vertices;
        public float floor = .25f, ceiling = 4f;
    }

    [Serializable]
    public sealed class StaircaseData
    {
        public string id = "stairs";
        public float x, z, axisX = 0f, axisZ = 1f;
        public int steps = 4;
        public float width = 1f, depth = .55f, baseHeight = 0f, rise = .25f;
    }

    [Serializable]
    public sealed class WallSegmentData
    {
        public string id = "wall";
        public float x1, z1, x2, z2, bottom = 0f, top = 4f, thickness = .25f;
        public bool secret = false, blocksMovement = true;
    }

    [Serializable]
    public sealed class DoorSwitchData
    {
        public int x,z,targetX,targetZ;
        public string name;
    }

    [Serializable]
    public sealed class AnimatedSurfaceData
    {
        public int x, z;
        public string kind;
        public float width = .9f;
    }

    [Serializable]
    public sealed class LevelBlueprint''')
patch('CampaignMap.cs','        public MechanismData[] mechanisms;','''        public MechanismData[] mechanisms;
        public PolygonSectorData[] polygonSectors;
        public WallSegmentData[] wallSegments;
        public StaircaseData[] stairs;
        public DoorSwitchData[] switches;
        public AnimatedSurfaceData[] animatedSurfaces;''')
patch('CampaignMap.cs','        private readonly bool[,] hasDynamicElevation;','''        private readonly bool[,] hasDynamicElevation;
        private readonly HashSet<int> openedPolygonWalls = new HashSet<int>();''')
patch('CampaignMap.cs','''        public float Surface(int x, int z) => Inside(x, z) ?
            (hasDynamicElevation[x, z] ? dynamicElevations[x, z] : elevations[x, z]) : 0f;''','''        public float Surface(int x, int z)
        {
            if (!Inside(x,z)) return 0f;
            if (hasDynamicElevation[x,z]) return dynamicElevations[x,z];
            float height = elevations[x,z];
            if (Level.polygonSectors != null)
                foreach (var poly in Level.polygonSectors)
                    if (poly.kind != "Solid" && SectorPolygon.Contains(poly.vertices, new Vector2(x,z)))
                        height = poly.floor;
            if (Level.stairs != null)
                foreach (var stair in Level.stairs)
                {
                    Vector2 d = new Vector2(stair.axisX,stair.axisZ).normalized;
                    if (d.sqrMagnitude <.5f) d=Vector2.up;
                    Vector2 relative = new Vector2(x-stair.x,z-stair.z);
                    float forward=Vector2.Dot(relative,d);
                    float side=Mathf.Abs(relative.x*d.y-relative.y*d.x);
                    if (forward >= 0 && forward < Mathf.Max(1,stair.steps)*stair.depth &&
                        side <= stair.width*.5f)
                        height = Mathf.Max(height,stair.baseHeight+
                            (Mathf.FloorToInt(forward/Mathf.Max(.1f,stair.depth))+1)*stair.rise);
                }
            return height;
        }
        public bool IsPit(int x,int z)
        {
            if(Level.polygonSectors==null)return false;
            foreach(var p in Level.polygonSectors)
                if(p.kind=="Pit" && SectorPolygon.Contains(p.vertices,new Vector2(x,z)))return true;
            return false;
        }
        public void UnsealPolygonWall(int index) => openedPolygonWalls.Add(index);''')
patch('CampaignMap.cs','''            if (c == '#') return false;
            if (c == 's' ''','''            if (c == '#') return false;
            if (Level.polygonSectors != null)
                foreach (var p in Level.polygonSectors)
                    if (p.kind == "Solid" && SectorPolygon.Contains(p.vertices,new Vector2(x,z))) return false;
            if (c == 's' ''')
patch('CampaignMap.cs','''        public bool CanStep(Vector2Int from, Vector2Int to)
        {
            return IsOpen(to.x, to.y) &&
                Mathf.Abs(Surface(from.x, from.y) - Surface(to.x, to.y)) <= 0.43f;
        }''','''        public bool CanStep(Vector2Int from, Vector2Int to)
        {
            if (!IsOpen(to.x,to.y) || !Inside(from.x,from.y) ||
                Mathf.Abs(Surface(from.x,from.y)-Surface(to.x,to.y))>.43f) return false;
            if (Level.wallSegments != null)
                for (int i=0;i<Level.wallSegments.Length;i++)
                {
                    var w = Level.wallSegments[i];
                    if (!w.blocksMovement || openedPolygonWalls.Contains(i)) continue;
                    if (SectorPolygon.SegmentsCross(new Vector2(from.x,from.y),new Vector2(to.x,to.y),
                        new Vector2(w.x1,w.z1),new Vector2(w.x2,w.z2)))return false;
                }
            return true;
        }''')

# Geometry integration. Real holes for pit cells approximated at tile centers in these legacy grid maps.
patch('DungeonBuilder.cs','''            Cube(geometry, "Base stone bed", new Vector3((map.Width - 1) * Tile * 0.5f,
                -0.18f, (map.Height - 1) * Tile * 0.5f),
                new Vector3(map.Width * Tile, 0.36f, map.Height * Tile), floor);''','''            if (map.Level.polygonSectors != null &&
                System.Array.Exists(map.Level.polygonSectors, p => p.kind == "Pit"))
                BuildTileFloorWithPitCutouts(geometry, map, floor);
            else
                Cube(geometry, "Base stone bed", new Vector3((map.Width - 1) * Tile * 0.5f,
                    -0.18f, (map.Height - 1) * Tile * 0.5f),
                    new Vector3(map.Width * Tile, 0.36f, map.Height * Tile), floor);''')
patch('DungeonBuilder.cs','''            // Authored hazards, moving sectors and non-colliding, theme-specific 2D props.''','''            // Independent polygon-sector layer: supports concave floors, angled walls,
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
            // Authored hazards, moving sectors and non-colliding, theme-specific 2D props.''')
patch('DungeonBuilder.cs','''            var world = new GameObject("ARCHITECTURE / SECTORS");''','''            AshAudio.Ambient(root, map.Level.episode);
            var world = new GameObject("ARCHITECTURE / SECTORS");''')
patch('DungeonBuilder.cs','''        private static (Color wall, Color accent, Color floor, Color ceiling, Color fire) GetTheme''','''        private static void BuildTileFloorWithPitCutouts(Transform parent,DungeonMap map,Material material)
        {
            // Tile-aligned cutouts are intentional for compatibility with legacy 1993-style
            // square maps. A future boolean polygon mesh is required for exact rim cutouts.
            var vertices=new System.Collections.Generic.List<Vector3>();
            var uv=new System.Collections.Generic.List<Vector2>();
            var triangles=new System.Collections.Generic.List<int>();
            for(int x=0;x<map.Width;x++)for(int z=0;z<map.Height;z++)
            {
                if (map.Cell(x,z)=='#' || map.IsPit(x,z))continue;
                float ax=(x-.5f)*Tile,bx=(x+.5f)*Tile;
                float az=(z-.5f)*Tile,bz=(z+.5f)*Tile;
                int n=vertices.Count;
                vertices.Add(new Vector3(ax,0f,az));vertices.Add(new Vector3(bx,0f,az));
                vertices.Add(new Vector3(ax,0f,bz));vertices.Add(new Vector3(bx,0f,bz));
                uv.Add(new Vector2(x,z));uv.Add(new Vector2(x+1,z));
                uv.Add(new Vector2(x,z+1));uv.Add(new Vector2(x+1,z+1));
                triangles.Add(n);triangles.Add(n+2);triangles.Add(n+1);
                triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n+3);
            }
            var mesh=new Mesh{ name="Tile floor with pit cutouts" };
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            var floor=new GameObject("FLOOR / PIT-CUT TILE MESH");floor.transform.SetParent(parent,false);
            floor.AddComponent<MeshFilter>().sharedMesh=mesh;
            floor.AddComponent<MeshRenderer>().sharedMaterial=material;
            floor.AddComponent<MeshCollider>().sharedMesh=mesh;
        }

        private static (Color wall, Color accent, Color floor, Color ceiling, Color fire) GetTheme''')
# animated decor
patch('DungeonBuilder.cs','''            art.transform.SetParent(root, true);
            var lantern = new GameObject("WARM / POINTLIGHT / FLICKER");''','''            art.transform.SetParent(root, true);
            art.AddComponent<EnvironmentSpriteAnimator>().Initialize(map.Level.episode == 1 ? "brazier" :
                map.Level.episode == 2 ? "mushrooms" : "crystal", 5f);
            var lantern = new GameObject("WARM / POINTLIGHT / FLICKER");''')
patch('LightingAndDecoration.cs','''            if (data.glow)
            {''','''            if (data.kind == "brazier" || data.kind == "mushrooms" ||
                data.kind == "crystal" || data.kind == "star_lantern")
                go.AddComponent<EnvironmentSpriteAnimator>().Initialize(data.kind, 4.2f);
            if (data.glow)
            {''')
# Combat and audio hooks
patch('AshPlayer.cs','''            Owner.AlertEnemies(transform.position, 20f);
            if (weapon.Melee)''','''            Owner.AlertEnemies(transform.position, 20f);
            AshAudio.OneShot(weapon.Melee ? "melee_swing" : "bolt_cast", View.transform.position, .38f);
            if (weapon.Melee)''')
patch('AshPlayer.cs','''            damageFeedbackUntil = Time.unscaledTime + .28f;
            if (Health <= 0)''','''            damageFeedbackUntil = Time.unscaledTime + .28f;
            AshAudio.OneShot("player_hurt",transform.position,.55f);
            if (Health <= 0)''')
patch('Projectile.cs','''                Destroy(gameObject);
                return;
            }
            transform.position += direction * travel;''','''                AshAudio.OneShot("weapon_impact", hit.point, .43f);
                if (hit.collider.GetComponentInParent<EnemyAI>() == null &&
                    hit.collider.GetComponentInParent<AshPlayer>() == null)
                    ImpactDecal.Place(hostile ? "rune_impact" : "scorch",hit.point,hit.normal,.45f);
                Destroy(gameObject);
                return;
            }
            transform.position += direction * travel;''')
patch('SectorMachinery.cs','''            if (kind == MechanismKind.Crusher && normalized > .62f) DamageCrushed();''','''            if (kind == MechanismKind.Crusher && normalized > .62f) DamageCrushed();''')
patch('SectorMachinery.cs','''                game.Player.TakeDamage(Mathf.Max(4, data.damage));
                CombatFeedback.Spawn''','''                game.Player.TakeDamage(Mathf.Max(4, data.damage));
                AshAudio.OneShot(kind == MechanismKind.Crusher ? "crusher_impact" : "rune_trap",
                    transform.position,.45f);
                ImpactDecal.AtFeet("crack",game.Player.transform.position,.8f);
                CombatFeedback.Spawn''')
# enemy attack animation delays and feedback.
patch('EnemyAI.cs','''        private float alertUntil, nextAttack, nextPath, nextPatrol, fallSpeed, windupEnds, chargeEnds, flashUntil;''','''        private float alertUntil, nextAttack, nextPath, nextPatrol, fallSpeed, windupEnds, chargeEnds, flashUntil;
        private float queuedImpactTime;
        private Vector3 queuedTarget;
        private float queuedDistance;
        private bool queuedAttack;''')
patch('EnemyAI.cs','''            float now = Time.time;
            if (sprite != null''','''            float now = Time.time;
            if (queuedAttack && now >= queuedImpactTime)
            {
                queuedAttack = false;
                ApplyAttack(queuedTarget,queuedDistance);
            }
            if (sprite != null''')
patch('EnemyAI.cs','''            CombatFeedback.Spawn(transform.position + Vector3.up * (Spec.VisualHeight * .65f),
                Spec.Tint, IsBoss);
            attackCounter++;
            switch (Spec.Attack)''','''            CombatFeedback.Spawn(transform.position + Vector3.up * (Spec.VisualHeight * .65f),
                Spec.Tint, IsBoss);
            queuedTarget=target;queuedDistance=distance;
            queuedImpactTime=Time.time + (IsBoss ? .34f : .20f);
            queuedAttack=true;
            AshAudio.OneShot(Spec.Attack == EnemyAttack.Claw || Spec.Attack == EnemyAttack.Charge ?
                "reaver_attack" : "bolt_cast",transform.position,IsBoss ? .85f : .55f);
        }

        private void ApplyAttack(Vector3 target,float distance)
        {
            if (Health <= 0 || game == null || game.Phase != GamePhase.Playing || game.Player == null)return;
            attackCounter++;
            switch (Spec.Attack)''')
patch('EnemyAI.cs','''            if (directional != null) directional.PlayHurt();
            CombatFeedback.Spawn''','''            if (directional != null) directional.PlayHurt();
            AshAudio.OneShot(Health <= 0 ? (IsBoss ? "boss_death" : "enemy_death") : "enemy_hurt",
                transform.position,IsBoss ? 1f : .56f);
            ImpactDecal.AtFeet(Health <= 0 ? "blood" : "crack",transform.position,IsBoss ? 1.5f : .68f);
            CombatFeedback.Spawn''')
patch('EnemyAI.cs','''                Awareness = EnemyAwareness.Dead;
                IsVisuallyMoving = false;''','''                Awareness = EnemyAwareness.Dead;
                queuedAttack = false;
                IsVisuallyMoving = false;''')
patch('EnemyAI.cs','''                Destroy(gameObject, 1.35f);''','''                Destroy(gameObject, IsBoss ? 3.25f : 2.1f);''')
print('Patched base runtime')
