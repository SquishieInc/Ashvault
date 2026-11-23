#!/usr/bin/env python3
"""Build original pixel art assets, procedural monster animation frames, and original WAV SFX for Ashvault v4."""
from PIL import Image,ImageDraw,ImageEnhance,ImageFilter
from pathlib import Path
import math,random, wave, numpy as np
root=Path(__file__).resolve().parents[1]
tex=root/'Assets/Ashvault/Resources/Textures'
(tex/'Animated').mkdir(parents=True, exist_ok=True)
(tex/'Decals').mkdir(parents=True, exist_ok=True)
(tex/'Directional').mkdir(parents=True, exist_ok=True)
audio=root/'Assets/Ashvault/Resources/Audio';audio.mkdir(parents=True,exist_ok=True)
# Original 8-view concept image, included here as a source reference; crop into 8 directions.
src=Image.open(root/'Docs/skeleton_reaver_eight_view_source.jpg').convert('RGB')
source_path=root/'Docs/skeleton_reaver_eight_view_source.jpg';src.save(source_path,quality=95)
W,H=src.size
views=[]
for i in range(8):
    gx=i%4;gy=i//4
    panel=src.crop((round(gx*W/4),round(gy*H/2),round((gx+1)*W/4),round((gy+1)*H/2))).convert('RGBA')
    # Image's off-white checker background is nearly monochrome; remove it without erasing cream bone.
    pix=panel.load()
    for y in range(panel.height):
        for x in range(panel.width):
            r,g,b,_=pix[x,y]
            level=min(r,g,b)
            neutral=max(r,g,b)-level
            if level >= 232 and neutral < 20:
                pix[x,y]=(r,g,b,0)
            elif level>=212 and neutral<12:
                pix[x,y]=(r,g,b,max(0,int((232-level)/20*255)))
    bb=panel.getchannel('A').getbbox()
    if bb: panel=panel.crop(bb)
    views.append(panel)

def pose_frame(source, row, size=(96,144)):
    frame=Image.new('RGBA',size,(0,0,0,0))
    s=source.copy()
    if row == 1: s=s.rotate(-2,resample=Image.Resampling.BICUBIC,expand=True)
    if row == 2: s=s.rotate(2,resample=Image.Resampling.BICUBIC,expand=True)
    if row == 3: s=s.rotate(-10,resample=Image.Resampling.BICUBIC,expand=True)
    if row == 4: s=s.rotate(12,resample=Image.Resampling.BICUBIC,expand=True)
    if row == 5: s=s.rotate(0,resample=Image.Resampling.BICUBIC,expand=True)
    if row == 6:
        r,g,b,a=s.split();r=r.point(lambda v:min(255,int(v*.85+44)));g=g.point(lambda v:int(v*.67));b=b.point(lambda v:int(v*.69));s=Image.merge('RGBA',(r,g,b,a))
    if row == 7: s=s.rotate(-18,resample=Image.Resampling.BICUBIC,expand=True)
    if row == 8: s=s.rotate(-54,resample=Image.Resampling.BICUBIC,expand=True)
    if row == 9: s=s.rotate(-78,resample=Image.Resampling.BICUBIC,expand=True)
    if row == 10: s=s.rotate(-85,resample=Image.Resampling.BICUBIC,expand=True)
    maxw,maxh=89,128
    if row in (8,9,10): maxh={8:110,9:78,10:42}[row]
    ratio=min(maxw/s.width,maxh/s.height)
    s=s.resize((max(1,int(s.width*ratio)),max(1,int(s.height*ratio))),Image.Resampling.LANCZOS)
    # deliberate crunchy pixel reduction so retro renderer doesn't shimmer at 426x240
    bx=frame.width//2-s.width//2
    by=frame.height-s.height-3 + (-2 if row==1 else 0) + (2 if row==2 else 0)
    if row == 3: by+=6
    frame.alpha_composite(s,(bx,max(0,by)))
    # attack impact stroke, kept within frame for sprite cutout
    if row==4:
        d=ImageDraw.Draw(frame,'RGBA')
        d.arc((3,28,92,109),start=240,end=355,fill=(255,228,157,220),width=3)
        d.arc((6,31,89,105),start=245,end=351,fill=(240,133,54,170),width=2)
    return frame

atlas=Image.new('RGBA',(96*8,144*11))
for r in range(11):
 for d, view in enumerate(views):
    atlas.alpha_composite(pose_frame(view,r),(d*96,(10-r)*144)) # top-to-bottom ordering
atlas.save(tex/'Directional/enemy_skeleton_directions.png',optimize=True)
# Animation strip previews and a standalone gallery for documentation
preview=Image.new('RGBA',(96*8,144*4),(27,22,28,255))
for rr,row in enumerate((0,3,4,10)):
 for d,view in enumerate(views):preview.alpha_composite(pose_frame(view,row),(d*96,rr*144))
preview.save(root/'Docs/skeleton_reaver_animation_preview.png')
# Animated 4x sprites: original stylized props, single row of 4 frames (128x128 each).
for name in ('brazier','mushrooms','crystal','star_lantern'):
    origin=Image.open(tex/f'deco_{name}.png').convert('RGBA')
    strip=Image.new('RGBA',(128*4,128))
    for f in range(4):
        p=origin.copy()
        bbox=p.getchannel('A').getbbox();p=p.crop(bbox) if bbox else p
        p.thumbnail((115,116),Image.Resampling.LANCZOS)
        brightness=(1.00,1.17,1.07,.88)[f]
        p=ImageEnhance.Brightness(p).enhance(brightness)
        cell=Image.new('RGBA',(128,128));cell.alpha_composite(p,((128-p.width)//2,(128-p.height)//2 + int(2*math.sin(f*math.pi/2))))
        if name in ('brazier','crystal','mushrooms'):
            draw=ImageDraw.Draw(cell,'RGBA');random.seed(f+len(name))
            for n in range(5):
                x=random.randrange(34,92);y=random.randrange(12,51)
                col=(250,126,29,175) if name=='brazier' else ((128,74,255,145) if name=='crystal' else (45,255,110,145))
                draw.ellipse((x,y,x+2,y+4),fill=col)
        strip.alpha_composite(cell,(128*f,0))
    strip.save(tex/'Animated'/f'{name}_frames.png',optimize=True)
# Animated scrolling liquid tiles for 3 episodes.
for name,base in [('lava',(217,62,18)),('mire',(44,102,68)),('aether',(71,52,152))]:
    random.seed(len(name)*13)
    img=Image.new('RGB',(64,64));d=ImageDraw.Draw(img)
    for y in range(64):
     for x in range(64):
      w=(math.sin(x*.31+y*.14)+math.sin(y*.38-x*.26)+math.cos((x+y)*.2))/3
      noise=random.random()*.14
      fac=.67+.36*w+noise
      d.point((x,y),fill=tuple(min(255,max(0,int(c*fac))) for c in base))
    img.save(tex/f'env_{name}.png')
# Three semi-transparent square decals, suitable for a runtime quad on walls or floors.
random.seed(914)
for kind in ('blood','scorch','crack','rune_impact'):
    img=Image.new('RGBA',(96,96));d=ImageDraw.Draw(img,'RGBA')
    if kind=='blood':
      for n in range(25):
       a=random.uniform(0,2*math.pi);rad=random.uniform(2,39)
       x=48+math.cos(a)*rad;y=48+math.sin(a)*rad;r=random.randint(1,9)
       d.ellipse((x-r,y-r,x+r,y+r),fill=(103+random.randint(0,55),8,12,random.randint(120,238)))
       if n%3==0: d.line([(x,y),(x+math.cos(a)*random.randrange(4,26),y+math.sin(a)*random.randrange(4,26))],fill=(90,4,9,180),width=random.randint(1,3))
    elif kind=='scorch':
      for n in range(22,2,-1):
       p=(22-n)*2.1;c=random.randint(11,55)
       d.ellipse((p,p,96-p,96-p),fill=(c,c-2 if c>2 else 0,c-8 if c>8 else 0,int((22-n)*7)))
      for _ in range(15):
       x,y=random.randrange(14,80),random.randrange(14,80)
       d.line((48,48,x,y),fill=(15,11,9,100),width=2)
    elif kind=='crack':
      for n in range(9):
       a=n*2*math.pi/9;cx=48;cy=48
       points=[(48,48)]
       for j in range(4):
        r=(j+1)*10;points.append((48+math.cos(a)*r+random.randrange(-3,4),48+math.sin(a)*r+random.randrange(-3,4)))
       d.line(points,fill=(23,19,24,190),width=2)
       d.line([(x+2,y+2) for x,y in points],fill=(190,147,119,100),width=1)
    else:
      for n in range(4):
       d.arc((10+n*4,10+n*4,86-n*4,86-n*4),n*25,n*25+220,fill=(140,61,255,180-n*23),width=2)
      d.line((48,18,48,78),fill=(250,184,96,192),width=2);d.line((21,48,75,48),fill=(250,184,96,192),width=2)
    img.save(tex/'Decals'/f'{kind}.png')
# Small original synthesised sound effects, no recordings, samples or third-party sound packs.
sr=22050
sounds={
 'reaver_attack':(.31,118,.26,'swish'), 'enemy_hurt':(.28,190,.31,'growl'),
 'enemy_death':(.78,92,.40,'growl'), 'boss_death':(1.65,62,.51,'growl'),
 'bolt_cast':(.38,310,.22,'magic'), 'melee_swing':(.25,180,.26,'swish'),
 'weapon_impact':(.23,82,.35,'impact'), 'player_hurt':(.43,140,.28,'growl'),
 'door_open':(.95,61,.42,'stone'), 'door_locked':(.31,138,.37,'stone'),
 'switch_press':(.26,330,.31,'click'), 'secret_reveal':(.79,190,.29,'magic'),
 'platform_motor':(.90,80,.28,'stone'), 'crusher_impact':(.48,56,.48,'impact'),
 'rune_trap':(.53,280,.30,'magic'), 'water_drip':(.38,520,.20,'drip'),
 'ambient_cinder':(3.0,64,.07,'ambient'), 'ambient_fen':(3.0,98,.07,'ambient'),
 'ambient_astral':(3.0,130,.07,'ambient'), 'torch_crackle':(1.8,62,.12,'crackle')
}
for idx,(name,(duration,freq,amp,kind)) in enumerate(sounds.items()):
 rng=np.random.default_rng(720+idx);t=np.arange(int(sr*duration))/sr
 phase=2*np.pi*(freq*t+freq*.25*t*t/(max(.1,duration)))
 tone=np.sin(phase)+.26*np.sin(2.1*phase)+.13*np.sin(3.9*phase)
 noise=rng.standard_normal(len(t));noise=np.convolve(noise,np.ones(11)/11,mode='same')
 if kind=='growl': waveout=.55*tone+.95*noise;env=np.sin(np.pi*np.clip(t/duration,0,1))**.7
 elif kind=='magic': waveout=.73*tone+noise*.22;env=(1-np.clip(t/duration,0,1))**.65
 elif kind=='swish': waveout=noise*1.6+tone*.08;env=np.sin(np.pi*np.clip(t/duration,0,1))**2.8
 elif kind=='impact': waveout=noise*1.1+tone*.7;env=np.exp(-8*t/duration)
 elif kind=='stone': waveout=noise*.82+tone*.42;env=np.exp(-2*t/duration)
 elif kind=='click': waveout=noise+tone*.3;env=np.exp(-13*t/duration)
 elif kind=='drip': waveout=tone*.88+noise*.07;env=np.exp(-8*t/duration)
 elif kind=='crackle': waveout=noise*1.4;env=.17+.8*(np.sin(t*29+np.sin(t*5))**18)
 else: waveout=.33*tone+.60*noise;env=.55+.20*np.sin(2*np.pi*t/duration)
 fade=np.minimum(1,np.minimum(t*120,(duration-t)*45))
 pcm=(np.clip(amp*waveout*env*np.maximum(0,fade),-.98,.98)*32767).astype('<i2')
 with wave.open(str(audio/(name+'.wav')),'wb') as w:
  w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(pcm.tobytes())
print('Built skeleton atlas',atlas.size,'animated strips',4,'decals',4,'sfx',len(sounds))
