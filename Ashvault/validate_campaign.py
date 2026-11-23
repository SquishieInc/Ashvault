"""Read, independently validate and report the exact nine shipped Unity map JSONs.

Usage: python3 validate_campaign.py
This validates data, topology and sprite references, not Unity physics or C# compilation.
"""
from pathlib import Path
import json, re, sys
from build_campaign import validate, CLASSES, EPISODES
ROOT=Path(__file__).parent
MAPS=ROOT/'Assets/Ashvault/Resources/Maps'
SPRITES=ROOT/'Assets/Ashvault/Resources/Textures'
all_files=sorted(MAPS.glob('E??_L??.json'))
assert len(all_files)==9,f'expected 9 maps, got {len(all_files)}'
assert len(CLASSES)==8
count=0; secrets=0; kinds=set(); terrain=set()
for fp in all_files:
  data=json.loads(fp.read_text())
  assert data['id']==fp.stem
  assert len(data['classDrops'])==8
  glyphs=''.join(data['rows'])
  assert all(glyphs.count(c)==1 for c in 'P XbacBACZ'.replace(' ','')),       f'missing or duplicated mandatory key/gate/exit marker in {data["id"]}'
  assert all(glyphs.count(str(i))==1 for i in range(1,9)),       f'missing class gate in {data["id"]}'
  assert len(data['sectors'])>=4
  for section in data['sectors']:
    assert 0<=section['floor']<=0.4,('unsafe sector elevation',section)
    assert 4.0 <= section['ceiling']<=5.8
  e,s=validate(data)
  if data['level']==3:
    bosses=[x for x in data['enemies'] if x['mode']=='Boss']
    assert len(bosses)==1,(fp,bosses)
    assert bosses[0]['kind']==EPISODES[data['episode']-1][3]
  else:
    assert not any(x['mode']=='Boss' for x in data['enemies'])
  for monster in data['enemies']: kinds.add(monster['kind'])
  terrain.add(tuple(data['rows']))
  count+=e; secrets+=s
  print(f"PASS  {data['id']}  {data['title']:<26} "
        f"encounters={e:>2}  secrets={s}  sectors={len(data['sectors'])}")
assert len(terrain)==9,'levels must have distinct authored maps'
assert len(kinds)==15,f'expected 15 enemy archetypes, got {len(kinds)}: {sorted(kinds)}'
# Gather the required image paths from C# catalogs, plus all core objects.
code='\n'.join(p.read_text() for p in (ROOT/'Assets/Ashvault/Scripts').glob('*.cs'))
required=set(re.findall(r'"((?:enemy|boss|weapon|projectile)_[a-z]+)"',code))
required|={'door_runes','wall_bricks','floor_tiles','ceiling_tiles','key_bronze',
           'key_azure','key_crimson','health_potion','exit_portal','menu_backdrop','torch'}
missing=[r for r in sorted(required) if not (SPRITES/f'{r}.png').exists() and not (ROOT/'Assets/Ashvault/Resources/Audio'/f'{r}.wav').exists()]
assert not missing,f'missing sprites: {missing}'
print(f'\nPASS: 9 levels / 3 episodes / 8 classes / {count} authored encounter anchors '
      f'/ {secrets} secrets / 15 enemy archetypes / 3 final bosses / {len(required)} asset references')
print('All levels have a three-key path and access to every secret for each class.')
print('WARNING: Unity compilation, real collision/path movement, iOS performance and builds NOT tested here.')
