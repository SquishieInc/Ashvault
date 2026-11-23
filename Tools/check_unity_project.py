#!/usr/bin/env python3
"""Source-only Ashvault checks. No Unity installation or third-party packages needed.

This catches repository and map-data regressions. It is NOT a Unity compile,
rendering test, collision simulation, or gameplay test.
"""
from collections import Counter, deque
from pathlib import Path
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets" / "Ashvault"
MAPS = ASSETS / "Resources" / "Maps"
REQUIRED_MARKERS = "PXabcBACZ12345678"
BLOCKED = set("#BACZs12345678")


def demand(condition, message):
    if not condition:
        raise ValueError(message)


def read(path):
    demand(path.is_file(), f"Required file missing: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def guid(path):
    found = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", read(path), re.MULTILINE)
    demand(found is not None, f"Missing asset GUID: {path.relative_to(ROOT)}")
    return found.group(1)


def scene_and_packages():
    version = read(ROOT / "ProjectSettings" / "ProjectVersion.txt")
    demand("m_EditorVersion:" in version, "ProjectVersion.txt does not contain an Editor version")
    manifest = json.loads(read(ROOT / "Packages" / "manifest.json"))
    deps = manifest.get("dependencies", {})
    for package in ("com.unity.render-pipelines.universal", "com.unity.inputsystem"):
        demand(package in deps, f"Unity package not declared: {package}")

    scene = ASSETS / "Scenes" / "Prototype.unity"
    scene_guid = guid(Path(str(scene) + ".meta"))
    game_guid = guid(ASSETS / "Scripts" / "AshGame.cs.meta")
    demand(f"guid: {game_guid}" in read(scene), "Prototype.unity does not reference AshGame.cs")
    build = read(ROOT / "ProjectSettings" / "EditorBuildSettings.asset")
    demand("Assets/Ashvault/Scenes/Prototype.unity" in build and
           f"guid: {scene_guid}" in build, "Prototype.unity is not registered for builds")
    print("OK: Editor version, URP/Input System packages and prototype scene GUIDs")


def cell(data, x, z):
    return data["rows"][data["height"] - 1 - z][x]


def accessible_route(data, class_id):
    width, height = data["width"], data["height"]
    start = next(( (x, z) for z in range(height) for x in range(width)
                   if cell(data, x, z) == "P"), None)
    if start is None:
        return False
    keys = set()
    # Each pass collects reachable keys and then revisits newly unlocked doors.
    for _ in range(5):
        found_keys = set(keys)
        visited = {start}
        pending = deque([start])
        while pending:
            x, z = pending.popleft()
            current = cell(data, x, z)
            if current in "abc":
                found_keys.add(current.upper())
            if current == "X" and len(found_keys) == 3:
                return True
            for dx, dz in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, nz = x + dx, z + dz
                if not (0 <= nx < width and 0 <= nz < height):
                    continue
                point = (nx, nz)
                if point in visited:
                    continue
                target = cell(data, nx, nz)
                if target == "#":
                    continue
                if target in "BAC" and target not in found_keys:
                    continue
                if target == "Z" and len(found_keys) != 3:
                    continue
                if target in "12345678" and target != str(class_id + 1):
                    continue
                # Secret walls are assumed discoverable for static route analysis.
                visited.add(point)
                pending.append(point)
        if found_keys == keys:
            return False
        keys = found_keys
    return False


def validate_maps():
    expected = {f"E{episode:02d}_L{level:02d}" for episode in range(1, 4)
                for level in range(1, 4)}
    actual = {p.stem for p in MAPS.glob("*.json")}
    demand(expected == actual, f"Map files differ from the 3x3 campaign: {expected ^ actual}")
    enum_source = read(ASSETS / "Scripts" / "EnemyCatalog.cs")
    enum_match = re.search(r"public enum EnemyArchetype\s*{([^}]*)}", enum_source, re.DOTALL)
    demand(enum_match is not None, "EnemyArchetype enum missing")
    valid_enemies = {name.strip() for name in enum_match.group(1).split(",")}

    route_count = 0
    spawn_count = 0
    for path in sorted(MAPS.glob("*.json")):
        data = json.loads(read(path))
        name = path.stem
        w, h = data["width"], data["height"]
        demand(data.get("id") == name, f"{name}: inconsistent level id")
        demand(w > 2 and h > 2 and len(data["rows"]) == h and
               all(isinstance(row, str) and len(row) == w for row in data["rows"]),
               f"{name}: invalid map dimensions or rows")
        tally = Counter("".join(data["rows"]))
        for mark in REQUIRED_MARKERS:
            demand(tally[mark] == 1, f"{name}: expected exactly one '{mark}', got {tally[mark]}")
        demand(tally["s"] >= 1, f"{name}: no hidden secret walls")

        for spawn in data.get("enemies", []):
            x, z = spawn["x"], spawn["z"]
            demand(spawn["kind"] in valid_enemies, f"{name}: unknown enemy {spawn['kind']}")
            demand(0 <= x < w and 0 <= z < h and cell(data, x, z) not in BLOCKED,
                   f"{name}: enemy spawn on blocked cell ({x},{z})")
            spawn_count += 1
        for drop in data.get("classDrops", []):
            demand(0 <= drop["classId"] < 8, f"{name}: invalid class drop")
            x, z = drop["x"], drop["z"]
            demand(0 <= x < w and 0 <= z < h and cell(data, x, z) not in BLOCKED,
                   f"{name}: class drop on blocked cell ({x},{z})")
        for class_id in range(8):
            demand(accessible_route(data, class_id), f"{name}: class {class_id + 1} cannot reach three-key exit")
            route_count += 1
    print(f"OK: {len(expected)} maps, {route_count} static class routes, {spawn_count} enemy spawns")
    print("NOTE: static route checks do not model Unity colliders, stairs or live sectors")


def check_code_and_layout():
    offenders = [str(p.relative_to(ROOT)) for p in (ASSETS / "Scripts").glob("*.cs")
                 if "GetInstanceID(" in read(p)]
    demand(not offenders, f"Obsolete Unity API GetInstanceID: {', '.join(offenders)}")
    if (ROOT / "Ashvault" / "Assets" / "Ashvault").is_dir():
        print("WARNING: duplicated source tree under Ashvault/Assets/Ashvault; root Assets is authoritative")
    print("OK: no GetInstanceID calls in active Ashvault runtime scripts")


def main():
    scene_and_packages()
    validate_maps()
    check_code_and_layout()
    print("PASS: Ashvault source-only repository audit")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError) as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        sys.exit(1)
