"""Create the namespace-fix review scene without changing scene behavior."""
from pathlib import Path
import argparse,json,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
source='Assets/_Project/Scenes/Jehad/Jehad_V1.21.unity'
scene='Assets/_Project/Scenes/Jehad/Jehad_V1.22.unity'
add(scene,(ROOT/source).read_text().replace('Jehad V1.21','Jehad V1.22'))
written=[]
for path,content in FILES.items():
    target=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not target.exists() or target.read_bytes()!=data:
        target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data);written.append(path)
report={'scene':scene,'source_scene':source,'files':sorted(FILES),'fixed_source':'Assets/_Project/Scripts/World/Jehad/V1_21/AirborneFeedbackV121.cs','new_runtime_components':0,'new_scene_roots':0}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'scene':scene,'written':len(written),'new_runtime_components':0}))
