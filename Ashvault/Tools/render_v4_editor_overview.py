#!/usr/bin/env python3
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
import json
root=Path(__file__).resolve().parents[1]
try:font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',14)
except:font=None
try:small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',10)
except:small=None
S=6;TW=48*S;TH=34*S;B=22;G=16
atlas=Image.new('RGB',(TW*3+G*4,(TH+B)*3+G*4+39),(16,19,26))
draw=ImageDraw.Draw(atlas)
for i,path in enumerate(sorted((root/'Assets/Ashvault/Resources/Maps').glob('E??_L??.json'))):
 d=json.loads(path.read_text());col=i%3;row=i//3;x0=G+col*(TW+G);y0=G+row*(TH+B+G)
 draw.rounded_rectangle((x0-3,y0-3,x0+TW+3,y0+B+TH+3),radius=5,outline=(77,85,97),width=1,fill=(29,32,41))
 draw.text((x0+8,y0+3),f"{d['id']}   {d['title']}",fill=(222,230,232),font=small)
 def pos(x,z):return (x0+(x+.5)*S,y0+B+(d['height']-z-.5)*S)
 for z in range(d['height']):
  for x in range(d['width']):
   c=d['rows'][d['height']-z-1][x]
   color=(52,55,63) if c=='#' else ((89,66,100) if c=='s' else ((94,74,60) if c in 'BACZ' else (84,93,91)))
   draw.rectangle((x0+x*S,y0+B+(d['height']-z-1)*S,x0+(x+1)*S-1,y0+B+(d['height']-z)*S-1),fill=color)
 for polygon in d['polygonSectors']:
  ring=[pos(p['x'],p['y']) for p in polygon['vertices']]
  color=(255,97,95) if polygon['kind']=='Pit' else (71,218,236)
  draw.line(ring+[ring[0]],fill=color,width=3,joint='curve')
  for p in ring:draw.ellipse((p[0]-2,p[1]-2,p[0]+2,p[1]+2),fill=color)
 for w in d['wallSegments']:
  draw.line((pos(w['x1'],w['z1']),pos(w['x2'],w['z2'])),fill=(222,83,248),width=4)
 for s in d['switches']:
  px,py=pos(s['x'],s['z']);draw.rectangle((px-3,py-3,px+3,py+3),fill=(250,216,99))
 for flow in d['animatedSurfaces']:
  px,py=pos(flow['x'],flow['z']);draw.ellipse((px-4,py-4,px+4,py+4),fill=(255,166,58))
 for s in d['stairs']:
  px,py=pos(s['x'],s['z']);draw.polygon([(px-4,py+3),(px+4,py+3),(px,py-5)],fill=(98,151,247))
legend_y=TH*3+B*3+G*4+5
x=16
for c,t in [((71,218,236),'Polygon'),((255,97,95),'Pit'),((222,83,248),'Secret segment'),((98,151,247),'Stairs'),((250,216,99),'Switch'),((255,166,58),'Flow')]:
 draw.rectangle((x,legend_y,x+9,legend_y+9),fill=c)
 draw.text((x+13,legend_y-3),t,font=small,fill=(235,238,241))
 x+=len(t)*7+30
p=root/'Docs/v4_polygon_sector_atlas.png';atlas.save(p,optimize=True);print(p,atlas.size)
