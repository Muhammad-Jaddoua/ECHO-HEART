"""Author the V1.3 scene/bindings without changing V1.2 assets. Uses apply_patch."""
from pathlib import Path
import sys,re,json,argparse,subprocess

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import *

parser=argparse.ArgumentParser();parser.add_argument('--base-tree-json',type=Path);parser.add_argument('--manifest',type=Path,default=ROOT/'Temp/JehadV13/manifest.json');args=parser.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.3.unity'
GATE='Assets/_Project/Prefabs/World/Jehad/V1_3/ArenaGate_V1_3.prefab'
PREFIX='Assets/_Project/Scripts/'
scriptpaths={p.stem:str(p.relative_to(ROOT)) for p in (ROOT/PREFIX).glob('**/V1_3/*.cs')}
for name,path in scriptpaths.items():
    add(path,(ROOT/path).read_text())
    FILES[path+'.meta']=f'''fileFormatVersion: 2
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

gate=Area('ArenaGate_Jehad_V1_3')
for i,x in enumerate([-3.6,-2.4,-1.2,0,1.2,2.4,3.6]):gate.obj('GateBar_'+str(i),(x,1.5,0),(.20,3,.25),'Cliff',collider=False)
for y in [.25,2.75]:gate.obj('GateBrace_'+str(y),(0,y,0),(7.8,.20,.30),'Gold',collider=False)
gid,tid,cid=gate.next_id,gate.next_id+1,gate.next_id+4;gate.children.append(tid)
mp={600295488:gid,600295492:tid,600295490:cid}
go=remap(T_GO,mp).replace('m_Name: Ground','m_Name: Gate blocker')
go=re.sub(r'm_Component:\n(?:  - component:.*\n)+',f'm_Component:\n  - component: {{fileID: {tid}}}\n  - component: {{fileID: {cid}}}\n',go)
gate.parts.extend([go,transform(tid,gid,10001,(0,1.5,0),(7.8,3,.4)),remap(T_COLLIDER,mp)])
gtext=gate.build().replace('  m_IsActive: 1','  m_IsActive: 0',1)
add(GATE,gtext,'prefab')

parts=[];roots=[]
def obj(gid,name,position=(0,0,0),components=(),parent=0,layer=0,tag='Untagged',children=()):
    tid=gid+1
    go=remap(T_GO,{600295488:gid}).replace('m_Name: Ground','m_Name: '+name).replace('m_Layer: 13','m_Layer: '+str(layer)).replace('m_TagString: Untagged','m_TagString: '+tag)
    go=re.sub(r'm_Component:\n(?:  - component:.*\n)+', 'm_Component:\n'+''.join(f'  - component: {{fileID: {i}}}\n' for i in [tid,*components]),go)
    parts.extend([go,transform(tid,gid,parent,position,children=children)])
    if not parent:roots.append(tid)
    return tid

def mono(fid,gid,cls,namespace,fields):
    parts.append(f'''--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(scriptpaths[cls])}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.{namespace}.{cls}
{fields}''')

scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.2.unity').read_text()
explorer=scriptpaths['JehadFlowExplorerV13']
scene=scene.replace('m_Name: Jehad V1.2 Explorer','m_Name: Jehad V1.3 Flow Explorer')
old=re.search(r'^--- !u!114 &1395263160\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
new=re.sub(r'guid: [0-9a-f]{32}',f'guid: {guid(explorer)}',old,count=1).replace('JehadWorldReviewV12','JehadFlowExplorerV13')
scene=scene.replace(old,new)
playergo=re.search(r'^--- !u!1 &1395263158\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
scene=scene.replace(playergo,playergo.replace('  m_Layer: 8','  - component: {fileID: 1395263170}\n  m_Layer: 8'))
parts.append('''--- !u!114 &1395263170
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1395263158}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe008e0334370dc42a2159149a49dfb3, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.Combat.HealthEventRelay
  _mode: 0
''')
spawn=re.search(r'm_LocalPosition: \{x: 0, y: ([^,]+), z: -100\}',scene)
spawn_y=float(spawn[1])

manager=31000000;flow=31000011;binding=31000012;retry=31000013;counter=31000014;audio=31000015;menus=31000016
pointgids=[31000100,31000110,31000120,31000130,31000140]
obj(manager,'Jehad V1.3 - Session and menus',components=(flow,binding,retry,counter,audio,menus),children=[p+1 for p in pointgids])
for gid,name,pos in zip(pointgids,['Main retry','Arena retry','Guardian spawn','Arena review entry','Heart review approach'],[(0,spawn_y,-100),(42,9.1,91.5),(42,8,102),(42,9.1,92),(42,9.1,110)]):obj(gid,name,pos,parent=manager+1)
mono(flow,manager,'SessionFlowV13','World',f'''  _player: {{fileID: {binding}}}
  _retry: {{fileID: {retry}}}
  _relics: {{fileID: {counter}}}
  _arena: {{fileID: 31000202}}
  _mainRetryPoint: {{fileID: 31000101}}
  _bossRetryPoint: {{fileID: 31000111}}
  _camera: {{fileID: 546103918}}
  _lockCursorWhilePlaying: 0
  _titlePosition: {{x: 101, y: 25, z: -60}}
  _titleLookAt: {{x: 35, y: 10, z: 8}}
''')
mono(binding,manager,'PlayerFlowBindingV13','World','''  _player: {fileID: 1395263164}
  _health: {fileID: 1395263159}
  _gameplayInput:
  - {fileID: 1395263160}
  _onRespawned:
    m_PersistentCalls:
      m_Calls:
      - m_Target: {fileID: 1395263160}
        m_TargetAssemblyTypeName: Echoheart.Testing.JehadFlowExplorerV13, Assembly-CSharp
        m_MethodName: ResetMotion
        m_Mode: 1
        m_Arguments:
          m_ObjectArgument: {fileID: 0}
          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
          m_IntArgument: 0
          m_FloatArgument: 0
          m_StringArgument:
          m_BoolArgument: 0
        m_CallState: 2
  _clearAbilityRequested:
    m_PersistentCalls:
      m_Calls: []
  _equipAbilityRequested:
    m_PersistentCalls:
      m_Calls: []
  _publishReviewAbilityChanges: 1
''')
mono(retry,manager,'RetryServiceV13','World','')
mono(counter,manager,'RelicCounterV13','World','  _worldRoot: {fileID: 20000001}\n')
mono(audio,manager,'MenuAudioV13','Audio','  _musicVolume: 0.65\n  _sfxVolume: 0.8\n  _musicSources: []\n  _sfxSources: []\n')
mono(menus,manager,'FlowMenuV13','UI',f'  _flow: {{fileID: {flow}}}\n  _audio: {{fileID: {audio}}}\n  _showReviewHud: 1\n')

obj(31000200,'Guardian encounter volume',(42,10,103),components=(31000202,31000203),layer=15)
mono(31000202,31000200,'ArenaEncounterV13','World',f'''  _flow: {{fileID: {flow}}}
  _bossPrefab: {{fileID: 5973304569983579869, guid: 3826a2c2a5d73a14e92af1648366817b, type: 3}}
  _arenaRoot: {{fileID: 20000011}}
  _bossPoint: {{fileID: 31000121}}
  _gate: {{fileID: 31500000}}
  _navigationBounds:
    m_Center: {{x: 42, y: 10, z: 102}}
    m_Extent: {{x: 16, y: 8, z: 16}}
''')
collider=remap(T_COLLIDER,{600295488:31000200,600295490:31000203})
collider=collider.replace('m_IsTrigger: 0','m_IsTrigger: 1').replace('m_Size: {x: 1, y: 1, z: 1}','m_Size: {x: 24, y: 6, z: 24}')
parts.append(collider)
obj(31000300,'Island Heart interaction',(42,9.8,112),components=(31000302,))
mono(31000302,31000300,'IslandHeartV13','World',f'''  _flow: {{fileID: {flow}}}
  _visual: {{fileID: 31000601}}
  _renderer: {{fileID: 31000603}}
  _interactDistance: 3
''')
obj(31000400,'Jehad flow acceptance harness',components=(31000402,),tag='EditorOnly')
mono(31000402,31000400,'FlowReviewHarnessV13','Testing',f'''  _flow: {{fileID: {flow}}}
  _arena: {{fileID: 31000202}}
  _heart: {{fileID: 31000302}}
  _menus: {{fileID: {menus}}}
  _arenaEntryPoint: {{fileID: 31000131}}
  _heartApproach: {{fileID: 31000141}}
  _sword: {{fileID: 11400000, guid: 9ed6550c8b5a0f44f87461d512129dc2, type: 2}}
''')
arena_path='Assets/_Project/Prefabs/World/Jehad/V1_2/BossArena_V1_2.prefab';ag=guid(arena_path)
for typ,fid,source,name in [(4,31000601,10211,'Transform'),(23,31000603,10213,'MeshRenderer')]:
    parts.append(f'''--- !u!{typ} &{fid} stripped
{name}:
  m_CorrespondingSourceObject: {{fileID: {source}, guid: {ag}, type: 3}}
  m_PrefabInstance: {{fileID: 20000010}}
  m_PrefabAsset: {{fileID: 0}}
''')
gg=guid(GATE)
mods=''
for axis,value in zip('xyz',(42,8,88)):mods+=f'    - target: {{fileID: 10001, guid: {gg}, type: 3}}\n      propertyPath: m_LocalPosition.{axis}\n      value: {value}\n      objectReference: {{fileID: 0}}\n'
parts.append(f'''--- !u!1001 &31500010
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
  m_SourcePrefab: {{fileID: 100100000, guid: {gg}, type: 3}}
--- !u!4 &31500001 stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: 10001, guid: {gg}, type: 3}}
  m_PrefabInstance: {{fileID: 31500010}}
  m_PrefabAsset: {{fileID: 0}}
--- !u!1 &31500000 stripped
GameObject:
  m_CorrespondingSourceObject: {{fileID: 10000, guid: {gg}, type: 3}}
  m_PrefabInstance: {{fileID: 31500010}}
  m_PrefabAsset: {{fileID: 0}}
''')
roots.append(31500001)
idx=scene.index('--- !u!1660057539');scene=scene[:idx]+''.join(parts)+scene[idx:]
scene=scene.replace('  m_Roots:\n','  m_Roots:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in roots))
add(SCENE,scene)

patch='*** Begin Patch\n'
for path,content in FILES.items():
    dst=ROOT/path
    if dst.exists():
        if dst.read_text().rstrip()==content.rstrip():continue
        patch+='*** Update File: '+str(dst)+'\n@@\n'+'\n'.join('-'+l for l in dst.read_text().rstrip().split('\n'))+'\n'+'\n'.join('+'+l for l in content.rstrip().split('\n'))+'\n'
    else:patch+='*** Add File: '+str(dst)+'\n'+'\n'.join('+'+l for l in content.rstrip().split('\n'))+'\n'
patch+='*** End Patch\n'
if '*** Add File:' in patch or '*** Update File:' in patch:subprocess.run(['apply_patch'],input=patch,text=True,check=True,stdout=subprocess.DEVNULL)
args.manifest.parent.mkdir(parents=True,exist_ok=True)
args.manifest.write_text(json.dumps({'scene':SCENE,'gate_prefab':GATE,'scripts':scriptpaths,'files':sorted(FILES),'roots':roots},indent=2)+'\n')
print(json.dumps({'scene':SCENE,'scripts':len(scriptpaths),'generated_files':len(FILES),'scene_bytes':len(scene.encode()),'gate_prefab':GATE}))
