from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1]
paths=sorted((root/'Assets/Ashvault/Resources/Maps').glob('E??_L??.json'))
font_path='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
f=ImageFont.truetype(font_path,14);ft=ImageFont.truetype(font_path,21)
color={'#':(52,48,59),'.':(122,111,111),'P':(55,188,229),'X':(238,197,80),
       'B':(180,101,57),'A':(77,147,230),'C':(216,78,73),'Z':(227,224,184),'s':(168,121,161),
       '+':(97,169,87),'w':(194,185,92),'b':(180,101,57),'a':(77,147,230),'c':(216,78,73)}
mcolor={'Lift':(44,197,208),'MovingPlatform':(93,226,134),'Crusher':(240,74,83),'RuneTrap':(244,153,37)}
def map_panel(d,scale=7):
 w,h=d['width'],d['height'];im=Image.new('RGB',(w*scale,h*scale),(24,23,29));draw=ImageDraw.Draw(im)
 for iz,row in enumerate(d['rows']):
  for x,ch in enumerate(row):
   bg=color.get(ch,(98,77,108) if ch.isdigit() else (111,105,113))
   draw.rectangle((x*scale,iz*scale,x*scale+scale-1,iz*scale+scale-1),fill=bg)
 for p in d['decorations']:
  x,y=p['x'],h-1-p['z'];cx=x*scale+scale//2;cy=y*scale+scale//2
  draw.ellipse((cx-2,cy-2,cx+2,cy+2),fill=(236,198,132))
 for light in d['fixtures']:
  x,y=light['x'],h-1-light['z'];cx=x*scale+scale//2;cy=y*scale+scale//2
  draw.ellipse((cx-3,cy-3,cx+3,cy+3),fill=(249,237,104),outline=(255,255,255))
 for machine in d['mechanisms']:
  x,y=machine['x'],h-1-machine['z'];cx=x*scale+scale//2;cy=y*scale+scale//2
  draw.rectangle((cx-4,cy-4,cx+4,cy+4),fill=mcolor[machine['kind']],outline=(10,10,15),width=1)
 return im
W,H=1200,1050;im=Image.new('RGB',(W,H),(19,19,28));draw=ImageDraw.Draw(im)
draw.text((35,18),'ASHVAULT  /  DYNAMIC SECTORS + ENVIRONMENT ATLAS',font=ft,fill=(240,223,205))
for idx,path in enumerate(paths):
 d=json.loads(path.read_text());col=idx%3;row=idx//3
 x=40+col*395;y=75+row*300
 draw.rounded_rectangle((x-12,y-30,x+362,y+260),radius=9,fill=(33,32,43))
 draw.text((x,y-24),d['id']+'  '+d['title'],font=f,fill=(242,222,197))
 panel=map_panel(d);panel.thumbnail((350,245),Image.Resampling.NEAREST)
 im.paste(panel,(x,y+3))
# legend bottom
ly=976
items=[('Lift',mcolor['Lift']),('Moving platform',mcolor['MovingPlatform']),('Crusher',mcolor['Crusher']),('Rune trap',mcolor['RuneTrap']),('Light',(249,237,104)),('Prop',(236,198,132))]
x=38
for name,c in items:
 draw.rectangle((x,ly,x+14,ly+14),fill=c);draw.text((x+21,ly-1),name,font=f,fill=(230,226,222));x+=200 if name=='Moving platform' else 160
im.save(root/'Docs'/'environment_map_atlas.png')
first=json.loads(paths[0].read_text());single=map_panel(first,scale=15)
single.save(root/'Docs'/'E01_L01_environment_overlay.png')
print('Generated',root/'Docs'/'environment_map_atlas.png')
