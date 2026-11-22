"""Create only V1.4 assets; reuse the V1.3 flow and V1.2 landscape unchanged."""
from pathlib import Path
import argparse,json,re,subprocess,sys

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import *
from landscape import height

p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.4.unity'
SCRIPT_ROOT=ROOT/'Assets/_Project/Scripts'
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in SCRIPT_ROOT.glob('**/V1_4/*.cs')}
for name,path in scripts.items():
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

enemies={
 'Sword':('f13a87f93b657ea4fa8c7e65c57452a6',2098280771408410335,'Gold'),
 'Bow':('b5ad6b658f8ab36478e9790043520db8',6657649362964423119,'Pine'),
 'Fire':('86f0ddf849670194c8bc4284a118971e',5712756238236456871,'Amber')}
prefabs={}
for power,(eg,ef,material) in enemies.items():
    path=f'Assets/_Project/Prefabs/World/Jehad/V1_4/Source_{power}_V1_4.prefab'
    a=Area('Replenishing '+power+' source V1.4')
    # A modest color marker identifies the existing enemy source; no new collision or interaction rules.
    a.obj('Source stone',(0,.015,0),(1.6,.03,1.6),'Stone',collider=False,yaw=45)
    a.obj(power+' marker post',(1.35,.45,0),(.18,.9,.18),'Wood',collider=False)
    a.obj(power+' marker light',(1.35,1.0,0),(.28,.28,.28),material,shape='sphere',collider=False)
    t=a.build().replace('  - component: {fileID: 10001}\n','  - component: {fileID: 10001}\n  - component: {fileID: 10002}\n',1).replace('  m_IsActive: 1','  m_IsActive: 0',1)
    t+=f'''--- !u!114 &10002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 10000}}
  m_Enabled: 0
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 04075f042e34a72459794ff02e05bec1, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.AI.EnemySpawner
  _enemyPrefab: {{fileID: {ef}, guid: {eg}, type: 3}}
  _spawnPoint: {{fileID: 10001}}
  _respawnDelay: 6
  _minPlayerDistance: 12
  _spawnOnStart: 1
  _resetOnBossArenaRetry: 0
  _countsTowardCap: 1
'''
    add(path,t,'prefab');prefabs[power]=path

parts=[];roots=[]
def obj(gid,name,pos=(0,0,0),components=(),parent=0,children=(),tag='Untagged'):
    go=remap(T_GO,{600295488:gid}).replace('m_Name: Ground','m_Name: '+name).replace('m_Layer: 13','m_Layer: 0').replace('m_TagString: Untagged','m_TagString: '+tag)
    go=re.sub(r'm_Component:\n(?:  - component:.*\n)+','m_Component:\n'+''.join(f'  - component: {{fileID: {c}}}\n' for c in [gid+1,*components]),go)
    parts.extend([go,transform(gid+1,gid,parent,pos,children=children)])
    if parent==0:roots.append(gid+1)

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

sources=[]
for i,(name,power,x,z) in enumerate([
 ('Entry Sword','Sword',10,-88),('Entry Bow','Bow',-8,-76),('Entry Fire','Fire',10,-68),
 ('Approach Sword','Sword',30,70),('Approach Bow','Bow',40,76),('Approach Fire','Fire',50,76)]):
    sources.append({'name':name,'power':power,'point':41000101+i*10,'position':[x,height(x,z)+.1,z],'prefab':prefabs[power]})
obj(41000000,'Jehad V1.4 - World navigation and population',components=(41000011,41000012),children=[s['point'] for s in sources])
for s in sources:obj(s['point']-1,s['name'],s['position'],parent=41000001)
mono(41000011,41000000,'MainNavigationV14','World','''  _worldRoot: {fileID: 20000001}
  _enemyPrefabs:
'''+''.join(f'  - {{fileID: {ef}, guid: {eg}, type: 3}}\n' for eg,ef,_ in enemies.values())+'  _sourcePoints:\n'+''.join(f"  - {{fileID: {s['point']}}}\n" for s in sources)+'''  _bounds:
    m_Center: {x: 0, y: 30, z: 0}
    m_Extent: {x: 120, y: 50, z: 120}
  _arenaExclusion:
    m_Center: {x: 42, y: 30, z: 102}
    m_Extent: {x: 17, y: 50, z: 17}
''')
mono(41000012,41000000,'EnemyPopulationV14','World','''  _navigation: {fileID: 41000011}
  _flow: {fileID: 31000011}
  _sources:
'''+''.join(f"  - _prefab: {{fileID: 10002, guid: {guid(s['prefab'])}, type: 3}}\n    _point: {{fileID: {s['point']}}}\n" for s in sources))
obj(41001000,'Jehad V1.4 population acceptance harness',components=(41001002,),tag='EditorOnly')
mono(41001002,41001000,'PopulationReviewV14','Testing','''  _navigation: {fileID: 41000011}
  _population: {fileID: 41000012}
  _flow: {fileID: 31000011}
  _mainPoint: {fileID: 31000101}
  _arenaEntry: {fileID: 31000131}
''')
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.3.unity').read_text().replace('Jehad V1.3 Flow Explorer','Jehad V1.4 Population Explorer').replace('Jehad V1.3 - Session and menus','Jehad V1.4 - Session and menus')
idx=scene.index('--- !u!1660057539');scene=scene[:idx]+''.join(parts)+scene[idx:]
scene=scene.replace('  m_Roots:\n','  m_Roots:\n'+''.join(f'  - {{fileID: {r}}}\n' for r in roots))
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
manifest={'scene':SCENE,'sources':sources,'source_prefabs':prefabs,'scripts':scripts,'files':sorted(FILES),'roots':roots}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({'scene':SCENE,'sources':len(sources),'prefabs':len(prefabs),'scripts':len(scripts),'generated_files':len(FILES)}))
