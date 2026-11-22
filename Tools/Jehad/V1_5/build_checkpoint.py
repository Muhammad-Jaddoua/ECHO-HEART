"""Author only V1.5 scene, audio metadata/bank, particle material, and a reusable hinged chest."""
from pathlib import Path
import argparse,json,re,subprocess,sys,math
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import *
from landscape import height,RIVER_UP,RIVER_DOWN
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--audio-manifest',type=Path,required=True);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
audio=json.loads(args.audio_manifest.read_text());SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.5.unity'
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_5/*.cs')}
for name,path in scripts.items():
    add(path,(ROOT/path).read_text());FILES[path+'.meta']=f'''fileFormatVersion: 2
guid: {guid(path)}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{fileID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
'''
for a in audio['assets']:
    path=a['path'];stream=a['loop']
    add(path+'.meta',f'''fileFormatVersion: 2
guid: {guid(path)}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: {2 if stream else 0}
    sampleRateSetting: 0
    sampleRateOverride: 22050
    compressionFormat: {1 if stream else 0}
    quality: 0.75
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 0
  loadInBackground: {1 if stream else 0}
  ambisonic: 0
  3D: 1
  userData:
  assetBundleName:
  assetBundleVariant:
''','text')
SHADER='Assets/_Project/Art/Shaders/Jehad/V1_5/WorldSparkV15.shader';add(SHADER,(ROOT/SHADER).read_text())
FILES[SHADER+'.meta']=f'''fileFormatVersion: 2
guid: {guid(SHADER)}
ShaderImporter:
  externalObjects: {{}}
  defaultTextures: []
  nonModifiableTextures: []
  preprocessorOverride: 0
  userData:
  assetBundleName:
  assetBundleVariant:
'''
materials={}
for name,soft in [('WorldSpark',1),('WorldWarning',0)]:
    path=f'Assets/_Project/Art/Materials/Jehad/V1_5/{name}.mat';materials[name]=path
    add(path,f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}_V1_5
  m_Shader: {{fileID: 4800000, guid: {guid(SHADER)}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: 3000
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties:
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats:
    - _SoftParticle: {soft}
    m_Colors: []
  m_BuildTextureStacks: []
''','material')

BANK='Assets/_Project/Data/Audio/Jehad/V1_5/WorldAudioBank_V1_5.asset'
music=['Meadow','Amber','Lake','Pine','Alpine','Guardian']
cues=['Relic','Chest','Secret','Capture','Hit','Defeat','PlayerHurt','Death','Restore','EnemyWarning','SlamWarning','RangedWarning','Slam','RangedShot','Power','StepGrass','StepStone','StepWater','Jump','Land','Sword','Bow','Fire']
musicpath='Assets/_Project/Audio/Music/Jehad/V1_5/';sfxpath='Assets/_Project/Audio/SFX/Jehad/V1_5/'
bank=f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(scripts['WorldAudioBankV15'])}, type: 3}}
  m_Name: WorldAudioBank_V1_5
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.Audio.WorldAudioBankV15
  _music:
'''+''.join(f'  - {{fileID: 8300000, guid: {guid(musicpath+n+".ogg")}, type: 3}}\n' for n in music)+'  _cues:\n'+''.join(f'  - {{fileID: 8300000, guid: {guid(sfxpath+n+".wav")}, type: 3}}\n' for n in cues)
for field,name in [('water','Water'),('forest','Forest'),('alpine','AlpineAir')]:bank+=f'  _{field}: {{fileID: 8300000, guid: {guid(musicpath+"Ambience_"+name+".ogg")}, type: 3}}\n'
add(BANK,bank);FILES[BANK+'.meta']=f'fileFormatVersion: 2\nguid: {guid(BANK)}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'

CHEST='Assets/_Project/Prefabs/World/Jehad/V1_5/SecretChest_V1_5.prefab'
chest=Area('Secret Chest V1.5');chest.obj('Solid timber body',(0,.38,0),(1.6,.65,1),'Wood',collider=True)
for x in [-.6,.6]:
    chest.obj('Body gold band',(x,.40,-.51),(.10,.61,.035),'Gold',collider=False)
    chest.obj('Back gold band',(x,.40,.51),(.10,.61,.035),'Gold',collider=False)
for x in [-.65,.65]:
    for z in [-.36,.36]:chest.obj('Stone foot',(x,.07,z),(.22,.14,.22),'Stone',collider=False)
chest.obj('Gold latch',(0,.52,-.54),(.25,.25,.08),'Gold',collider=False)
chest.obj('Mint seal',(0,.52,-.59),(.11,.11,.07),'MeadowLight',shape='sphere',collider=False)
chest.children.append(20001)
meshpaths=[]
def arched_mesh(name,width,raise_by=0,caps=True):
    positions=[];normals=[];indices=[]
    def triangle(a,b,c):
        u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
        n=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]);length=math.sqrt(sum(x*x for x in n));n=tuple(x/length for x in n)
        for point in [a,b,c]:indices.append(len(positions));positions.append(point);normals.append(n)
    profile=[(.10+.22*math.sin(i*math.pi/16)+raise_by,-.5+.53*math.cos(i*math.pi/16)) for i in range(17)]
    for (y,z),(yy,zz) in zip(profile,profile[1:]):
        a=(-width/2,y,z);b=(width/2,y,z);c=(-width/2,yy,zz);d=(width/2,yy,zz)
        triangle(a,b,c);triangle(b,d,c)
    if caps:
        outline=profile+[(.01,-1.03),(.01,.03)]
        for x in [-width/2,width/2]:
            center=(x,.13,-.5)
            for a,b in zip(outline,outline[1:]+outline[:1]):
                aa=(x,*a);bb=(x,*b)
                triangle(center,aa,bb) if x<0 else triangle(center,bb,aa)
        triangle((-width/2,.01,-1.03),(width/2,.01,.03),(-width/2,.01,.03))
        triangle((-width/2,.01,-1.03),(width/2,.01,-1.03),(width/2,.01,.03))
    path=f'Assets/_Project/Art/Meshes/Jehad/V1_5/{name}.asset';mesh_asset(path,positions,normals,{'surface':indices});meshpaths.append(path);return path
roof=arched_mesh('ChestArchedLid',1.66);band=arched_mesh('ChestCurvedBand',.11,.012,False)
lid=Area('Lid hinge')
def mesh_obj(name,path,position,material):
    first=len(lid.parts);lid.obj(name,position,(1,1,1),material,collider=False)
    for i in range(first,len(lid.parts)):
        if 'MeshFilter:' in lid.parts[i]:lid.parts[i]=lid.parts[i].replace('fileID: 10202, guid: 0000000000000000e000000000000000, type: 0',f'fileID: 4300000, guid: {guid(path)}, type: 2')
mesh_obj('Arched timber lid',roof,(0,0,0),'WoodLight')
for x in [-.6,.6]:mesh_obj('Curved gold band',band,(x,0,0),'Gold')
lid.obj('Crest',(0,.345,-.5),(.27,.03,.25),'Gold',collider=False,yaw=45)
text=lid.build();ids=list(map(int,re.findall(r'^--- !u!\d+ &(\d+)',text,re.M)));text=remap(text,{i:i+10000 for i in ids})
text=re.sub(r'^%.*\n','',text,flags=re.M);text=text.replace('  m_LocalPosition: {x: 0, y: 0, z: 0}','  m_LocalPosition: {x: 0, y: 0.72, z: 0.5}',1).replace('  m_Father: {fileID: 0}','  m_Father: {fileID: 10001}',1)
add(CHEST,chest.build()+text,'prefab')

parts=[];roots=[]
def obj(gid,name,pos=(0,0,0),components=(),parent=0,children=(),tag='Untagged'):
    go=remap(T_GO,{600295488:gid}).replace('m_Name: Ground','m_Name: '+name).replace('m_Layer: 13','m_Layer: 0').replace('m_TagString: Untagged','m_TagString: '+tag)
    go=re.sub(r'm_Component:\n(?:  - component:.*\n)+','m_Component:\n'+''.join(f'  - component: {{fileID: {c}}}\n' for c in [gid+1,*components]),go)
    parts.extend([go,transform(gid+1,gid,parent,pos,children=children)])
    if not parent:roots.append(gid+1)
def mono(fid,gid,name,area,fields):
    parts.append(f'''--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(scripts[name])}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.{area}.{name}
{fields}''')
obj(51000000,'Jehad V1.5 - Music and world presentation',components=(51000011,51000012,51000013,51000014))
water=[(x,y,z) for x,z,y in RIVER_UP]+[(52,1.55,-3)]+[(x,y,z) for x,z,y in RIVER_DOWN]
mono(51000011,51000000,'WorldAudioV15','Audio',f'''  _bank: {{fileID: 11400000, guid: {guid(BANK)}, type: 2}}
  _volumes: {{fileID: 31000015}}
  _flow: {{fileID: 31000011}}
  _player: {{fileID: 1395263164}}
  _waterLine:
'''+''.join('  - '+vector(v)+'\n' for v in water))
mono(51000012,51000000,'WorldParticlesV15','World',f'''  _material: {{fileID: 2100000, guid: {guid(materials['WorldSpark'])}, type: 2}}
  _flow: {{fileID: 31000011}}
  _audio: {{fileID: 51000011}}
  _player: {{fileID: 1395263164}}
''')
mono(51000013,51000000,'WorldToastV15','UI','  _flow: {fileID: 31000011}\n')
mono(51000014,51000000,'WorldFeedbackV15','World',f'''  _flow: {{fileID: 31000011}}
  _audio: {{fileID: 51000011}}
  _particles: {{fileID: 51000012}}
  _toast: {{fileID: 51000013}}
  _relics: {{fileID: 31000014}}
  _population: {{fileID: 41000012}}
  _arena: {{fileID: 31000202}}
  _warningMaterial: {{fileID: 2100000, guid: {guid(materials['WorldWarning'])}, type: 2}}
  _heart: {{fileID: 31000301}}
''')
world=(ROOT/'Assets/_Project/Prefabs/World/Jehad/V1_2/MainLandscape_V1_2.prefab').read_text();worldguid=guid('Assets/_Project/Prefabs/World/Jehad/V1_2/MainLandscape_V1_2.prefab')
chests=[]
for i,(name,x,z,title) in enumerate([('RuinedGarden',-65,-7,'The Forgotten Garden'),('SummitSanctuary',-56,58,'The Summit Cache')]):
    monoBlock=re.search(r'^--- !u!114 &(\d+)\nMonoBehaviour:\n(?:(?!^---).)*?_id: [^\n]*'+name+r'\n(?:(?!^---).)*',world,re.M|re.S).group()
    rewardID=int(re.search(r'^--- !u!114 &(\d+)',monoBlock)[1]);goID=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',monoBlock)[1])
    base=51001000+i*1000;instance=51500010+i*100000;stripTF=51500001+i*100000;lidref=base+201;rewardref=base+106;goref=base+100
    obj(base,title+' chest',(x,height(x,z),z),components=(base+2,))
    mono(base+2,base,'SecretChestV15','World',f'''  _flow: {{fileID: 31000011}}
  _reward: {{fileID: {rewardref}}}
  _lid: {{fileID: {lidref}}}
  _feedback: {{fileID: 51000014}}
  _secretName: {title}
''')
    parts.append(f'''--- !u!1 &{goref} stripped
GameObject:
  m_CorrespondingSourceObject: {{fileID: {goID}, guid: {worldguid}, type: 3}}
  m_PrefabInstance: {{fileID: 20000000}}
  m_PrefabAsset: {{fileID: 0}}
--- !u!114 &{rewardref} stripped
MonoBehaviour:
  m_CorrespondingSourceObject: {{fileID: {rewardID}, guid: {worldguid}, type: 3}}
  m_PrefabInstance: {{fileID: 20000000}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {goref}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 17951aa31a6a5fe2b8c448c84390bea6, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.World.HiddenRelicV12
''')
    cg=guid(CHEST);mods=''
    for axis,value in zip('xyz',(x,height(x,z),z)):mods+=f'    - target: {{fileID: 10001, guid: {cg}, type: 3}}\n      propertyPath: m_LocalPosition.{axis}\n      value: {value:g}\n      objectReference: {{fileID: 0}}\n'
    parts.append(f'''--- !u!1001 &{instance}
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
  m_SourcePrefab: {{fileID: 100100000, guid: {cg}, type: 3}}
--- !u!4 &{stripTF} stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: 10001, guid: {cg}, type: 3}}
  m_PrefabInstance: {{fileID: {instance}}}
  m_PrefabAsset: {{fileID: 0}}
--- !u!4 &{lidref} stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: 20001, guid: {cg}, type: 3}}
  m_PrefabInstance: {{fileID: {instance}}}
  m_PrefabAsset: {{fileID: 0}}
''');roots.append(stripTF)
    chests.append({'name':name,'title':title,'controller':base+2,'reward_source_id':rewardID,'reward_source_go':goID,'position':[x,height(x,z),z]})
for i,(name,x,z) in enumerate([('The Mushroom Circle',-90,-24),('The Hidden Spring',-28,38)]):
    gid=51004000+i*100;obj(gid,name,(x,height(x,z)+1,z),components=(gid+2,))
    mono(gid+2,gid,'SecretDiscoveryV15','World',f'''  _flow: {{fileID: 31000011}}
  _feedback: {{fileID: 51000014}}
  _secretName: {name}
  _radius: 5
''')
obj(51005000,'Jehad V1.5 world polish acceptance harness',components=(51005002,),tag='EditorOnly')
mono(51005002,51005000,'WorldPolishReviewV15','Testing',f'''  _flow: {{fileID: 31000011}}
  _chests:
'''+''.join(f'  - {{fileID: {c["controller"]}}}\n' for c in chests)+f'''  _bank: {{fileID: 11400000, guid: {guid(BANK)}, type: 2}}
  _audio: {{fileID: 51000011}}
  _particles: {{fileID: 51000012}}
''')
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.4.unity').read_text().replace('Jehad V1.4 Population Explorer','Jehad V1.5 World Explorer').replace('Jehad V1.4 - Session and menus','Jehad V1.5 - Session and menus')
# Keep its existing volume/UI click methods, disable its automatic pickup/restoration subscriptions.
old=re.search(r'^--- !u!114 &31000015\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group();scene=scene.replace(old,old.replace('m_Enabled: 1','m_Enabled: 0',1))
idx=scene.index('--- !u!1660057539');scene=scene[:idx]+''.join(parts)+scene[idx:];scene=scene.replace('  m_Roots:\n','  m_Roots:\n'+''.join(f'  - {{fileID: {r}}}\n' for r in roots));add(SCENE,scene)
patch='*** Begin Patch\n'
for path,content in FILES.items():
    dst=ROOT/path
    if dst.exists():
        if dst.read_text().rstrip()==content.rstrip():continue
        patch+='*** Update File: '+str(dst)+'\n@@\n'+'\n'.join('-'+l for l in dst.read_text().rstrip().split('\n'))+'\n'+'\n'.join('+'+l for l in content.rstrip().split('\n'))+'\n'
    else:patch+='*** Add File: '+str(dst)+'\n'+'\n'.join('+'+l for l in content.rstrip().split('\n'))+'\n'
patch+='*** End Patch\n'
if '*** Add File:' in patch or '*** Update File:' in patch:subprocess.run(['apply_patch'],input=patch,text=True,check=True,stdout=subprocess.DEVNULL)
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps({'scene':SCENE,'bank':BANK,'chest_prefab':CHEST,'chests':chests,'scripts':scripts,'materials':materials,'shader':SHADER,'meshes':meshpaths,'audio':audio,'files':sorted(FILES),'music_order':music,'cue_order':cues},indent=2)+'\n')
print(json.dumps({'scene':SCENE,'scripts':len(scripts),'audio_assets':len(audio['assets']),'chests':2,'loose_relics':14,'total_relics':16,'generated_text_assets':len(FILES)}))
