#!/usr/bin/env python3
"""Static data/content/route QA, not a Unity C# compile or physics/rendering test."""
from pathlib import Path
from collections import deque
from PIL import Image
from shapely.geometry import Polygon, LineString, Point
import json, wave, math
root=Path(__file__).resolve().parent
maps=root/'Assets/Ashvault/Resources/Maps'
tex=root/'Assets/Ashvault/Resources/Textures'
audio=root/'Assets/Ashvault/Resources/Audio'
counts={'switches':0,'flows':0,'polygonSectors':0,'stairs':0,'secretSegments':0,'routeChecks':0}
for path in sorted(maps.glob('E??_L??.json')):
 d=json.loads(path.read_text());w=d['width'];h=d['height'];rows=list(reversed(d['rows']))
 cell=lambda x,z: rows[z][x] if 0<=x<w and 0<=z<h else '#'
 polygons=[]
 for shape in d['polygonSectors']:
  verts=[(v['x'],v['y']) for v in shape['vertices']]
  poly=Polygon(verts)
  assert poly.is_valid and poly.area>0.2,(path,shape)
  polygons.append((poly,shape))
 assert len(polygons)==2,path
 assert not polygons[0][0].intersects(polygons[1][0]),(path,'platform/pit overlap')
 assert {s['kind'] for _,s in polygons}=={'Platform','Pit'}
 for p,s in polygons:
  for xx,zz in p.exterior.coords:
   assert 0 <= xx < w and 0 <= zz < h,(path,xx,zz)
 for stairs in d['stairs']:
  assert 0<stairs['rise']<=0.3 and 0<stairs['steps']<=15 and stairs['depth']>0.1
 assert len(d['stairs'])==1
 walls=d['wallSegments'];assert len(walls)==1 and walls[0]['secret']
 for door in d['switches']:
  assert cell(door['x'],door['z'])=='.',(path,door)
  assert cell(door['targetX'],door['targetZ']) in 'BAC',(path,door)
  assert abs(door['targetX']-door['x'])+abs(door['targetZ']-door['z'])==1
 assert len(d['switches'])==3
 assert len(d['animatedSurfaces'])==3
 for flow in d['animatedSurfaces']:
  assert cell(flow['x'],flow['z'])=='.',(path,flow)
  assert flow['kind']=={1:'lava',2:'mire',3:'aether'}[d['episode']]
 def surface(x,z):
  height=0
  for sector in d['sectors']:
   if sector['minX']<=x<=sector['maxX'] and sector['minZ']<=z<=sector['maxZ']:
    height=max(height,sector['floor'])
  for poly,shape in polygons:
   if shape['kind']!='Solid' and poly.contains(Point(x,z)):height=shape['floor']
  for stair in d['stairs']:
   dx,dz=stair['axisX'],stair['axisZ'];mag=math.hypot(dx,dz);dx/=mag;dz/=mag
   rx,rz=x-stair['x'],z-stair['z'];fw=rx*dx+rz*dz;side=abs(rx*dz-rz*dx)
   if 0<=fw<stair['steps']*stair['depth'] and side<=stair['width']*.5:
    height=max(height,stair['baseHeight']+(math.floor(fw/stair['depth'])+1)*stair['rise'])
  return height
 def open_cell(x,z,hero):
  c=cell(x,z)
  if c=='#':return False
  if c in '12345678' and c !=str(hero):return False
  return True
 def blocked(a,b):
  ab=LineString([a,b])
  for s in walls:
   if not s['blocksMovement']:continue
   bar=LineString([(s['x1'],s['z1']),(s['x2'],s['z2'])])
   if ab.crosses(bar):return True
  return False
 start=next((x,z) for z in range(h) for x in range(w) if cell(x,z)=='P')
 exit=next((x,z) for z in range(h) for x in range(w) if cell(x,z)=='X')
 goals=[(x,z) for z in range(h) for x in range(w) if cell(x,z) in 'bacX']
 for hero in range(1,9):
  seen={start};q=deque([start])
  while q:
   x,z=q.popleft()
   for nx,nz in ((x+1,z),(x-1,z),(x,z+1),(x,z-1)):
    if (nx,nz) in seen or not open_cell(nx,nz,hero) or blocked((x,z),(nx,nz)):
     continue
    if abs(surface(x,z)-surface(nx,nz))>.43:continue
    seen.add((nx,nz));q.append((nx,nz))
  cache=d['classDrops'][hero-1]
  assert all(g in seen for g in goals), (path.stem,hero,'key/exit blocked',set(goals)-seen)
  assert (cache['x'],cache['z']) in seen,(path.stem,hero,'cache blocked')
  counts['routeChecks']+=1
 for k,num in (('switches',3),('flows',3),('polygonSectors',2),('stairs',1),('secretSegments',1)):
  counts[k]+=num
 print('PASS',path.stem,'route 8/8, polygon triangulation geometry, 3 switches, 3 flows, 1 stair, 1 secret segment')
for name in ['enemy_skeleton']:
 with Image.open(tex/'Directional'/f'{name}_directions.png') as im:
  assert im.size==(768,1584) and im.mode=='RGBA'
  assert im.getchannel('A').getextrema()==(0,255)
for art in ('brazier','mushrooms','crystal','star_lantern'):
 with Image.open(tex/'Animated'/f'{art}_frames.png') as im:
  assert im.size==(512,128) and im.mode=='RGBA'
for fx in ('blood','scorch','crack','rune_impact'):
 with Image.open(tex/'Decals'/f'{fx}.png') as im:
  assert im.size==(96,96) and im.mode=='RGBA'
assert len(list(audio.glob('*.wav')))==20
for sound in audio.glob('*.wav'):
 with wave.open(str(sound),'rb') as w:
  assert w.getframerate()==22050 and w.getnchannels()==1 and w.getsampwidth()==2
  assert w.getnframes()>2500
assert counts=={'switches':27,'flows':27,'polygonSectors':18,'stairs':9,'secretSegments':9,'routeChecks':72}
print('PASS',counts)
print('PASS artwork: 8x11 Skeleton Reaver atlas + 4 animated props + 4 decals + 20 original WAV sounds')
print('WARNING: no Unity Editor C# compilation, engine physics, shadow rendering, device audio or performance tests were performed.')
