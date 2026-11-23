"""Create V1.7 from V1.6, replacing only the owned UI and typed review components."""
from pathlib import Path
import argparse,json,re,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add,guid
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_7/*.cs')}
assert len(scripts)==12
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
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.6.unity').read_text()
replacements={31000016:'FlowMenuV17',31000402:'FlowReviewHarnessV17',61000011:'UiSettingsV17',61000012:'WorldToastSkinV17',61001002:'UiReviewHarnessV17'}
for fid,name in replacements.items():
    old=re.search(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
    new=re.sub(r'guid: [0-9a-f]{32}',f'guid: {guid(scripts[name])}',old,count=1).replace(name.replace('V17','V16'),name)
    scene=scene.replace(old,new)
scene=scene.replace('Jehad V1.6','Jehad V1.7')
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.7.unity';add(SCENE,scene)
written=[]
for path,content in FILES.items():
    dst=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not dst.exists() or dst.read_bytes()!=data:dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data);written.append(path)
report={'scene':SCENE,'scripts':scripts,'files':sorted(FILES),'replaced_scene_components':replacements,'reused_ui':'V1.6 graphic primitives, preference values and preview timer','reused_world':'V1.5 presentation; V1.4 population; V1.3 flow; V1.2 landscape'}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'scene':SCENE,'scripts':len(scripts),'written':len(written)}))
