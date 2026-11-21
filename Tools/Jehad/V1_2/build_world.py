"""Rebuild only the Jehad V1.2 snapshot. Requires Python, Shapely 2.1+ and Blender 4.3+."""
from pathlib import Path
import argparse,json,math,re,random,subprocess,shutil,hashlib
from shapely.geometry import Point,Polygon,LineString,box
from shapely.ops import unary_union
from shapely import constrained_delaunay_triangles
from unity_authoring import *
from landscape import *

args=argparse.ArgumentParser();args.add_argument('--work-dir',type=Path,default=ROOT/'Temp/JehadV12')
WORK=args.parse_args().work_dir;WORK.mkdir(parents=True,exist_ok=True)
TP='Assets/ThirdParty/KayKitForest/'
MODELDIR=TP+'Meshes/'
MESHDIR=BASE+'Art/Meshes/Jehad/V1_2/'
SHADER=BASE+'Art/Shaders/Jehad/V1_2/ValleyWater.shader'
RELIC=BASE+'Scripts/World/Jehad/V1_2/HiddenRelicV12.cs'
REVIEW=BASE+'Scripts/Testing/Jehad/V1_2/JehadWorldReviewV12.cs'
FOLIAGE=BASE+'Scripts/World/Jehad/V1_2/InstancedFoliageV12.cs'
models={};COLLIDERS=[];TREES=[];BRIDGES=[]
PALETTE={'Grass':(.29,.53,.13),'Meadow':(.31,.57,.15),'MeadowLight':(.39,.62,.19),'Pine':(.16,.37,.24),'Amber':(.57,.43,.20),'Rock':(.42,.47,.49),'Snow':(.85,.93,.91),'Sand':(.75,.70,.45),'Path':(.68,.57,.36),'Stone':(.46,.53,.52),'Cliff':(.25,.32,.35),'Gold':(.98,.67,.12),'Wood':(.38,.22,.12),'WoodLight':(.58,.36,.18),'Cloud':(.94,.98,1.)}

def mat(name):return ART+name+'.mat'

def make_mat(path,rgb,texture=None):
    t=MAT.replace('m_Name: M_Ground','m_Name: '+Path(path).stem)
    rgba='{r: %g, g: %g, b: %g, a: 1}'%tuple(rgb)
    t=re.sub(r'(- _(?:BaseColor|Color): )\{[^}]+\}',lambda m:m.group(1)+rgba,t)
    t=t.replace('- _Smoothness: 0.5','- _Smoothness: 0.1').replace('m_EnableInstancingVariants: 0','m_EnableInstancingVariants: 1')
    if texture:
        t=t.replace('m_Texture: {fileID: 0}',f'm_Texture: {{fileID: 2800000, guid: {guid(texture)}, type: 3}}',1)
    add(path,t,'material')

for name,rgb in PALETTE.items():make_mat(mat(name),rgb)
atlas=TP+'Textures/forest_texture.png';forestmat=TP+'Materials/ForestAtlas.mat'
make_mat(forestmat,(1,1,1),atlas)
add(atlas,'');FILES.pop(atlas)
FILES[atlas+'.meta']=f'''fileFormatVersion: 2
guid: {guid(atlas)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  isReadable: 0
  streamingMipmaps: 1
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 1024
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 4
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 1
  compressionQuality: 50
  spriteMode: 0
  alphaUsage: 0
  alphaIsTransparency: 0
  textureType: 0
  textureShape: 1
  platformSettings: []
  userData:
  assetBundleName:
  assetBundleVariant:
'''

def read_obj(text):
    v=[];n=[];uv=[];pos=[];norm=[];tex=[];groups={};lookup={};material='forest'
    for line in text.splitlines():
        w=line.split()
        if not w:continue
        if w[0]=='v':v.append(tuple(map(float,w[1:4])))
        elif w[0]=='vn':n.append(tuple(map(float,w[1:4])))
        elif w[0]=='vt':uv.append(tuple(map(float,w[1:3])))
        elif w[0]=='usemtl':material=w[1]
        elif w[0]=='f':
            face=[]
            for token in w[1:]:
                a=token.split('/');key=(int(a[0])-1,int(a[1])-1 if len(a)>1 and a[1] else -1,int(a[2])-1)
                if key not in lookup:
                    lookup[key]=len(pos);pos.append(v[key[0]]);tex.append(uv[key[1]] if key[1]>=0 else (0,0));norm.append(n[key[2]])
                face.append(lookup[key])
            for i in range(1,len(face)-1):groups.setdefault(material,[]).extend([face[0],face[i],face[i+1]])
    return {'positions':pos,'normals':norm,'uvs':tex,'groups':groups}

for p in sorted((ROOT/TP/'Source').glob('*.obj.txt')):
    name=p.name.removesuffix('.obj.txt');data=read_obj(p.read_text());path=MODELDIR+name+'.asset'
    mesh_asset(path,data['positions'],data['normals'],data['groups'],data['uvs'])
    models[name]={'path':path,'materials':[forestmat]}
    add(str(p.relative_to(ROOT)),p.read_text())
for path in [TP+'Source/forest.mtl.txt',TP+'LICENSE.txt',TP+'PROVENANCE.downloads.json']:
    add(path,(ROOT/path).read_text())

tree_data={k:MESHES[v['path']] for k,v in models.items() if k.startswith('Tree_')}
(WORK/'lod-input.json').write_text(json.dumps(tree_data))
subprocess.run(['blender','-b','-t','4','--python',str(Path(__file__).with_name('make_lods.py')),'--',str(WORK/'lod-input.json'),str(WORK/'lod-output.json')],check=True,stdout=subprocess.DEVNULL)
for name,data in json.loads((WORK/'lod-output.json').read_text()).items():
    path=MODELDIR+name+'_LOD1.asset'
    mesh_asset(path,data['positions'],data['normals'],data['groups'],data['uvs']);models[name]['lod']=path

# Reuse the previously credited small flowers, mushrooms and logs.
for name in ['flower_yellowA','flower_purpleB','mushroom_redGroup','log','stump_roundDetailed']:
    path='Assets/ThirdParty/KenneyNatureKit/Meshes/'+name+'.asset'
    data=read_obj((ROOT/'Assets/ThirdParty/KenneyNatureKit/Source'/(name+'.obj.txt')).read_text())
    MESHES[path]=data
    models[name]={'path':path,'materials':['Assets/ThirdParty/KenneyNatureKit/Materials/'+n+'.mat' for n in data['groups']]}

class World(Area):
    def obj(self,*args,**kwargs):
        super().obj(*args,**kwargs)
        self.shapes[-1]['material']=kwargs.get('mat',args[3] if len(args)>3 else 'Stone')
        self.shapes[-1]['yaw']=kwargs.get('yaw',0)
    def mesh(self,name,path,mats,pos=(0,0,0),scale=(1,1,1),yaw=0,collider=False,shadows=True,preview=True):
        gid,tid,mid,rid,cid=range(self.next_id,self.next_id+5);self.next_id+=10;self.children.append(tid)
        mp={600295488:gid,600295492:tid,600295491:mid,600295489:rid,600295490:cid}
        go=remap(T_GO,mp).replace('m_Name: Ground','m_Name: '+name)
        if not collider:go=go.replace(f'  - component: {{fileID: {cid}}}\n','')
        mf=re.sub(r'm_Mesh: \{[^}]+\}',f'm_Mesh: {{fileID: 4300000, guid: {guid(path)}, type: 2}}',remap(T_MESH,mp))
        renderer=re.sub(r'm_Materials:\n(?:  - .*\n)+','m_Materials:\n'+''.join(f'  - {{fileID: 2100000, guid: {guid(p)}, type: 2}}\n' for p in mats),remap(T_RENDER,mp))
        if not shadows:renderer=renderer.replace('m_CastShadows: 1','m_CastShadows: 0')
        self.parts.extend([go,transform(tid,gid,10001,pos,scale,yaw),mf,renderer])
        if collider:
            self.parts.append(f'''--- !u!64 &{cid}
MeshCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gid}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers: {{serializedVersion: 2, m_Bits: 0}}
  m_ExcludeLayers: {{serializedVersion: 2, m_Bits: 0}}
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 5
  m_Convex: 0
  m_CookingOptions: 30
  m_Mesh: {{fileID: 4300000, guid: {guid(path)}, type: 2}}
''')
            COLLIDERS.append({'name':name,'mesh':path,'position':pos})
        if preview:DETAILS.append({'name':name,'mesh':path,'materials':mats,'pos':pos,'scale':scale,'yaw':yaw})
        return gid,tid,rid
    def model(self,name,asset,x,z,s=1,yaw=0,y=None,lod=False):
        if isinstance(s,(int,float)):s=(s,)*3
        model=models[asset];p=(x,height(x,z) if y is None else y,z)
        gid,tid,rid=self.mesh(name,model['path'],model['materials'],p,s,yaw)
        if lod and 'lod' in model:
            lgid=self.next_id+6
            _,ctid,crid=self.mesh(name+'_Distant',model['lod'],model['materials'],preview=False)
            self.children.remove(ctid)
            # The distant renderer is a child with an identity transform.
            for i,part in enumerate(self.parts):
                if part.startswith(f'--- !u!4 &{ctid}\n'):self.parts[i]=part.replace('m_Father: {fileID: 10001}',f'm_Father: {{fileID: {tid}}}')
                elif part.startswith(f'--- !u!4 &{tid}\n'):self.parts[i]=part.replace('m_Children: []',f'm_Children:\n  - {{fileID: {ctid}}}')
                elif part.startswith(f'--- !u!1 &{gid}\n'):self.parts[i]=part.replace('  m_Layer:',f'  - component: {{fileID: {lgid}}}\n  m_Layer:')
            b=MESHES[model['path']]['bounds'];center=[(a+b)/2 for a,b in zip(*b)];size=max(b[1][i]-b[0][i] for i in range(3))
            self.parts.append(f'''--- !u!205 &{lgid}
LODGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gid}}}
  serializedVersion: 2
  m_LocalReferencePoint: {vector(center)}
  m_Size: {size}
  m_FadeMode: 0
  m_AnimateCrossFading: 0
  m_LastLODIsBillboard: 0
  m_LODs:
  - screenRelativeHeight: 0.065
    fadeTransitionWidth: 0
    renderers:
    - renderer: {{fileID: {rid}}}
  - screenRelativeHeight: 0.009
    fadeTransitionWidth: 0
    renderers:
    - renderer: {{fileID: {crid}}}
  m_Enabled: 1
''')
    def blocker(self,name,pos,size):
        gid,tid,cid=self.next_id,self.next_id+1,self.next_id+4;self.next_id+=10;self.children.append(tid)
        mp={600295488:gid,600295492:tid,600295490:cid}
        go=remap(T_GO,mp).replace('m_Name: Ground','m_Name: '+name)
        go=re.sub(r'm_Component:\n(?:  - component:.*\n)+',f'm_Component:\n  - component: {{fileID: {tid}}}\n  - component: {{fileID: {cid}}}\n',go)
        self.parts.extend([go,transform(tid,gid,10001,pos,size),remap(T_COLLIDER,mp)])
        self.shapes.append({'name':name,'pos':pos,'size':size,'collider':True,'shape':'invisible_box'})

world=World('MainLandscape_Jehad_V1_2')

def ground_material(x,z,n):
    y=height(x,z);r=lake_radius(x,z)
    if y>43:return 'Snow'
    if y>28 or n[1]<.78:return 'Rock'
    if .91<r<1.16:return 'Sand'
    b=biome(x,z)
    if b=='Amber':return 'Amber'
    if b=='Pine':return 'Pine'
    return 'MeadowLight' if math.sin(x*.11+z*.08)>.45 else 'Meadow'

# Four shared-edge terrain tiles, each with a real concave static collider.
for tz in range(2):
    for tx in range(2):
        pos=[];norm=[];uv=[];groups={}
        for iz in range(61):
            for ix in range(61):
                x=-120+(tx*60+ix)*2;z=-120+(tz*60+iz)*2
                pos.append((x,height(x,z),z));norm.append(normal(x,z));uv.append((x/32,z/32))
        for iz in range(60):
            for ix in range(60):
                a=iz*61+ix;b=a+1;c=a+61;d=c+1
                for tri in [(a,c,b),(b,c,d)]:
                    x=sum(pos[i][0] for i in tri)/3;z=sum(pos[i][2] for i in tri)/3
                    name=ground_material(x,z,normal(x,z));groups.setdefault(name,[]).extend(tri)
        path=MESHDIR+f'Landscape_{tx}_{tz}.asset';mesh_asset(path,pos,norm,groups,uv)
        world.mesh(f'Terrain_{tx}_{tz}',path,[mat(n) for n in groups],collider=True)

def smooth(points):
    p=[points[0]]+points+[points[-1]];out=[]
    for i in range(1,len(p)-2):
        p0,p1,p2,p3=p[i-1:i+3]
        for j in range(8):
            t=j/8;out.append(tuple(.5*(2*p1[k]+(-p0[k]+p2[k])*t+(2*p0[k]-5*p1[k]+4*p2[k]-p3[k])*t*t+(-p0[k]+3*p1[k]-3*p2[k]+p3[k])*t*t*t) for k in range(2)))
    return out+[points[-1]]
route_lines=[(name,LineString(points if name=='SummitTrail' else smooth(points)),width) for name,points,width in ROUTES]
route_lines.append(('CliffSpur',LineString([(-89,35),(-100,6)]),2.2))
road=unary_union([l.buffer(w/2,quad_segs=3) for _,l,w in route_lines])
lakepoints=[]
for i in range(128):
    a=i/128*math.tau;r=1+.065*math.sin(3*a)+.035*math.cos(5*a)
    lakepoints.append((52+30*math.cos(a)*r,-3+36*math.sin(a)*r))
lake=Polygon(lakepoints).difference(Point(57,2).buffer(8.3,quad_segs=24))
water=unary_union([lake,LineString([(x,z) for x,z,y in RIVER_UP]).buffer(3.5,quad_segs=6),LineString([(x,z) for x,z,y in RIVER_DOWN]).buffer(3.5,quad_segs=6)])

def polygon_mesh(path,poly,y_fn,uv_fn,norm_fn):
    positions=[];normals=[];uvs=[];indices=[];lookup={}
    minx,minz,maxx,maxz=poly.bounds
    for iz in range(max(0,int((minz+120)//2)),min(120,int((maxz+120)//2)+1)):
        for ix in range(max(0,int((minx+120)//2)),min(120,int((maxx+120)//2)+1)):
            x=-120+ix*2;z=-120+iz*2
            for coords in [[(x,z),(x,z+2),(x+2,z)],[(x+2,z),(x,z+2),(x+2,z+2)]]:
                clipped=poly.intersection(Polygon(coords))
                if clipped.is_empty or clipped.area<1e-8:continue
                for tri in constrained_delaunay_triangles(clipped).geoms:
                    coords=[tuple(struct.unpack('<f',struct.pack('<f',round(v,6)))[0] for v in p) for p in list(tri.exterior.coords)[:3]]
                    a,b,c=coords;cross=(b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
                    if abs(cross)<1e-7:continue
                    if cross>0:coords.reverse()
                    for px,pz in coords:
                        key=(px,pz)
                        if key not in lookup:
                            lookup[key]=len(positions);positions.append((px,y_fn(px,pz),pz));normals.append(norm_fn(px,pz));uvs.append(uv_fn(px,pz))
                        indices.append(lookup[key])
    mesh_asset(path,positions,normals,{'surface':indices},uvs)

path=MESHDIR+'WindingRoutes.asset'
polygon_mesh(path,road.difference(water.buffer(.15)),lambda x,z:height(x,z)+.035,lambda x,z:(x/8,z/8),normal)
world.mesh('Connected_Valley_Trails',path,[mat('Path')],shadows=False)

def water_y(x,z):
    if lake_radius(x,z)<1.04:return WATER_LEVEL+.02
    a=nearest(RIVER_UP,x,z);b=nearest(RIVER_DOWN,x,z)
    return (a if a[0]<b[0] else b)[1]+.025
path=MESHDIR+'LakeAndRivers.asset'
polygon_mesh(path,water,water_y,lambda x,z:(max(0,1-water.boundary.distance(Point(x,z))/4),0),lambda x,z:(0,1,0))
watermat=mat('ValleyWater')
add(watermat,f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: ValleyWater
  m_Shader: {{fileID: 4800000, guid: {guid(SHADER)}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats:
    - _Speed: 0.65
    m_Colors:
    - _DeepColor: {{r: 0.025, g: 0.38, b: 0.54, a: 1}}
    - _ShallowColor: {{r: 0.08, g: 0.7, b: 0.73, a: 1}}
    - _FoamColor: {{r: 0.82, g: 0.98, b: 0.95, a: 1}}
''','material')
world.mesh('Lake_River_Water',path,[watermat],shadows=False)

def make_bridge(name,a,b,width=3.5):
    dx,dz=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dz);ux,uz=dx/length,dz/length;px,pz=-uz,ux
    end_a=height(*a)+.12;end_b=height(*b)+.12
    level=max(end_a,end_b,water_y((a[0]+b[0])/2,(a[1]+b[1])/2)+1.2)
    p=[];n=[];uv=[];ix=[];samples=max(4,math.ceil(length/1.5))
    for j in range(samples+1):
        t=j/samples;y=end_a+(level-end_a)*min(t/.2,1) if t<.5 else end_b+(level-end_b)*min((1-t)/.2,1)
        x,z=a[0]+dx*t,a[1]+dz*t
        for side in [-1,1]:p.append((x+px*width/2*side,y,z+pz*width/2*side));n.append((0,1,0));uv.append((t,side))
        if j<samples:
            k=j*2;ix.extend([k,k+1,k+2,k+2,k+1,k+3])
    # Normalize winding against the bridge's orientation.
    av,bv,cv=[p[i] for i in ix[:3]]
    if (bv[2]-av[2])*(cv[0]-av[0])-(bv[0]-av[0])*(cv[2]-av[2])<0:
        for j in range(0,len(ix),3):ix[j+1],ix[j+2]=ix[j+2],ix[j+1]
    path=MESHDIR+name+'.asset';mesh_asset(path,p,n,{'deck':ix},uv);world.mesh(name,path,[mat('WoodLight')],collider=True)
    # Posts and rails make the crossings read clearly from a distance.
    yaw=math.degrees(math.atan2(ux,uz))
    for side in [-1,1]:
        for t in [.2,.5,.8]:
            x,z=a[0]+dx*t+px*(width/2+.1)*side,a[1]+dz*t+pz*(width/2+.1)*side
            world.obj(name+f'_Post_{side}_{t}',(x,level+.55,z),(.22,1.1,.22),'Wood',collider=False)
        x,z=(a[0]+b[0])/2+px*(width/2+.1)*side,(a[1]+b[1])/2+pz*(width/2+.1)*side
        world.obj(name+f'_Rail_{side}',(x,level+.9,z),(.16,.17,length*.6),'Wood',collider=False,yaw=yaw)
    BRIDGES.append({'name':name,'a':a,'b':b,'deck_height':level,'width':width})

make_bridge('WillowCrossing',(-12,27),(-4,41),4)
make_bridge('LakeOutflowBridge',(47,-49),(63,-59),4)
make_bridge('ReedIslandFootbridge',(83,5),(64,5),2.8)
make_bridge('RiverbendCrossing',(14,11),(26,20),3.6)

rng=random.Random(12024)
tree_choices={'Meadow':['Tree_1_C_Color1','Tree_2_D_Color1','Tree_3_B_Color1'],'Amber':['Tree_1_C_Color6','Tree_3_B_Color6','Tree_5_C_Color6'],'Pine':['Tree_4_C_Color1','Tree_5_C_Color1','Tree_7_B_Color1']}
trees=[]
for attempt in range(9000):
    if len(trees)>=330:break
    x,z=rng.uniform(-110,110),rng.uniform(-109,110);h=height(x,z)
    if h>32 or normal(x,z)[1]<.85 or water.distance(Point(x,z))<2.5:continue
    if road.distance(Point(x,z))<3.4 or math.hypot(x-42,z-102)<21:continue
    if any(math.hypot(x-sx,z-sz)<4 for _,sx,sz,_ in SECRETS):continue
    if any(math.hypot(x-tx,z-tz)<6.8 for tx,tz in trees):continue
    b=biome(x,z);b='Pine' if b=='Alpine' else b
    asset=rng.choice(tree_choices[b]);scale=rng.uniform(1.05,1.65);yaw=rng.uniform(0,360)
    world.model('ValleyTree_%03d'%len(trees),asset,x,z,scale,yaw,lod=True)
    world.blocker('Trunk_%03d'%len(trees),(x,h+scale,z),(.5*scale,2*scale,.5*scale))
    trees.append((x,z));TREES.append({'x':x,'z':z,'asset':asset,'scale':scale})
for i,(x,z) in enumerate([(24,-25),(29,25),(75,25),(87,0)]):
    s=1.65;world.model('ShoreTree_'+str(i),'Tree_6_B_Color1',x,z,s,i*61,lod=True)
    world.blocker('ShoreTrunk_'+str(i),(x,height(x,z)+s,z),(.6*s,2*s,.6*s))
    TREES.append({'x':x,'z':z,'asset':'Tree_6_B_Color1','scale':s})
for i,(x,z) in enumerate([(-72,95),(-99,71),(-45,91)]):
    s=1.7;world.model('AlpineBareTree_'+str(i),'Tree_Bare_1_A_Color1',x,z,s,i*74,lod=True)
    world.blocker('AlpineTrunk_'+str(i),(x,height(x,z)+s,z),(.45*s,2*s,.45*s))
    TREES.append({'x':x,'z':z,'asset':'Tree_Bare_1_A_Color1','scale':s})

# Spatially grouped instance transforms share source meshes rather than copying vertices.
batches={}
for i in range(6400):
    x,z=rng.uniform(-114,114),rng.uniform(-114,114);h=height(x,z);point=Point(x,z)
    if h>36 or normal(x,z)[1]<.85 or water.distance(point)<.45 or road.distance(point)<.5:continue
    if math.hypot(x-42,z-102)<18:continue
    kind=rng.choices(['Grass_1_B_Color1','Grass_2_B_Color1','Bush_1_E_Color1','Bush_2_E_Color1'],[45,40,10,5])[0]
    if biome(x,z)=='Amber' and kind.startswith('Bush'):kind='Bush_1_E_Color6'
    s=rng.uniform(.8,1.4) if kind.startswith('Bush') else rng.uniform(1,1.7)
    key=(int((x+120)//40),int((z+120)//40));batches.setdefault(key,[]).append((kind,(x,h,z),s,rng.uniform(0,math.tau)))
foliage_count=0;serialized_batches='';INSTANCE_DETAILS=[];FOLIAGE_BATCHES=[]
for key,items in sorted(batches.items()):
    for asset in sorted({i[0] for i in items}):
        selected=[i for i in items if i[0]==asset]
        for offset in range(0,len(selected),256):
            group=selected[offset:offset+256];path=models[asset]['path'];bounds=MESHES[path]['bounds']
            lo=[1e9]*3;hi=[-1e9]*3
            for _,p,s,yaw in group:
                radius=max(math.sqrt(sum(v*v for v in corner)) for corner in bounds)*s
                for j in range(3):lo[j]=min(lo[j],p[j]-radius);hi[j]=max(hi[j],p[j]+radius)
                INSTANCE_DETAILS.append({'name':'FoliageInstance','mesh':path,'materials':[forestmat],'pos':p,'scale':(s,s,s),'yaw':math.degrees(yaw)})
            center=[(a+b)/2 for a,b in zip(lo,hi)];extent=[(b-a)/2 for a,b in zip(lo,hi)]
            serialized_batches+=f'  - _mesh: {{fileID: 4300000, guid: {guid(path)}, type: 2}}\n    _bounds:\n      m_Center: {vector(center)}\n      m_Extent: {vector(extent)}\n    _positions:\n'
            serialized_batches+=''.join('    - '+vector(p)+'\n' for _,p,_,_ in group)
            serialized_batches+='    _rotations:\n'+''.join('    - {x: 0, y: %g, z: 0, w: %g}\n'%(math.sin(yaw/2),math.cos(yaw/2)) for _,_,_,yaw in group)
            serialized_batches+='    _scales:\n'+''.join('    - %g\n'%s for _,_,s,_ in group)
            FOLIAGE_BATCHES.append({'mesh':path,'count':len(group),'center':center,'extent':extent})
    foliage_count+=len(items)
fgid,ftid,fid=world.next_id,world.next_id+1,world.next_id+6;world.next_id+=10;world.children.append(ftid)
fgo=remap(T_GO,{600295488:fgid}).replace('m_Name: Ground','m_Name: Spatial_Foliage')
fgo=re.sub(r'm_Component:\n(?:  - component:.*\n)+',f'm_Component:\n  - component: {{fileID: {ftid}}}\n  - component: {{fileID: {fid}}}\n',fgo)
world.parts.extend([fgo,transform(ftid,fgid,10001),f'''--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {fgid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(FOLIAGE)}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.World.InstancedFoliageV12
  _material: {{fileID: 2100000, guid: {guid(forestmat)}, type: 2}}
  _batches:
{serialized_batches}'''])

for i in range(95):
    x,z=rng.uniform(-108,108),rng.uniform(-104,108)
    if road.distance(Point(x,z))<2 or water.distance(Point(x,z))<.4 or math.hypot(x-42,z-102)<19:continue
    if any(math.hypot(x-sx,z-sz)<5 for _,sx,sz,_ in SECRETS):continue
    scale=rng.uniform(.65,2.0);asset=rng.choice(['Rock_1_J_Color1','Rock_2_E_Color1'])
    world.model('ValleyRock_%02d'%i,asset,x,z,scale,rng.uniform(0,360))
    world.blocker('RockCollider_%02d'%i,(x,height(x,z)+scale*.8,z),(scale*1.6,scale*1.6,scale*1.6))

for name,x,z,description in SECRETS:
    world.marker('Anchor_Secret_'+name,(x,height(x,z)+1.2,z))
# A solid arch with a genuinely open passage, shared by the spring and high trail.
ap=[];an=[];ai=[]
for segment in range(14):
    a=segment/14*math.pi;b=(segment+1)/14*math.pi
    v=[(r*math.cos(t),r*math.sin(t),z) for z in [-1.2,1.2] for t,r in [(a,2.7),(a,3.6),(b,3.6),(b,2.7)]]
    faces=[(0,3,2,1),(4,5,6,7),(1,2,6,5),(0,4,7,3)]
    if segment==0:faces.append((0,1,5,4))
    if segment==13:faces.append((3,7,6,2))
    for f in faces:
        aa,bb,cc=[v[i] for i in f[:3]];u=[bb[i]-aa[i] for i in range(3)];w=[cc[i]-aa[i] for i in range(3)]
        n=(u[1]*w[2]-u[2]*w[1],u[2]*w[0]-u[0]*w[2],u[0]*w[1]-u[1]*w[0]);length=math.sqrt(sum(x*x for x in n));n=tuple(x/length for x in n)
        start=len(ap);ap.extend(v[i] for i in f);an.extend([n]*4);ai.extend([start,start+1,start+2,start,start+2,start+3])
archpath=MESHDIR+'RockOverhang.asset';mesh_asset(archpath,ap,an,{'arch':ai})
world.mesh('SpringRockOverhang',archpath,[mat('Rock')],(-28,6.6,39),collider=True)
# Distinct landmark structures around the hidden relics.
for i,(x,z) in enumerate([(-78,-70),(-74,-71),(-76,-74)]):world.model('RootHollowShrub_'+str(i),'Bush_1_E_Color6',x,z,2.5,i*50)
for i in range(9):
    a=i/9*math.tau;world.model('MushroomRing_'+str(i),'mushroom_redGroup',-90+3*math.cos(a),-24+3*math.sin(a),2,math.degrees(a))
for i,(x,z) in enumerate([(-68,-5),(-62,-5),(-68,-10),(-62,-10)]):world.obj('GardenRuin_'+str(i),(x,height(x,z)+2,z),(.9,4,.9),'Stone')
for x,z in [(-65,-5),(-65,-10)]:world.obj('GardenRuin_Lintel_'+str(z),(x,height(x,z)+4.1,z),(7,.6,1.2),'Stone')
for i in range(7):
    a=i/7*math.tau;x,z=4+4*math.cos(a),100+4*math.sin(a)
    world.model('Moonstone_'+str(i),'Rock_2_E_Color1',x,z,(.45,.65,.45),i*43)
for i,(x,z) in enumerate([(-69,78),(-63,78)]):world.model('SnowArchPillar_'+str(i),'Rock_1_J_Color1',x,z,(.8,1.3,.8),0)
world.mesh('SnowArch',archpath,[mat('Snow')],(-66,height(-66,78)-.15,78),collider=True)
world.model('FallenPineLog','log',86,57,14,90)
world.blocker('FallenPineLogCollider',(86,height(86,57)+1,57),(9,2,2))
for i in range(20):
    a=rng.uniform(0,math.tau);r=rng.uniform(1.5,6)
    x,z=-14+math.cos(a)*r,-100+math.sin(a)*r
    world.model('WildflowerPatch_'+str(i),rng.choice(['flower_purpleB','flower_yellowA']),x,z,3,rng.uniform(0,360))
# The former jetty: a short landing beside the cove, with no new movement mechanic.
make_bridge('OldShoreJetty',(34,-35),(42,-29),2.5)

# Small gold octahedra are real once-only pickups, not just secret anchors.
orbpos=[(0,.6,0),(.38,0,0),(0,0,.38),(-.38,0,0),(0,0,-.38),(0,-.6,0)]
orbix=[0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1]
orbpath=MESHDIR+'HiddenRelic.asset';onorm=[]
for x,y,z in orbpos:
    l=math.sqrt(x*x+y*y+z*z);onorm.append((x/l,y/l,z/l))
mesh_asset(orbpath,orbpos,onorm,{'Gold':orbix})
for i,(name,x,z,description) in enumerate(SECRETS,1):
    gid,tid,rid=world.mesh('HiddenRelic_'+name,orbpath,[mat('Gold')],(x,height(x,z)+1.2,z))
    sid,mid=world.next_id+5,world.next_id+6
    for j,p in enumerate(world.parts):
        if p.startswith(f'--- !u!1 &{gid}\n'):
            world.parts[j]=p.replace('  m_Layer:',f'  - component: {{fileID: {sid}}}\n  - component: {{fileID: {mid}}}\n  m_Layer:').replace('m_Layer: 13','m_Layer: 14')
    world.next_id+=10
    world.parts.append(f'''--- !u!135 &{sid}
SphereCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gid}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers: {{serializedVersion: 2, m_Bits: 0}}
  m_ExcludeLayers: {{serializedVersion: 2, m_Bits: 0}}
  m_LayerOverridePriority: 0
  m_IsTrigger: 1
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Radius: 1.1
  m_Center: {{x: 0, y: 0, z: 0}}
--- !u!114 &{mid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(RELIC)}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.World.HiddenRelicV12
  _id: jehad-v12-{i:02d}-{name}
  _displayName: {name}
  _onDiscovered:
    m_PersistentCalls:
      m_Calls: []
''')

for name,(x,z) in {'MainSpawn':(0,-100),'MainRetry':(0,-100),'BladeBeetle':(-20,-45),'ThornArcher':(-60,-30),'Emberling':(85,38),'ArenaEntry':(42,87)}.items():world.marker('Anchor_'+name,(x,height(x,z)+(1.1 if 'Spawn' in name or 'Retry' in name else 0),z))
# Edge blockers are hidden in the enclosing ridge, not placed along internal routes.
for name,p,s in [('West',(-120,35,0),(1,100,241)),('East',(120,35,0),(1,100,241)),('South',(0,35,-120),(241,100,1)),('North',(0,35,120),(241,100,1))]:world.blocker('WorldEdge_'+name,p,s)

main_path=WORLD+'MainLandscape_V1_2.prefab';add(main_path,world.build(),'prefab')
# Compact boss arena stays a separate prefab with the original integration anchor names.
arena=World('BossArena_Jehad_V1_2')
arena.obj('Arena_Floor',(0,-.5,0),(28,1,28),'Stone')
arena.obj('Arena_Inlay',(0,.025,0),(14,.05,14),'Cliff',collider=False,yaw=45)
for i,(x,z) in enumerate([(-13,-8),(13,-8),(-13,10),(13,10),(-7,13),(7,13)]):
    arena.obj('ArenaPillar_'+str(i),(x,2,z),(1.1,4,1.1),'Stone')
    arena.obj('ArenaPillarCap_'+str(i),(x,4.2,z),(1.5,.4,1.5),'Gold')
for name,p,s in [('West',(-14,1.5,0),(.7,3,28)),('East',(14,1.5,0),(.7,3,28)),('North',(0,1.5,14),(28,3,.7)),('SouthL',(-9,1.5,-14),(10,3,.7)),('SouthR',(9,1.5,-14),(10,3,.7))]:arena.obj('ArenaWall_'+name,p,s,'Stone')
arena.obj('HeartPedestal',(0,.4,10),(3,.8,3),'Gold')
arena.obj('Heart_Placeholder',(0,1.8,10),(1,1.7,1),'Gold','sphere',False)
for name,p in [('ArenaRetry',(0,1.1,-10)),('Guardian',(0,0,0)),('Heart',(0,1.8,10)),('SwordSource',(-10,0,-9)),('BowSource',(-10,0,-5)),('FireSource',(10,0,-9))]:arena.marker('Anchor_'+name,p)
arena_path=WORLD+'BossArena_V1_2.prefab';add(arena_path,arena.build(),'prefab')

def instance(path,iid,tid,pos):
    g=guid(path);mods=''
    for a,v in zip('xyz',pos):mods+=f'    - target: {{fileID: 10001, guid: {g}, type: 3}}\n      propertyPath: m_LocalPosition.{a}\n      value: {v}\n      objectReference: {{fileID: 0}}\n'
    return f'''--- !u!1001 &{iid}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: 0}}
    m_Modifications:
{mods}    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {g}, type: 3}}
--- !u!4 &{tid} stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: 10001, guid: {g}, type: 3}}
  m_PrefabInstance: {{fileID: {iid}}}
  m_PrefabAsset: {{fileID: 0}}
'''

scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.1.unity').read_text()
start=scene.index('--- !u!1001 &20000000');end=scene.index('--- !u!1660057539',start)
scene=scene[:start]+instance(main_path,20000000,20000001,(0,0,0))+instance(arena_path,20000010,20000011,(42,8,102))+scene[end:]
scene=scene.replace('m_LocalPosition: {x: 0, y: 1.1, z: -25}','m_LocalPosition: '+vector((0,height(0,-100)+1.1,-100)))
scene=scene.replace('m_Name: Jehad V1.1 Layout Tester','m_Name: Jehad V1.2 Explorer')
old=re.search(r'^--- !u!114 &1395263160\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
new=old.replace('43c86d8e40cd23344aece63619799780',guid(REVIEW)).replace('Echoheart.Testing.TestPlayerMover','Echoheart.Testing.JehadWorldReviewV12')
new=new[:new.index('  _speed:')]+'''  _camera: {fileID: 546103918}
  _walkSpeed: 7
  _sprintSpeed: 16
  _obstructionMask:
    serializedVersion: 2
    m_Bits: 8192
'''
scene=scene.replace(old,new).replace('far clip plane: 1000','far clip plane: 650')
scene=scene.replace('m_Fog: 0','m_Fog: 1').replace('m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}','m_FogColor: {r: 0.60, g: 0.78, b: 0.84, a: 1}')
scene=scene.replace('m_FogMode: 3','m_FogMode: 1').replace('m_LinearFogStart: 0','m_LinearFogStart: 140').replace('m_LinearFogEnd: 300','m_LinearFogEnd: 430')
add(SCENE,scene)

for path,kind in [(RELIC,'script'),(REVIEW,'script'),(FOLIAGE,'script'),(SHADER,'shader')]:
    add(path,(ROOT/path).read_text())
    importer='MonoImporter' if kind=='script' else 'ShaderImporter'
    FILES[path+'.meta']=f'fileFormatVersion: 2\nguid: {guid(path)}\n{importer}:\n  externalObjects: {{}}\n'+('  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {fileID: 0}\n' if kind=='script' else '  defaultTextures: []\n  nonModifiableTextures: []\n')+'  userData:\n  assetBundleName:\n  assetBundleVariant:\n'

add(TP+'README.md','''# KayKit Forest Nature Pack — selected CC0 models

Creator: Kay Lousberg, https://kaylousberg.itch.io/kaykit-forest .
License: CC0-1.0; see LICENSE.txt. Selected models are used inside Jehad V1.2,
not sold as an asset pack. They are third-party art, not original team models.

Source OBJ/MTL files are preserved under Source/ as text, avoiding duplicate Unity
imports. Native meshes preserve source positions, normals, UVs and topology.
One original 1024px gradient atlas is shared by all models. Color1 is the green
palette and Color6 provides the autumn variations. Tree LOD1 meshes are derived
using Blender's Decimate modifier at ratio 0.3, with the UVs retained.

Pinned mirror: GeorgeQLe/assets-kaykit-3d-environments @
d5e6c73284e9385a389ac7181dec3d0dc9e9021d. PROVENANCE.downloads.json records
download URLs and original SHA-256 hashes. The contents.png catalog was consulted
for selection and is not imported. Rebuild tools: Tools/Jehad/V1_2/.
''')
credits=(ROOT/'docs/ASSET_CREDITS.md').read_text()
row='| Forest Nature Pack (18 selected models and shared gradient atlas) | Kay Lousberg | https://kaylousberg.itch.io/kaykit-forest | CC0 1.0; included LICENSE.txt | Textured trees, autumn variants, shrubs, grass and rocks in Jehad V1.2; derived distant tree LODs | Jehad_V1.2 |\n'
credit_lines=[line for line in credits.splitlines() if not line.startswith('| Forest Nature Pack')]
insert=next(i+1 for i,line in enumerate(credit_lines) if line.startswith('| Nature Kit'))
credit_lines.insert(insert,row.rstrip());credits='\n'.join(credit_lines)+'\n'
if 'KayKit provenance:' not in credits:
    credits=credits.replace('\nEngine/packages:', '\nKayKit provenance: `Assets/ThirdParty/KayKitForest/PROVENANCE.downloads.json`.\nSource geometry, normals and UVs are retained in the full-resolution meshes;\ndistant tree LODs are derived with Blender. The original forest atlas is unchanged.\n\nEngine/packages:')
add('docs/ASSET_CREDITS.md',credits,'text')

manifest={'scene':SCENE,'main_prefab':main_path,'arena_prefab':arena_path,'world_bounds':[-120,120,-120,120],'main_footprint_m2':57600,'previous_footprint_m2':4896,'peak_height':max(map(max,HEIGHTS)),'secrets':[{'id':name,'x':x,'z':z,'y':height(x,z)+1.2,'description':description} for name,x,z,description in SECRETS],'trees':TREES,'ground_cover_instances':foliage_count,'ground_cover_regions':len(batches),'foliage_batches':FOLIAGE_BATCHES,'bridges':BRIDGES,'mesh_colliders':COLLIDERS,'shapes':world.shapes,'arena_shapes':arena.shapes,'routes':ROUTES,'source_models':list(models),'files':sorted(FILES)}
(WORK/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
(WORK/'preview-data.json').write_text(json.dumps({'meshes':MESHES,'details':DETAILS+INSTANCE_DETAILS,'palette':PALETTE,'shapes':world.shapes,'arena_shapes':arena.shapes,'texture':str(ROOT/atlas),'forest_material':forestmat,'arena_offset':(42,8,102)}))

patch='*** Begin Patch\n'
for path,content in FILES.items():
    dst=ROOT/path
    # Source and script files already exist; do not rewrite equal bytes.
    if dst.exists():
        if dst.read_text().rstrip()==content.rstrip():continue
        patch+='*** Update File: '+str(dst)+'\n@@\n'+'\n'.join('-'+l for l in dst.read_text().rstrip().split('\n'))+'\n'+'\n'.join('+'+l for l in content.rstrip().split('\n'))+'\n'
    else:patch+='*** Add File: '+str(dst)+'\n'+'\n'.join('+'+l for l in content.rstrip().split('\n'))+'\n'
patch+='*** End Patch\n'
if '*** Add File:' in patch or '*** Update File:' in patch:subprocess.run(['apply_patch'],input=patch,text=True,check=True,stdout=subprocess.DEVNULL)
print(json.dumps({'scene':SCENE,'trees':len(TREES),'ground_cover':foliage_count,'secrets':len(SECRETS),'native_meshes':len(MESHES),'files':len(FILES),'bytes':sum(len(v.encode()) for v in FILES.values()),'peak_height_m':manifest['peak_height']}))
