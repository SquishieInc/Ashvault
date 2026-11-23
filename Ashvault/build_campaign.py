"""Generate and validate 9 original Ashvault ASCII-authored level assets.
Run from the repository root: python3 build_campaign.py
Unity consumes the resulting JSON directly; validation is against the same files.
"""
from collections import deque
from pathlib import Path
from PIL import Image, ImageDraw
import json, random

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'Assets/Ashvault/Resources/Maps'
OUT.mkdir(parents=True, exist_ok=True)
DOC = ROOT / 'Docs'
DOC.mkdir(exist_ok=True)
W,H=48,34
CLASSES=['Knight','Ranger','Arcanist','Shadowblade','Cleric','Berserker','Druid','Warlock']
EPISODES=[
 ('THE CINDER KEEP', ['Ashen Threshold','The Iron Reliquary','Throne of the Ember King'],
   ['SkeletonReaver','BoneRunner','HexCultist','IronWarden'], 'EmberLord'),
 ('THE WEEPING FEN', ['Mirewatch Crossing','Rotwater Sanctuary','Heart of the Mire'],
   ['GraveHound','FenWisp','PlagueShaman','MossGolem'], 'FenMatriarch'),
 ('THE ASTRAL CITADEL', ['Broken Observatory','The Starless Chapel','Crown of the Void'],
   ['VoidStalker','AstralAdept','StarSentinel','RiftSummoner'], 'AstralLich')]

def make(e,l):
    rng=random.Random(1200+e*100+l*11)
    a=[['#']*W for _ in range(H)]
    def pt(x,z,v='.'):
        if 0<=x<W and 0<=z<H:a[z][x]=v
    def room(x0,z0,x1,z1):
        for x in range(x0,x1+1):
            for z in range(z0,z1+1):pt(x,z)
    def lx(x0,x1,z):
        for x in range(x0,x1+1):pt(x,z)
    def lz(z0,z1,x):
        for z in range(z0,z1+1):pt(x,z)
    # Common progression spine guarantees a class-agnostic route.
    room(3,4,11,12);room(17,4,27,14);room(17,22,27,30)
    room(34,5,43,15);room(46,7,46,9)
    lx(12,16,8); lz(15,21,22); lx(28,33,9); lx(44,46,8)
    # Class-specific architectural wings; each has an independent lock.
    lz(13,26,2);lx(3,17,26);pt(2,12);pt(2,13,'1')
    lx(28,39,28);lz(16,27,39);pt(28,28,'2')
    lx(10,19,2);lz(3,4,10);lz(3,3,19);pt(10,3,'3')
    lx(27,35,17);lz(15,16,35);lz(14,16,27);pt(28,17,'4')
    lx(28,41,19);lz(14,18,28);pt(28,14);lz(16,18,41);pt(28,19,'5')
    lx(12,26,1);lz(2,4,26);lz(1,3,12);pt(12,4,'6')
    lx(18,40,32);lz(29,31,40);pt(18,31,'7')
    lx(28,33,3);lz(4,5,33);lz(3,4,28);pt(28,5,'8')
    # The class paths connect the main paths, but no key depends on one.
    # Optional side loops and thematic combat rooms vary by episode and map.
    if e==0:
        room(11,17,16,21);lx(12,22,19);room(30,20,35,25);lx(27,30,23)
    elif e==1:
        room(6,17,13,21);lx(9,22,20);room(29,23,34,27);lx(27,29,25)
        room(29,6,32,11);lx(27,29,10)
    else:
        room(12,16,17,20);lx(15,22,18);room(29,20,34,25);lx(27,29,23)
        room(29,11,32,14);lx(27,29,12)
    if l==1:
        room(5,14,10,18);lz(13,14,8)
        room(30,15,33,19);lx(27,30,16)
    elif l==2:
        room(6,20,11,24);lx(11,17,24)
        room(31,27,36,30);lx(27,31,29)
        lx(12,16,11);lz(11,16,16)
    # Hollow-out readable non-critical pillar islands (physics/Nav both block them).
    for x,z in [(19,6),(25,6),(19,12),(25,12),(36,7),(41,7),(36,14),(41,14)]:
        pt(x,z,'#')
    # Reinstate class seals after carving all intersecting episode/side corridors;
    # otherwise optional annexes could accidentally overwrite a class gate.
    for x,z,glyph in [(2,13,'1'),(28,28,'2'),(10,3,'3'),(28,17,'4'),
                      (28,19,'5'),(12,4,'6'),(18,31,'7'),(28,5,'8')]:pt(x,z,glyph)
    # Three interact-to-reveal secret walls and their reward chambers.
    room(0,8,1,10);pt(2,9,'s')
    room(29,25,31,27);pt(28,26,'s')
    room(45,11,46,13);pt(44,12,'s')
    # Keys and locked cells. The final two gates demand crimson and all keys.
    for x,z,c in [(6,8,'P'),(9,10,'b'),(14,8,'B'),(24,27,'a'),
                  (31,9,'A'),(40,12,'c'),(44,8,'C'),(45,8,'Z'),(46,8,'X')]:pt(x,z,c)
    # Healing + upgrade pickups; class-specific drops are specified separately.
    for x,z,c in [(1,9,'+'),(30,26,'+'),(45,12,'+'),(21,9,'w'),
                  (23,24,'+'),(37,10,'+'),(7,6,'+')]:pt(x,z,c)
    # Class routes lead to an additional cache for that class, not a required key.
    drops=[(8,26),(29,28),(17,2),(33,17),(30,19),(13,1),(34,32),(29,3)]
    # For map variants, place stable enemies on traversable cells; spawn modes
    # are data-driven, never randomly distributed in gameplay.
    spawns=[(10,11,0,'Guard'),(18,8,0,'Patrol'),(23,11,1,'Guard'),
      (20,24,1,'Patrol'),(26,27,2,'Guard'),(36,9,0,'Guard'),
      (38,13,2,'Ambush'),(39,10,3,'Guard'),(24,8,1,'Patrol'),
      (19,13,0,'Ambush'),(11,19,2,'Patrol'),(33,23,1,'Ambush')]
    if l>=1:spawns += [(25,23,3,'Guard'),(24,12,2,'Patrol'),(40,13,1,'Ambush')]
    if l==2:spawns += [(21,26,3,'Guard'),(35,12,3,'Guard')]
    kinds=EPISODES[e][2]
    enemies=[{'x':x,'z':z,'kind':kinds[k],'mode':mode} for x,z,k,mode in spawns if a[z][x]!='#']
    if l==2:enemies.append({'x':38,'z':11,'kind':EPISODES[e][3],'mode':'Boss'})
    if e==1: # extra wisp lurking near the fen's hidden inlet
        enemies.append({'x':7,'z':20,'kind':'FenWisp','mode':'Ambush'})
    sectors=[
      dict(name='Entrance',minX=3,minZ=4,maxX=11,maxZ=12,floor=0.0,ceiling=4.6),
      dict(name='Great Hall',minX=17,minZ=4,maxX=27,maxZ=14,floor=0.24,ceiling=5.2),
      dict(name='Sigil Vault',minX=17,minZ=22,maxX=27,maxZ=30,floor=0.22,ceiling=4.45),
      dict(name='Throne',minX=34,minZ=5,maxX=43,maxZ=15,floor=0.3,ceiling=5.6),
    ]
    # Mirror per floor to make route geography differ from level to level.
    # Transform metadata (sector AABBs, drops, enemies) identically.
    mx=(l==1); mz=(l==2)
    def transform(x,z):return (W-1-x if mx else x,H-1-z if mz else z)
    if mx:a=[list(reversed(row)) for row in a]
    if mz:a=list(reversed(a))
    for entry in enemies: entry['x'],entry['z']=transform(entry['x'],entry['z'])
    drops=[dict(classId=i,x=transform(x,z)[0],z=transform(x,z)[1]) for i,(x,z) in enumerate(drops)]
    for s in sectors:
        p=transform(s['minX'],s['minZ']);q=transform(s['maxX'],s['maxZ'])
        s['minX'],s['maxX']=min(p[0],q[0]),max(p[0],q[0]);s['minZ'],s['maxZ']=min(p[1],q[1]),max(p[1],q[1])
    data=dict(id=f'E{e+1:02d}_L{l+1:02d}',episode=e+1,level=l+1,
      title=EPISODES[e][1][l],episodeTitle=EPISODES[e][0],
      intro=f'EPISODE {e+1} / MAP {l+1}: {EPISODES[e][1][l].upper()}',
      width=W,height=H,rows=[''.join(a[z]) for z in reversed(range(H))],
      sectors=sectors,enemies=enemies,classDrops=drops)
    return data

def cell(data,x,z):return data['rows'][data['height']-1-z][x]

def validate(data):
    w,h=data['width'],data['height'];loc={}
    for z in range(h):
      for x in range(w):
        c=cell(data,x,z)
        if c in 'PXbac':loc[c]=(x,z)
    assert all(c in loc for c in 'PXbac'),(data['id'],loc)
    secrets={(x,z) for x in range(w) for z in range(h) if cell(data,x,z)=='s'}
    assert len(secrets)>=3
    for i,kind in enumerate(CLASSES):
      q=deque([(loc['P'][0],loc['P'][1],0)]);seen=set(q);winning=False;unsealed=set()
      while q:
        x,z,keys=q.popleft();v=cell(data,x,z)
        if v=='b':keys|=1
        if v=='a':keys|=2
        if v=='c':keys|=4
        unsealed.add((x,z))
        if (x,z)==loc['X'] and keys==7:winning=True
        for nx,nz in ((x+1,z),(x-1,z),(x,z+1),(x,z-1)):
          if not (0<=nx<w and 0<=nz<h):continue
          v=cell(data,nx,nz)
          if v=='#':continue
          if v=='B' and not keys&1:continue
          if v=='A' and not keys&2:continue
          if v=='C' and not keys&4:continue
          if v=='Z' and keys!=7:continue
          if v in '12345678' and int(v)-1!=i:continue
          n=(nx,nz,keys)
          if n not in seen:seen.add(n);q.append(n)
      assert winning,(data['id'],kind,'unwinnable')
      assert secrets<=unsealed,(data['id'],kind,'secrets unreachable')
      # A class cache must be reachable after opening that class's door.
      drop=data['classDrops'][i]
      assert (drop['x'],drop['z']) in unsealed,(data['id'],kind,'class cache inaccessible')
    for e in data['enemies']:
      assert cell(data,e['x'],e['z']) not in '#sBACZ12345678',(data['id'],'spawn in blocked cell',e)
    return len(data['enemies']),len(secrets)

def draw(data):
    size=13
    im=Image.new('RGB',(W*size,H*size+35),(17,17,23));d=ImageDraw.Draw(im)
    palette={'#':(48,44,56),'.':(121,104,91),'s':(185,78,180),'P':(87,240,142),'X':(245,246,164),
      'B':(230,172,85),'A':(87,178,238),'C':(230,91,72),'Z':(242,60,65),
      'b':(225,172,86),'a':(87,178,238),'c':(228,94,71),'+':(70,190,123),'w':(235,214,105)}
    for rz,row in enumerate(data['rows']):
      for x,c in enumerate(row):
        tint=palette.get(c,(126,89,211) if c in '12345678' else (121,104,91))
        d.rectangle((x*size,rz*size,x*size+size-2,rz*size+size-2),fill=tint)
        if c in '12345678':d.text((x*size+2,rz*size+1),c,fill='white')
    for e in data['enemies']:
      x=e['x'];z=e['z'];rz=H-1-z
      t=(238,80,66) if e['mode']=='Boss' else (160,179,219)
      d.ellipse((x*size+3,rz*size+3,x*size+9,rz*size+9),fill=t)
    d.text((8,H*size+8),data['id']+' — '+data['title']+'  |  pink=secrets  purple=class gates',fill=(224,212,184))
    im.save(DOC/(data['id']+'_overview.png'))
    return im

if __name__=='__main__':
  all_maps=[];images=[];stats=[]
  for e in range(3):
    for l in range(3):
      level=make(e,l);enemies,secrets=validate(level)
      (OUT/(level['id']+'.json')).write_text(json.dumps(level,indent=2)+'\n',encoding='utf-8')
      all_maps.append(level)
      images.append(draw(level));stats.append((level['id'],enemies,secrets))
  tw,th=images[0].size
  sheet=Image.new('RGB',(tw*3,th*3),(18,17,22))
  for i,im in enumerate(images):sheet.paste(im,((i%3)*tw,(i//3)*th))
  sheet.save(DOC/'campaign_overview.png')
  print('Validated',len(all_maps),'level assets, all',len(CLASSES),'classes, keys, secrets, boss maps')
  for row in stats:print('%s enemies=%d secrets=%d'%row)
