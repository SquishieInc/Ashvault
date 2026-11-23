"""Generate readable spawn coordinate CSV and a visual original-creature sprite atlas."""
from pathlib import Path
import csv, json
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).parent
DOC=ROOT/'Docs'
R=ROOT/'Assets/Ashvault/Resources'
MAPS=R/'Maps';TEX=R/'Textures'
files=sorted(MAPS.glob('E??_L??.json'))
with (DOC/'ENEMY_SPAWNS.csv').open('w',newline='',encoding='utf-8') as f:
    writer=csv.writer(f)
    writer.writerow(['map_id','episode','level','map_title','enemy_archetype','spawn_mode',
                     'grid_x','grid_z','world_x','world_z'])
    for p in files:
        d=json.loads(p.read_text())
        for a in d['enemies']:
            writer.writerow([d['id'],d['episode'],d['level'],d['title'],
                             a['kind'],a['mode'],a['x'],a['z'],a['x']*3,a['z']*3])
NAMES=[
 ('enemy_skeleton','CRYPT REAVER'),('enemy_bonerunner','BONE RUNNER'),
 ('enemy_cultist','HEX CULTIST'),('enemy_guardian','IRON WARDEN'),
 ('enemy_hound','GRAVE HOUND'),('enemy_wisp','FEN WISP'),
 ('enemy_shaman','PLAGUE SHAMAN'),('enemy_golem','MOSS GOLEM'),
 ('enemy_stalker','VOID STALKER'),('enemy_adept','ASTRAL ADEPT'),
 ('enemy_sentinel','STAR SENTINEL'),('enemy_summoner','RIFT SUMMONER'),
 ('boss_ember','THE EMBER KING'),('boss_fen','MIRE MATRIARCH'),('boss_astral','STARLESS LICH')]
w,h=210,240
atlas=Image.new('RGBA',(w*5,h*3),(18,20,31,255))
d=ImageDraw.Draw(atlas)
for i,(name,title) in enumerate(NAMES):
    col,row=i%5,i//5
    x,y=col*w,row*h
    d.rounded_rectangle((x+6,y+6,x+w-6,y+h-7),radius=13,fill=(37,35,51),outline=(88,76,100),width=2)
    src=Image.open(TEX/(name+'.png')).convert('RGBA')
    scale=min(172/src.width,186/src.height)
    sw,sh=round(src.width*scale),round(src.height*scale)
    src=src.resize((sw,sh),Image.Resampling.NEAREST)
    atlas.alpha_composite(src,(x+(w-sw)//2,y+12+(191-sh)//2))
    d.text((x+12,y+h-29),title,fill=(242,221,180))
atlas.convert('RGB').save(DOC/'enemy_sprite_roster.png',quality=90)
print('Wrote ENEMY_SPAWNS.csv (135 rows) and Docs/enemy_sprite_roster.png')
