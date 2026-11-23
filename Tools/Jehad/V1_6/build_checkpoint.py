"""Version the complete UI scene while keeping V1.0-V1.5 and shared gameplay implementations intact."""
from pathlib import Path
import argparse,json,re,subprocess,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import *
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.6.unity'
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_6/*.cs')}
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
parts=[];roots=[]
def obj(gid,name,components=(),tag='Untagged'):
    go=remap(T_GO,{600295488:gid}).replace('m_Name: Ground','m_Name: '+name).replace('m_Layer: 13','m_Layer: 0').replace('m_TagString: Untagged','m_TagString: '+tag)
    go=re.sub(r'm_Component:\n(?:  - component:.*\n)+','m_Component:\n'+''.join(f'  - component: {{fileID: {c}}}\n' for c in [gid+1,*components]),go)
    parts.extend([go,transform(gid+1,gid,0)]);roots.append(gid+1)
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
obj(61000000,'Jehad V1.6 - Interface settings',components=(61000011,61000012))
mono(61000011,61000000,'UiSettingsV16','UI','  _audio: {fileID: 31000015}\n')
mono(61000012,61000000,'WorldToastSkinV16','UI','  _toast: {fileID: 51000013}\n  _settings: {fileID: 61000011}\n')
obj(61001000,'Jehad V1.6 UI acceptance harness',components=(61001002,),tag='EditorOnly')
mono(61001002,61001000,'UiReviewHarnessV16','Testing','''  _flow: {fileID: 31000011}
  _menu: {fileID: 31000016}
  _settings: {fileID: 61000011}
  _volumes: {fileID: 31000015}
  _worldAudio: {fileID: 51000011}
''')
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.5.unity').read_text().replace('Jehad V1.5 World Explorer','Jehad V1.6 UI Explorer').replace('Jehad V1.5 - Session and menus','Jehad V1.6 - Session and menus')
old=re.search(r'^--- !u!114 &31000016\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
new=re.sub(r'guid: [0-9a-f]{32}',f'guid: {guid(scripts["FlowMenuV16"])}',old,count=1).replace('FlowMenuV13','FlowMenuV16').replace('  _showReviewHud: 1\n','  _settings: {fileID: 61000011}\n')
scene=scene.replace(old,new)
old=re.search(r'^--- !u!114 &31000402\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
new=re.sub(r'guid: [0-9a-f]{32}',f'guid: {guid(scripts["FlowReviewHarnessV16"])}',old,count=1).replace('FlowReviewHarnessV13','FlowReviewHarnessV16');scene=scene.replace(old,new)
scene=scene.replace('m_Name: Jehad flow acceptance harness','m_Name: Jehad V1.6 flow acceptance harness')
idx=scene.index('--- !u!1660057539');scene=scene[:idx]+''.join(parts)+scene[idx:];scene=scene.replace('  m_Roots:\n','  m_Roots:\n'+''.join(f'  - {{fileID: {r}}}\n' for r in roots));add(SCENE,scene)
patch='*** Begin Patch\n'
for path,content in FILES.items():
    dst=ROOT/path
    if dst.exists():
        if dst.read_text().rstrip()==content.rstrip():continue
        patch+='*** Update File: '+str(dst)+'\n@@\n'+'\n'.join('-'+l for l in dst.read_text().rstrip().split('\n'))+'\n'+'\n'.join('+'+l for l in content.rstrip().split('\n'))+'\n'
    else:patch+='*** Add File: '+str(dst)+'\n'+'\n'.join('+'+l for l in content.rstrip().split('\n'))+'\n'
patch+='*** End Patch\n'
if '*** Add File:' in patch or '*** Update File:' in patch:subprocess.run(['apply_patch'],input=patch,text=True,check=True,stdout=subprocess.DEVNULL)
report={'scene':SCENE,'scripts':scripts,'files':sorted(FILES),'roots':roots,'reused_world':'V1.5 audio/particles/chests; V1.4 population; V1.3 flow; V1.2 landscape','replaced_scene_components':{'31000016':'FlowMenuV16','31000402':'FlowReviewHarnessV16'}}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'scene':SCENE,'scripts':len(scripts),'text_assets_written':len(FILES)}))
