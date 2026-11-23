"""Opt the two authored chests into lid-synchronized reveal in V1.20."""
from pathlib import Path
import argparse,json,re,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add,guid
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_20/*.cs')}
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
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.19.unity').read_text()
def block(kind,fid):return re.search(r'^--- !u!'+str(kind)+' &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
before=block(1,51000000);line='  - component: {fileID: 51000019}\n';assert line in before
scene=scene.replace(before,before.replace(line,line+'  - component: {fileID: 51000020}\n'))
before=block(114,51005002);assert '.WorldPolishReviewV15' in before
after=re.sub(r'(m_Script: \{fileID: 11500000, guid: )[0-9a-f]{32}',lambda m:m[1]+guid(scripts['WorldPolishReviewV120']),before).replace('.WorldPolishReviewV15','.WorldPolishReviewV120')
scene=scene.replace(before,after)
new=f'''--- !u!114 &51000020
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 51000000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid(scripts['ChestRevealGateV120'])}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Echoheart.World.ChestRevealGateV120
  _chests:
  - {{fileID: 51001002}}
  - {{fileID: 51002002}}
'''
index=scene.index('--- !u!1660057539');scene=scene[:index]+new+scene[index:];scene=scene.replace('Jehad V1.19','Jehad V1.20')
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.20.unity';add(SCENE,scene)
written=[]
for path,content in FILES.items():
    dst=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not dst.exists() or dst.read_bytes()!=data:dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data);written.append(path)
report={'scene':SCENE,'scripts':scripts,'extended_source':'Assets/_Project/Scripts/World/Jehad/V1_5/SecretChestV15.cs','files':sorted(FILES),'added_components':[51000020],'rebound_components':[51005002],'extended_gameobjects':[51000000],'new_scene_roots':0,'reused':'Original chest/pickup/reset owners, 16 relics, opening audio/particles and all V1.19 presentation; lid gate defaults off for earlier scenes'}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'scene':SCENE,'new_scripts':len(scripts),'extended_sources':1,'written':len(written)}))
