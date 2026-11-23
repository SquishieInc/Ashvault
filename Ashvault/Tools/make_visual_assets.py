"""Generate replaceable prototype atlas frames from the base original enemy art.
Run: python Tools/make_visual_assets.py; source decorations generated separately.
Frame rows: idle, walk A, walk B, attack, hurt, death; columns: 8 viewpoint directions.
These frames are placeholders, not a claim of hand-drawn eight-angle animation.
"""
from PIL import Image,ImageEnhance,ImageOps,ImageDraw
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'Assets/Ashvault/Resources/Textures'
frames_dir=ROOT/'Directional'
frames_dir.mkdir(exist_ok=True)
ART=['enemy_skeleton','enemy_bonerunner','enemy_cultist','enemy_guardian','enemy_hound','enemy_wisp','enemy_shaman','enemy_golem','enemy_stalker','enemy_adept','enemy_sentinel','enemy_summoner','boss_ember','boss_fen','boss_astral']
W,H=96,144
for name in ART:
    base=Image.open(ROOT/(name+'.png')).convert('RGBA')
    base.thumbnail((86,129),Image.Resampling.NEAREST)
    field=Image.new('RGBA',(W,H));field.alpha_composite(base,((W-base.width)//2,H-base.height-2))
    atlas=Image.new('RGBA',(8*W,6*H))
    for direction in range(8):
        # front; front 3/4; profile; rear 3/4; rear; mirrored profile
        mirrored=direction>=5
        depth=[1,.88,.62,.82,1,.82,.62,.88][direction]
        for state in range(6):
            pose=field.copy()
            if direction in (3,4,5):
                # Approximate back view: darkened face/head region; keep silhouette.
                overlay=Image.new('RGBA',pose.size,(0,0,0,0)); draw=ImageDraw.Draw(overlay)
                draw.rectangle((22,12,73,49),fill=(13,12,26,90 if direction==4 else 45))
                pose=Image.alpha_composite(pose,overlay)
            if depth!=1:
                nw=max(1,int(W*depth));pose=pose.resize((nw,H),Image.Resampling.NEAREST)
                tmp=Image.new('RGBA',(W,H));tmp.alpha_composite(pose,((W-nw)//2 + (2 if direction in (1,2,3) else -2),0));pose=tmp
            if mirrored: pose=ImageOps.mirror(pose)
            if state in (1,2):
                step= -3 if state==1 else 3
                pose=Image.new('RGBA',(W,H));pose.alpha_composite(field.resize((max(1,int(W*depth)),H),Image.Resampling.NEAREST),((W-int(W*depth))//2,step)) if False else pose
                # Slight rise / fall for gait, preserving directional shape
                tmp=Image.new('RGBA',(W,H));tmp.alpha_composite(pose,(0,step));pose=tmp
            if state==3:
                tmp=pose.resize((W+7,H+5),Image.Resampling.NEAREST);pose=tmp.crop((3,5,3+W,5+H))
                pose=ImageEnhance.Brightness(pose).enhance(1.25)
            if state==4:
                r,g,b,a=pose.split();r=r.point(lambda p:min(255,int(p*1.3+35)))
                g=g.point(lambda p:int(p*.42));b=b.point(lambda p:int(p*.45));pose=Image.merge('RGBA',(r,g,b,a))
            if state==5:
                flattened=pose.resize((W,44),Image.Resampling.NEAREST)
                pose=Image.new('RGBA',(W,H));pose.alpha_composite(flattened,(0,H-46))
            atlas.alpha_composite(pose,(direction*W,state*H))
    atlas.save(frames_dir/(name+'_directions.png'),optimize=True)
print('Generated',len(ART),'directional placeholder atlases',W,H)
