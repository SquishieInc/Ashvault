"""Deterministically augment the nine campaign JSON maps with machinery, fixtures and decorations.
Safe to re-run; replaces only 'mechanisms', 'fixtures', 'decorations' fields.
Run after build_campaign.py if regenerating the level layout.
"""
import json,random
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'Assets/Ashvault/Resources/Maps'
KINDS={1:['brazier','banner','sarcophagus','cage'],2:['mushrooms','roots','idol','totem'],3:['crystal','armillary','star_lantern','monolith']}
GLOW={'brazier','cage','mushrooms','crystal','star_lantern'}
SIZES={'brazier':(1.8,2.5),'banner':(1.6,3.6),'sarcophagus':(2.9,1.55),'cage':(1.75,2.9),
       'mushrooms':(2.0,1.9),'roots':(2.6,2.2),'idol':(2.25,2.8),'totem':(1.8,2.8),
       'crystal':(1.9,2.65),'armillary':(2.2,2.5),'star_lantern':(1.75,2.6),'monolith':(2.15,3.35)}

def access(level,x,z): return level['rows'][level['height']-1-z][x]
def run():
 for p in sorted(ROOT.glob('E??_L??.json')):
    d=json.loads(p.read_text());randomizer=random.Random(d['episode']*17891+d['level']*721)
    w,h=d['width'],d['height']
    reserved=set((s['x'],s['z']) for s in d.get('enemies',[]))
    reserved.update((s['x'],s['z']) for s in d.get('classDrops',[]))
    for z in range(h):
        for x in range(w):
            if access(d,x,z) in 'P+bacwXBAZCs12345678':reserved.add((x,z))
    def safe(x,z,margin=0):
        return 2<=x<w-2 and 2<=z<h-2 and access(d,x,z)=='.' and all(
            abs(x-rx)+abs(z-rz)>margin for rx,rz in reserved)
    candidates=[(x,z) for z in range(2,h-2) for x in range(2,w-2) if safe(x,z,2)]
    randomizer.shuffle(candidates)
    def clear3(x,z):return all(access(d,x+dx,z+dz)=='.' for dx,dz in [(0,0),(-1,0),(1,0),(0,-1),(0,1)])
    mechanics=[];blocked=[]
    for kind in ['Lift','MovingPlatform','Crusher','RuneTrap']:
        for x,z in candidates:
            if any(abs(x-bx)+abs(z-bz)<6 for bx,bz in blocked):continue
            if not clear3(x,z): continue
            # moving platforms move within a small alcove; these placements do not alter key-door paths
            delta=1.25 if kind=='MovingPlatform' else (.8 if kind=='Lift' else 3.15 if kind=='Crusher' else .1)
            entry=dict(kind=kind,x=x,z=z,travel=delta,period=round(randomizer.uniform(4.3,7.3),2),
                       phase=round(randomizer.random(),3),damage=21 if kind=='Crusher' else 12 if kind=='RuneTrap' else 0,
                       axisX=1.0 if kind=='MovingPlatform' else 0.0,axisZ=0.0)
            mechanics.append(entry);blocked.append((x,z));reserved.add((x,z));break
        else: raise RuntimeError('Cannot place '+kind+' on '+p.name)
    fixtures=[]
    for x,z in candidates:
        if len(fixtures)>=8:break
        if any(abs(x-f['x'])+abs(z-f['z'])<6 for f in fixtures):continue
        if any(abs(x-bx)+abs(z-bz)<3 for bx,bz in blocked):continue
        fixture=dict(x=x,z=z,kind=['Brazier','WallTorch','Lantern'][len(fixtures)%3],shadows=len(fixtures)<2)
        fixtures.append(fixture);reserved.add((x,z))
    if len(fixtures)<5:raise RuntimeError('Too few fixtures '+p.name)
    decorations=[]
    # Prioritize context near walls, but keep game objectives and machinery clear.
    decorCandidates=[(x,z) for x,z in candidates if any(access(d,x+dx,z+dz)=='#' for dx,dz in [(1,0),(-1,0),(0,1),(0,-1)])]
    decorCandidates+=candidates
    for x,z in decorCandidates:
        if len(decorations)>=18:break
        if (x,z) in reserved or any(abs(x-f['x'])+abs(z-f['z'])<2 for f in fixtures): continue
        if any(abs(x-o['x'])+abs(z-o['z'])<3 for o in decorations): continue
        if any(abs(x-bx)+abs(z-bz)<3 for bx,bz in blocked):continue
        kind=KINDS[d['episode']][len(decorations)%4]
        width,height=SIZES[kind]
        decorations.append(dict(x=x,z=z,kind=kind,width=width,height=height,glow=kind in GLOW))
        reserved.add((x,z))
    if len(decorations)<12:raise RuntimeError('Too few props '+p.name)
    d['mechanisms']=mechanics;d['fixtures']=fixtures;d['decorations']=decorations
    p.write_text(json.dumps(d,indent=2)+'\n')
    print(p.stem,'4 mechanisms',len(fixtures),'lights',len(decorations),'props')
if __name__=='__main__':run()
