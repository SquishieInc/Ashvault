#!/usr/bin/env python3
"""One-time deterministic v4 authoring: interactive switches, flow patches and optional free-form geometry."""
import json,math
from pathlib import Path
root=Path(__file__).resolve().parents[1]
base=root/'Assets/Ashvault/Resources/Maps'
for path in sorted(base.glob('E??_L??.json')):
    d=json.loads(path.read_text());w,h=d['width'],d['height']
    def char(x,z):return d['rows'][h-1-z][x] if 0<=x<w and 0<=z<h else '#'
    markers={c:next((x,z) for x in range(w) for z in range(h) if char(x,z)==c) for c in 'PBAC'}
    used={(m['x'],m['z']) for k in ('mechanisms','fixtures','decorations','enemies') for m in d.get(k,[])}
    used|=set(markers.values())
    switches=[]
    for c in 'BAC':
        x,z=markers[c]
        candidates=[(x+dx,z+dz) for dx,dz in ((-1,0),(0,-1),(0,1),(1,0)) if char(x+dx,z+dz)=='.']
        candidates.sort(key=lambda p:(p in used,abs(p[0]-markers['P'][0])+abs(p[1]-markers['P'][1])))
        assert candidates,(path,c)
        a,b=candidates[0]
        switches.append(dict(x=a,z=b,targetX=x,targetZ=z,name=f'{c}_rune_lever'))
        used.add((a,b))
    d['switches']=switches
    # Representative flowing-floor visuals in three episode palettes.
    candidates=[(x,z) for z in range(3,h-3) for x in range(3,w-3) if char(x,z)=='.' and (x,z) not in used and
        all(char(x+dx,z+dz)=='.' for dx,dz in ((-1,0),(1,0),(0,-1),(0,1)))]
    candidates.sort(key=lambda p:(p[1]*29+p[0]*17)%97)
    flows=[]
    for loc in candidates:
        if all(abs(loc[0]-u['x'])+abs(loc[1]-u['z'])>7 for u in flows):
            flows.append(dict(x=loc[0],z=loc[1],kind={1:'lava',2:'mire',3:'aether'}[d['episode']],width=.82))
        if len(flows)==3:break
    assert len(flows)==3,path
    d['animatedSurfaces']=flows
    # Each map receives one optional concave raised floor, one shallow pit, a stair,
    # and a reversible secret barrier located off the mandatory main route.
    reserved=set(used)|{(f['x'],f['z']) for f in flows}
    options=[(x,z) for z in range(4,h-4) for x in range(4,w-4) if char(x,z)=='.' and
        all(char(x+dx,z+dz)=='.' for dx in (-2,-1,0,1,2) for dz in (-2,-1,0,1,2)) and
        all(abs(x-rx)+abs(z-rz)>4 for rx,rz in reserved)]
    if not options:
        options=[(x,z) for z in range(4,h-4) for x in range(4,w-4) if char(x,z)=='.' and
            all(char(x+dx,z+dz)=='.' for dx in (-1,0,1) for dz in (-1,0,1)) and
            all(abs(x-rx)+abs(z-rz)>3 for rx,rz in reserved)]
    assert options, (path,'no spacious optional cell')
    # Choose distant optional centers near edges; no required route depends on these.
    options.sort(key=lambda a:(-abs(a[0]-w//2)-abs(a[1]-h//2),a[1],a[0]))
    pt=options[0]
    second=next((a for a in options if abs(a[0]-pt[0])+abs(a[1]-pt[1])>=8), None)
    if second is None:
        # A pit only needs one free cell plus four neighbours. More spacious sectors
        # remain desirable, but never overlap a raised polygon or a required objective.
        far=[(x,z) for z in range(3,h-3) for x in range(3,w-3) if char(x,z)=='.' and
             all(char(x+dx,z+dz)=='.' for dx,dz in ((-1,0),(1,0),(0,-1),(0,1))) and
             abs(x-pt[0])+abs(z-pt[1])>=9 and
             (x,z) not in reserved and
             all(abs(x-rx)+abs(z-rz)>2 for rx,rz in reserved)]
        if not far:
            far=[(x,z) for z in range(3,h-3) for x in range(3,w-3) if char(x,z)=='.' and
                all(char(x+dx,z+dz)=='.' for dx,dz in ((-1,0),(1,0),(0,-1),(0,1))) and
                abs(x-pt[0])+abs(z-pt[1])>=9 and (x,z) not in reserved]
        assert far,(path,'no separate pit cell')
        second=sorted(far,key=lambda a:(-abs(a[0]-pt[0])-abs(a[1]-pt[1]),a[1],a[0]))[0]
    px,pz=pt;qx,qz=second
    def ring(coords):return [{'x':round(a,2),'y':round(b,2)} for a,b in coords]
    d['polygonSectors']=[
       dict(id='Hexagonal_Dais',kind='Platform',floor=.28,ceiling=4.2,
            vertices=ring([(px-1.1,pz-1.0),(px+.6,pz-1.05),(px+1.1,pz-.2),
                           (px+.35,pz+.15),(px+.9,pz+1.1),(px-1.0,pz+.85)])),
       dict(id='Broken_Floor_Pit',kind='Pit',floor=-.38,ceiling=4.8,
            vertices=ring([(qx-.50,qz-.50),(qx+.50,qz-.50),(qx+.50,qz+.50),(qx-.50,qz+.50)]))
    ]
    # Thin secret barrier near the pit, parallel with its north edge; placed in an
    # optional open area so route validation never needs its opening to succeed.
    d['wallSegments']=[dict(id='False_Ashlar_Passage',x1=qx-.47,z1=qz+.55,
          x2=qx+.47,z2=qz+.55,bottom=0,top=3.2,thickness=.18,secret=True,blocksMovement=True)]
    d['stairs']=[dict(id='Ritual_Stairs',x=px-1.0,z=pz-1.7,axisX=0,axisZ=1,steps=3,
                     width=.66,depth=.48,baseHeight=0,rise=.10)]
    path.write_text(json.dumps(d,indent=2,ensure_ascii=False)+'\n')
    print(path.stem,'switches',[(v['x'],v['z']) for v in switches],
       'flows',[(f['x'],f['z']) for f in flows],'polygon center',pt,'pit',second)
