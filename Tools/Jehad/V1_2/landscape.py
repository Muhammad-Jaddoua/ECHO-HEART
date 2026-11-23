"""Seed-independent terrain and route definitions for the V1.2 landscape."""
import math

SIZE=240
STEP=2
LAKE=(52.,-3.,30.,36.)
WATER_LEVEL=1.55
MOUNTAIN_TRAIL=[(-24,13,9),(-62,7,17),(-89,35,25),(-83,62,33),(-64,81,40),(-43,70,46),(-56,58,52)]
RIVER_UP=[(-28,37,7),(-13,36,5),(0,29,3.8),(14,20,2.8),(23,10,1.9),(32,0,1.55)]
RIVER_DOWN=[(62,-30,1.55),(59,-46,1.3),(50,-62,.9),(61,-78,.4),(52,-100,-.8),(55,-120,-2)]
ROUTES=[
 ('HeartRoute',[(0,-100),(0,-82),(-5,-66),(-22,-52),(-30,-25),(-24,0),(-16,21),(-8,34),(0,45),(20,65),(39,80),(42,88)],4.8),
 ('AmberLoop',[(-22,-52),(-60,-60),(-82,-40),(-78,-8),(-58,13),(-35,15),(-16,21)],3.8),
 ('LakeLoop',[(-5,-66),(24,-65),(38,-58),(47,-49),(55,-54),(63,-59),(82,-48),(95,-15),(91,27),(72,48),(40,50),(20,65)],3.8),
 ('RiverWalk',[(-24,0),(-5,2),(15,11),(20,15),(38,28),(40,50)],3.2),
 ('PineLoop',[(20,65),(-3,77),(2,100),(23,109),(42,88)],3.6),
 ('SummitTrail',[(x,z) for x,z,y in MOUNTAIN_TRAIL],3.2),
 ('SummitApproach',[(-35,15),(-24,13)],3.2),
 ('IslandBridge',[(83,5),(64,5),(57,2)],2.6),
]
SECRETS=[
 ('WildflowerCache',-14,-100,'A flower-screened entrance clearing'),
 ('AncientRoots',-76,-70,'A sheltered hollow beside a spreading tree'),
 ('MushroomRing',-90,-24,'A mushroom ring behind the amber grove'),
 ('RuinedGarden',-65,-7,'A small ruin screened by autumn shrubs'),
 ('SpringGrotto',-28,38,'A rock overhang at the river spring'),
 ('UnderWillowBridge',-5,31,'A shallow-water route below the upstream crossing'),
 ('ReedIsland',57,2,'The lake island reached by a timber footbridge'),
 ('LilyCove',78,-27,'A concealed lakeside cove'),
 ('OldJetty',39,-31,'A forgotten landing behind reeds'),
 ('FallenPine',87,53,'A rock-screened clearing in the pine forest'),
 ('MoonstoneCircle',4,100,'A stone circle at the end of the northern loop'),
 ('PineHollow',-6,62,'A secluded grove below the mountain shoulder'),
 ('MountainShelf',-86,35,'A side shelf on the ascent'),
 ('SummitSanctuary',-56,58,'The summit lookout above the valley'),
 ('SnowArch',-66,78,'A rocky arch beside the high trail'),
 ('CliffLookout',-100,6,'A western spur overlooking the amber grove'),
]

def smoothstep(a,b,x):
    t=max(0.,min(1.,(x-a)/(b-a)));return t*t*(3-2*t)

def nearest(points,x,z):
    best=(1e20,0,0,0)
    for a,b in zip(points,points[1:]):
        dx,dz=b[0]-a[0],b[1]-a[1]
        t=max(0,min(1,((x-a[0])*dx+(z-a[1])*dz)/(dx*dx+dz*dz)))
        px,pz=a[0]+t*dx,a[1]+t*dz
        d=math.hypot(x-px,z-pz)
        if d<best[0]:best=(d,a[2]+t*(b[2]-a[2]),px,pz)
    return best

def lake_radius(x,z):
    cx,cz,rx,rz=LAKE
    a=math.atan2((z-cz)/rz,(x-cx)/rx)
    wobble=1+.065*math.sin(3*a)+.035*math.cos(5*a)
    return math.hypot((x-cx)/rx,(z-cz)/rz)/wobble

def base_height(x,z):
    h=3+1.7*math.sin(x/25)*math.cos(z/29)+.8*math.sin(z/13)+.5*math.cos((x+z)/17)
    h+=49*math.exp(-(((x+58)/34)**2+((z-65)/34)**2))
    # An uneven enclosing ridge replaces the repeated cliff fence of earlier versions.
    edge=max(abs(x),abs(z));h+=(14+4*math.sin(x/19)+3*math.cos(z/17))*smoothstep(101,120,edge)
    r=lake_radius(x,z)
    if r<1.22:
        bed=.35+1.2*min(r,1)**4
        h=bed*(1-smoothstep(1,1.22,r))+max(h,1.55+(r-1)*2)*smoothstep(1,1.22,r)
        # Small lake island, kept above the water surface.
        island=math.hypot(x-57,z-2)
        if island<8.5:h=max(h,1.55+2.6*(1-smoothstep(4,8.5,island)))
    for river in [RIVER_UP,RIVER_DOWN]:
        d,y,_,_=nearest(river,x,z)
        if d<7:
            bed=y-.65+.65*min(d/3.5,1)**3
            t=smoothstep(3.5,7,d);carved=bed*(1-t)+max(h,y+.12)*t
            # Joining rivers must not raise artificial banks inside the lake.
            h=min(h,carved) if r<1 else carved
    # A broad bench makes the mountain trail walkable in a 45-degree controller.
    d,y,_,_=nearest(MOUNTAIN_TRAIL,x,z)
    if d<8:
        t=smoothstep(4.5,8,d);h=y*(1-t)+h*t
    # Flat boss landing and a gradual approach retain a safe final encounter space.
    d=math.hypot((x-42)*1.1,z-102)
    if d<23:
        t=smoothstep(17,23,d);h=7.88*(1-t)+h*t
    return h

HEIGHTS=[[base_height(-120+x*STEP,-120+z*STEP) for x in range(121)] for z in range(121)]

def height(x,z):
    """Sample the same two triangles per cell that the actual collider uses."""
    fx=max(0,min(119.999999,(x+120)/STEP));fz=max(0,min(119.999999,(z+120)/STEP))
    ix,iz=int(fx),int(fz);u,v=fx-ix,fz-iz
    a,b=HEIGHTS[iz][ix],HEIGHTS[iz][ix+1]
    c,d=HEIGHTS[iz+1][ix],HEIGHTS[iz+1][ix+1]
    if u+v<=1:return a+(b-a)*u+(c-a)*v
    return d+(c-d)*(1-u)+(b-d)*(1-v)

def normal(x,z):
    e=.2;dx=(height(x+e,z)-height(x-e,z))/(2*e);dz=(height(x,z+e)-height(x,z-e))/(2*e)
    length=math.sqrt(dx*dx+1+dz*dz);return (-dx/length,1/length,-dz/length)

def biome(x,z):
    if height(x,z)>28:return 'Alpine'
    if z>32:return 'Pine'
    if x<-35 and z<25:return 'Amber'
    return 'Meadow'
