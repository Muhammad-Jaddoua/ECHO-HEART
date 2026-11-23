"""Add bounded collectible sparkles to a copy of V1.12; preserve existing owners."""
from pathlib import Path
import argparse,json,re,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add,guid
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_13/*.cs')}
assert len(scripts)==3
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
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.12.unity').read_text()
def block(kind,fid):return re.search(r'^--- !u!'+str(kind)+' &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
material=re.search(r'^  _material: (.*)$',block(114,51000012),re.M).group(1)
for gid,existing,added in [(51000000,51000016,51000017),(61001000,61001004,61001005)]:
    old=block(1,gid);line=f'  - component: {{fileID: {existing}}}\n';assert line in old
    scene=scene.replace(old,old.replace(line,line+f'  - component: {{fileID: {added}}}\n'))
def mono(fid,gid,name,area,fields):return f'''--- !u!114 &{fid}
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
{fields}'''
new=mono(51000017,51000000,'RelicGlintsV113','World',f'''  _flow: {{fileID: 31000011}}
  _counter: {{fileID: 31000014}}
  _camera: {{fileID: 546103920}}
  _material: {material}
''')+mono(61001005,61001000,'RelicGlintReviewV113','Testing','''  _flow: {fileID: 31000011}
  _glints: {fileID: 51000017}
  _chests:
  - {fileID: 51001002}
  - {fileID: 51002002}
''')
index=scene.index('--- !u!1660057539');scene=scene[:index]+new+scene[index:];scene=scene.replace('Jehad V1.12','Jehad V1.13')
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.13.unity';add(SCENE,scene)
written=[]
for path,content in FILES.items():
    dst=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not dst.exists() or dst.read_bytes()!=data:dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data);written.append(path)
report={'scene':SCENE,'scripts':scripts,'files':sorted(FILES),'added_components':[51000017,61001005],'changed_existing_components':[],'extended_gameobjects':[51000000,61001000],'new_scene_roots':0,'reused':'V1.12 guide; V1.11 Guardian; V1.10 notifications; V1.9 menus/HUD; V1.5 particle material; original world/rewards/AI/flow'}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'scene':SCENE,'scripts':len(scripts),'written':len(written)}))
