"""Opt V1.19 into immediate ambient silence at zero SFX volume."""
from pathlib import Path
import argparse,json,re,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add,guid
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_19/*.cs')}
assert len(scripts)==2
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
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.18.unity').read_text()
def block(kind,fid):return re.search(r'^--- !u!'+str(kind)+' &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
for gid,existing,added in [(51000000,51000018,51000019),(61001000,61001007,61001008)]:
    before=block(1,gid);line=f'  - component: {{fileID: {existing}}}\n';assert line in before
    scene=scene.replace(before,before.replace(line,line+f'  - component: {{fileID: {added}}}\n'))
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
new=mono(51000019,51000000,'AmbientMuteV119','Audio','''  _settings: {fileID: 61000011}
  _audio: {fileID: 51000011}
''')+mono(61001008,61001000,'AmbientMuteReviewV119','Testing','''  _flow: {fileID: 31000011}
  _settings: {fileID: 61000011}
  _preferences: {fileID: 61000013}
  _audio: {fileID: 51000011}
''')
index=scene.index('--- !u!1660057539');scene=scene[:index]+new+scene[index:];scene=scene.replace('Jehad V1.18','Jehad V1.19')
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.19.unity';add(SCENE,scene)
written=[]
for path,content in FILES.items():
    dst=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not dst.exists() or dst.read_bytes()!=data:dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data);written.append(path)
report={'scene':SCENE,'scripts':scripts,'extended_source':'Assets/_Project/Scripts/Audio/Jehad/V1_5/WorldAudioV15.cs','files':sorted(FILES),'added_components':[51000019,61001008],'changed_existing_components':[],'extended_gameobjects':[51000000,61001000],'new_scene_roots':0,'reused':'Existing UI settings/audio owner, ambient fades and V1.18 boss resolution; older scenes do not opt into immediate ambient mute'}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'scene':SCENE,'new_scripts':len(scripts),'extended_sources':1,'written':len(written)}))
