"""Bind immediate health/loss-trail HUD presentation in a copy of V1.14."""
from pathlib import Path
import argparse,json,re,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add,guid
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_15/*.cs')}
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
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.14.unity').read_text()
def block(fid):return re.search(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
updates=[(31000016,'FlowMenuV114','FlowMenuV115'),(31000402,'FlowReviewHarnessV114','FlowReviewHarnessV115'),(61001002,'UiReviewHarnessV114','UiReviewHarnessV115')]
for fid,old,new in updates:
    before=block(fid);assert before.count('.'+old)==1
    after=re.sub(r'(m_Script: \{fileID: 11500000, guid: )[0-9a-f]{32}',lambda m:m[1]+guid(scripts[new]),before).replace('.'+old,'.'+new)
    scene=scene.replace(before,after)
scene=scene.replace('Jehad V1.14','Jehad V1.15')
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.15.unity';add(SCENE,scene)
written=[]
for path,content in FILES.items():
    dst=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not dst.exists() or dst.read_bytes()!=data:dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data);written.append(path)
report={'scene':SCENE,'scripts':scripts,'files':sorted(FILES),'added_components':[],'rebound_components':[x[0] for x in updates],'new_scene_roots':0,'reused':'V1.14 preferences; V1.9 layout/navigation; all prior world/audio/flow/combat owners; new passive health presentation only'}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'scene':SCENE,'scripts':len(scripts),'written':len(written)}))
