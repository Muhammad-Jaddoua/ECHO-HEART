"""Add audio/interface preference persistence in a copy of V1.13."""
from pathlib import Path
import argparse,json,re,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add,guid
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_14/*.cs')}
assert len(scripts)==7
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
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.13.unity').read_text()
def block(kind,fid):return re.search(r'^--- !u!'+str(kind)+' &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
updates=[(31000016,'FlowMenuV19','FlowMenuV114'),(31000402,'FlowReviewHarnessV19','FlowReviewHarnessV114'),(61001002,'UiReviewHarnessV110','UiReviewHarnessV114')]
for fid,old,new in updates:
    before=block(114,fid);assert before.count('.'+old)==1
    after=re.sub(r'(m_Script: \{fileID: 11500000, guid: )[0-9a-f]{32}',lambda m:m[1]+guid(scripts[new]),before).replace('.'+old,'.'+new)
    if fid==31000016:after=after.replace('  _settings: {fileID: 61000011}\n','  _settings: {fileID: 61000011}\n  _preferences: {fileID: 61000013}\n')
    if fid==61001002:after=after.replace('  _toast: {fileID: 51000013}\n','  _toast: {fileID: 51000013}\n  _preferences: {fileID: 61000013}\n')
    scene=scene.replace(before,after)
before=block(1,61000000);line='  - component: {fileID: 61000012}\n';assert line in before
scene=scene.replace(before,before.replace(line,line+'  - component: {fileID: 61000013}\n'))
new=f'''--- !u!114 &61000013
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 61000000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(scripts['PreferenceStoreV114'])}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.UI.PreferenceStoreV114
  _settings: {{fileID: 61000011}}
  _flow: {{fileID: 31000011}}
'''
index=scene.index('--- !u!1660057539');scene=scene[:index]+new+scene[index:];scene=scene.replace('Jehad V1.13','Jehad V1.14')
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.14.unity';add(SCENE,scene)
written=[]
for path,content in FILES.items():
    dst=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not dst.exists() or dst.read_bytes()!=data:dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data);written.append(path)
report={'scene':SCENE,'scripts':scripts,'files':sorted(FILES),'added_components':[61000013],'rebound_components':[x[0] for x in updates],'extended_gameobjects':[61000000],'new_scene_roots':0,'reused':'UiSettingsV17, V1.9 HUD/layout/navigation, all V1.13 world presentation and gameplay owners; no display or run-state persistence'}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'scene':SCENE,'scripts':len(scripts),'written':len(written)}))
