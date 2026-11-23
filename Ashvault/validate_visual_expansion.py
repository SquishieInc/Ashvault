"""Offline static/asset QA for directional atlases, environment authored JSON & visuals.
Not a replacement for compiling/running the project in Unity.
"""
from pathlib import Path
import json
from PIL import Image
r=Path(__file__).resolve().parent
maps=r/'Assets/Ashvault/Resources/Maps'
tex=r/'Assets/Ashvault/Resources/Textures'
base=['enemy_skeleton','enemy_bonerunner','enemy_cultist','enemy_guardian','enemy_hound','enemy_wisp','enemy_shaman','enemy_golem','enemy_stalker','enemy_adept','enemy_sentinel','enemy_summoner','boss_ember','boss_fen','boss_astral']
for name in base:
 p=tex/'Directional'/f'{name}_directions.png'
 assert p.is_file(),p
 with Image.open(p) as im:
  assert im.size==(96*8,144*(11 if name=='enemy_skeleton' else 6)),(p,im.size)
  assert im.mode=='RGBA',p
  assert im.getchannel('A').getextrema()==(0,255),p
props={1:['brazier','banner','sarcophagus','cage'],2:['mushrooms','roots','idol','totem'],3:['crystal','armillary','star_lantern','monolith']}
for name in sum(props.values(),[]):
 p=tex/f'deco_{name}.png'
 assert p.is_file(),p
 with Image.open(p) as im:
  assert im.size==(144,160),p
  assert im.mode=='RGBA',p
 assert (tex/'fx_impact.png').is_file()
counts=dict(machines=0,fixtures=0,props=0)
for path in sorted(maps.glob('E??_L??.json')):
 d=json.loads(path.read_text()); w,h=d['width'],d['height']; episode=d['episode']
 def tile(x,z):return d['rows'][h-1-z][x]
 def valid(x,z):return 0<=x<w and 0<=z<h and tile(x,z)=='.'
 m=d.get('mechanisms',[]);f=d.get('fixtures',[]);p=d.get('decorations',[])
 assert len(m)==4 and {x['kind'] for x in m}=={'Lift','MovingPlatform','Crusher','RuneTrap'},path
 assert len(f)==8 and len(p)==18,path
 used=[]
 for machine in m:
  x,z=machine['x'],machine['z'];assert valid(x,z), (path,machine)
  assert all(valid(x+dx,z+dz) for dx,dz in [(-1,0),(1,0),(0,-1),(0,1)])
  assert machine['period']>2 and machine['travel']>0
  assert (x,z) not in used
  used.append((x,z))
 for fixture in f:
  assert valid(fixture['x'],fixture['z']),(path,fixture)
  assert (fixture['x'],fixture['z']) not in used
  used.append((fixture['x'],fixture['z']))
 for prop in p:
  assert valid(prop['x'],prop['z']), (path,prop)
  assert prop['kind'] in props[episode],(path,prop)
  assert (prop['x'],prop['z']) not in used
  used.append((prop['x'],prop['z']))
 for k,arr in [('machines',m),('fixtures',f),('props',p)]:counts[k]+=len(arr)
 print(f"PASS {path.stem} / 4 machinery / 8 lights / 18 themed sprites")
assert counts=={'machines':36,'fixtures':72,'props':162},counts
print(f"PASS: {len(base)} enemy atlases (8 facings x 6 motion rows), "
      f"12 decorations, {counts['machines']} dynamic sectors, "
      f"{counts['fixtures']} fixtures, {counts['props']} props")
print('WARNING: Unity C# compile, lighting correctness, physics, scene render and iOS profiles NOT tested.')
