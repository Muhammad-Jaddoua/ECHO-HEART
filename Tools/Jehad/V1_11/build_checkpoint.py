"""Add a scene-owned Guardian presenter while preserving the original boss prefab and combat."""
from pathlib import Path
import argparse,json,re,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add,guid
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_11/*.cs')}
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
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.10.unity').read_text()
def block(kind,fid):return re.search(r'^--- !u!'+str(kind)+' &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
old=block(114,51000014);scene=scene.replace(old,old.replace('  _arena: {fileID: 31000202}','  _arena: {fileID: 0}'))
for gid,existing,added in [(51000000,51000014,51000015),(61001000,61001002,61001003)]:
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
new=mono(51000015,51000000,'GuardianFeedbackV111','World','''  _flow: {fileID: 31000011}
  _arena: {fileID: 31000202}
  _feedback: {fileID: 51000014}
  _bank: {fileID: 11400000, guid: 30da399551cd54af99f0efdc7ed20c98, type: 2}
  _warningMaterial: {fileID: 2100000, guid: d9f2be90796d5de6944f9097bec1cec9, type: 2}
  _muzzlePath: Muzzle
''')+mono(61001003,61001000,'GuardianReviewV111','Testing','''  _flow: {fileID: 31000011}
  _arena: {fileID: 31000202}
  _presentation: {fileID: 51000015}
  _arenaEntryPoint: {fileID: 31000131}
''')
index=scene.index('--- !u!1660057539');scene=scene[:index]+new+scene[index:];scene=scene.replace('Jehad V1.10','Jehad V1.11')
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.11.unity';add(SCENE,scene)
written=[]
for path,content in FILES.items():
    dst=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not dst.exists() or dst.read_bytes()!=data:dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data);written.append(path)
report={'scene':SCENE,'scripts':scripts,'files':sorted(FILES),'added_components':[51000015,61001003],'changed_existing_components':[51000014],'extended_gameobjects':[51000000,61001000],'new_scene_roots':0,'reused':'Original boss prefab/definition/AI; V1.5 actor feedback/audio bank/particles; V1.10 notifications; V1.9 UI; V1.3 flow'}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'scene':SCENE,'scripts':len(scripts),'written':len(written)}))
