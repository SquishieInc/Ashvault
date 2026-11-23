"""Check all class paths remain connected when lift cells are fully raised/impassable.
Conservative offline test, not Unity runtime nav/physics verification.
"""
import json
from collections import deque
from pathlib import Path
R=Path(__file__).resolve().parents[1]/'Assets/Ashvault/Resources/Maps'
valid=0
for p in sorted(R.glob('E??_L??.json')):
 d=json.loads(p.read_text());h=d['height'];w=d['width'];rows=list(reversed(d['rows']))
 cells={(x,z):rows[z][x] for z in range(h) for x in range(w)}
 markers={k:next(pt for pt,c in cells.items() if c==k) for k in 'PXbac'}
 lift=next((m['x'],m['z']) for m in d['mechanisms'] if m['kind']=='Lift')
 def bfs(hero):
  allowed=set('.P+bacwX')|set('BACZs')|{str(hero)}
  allowed.add('P')
  seen={markers['P']};q=deque(seen)
  while q:
   x,z=q.popleft()
   for pt in [(x+1,z),(x-1,z),(x,z+1),(x,z-1)]:
    if pt not in seen and pt!=lift and cells.get(pt,'#') in allowed:
     seen.add(pt);q.append(pt)
  return seen
 for hero in range(1,9):
  visited=bfs(hero)
  goals=[markers[c] for c in 'Xbac']
  goals.append((d['classDrops'][hero-1]['x'],d['classDrops'][hero-1]['z']))
  assert all(pt in visited for pt in goals), (p.stem,hero,goals, lift)
  valid+=1
 print('PASS:',p.stem,'8 classes can reach keys, cache and exit with lift unavailable')
print('PASS',valid,'class/map tests; conservative worst-case lift obstruction')
