"""Build the combined Jehad closeout review checkpoint."""
from pathlib import Path
import argparse,json,re,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add,guid
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_27/*.cs')}
assert len(scripts)==1
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
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.26.unity').read_text()
def block(kind,fid):return re.search(r'^--- !u!'+str(kind)+' &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
for go,last,newid in [(61001000,61001012,61001013)]:
    before=block(1,go);line='  - component: {fileID: '+str(last)+'}\n';assert line in before
    scene=scene.replace(before,before.replace(line,line+'  - component: {fileID: '+str(newid)+'}\n'))
def component(fid,go,name,namespace,fields):
    return f'''--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(scripts[name])}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.{namespace}.{name}
{fields}'''
bank=re.search(r'^  _bank: (\{.*\})$',block(114,51000011),re.M).group(1)
fields='  _flow: {fileID: 31000011}\n  _menus: {fileID: 31000016}\n  _feedback: {fileID: 51000014}\n  _bank: '+bank+'\n'
new=component(61001013,61001000,'JehadFinalReviewV127','Testing',fields)
index=scene.index('--- !u!1660057539');scene=scene[:index]+new+scene[index:];scene=scene.replace('Jehad V1.26','Jehad V1.27')
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.27.unity';add(SCENE,scene)
written=[]
for path,content in FILES.items():
    dst=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not dst.exists() or dst.read_bytes()!=data:dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data);written.append(path)
report={'scene':SCENE,'scripts':scripts,'extended_sources':['Assets/_Project/Scripts/World/Jehad/V1_5/WorldFeedbackV15.cs','Assets/_Project/Scripts/World/Jehad/V1_10/WorldNoticesV110.cs'],'files':sorted(FILES),'added_components':[61001013],'new_runtime_components':0,'extended_gameobjects':[61001000],'new_scene_roots':0,'reused':'All V1.26 runtime owners, assets and bindings; combined acceptance fixture plus deferred paused pickup feedback and corrected chest wording'}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'scene':SCENE,'new_scripts':len(scripts),'extended_sources':2,'written':len(written)}))
