"""Deterministic original pixel-art placeholder assets; no external/source artwork."""
from PIL import Image, ImageDraw, ImageFilter
import random, math, os
root=os.path.join(os.path.dirname(__file__),'Assets','Ashvault','Resources','Textures')
os.makedirs(root,exist_ok=True)
random.seed(47)

def save(name, im, scale=1):
    if scale!=1: im=im.resize((im.width*scale, im.height*scale),Image.Resampling.NEAREST)
    im.save(os.path.join(root,name+'.png'))

def rect(d,x0,y0,x1,y1,c): d.rectangle((x0,y0,x1,y1),fill=c)

def wall():
    im=Image.new('RGB',(64,64),(30,27,36)); d=ImageDraw.Draw(im)
    for row in range(8):
        y=row*8; offset=-(row%2)*13
        for x in range(offset,65,26):
            shade=random.randint(-8,14)
            col=tuple(max(0,min(255,c+shade)) for c in (91,78,85))
            rect(d,x+1,y+1,min(63,x+24),y+6,col)
            if random.random()<.40:
                d.line((x+random.randint(4,15),y+2,x+random.randint(15,23),y+4),fill=(48,41,47),width=1)
    return im
save('wall_bricks',wall())

def floor():
    im=Image.new('RGB',(64,64),(30,24,32)); d=ImageDraw.Draw(im)
    for y in range(0,64,16):
        for x in range(0,64,16):
            c=random.choice([(72,62,67),(81,68,69),(86,71,69),(67,57,61)])
            rect(d,x+1,y+1,x+14,y+14,c)
            d.line((x+3,y+3,x+12,y+3),fill=(108,88,84))
            if random.random()<0.4: rect(d,x+9,y+10,x+12,y+11,(43,35,40))
    return im
save('floor_tiles',floor())

def ceiling():
    im=Image.new('RGB',(64,64),(30,27,37)); d=ImageDraw.Draw(im)
    for y in range(0,64,16):
        for x in range(0,64,16):
            rect(d,x+1,y+1,x+14,y+14,random.choice([(69,58,67),(62,54,64),(77,67,73)]))
            d.line((x+2,y+12,x+12,y+12),fill=(42,35,48))
    return im
save('ceiling_tiles',ceiling())

def door():
    im=Image.new('RGB',(64,64),(36,28,38));d=ImageDraw.Draw(im)
    for x in range(0,64,8):
        rect(d,x+1,0,x+3,63,(60,49,60))
    for y in range(0,64,9):
        rect(d,0,y,63,y+1,(17,15,24))
    d.ellipse((12,9,51,48),outline=(225,196,150),width=3)
    d.polygon([(32,13),(42,31),(32,48),(22,31)],outline=(250,225,173),fill=(71,60,83))
    d.polygon([(32,21),(36,31),(32,40),(28,31)],fill=(254,238,189))
    return im
save('door_runes',door())

def torch():
    im=Image.new('RGB',(32,32),(60,37,31));d=ImageDraw.Draw(im)
    for i in range(6):
        d.polygon([(16,2+i*2),(7+i,19),(25-i,19)],fill=(255,130+i*15,25))
    rect(d,8,19,23,30,(91,60,37))
    return im
save('torch',torch())

def canvas(w=48,h=72):
    return Image.new('RGBA',(w,h),(0,0,0,0))

def skeleton():
    im=canvas();d=ImageDraw.Draw(im)
    # rear cloak, boots
    d.polygon([(13,31),(35,30),(40,62),(8,62)],fill=(61,44,47,255))
    rect(d,13,51,21,69,(66,65,70));rect(d,27,51,35,69,(69,66,68))
    # skeleton body ribs
    rect(d,17,24,31,46,(196,182,154));rect(d,20,28,28,44,(43,36,41))
    for y in [29,33,37,41]:
        rect(d,14,y,34,y+2,(193,178,152))
    # arms
    d.line((13,28,7,44),fill=(216,203,176),width=5)
    d.line((35,28,40,46),fill=(210,196,169),width=5)
    d.polygon([(37,42),(44,47),(43,51),(36,48)],fill=(149,157,156))
    # skull and helmet
    d.ellipse((14,6,34,27),fill=(210,199,172),outline=(77,61,65),width=2)
    rect(d,16,16,21,20,(26,20,27));rect(d,27,16,32,20,(26,20,27))
    d.polygon([(23,21),(25,21),(24,24)],fill=(57,39,42))
    rect(d,15,9,33,12,(110,105,108));rect(d,21,3,27,10,(103,100,104))
    return im
save('enemy_skeleton',skeleton(),2)

def cultist():
    im=canvas();d=ImageDraw.Draw(im)
    d.polygon([(12,23),(36,23),(43,68),(5,68)],fill=(59,37,80))
    d.polygon([(15,23),(33,23),(35,65),(12,65)],fill=(107,63,111))
    d.polygon([(24,2),(9,23),(13,32),(35,32),(40,23)],fill=(76,42,85))
    d.ellipse((15,14,33,33),fill=(23,19,29))
    rect(d,17,23,21,26,(245,78,62));rect(d,27,23,31,26,(244,74,63))
    d.line((10,31,5,47),fill=(151,100,97),width=5)
    d.line((38,31,43,47),fill=(150,91,93),width=5)
    d.ellipse((27,34,46,53),fill=(240,96,35),outline=(255,181,42),width=2)
    d.ellipse((32,38,41,47),fill=(255,235,101))
    rect(d,17,42,31,46,(201,154,74))
    return im
save('enemy_cultist',cultist(),2)

def guardian():
    im=canvas(64,80);d=ImageDraw.Draw(im)
    d.polygon([(8,24),(56,24),(61,74),(3,74)],fill=(62,35,37))
    rect(d,17,30,46,61,(112,91,85))
    d.polygon([(20,31),(32,26),(44,31),(41,52),(23,52)],fill=(143,112,92))
    for x in [8,43]: rect(d,x,29,x+12,48,(102,90,93))
    rect(d,17,58,27,78,(76,68,70));rect(d,37,58,47,78,(76,68,70))
    d.polygon([(14,12),(20,2),(24,18)],fill=(190,175,133))
    d.polygon([(50,12),(43,2),(40,18)],fill=(190,175,133))
    d.polygon([(19,13),(45,13),(49,32),(15,32)],fill=(106,102,103))
    rect(d,22,23,29,26,(248,77,43));rect(d,35,23,42,26,(248,77,43))
    rect(d,29,28,35,37,(46,39,44))
    rect(d,30,41,34,63,(200,109,56))
    d.polygon([(9,44),(17,42),(20,63),(12,69)],fill=(167,125,83))
    d.polygon([(55,44),(47,42),(44,63),(52,69)],fill=(167,125,83))
    return im
save('enemy_guardian',guardian(),2)

def weapon(kind):
    im=canvas(128,128);d=ImageDraw.Draw(im)
    if kind=='sword':
        d.polygon([(62,18),(69,18),(77,83),(62,95)],fill=(195,214,217),outline=(67,76,81))
        d.polygon([(64,22),(69,20),(72,76),(65,88)],fill=(250,244,218))
        rect(d,36,86,92,93,(131,91,53));rect(d,61,87,69,126,(85,54,41))
        rect(d,60,105,70,109,(218,168,77));d.ellipse((60,118,70,127),fill=(225,166,64))
        d.polygon([(34,89),(40,81),(60,85),(58,94)],fill=(151,110,53))
        d.polygon([(94,89),(88,81),(70,85),(72,94)],fill=(151,110,53))
    elif kind=='crossbow':
        d.arc((7,30,120,100),8,172,fill=(171,112,65),width=9)
        d.line((10,68,118,68),fill=(209,188,147),width=3)
        d.polygon([(50,49),(78,49),(71,127),(57,127)],fill=(93,64,45),outline=(190,126,62))
        d.polygon([(60,16),(68,16),(68,88),(60,88)],fill=(196,195,171))
        rect(d,47,79,81,94,(144,100,56));rect(d,58,96,70,126,(108,75,46))
    elif kind=='staff':
        d.polygon([(58,45),(69,44),(74,127),(58,127)],fill=(104,67,49))
        d.polygon([(64,5),(85,31),(65,60),(43,32)],fill=(76,151,208),outline=(187,219,249))
        d.polygon([(64,10),(70,32),(64,55),(56,33)],fill=(169,233,252))
        d.line((57,64,69,64),fill=(221,171,69),width=6)
        d.line((58,90,72,90),fill=(221,171,69),width=5)
    elif kind=='daggers':
        d.polygon([(28,8),(42,48),(79,105),(73,112),(31,65)],fill=(197,210,215),outline=(89,73,100))
        d.polygon([(99,8),(86,48),(47,105),(55,112),(98,65)],fill=(190,200,208),outline=(89,73,100))
        d.line((50,94,85,125),fill=(98,55,48),width=11)
        d.line((76,93,40,125),fill=(98,55,48),width=11)
        d.line((47,95,85,95),fill=(226,185,69),width=5)
    elif kind=='hammer':
        rect(d,60,38,70,127,(123,83,49))
        d.polygon([(15,20),(104,20),(117,55),(10,55)],fill=(121,118,131),outline=(211,186,120))
        d.line((27,26,92,26),fill=(199,192,180),width=5)
        rect(d,61,62,69,70,(213,167,82))
    return im
for kind in ['sword','crossbow','staff','daggers','hammer']:
    save('weapon_'+kind,weapon(kind),2)

# Loot and shots
im=canvas(40,40);d=ImageDraw.Draw(im)
d.ellipse((3,3,36,36),fill=(47,23,63),outline=(246,189,89),width=3)
d.polygon([(20,6),(33,20),(20,34),(7,20)],fill=(241,215,110))
d.polygon([(20,10),(27,20),(20,29),(13,20)],fill=(250,249,225))
save('weapon_pickup',im,2)

for color,name in [((215,141,76),'bronze'),((76,156,245),'azure'),((233,80,79),'crimson')]:
    im=canvas(40,48);d=ImageDraw.Draw(im)
    d.ellipse((7,1,32,27),fill=(42,33,43),outline=color,width=4)
    rect(d,16,25,23,42,color);rect(d,20,36,29,41,color)
    d.ellipse((13,8,26,21),fill=(16,16,30))
    d.polygon([(20,8),(24,15),(20,23),(16,15)],fill=color)
    save('key_'+name,im,2)

im=canvas(40,50);d=ImageDraw.Draw(im)
d.polygon([(11,14),(29,14),(33,36),(7,36)],fill=(83,158,142),outline=(205,234,190))
rect(d,15,7,25,15,(174,119,65));rect(d,14,4,26,9,(222,170,88))
d.ellipse((14,20,26,31),fill=(225,69,75));d.polygon([(20,18),(25,25),(20,32),(15,25)],fill=(248,176,166))
save('health_potion',im,2)

im=canvas(40,40);d=ImageDraw.Draw(im)
d.polygon([(3,20),(27,15),(27,11),(38,20),(27,29),(27,24)],fill=(234,208,153),outline=(84,69,63))
d.line((4,20,28,20),fill=(255,248,214),width=3)
save('projectile_arrow',im,2)
im=canvas(40,40);d=ImageDraw.Draw(im)
for r,c in [(18,(76,52,121)),(15,(101,103,236)),(11,(118,190,250)),(7,(230,247,255))]:
    d.ellipse((20-r,20-r,20+r,20+r),fill=c)
d.polygon([(4,18),(12,5),(9,19),(4,24)],fill=(112,170,246))
save('projectile_orb',im,2)

im=canvas(48,64);d=ImageDraw.Draw(im)
d.ellipse((4,4,44,59),fill=(41,17,62,100),outline=(189,123,246),width=4)
d.ellipse((10,10,38,53),fill=(73,49,171,130),outline=(241,151,248),width=3)
d.polygon([(23,8),(32,30),(23,50),(16,30)],fill=(240,188,252))
save('exit_portal',im,2)

# Atmospheric original menu painting, drawn pixel-by-pixel with coarse forms.
w,h=320,180
im=Image.new('RGB',(w,h),(29,23,46));d=ImageDraw.Draw(im)
for y in range(h):
    t=y/h
    d.line((0,y,w,y),fill=(int(48-25*t),int(27+4*t),int(69-34*t)))
# moons and distant mountain shadows
d.ellipse((238,15,278,55),fill=(186,145,124))
d.ellipse((244,7,286,50),fill=(44,26,60))
d.polygon([(0,118),(43,67),(87,112),(142,58),(206,111),(274,72),(319,119),(319,179),(0,179)],fill=(29,27,45))
d.polygon([(0,139),(51,98),(109,143),(175,102),(226,148),(298,97),(319,123),(319,179),(0,179)],fill=(22,21,31))
# monolithic keep + ruined towers
rect(d,115,72,207,169,(47,39,54));rect(d,133,51,190,144,(58,49,60))
for x,y in [(105,58),(188,58),(145,35)]:
    rect(d,x,y,x+21,155,(65,55,65))
    for dx in range(0,22,7): rect(d,x+dx,y-7,x+dx+4,y+5,(64,52,65))
for y in range(82,163,17):
    for x in range(116,205,19):
        d.line((x+(y%3)*3,y,x+15,y),fill=(22,20,31),width=2)
# large haunted doorway
rect(d,150,107,176,169,(15,13,25));d.ellipse((150,93,176,122),fill=(16,13,26))
for y in range(116,167,7):d.line((153,y,174,y),fill=(72,35,45),width=1)
# torches
for x in [139,185]:
    d.polygon([(x,113),(x-5,101),(x,86),(x+5,100)],fill=(250,115,39))
    d.polygon([(x,109),(x-2,99),(x,93),(x+2,99)],fill=(255,215,103))
# distressed foreground
rect(d,0,164,319,179,(24,21,26))
for _ in range(280):
    x=random.randrange(w);y=random.randrange(165,180)
    rect(d,x,y,x+random.randint(1,5),y+1,random.choice([(55,42,49),(62,47,44),(44,34,37)]))
save('menu_backdrop',im,2)
print('Generated',len(os.listdir(root)),'original texture assets')

# Expansion: distinct silhouettes for additional episode families. All sprites
# are drawn locally from primitives, never copied from existing commercial games.

def monster_sprite(name, body, trim, eyes, kind='humanoid', size=(64,80)):
    w,h=size; im=canvas(w,h);d=ImageDraw.Draw(im)
    bx=lambda v:int(v*w/64)
    by=lambda v:int(v*h/80)
    body=tuple(body)+(255,);trim=tuple(trim)+(255,);eyes=tuple(eyes)+(255,)
    if kind in ('wisp','flying'):
        d.polygon([(bx(31),by(6)),(bx(49),by(26)),(bx(56),by(50)),(bx(33),by(74)),(bx(9),by(47)),(bx(16),by(24))],fill=body)
        d.ellipse((bx(15),by(22),bx(47),by(52)),fill=trim)
        d.polygon([(bx(32),by(11)),(bx(42),by(34)),(bx(32),by(64)),(bx(22),by(34))],fill=body)
        for x in [25,39]:rect(d,bx(x),by(32),bx(x+5),by(38),eyes)
        d.polygon([(bx(4),by(28)),(bx(17),by(18)),(bx(15),by(57))],fill=trim)
        d.polygon([(bx(60),by(28)),(bx(47),by(18)),(bx(49),by(57))],fill=trim)
    elif kind == 'hound':
        d.polygon([(bx(6),by(49)),(bx(19),by(28)),(bx(47),by(31)),(bx(54),by(50)),(bx(47),by(56)),(bx(20),by(56))],fill=body)
        for x in [18,44]:
            rect(d,bx(x),by(53),bx(x+7),by(72),trim)
        d.polygon([(bx(11),by(29)),(bx(21),by(9)),(bx(27),by(25))],fill=trim)
        d.polygon([(bx(47),by(29)),(bx(41),by(9)),(bx(34),by(25))],fill=trim)
        d.polygon([(bx(10),by(31)),(bx(49),by(29)),(bx(55),by(43)),(bx(32),by(49)),(bx(5),by(41))],fill=body)
        for x in [18,42]:rect(d,bx(x),by(36),bx(x+6),by(40),eyes)
        d.polygon([(bx(25),by(44)),(bx(37),by(44)),(bx(32),by(55))],fill=trim)
    else:
        d.polygon([(bx(19),by(24)),(bx(46),by(23)),(bx(55),by(70)),(bx(11),by(70))],fill=body)
        d.polygon([(bx(23),by(32)),(bx(42),by(32)),(bx(44),by(51)),(bx(22),by(52))],fill=trim)
        for x in (17,41):
            rect(d,bx(x),by(32),bx(x+6),by(57),body)
            rect(d,bx(x+1),by(55),bx(x+5),by(73),trim)
        d.polygon([(bx(15),by(15)),(bx(24),by(4)),(bx(43),by(5)),(bx(51),by(17)),(bx(45),by(34)),(bx(20),by(34))],fill=trim)
        d.ellipse((bx(21),by(14),bx(43),by(34)),fill=body)
        for x in [24,37]:rect(d,bx(x),by(24),bx(x+5),by(28),eyes)
        if kind in ('horned','boss'):
            d.polygon([(bx(21),by(21)),(bx(10),by(1)),(bx(29),by(17))],fill=trim)
            d.polygon([(bx(43),by(21)),(bx(54),by(1)),(bx(35),by(17))],fill=trim)
        if kind in ('shaman','summoner','boss'):
            d.line((bx(52),by(31),bx(58),by(72)),fill=trim,width=max(3,w//25))
            d.ellipse((bx(48),by(15),bx(62),by(31)),fill=eyes)
    if kind == 'golem':
        for x,y in [(16,26),(40,26),(23,43),(35,43)]:
            rect(d,bx(x),by(y),bx(x+9),by(y+8),trim)
    save(name,im,2)

monster_sprite('enemy_bonerunner',(97,79,73),(205,182,151),(244,80,49),'horned')
monster_sprite('enemy_hound',(68,83,65),(154,178,110),(248,104,58),'hound')
monster_sprite('enemy_wisp',(43,97,75),(78,209,153),(202,255,129),'wisp')
monster_sprite('enemy_shaman',(64,94,61),(112,145,88),(222,236,105),'shaman')
monster_sprite('enemy_golem',(55,84,61),(109,145,106),(221,250,168),'golem')
monster_sprite('enemy_stalker',(41,39,78),(104,81,156),(220,71,238),'horned')
monster_sprite('enemy_adept',(54,67,130),(132,123,219),(237,202,255),'shaman')
monster_sprite('enemy_sentinel',(64,64,120),(156,152,214),(158,232,255),'golem')
monster_sprite('enemy_summoner',(37,34,82),(119,81,154),(183,105,255),'summoner')
monster_sprite('boss_ember',(98,35,37),(232,143,67),(255,217,91),'boss',(96,120))
monster_sprite('boss_fen',(48,94,56),(135,171,66),(211,255,140),'boss',(96,120))
monster_sprite('boss_astral',(52,39,101),(144,112,207),(203,237,255),'boss',(96,120))

# Additional first-person fantasy weapons for new classes.
for name in ('mace','axe','sickle','grimoire'):
    im=canvas(128,128);d=ImageDraw.Draw(im)
    if name == 'mace':
        d.line((64,39,64,126),fill=(103,71,48),width=14)
        d.ellipse((35,12,94,62),fill=(213,190,103),outline=(82,72,82),width=5)
        d.polygon([(64,0),(72,18),(64,32),(55,18)],fill=(250,244,152))
        d.line((44,37,84,37),fill=(250,238,153),width=5)
    elif name == 'axe':
        d.line((65,22,65,126),fill=(119,72,47),width=14)
        d.polygon([(56,21),(20,17),(12,47),(20,76),(54,58)],fill=(212,208,191),outline=(65,62,70))
        d.polygon([(72,21),(107,17),(115,47),(107,76),(74,58)],fill=(181,182,194),outline=(65,62,70))
        d.polygon([(49,34),(65,23),(80,34),(80,53),(65,64),(49,53)],fill=(193,98,66))
    elif name == 'sickle':
        d.line((65,43,65,126),fill=(80,69,42),width=13)
        d.arc((19,5,111,88),15,240,fill=(157,214,136),width=12)
        d.polygon([(84,12),(106,10),(94,34),(72,43)],fill=(207,243,160))
        d.ellipse((51,31,78,58),fill=(55,171,97),outline=(188,239,147),width=4)
    else:
        d.polygon([(14,31),(57,18),(64,29),(71,18),(114,31),(106,117),(69,110),(64,122),(59,110),(22,117)],fill=(62,46,102),outline=(188,146,229))
        d.line((64,30,64,112),fill=(216,173,231),width=4)
        d.ellipse((47,55,80,89),fill=(163,76,230),outline=(242,205,249),width=5)
        d.polygon([(64,57),(74,70),(64,88),(54,70)],fill=(238,230,255))
    save('weapon_'+name,im,2)

im=canvas(40,40);d=ImageDraw.Draw(im)
d.polygon([(20,1),(32,20),(20,39),(8,20)],fill=(61,173,94),outline=(159,232,109))
d.polygon([(19,8),(26,21),(19,34),(13,21)],fill=(205,255,150))
for i in [6,30]:d.line((20,19,i,6),fill=(83,216,105),width=3)
save('projectile_thorn',im,2)
print('Added 12 original episode creature sprites, 4 class weapons and thorn shot')
