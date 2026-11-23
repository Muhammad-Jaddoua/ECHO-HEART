"""Create a queued-notification scene without changing reward, audio, particle or menu owners."""
from pathlib import Path
import argparse,json,re,sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from unity_authoring import FILES,EXISTING,add,guid
p=argparse.ArgumentParser();p.add_argument('--base-tree-json',type=Path);p.add_argument('--manifest',type=Path,required=True);args=p.parse_args()
if args.base_tree_json:EXISTING.update(x['path'] for x in json.loads(args.base_tree_json.read_text())['tree'])
scripts={f.stem:f.relative_to(ROOT).as_posix() for f in (ROOT/'Assets/_Project/Scripts').glob('**/V1_10/*.cs')}
assert len(scripts)==5
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
scene=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.9.unity').read_text()
def block(fid):return re.search(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
def replace(fid,old_name,new_name):
    old=block(fid);new=re.sub(r'guid: [0-9a-f]{32}',f'guid: {guid(scripts[new_name])}',old,count=1).replace(old_name,new_name)
    return old,new
old,new=replace(51000013,'WorldToastV15','WorldToastV110');new+='  _settings: {fileID: 61000011}\n';scene=scene.replace(old,new)
old,new=replace(61001002,'UiReviewHarnessV19','UiReviewHarnessV110');new=new.replace('  _toastSkin: {fileID: 61000012}','  _toast: {fileID: 51000013}');scene=scene.replace(old,new)
old=block(61000012);new=re.sub(r'guid: [0-9a-f]{32}',f'guid: {guid(scripts["WorldNoticesV110"])}',old,count=1)
new=new.replace('Echoheart.UI.WorldToastSkinV19','Echoheart.World.WorldNoticesV110')
new=new[:new.index('  _toast:')]+'''  _flow: {fileID: 31000011}
  _relics: {fileID: 31000014}
  _toast: {fileID: 51000013}
  _chests:
  - {fileID: 51001002}
  - {fileID: 51002002}
''';scene=scene.replace(old,new)
old=block(51000014);new=old.replace('  _toast: {fileID: 51000013}','  _toast: {fileID: 0}');scene=scene.replace(old,new)
secrets=[]
for match in re.finditer(r'^--- !u!114 &(\d+)\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S):
    if 'Echoheart.World.SecretDiscoveryV15\n' in match[0]:secrets.append(int(match[1]))
assert len(secrets)==2,secrets
for fid in secrets:
    old,new=replace(fid,'SecretDiscoveryV15','SecretDiscoveryV110');new+='  _toast: {fileID: 51000013}\n';scene=scene.replace(old,new)
scene=scene.replace('Jehad V1.9','Jehad V1.10')
SCENE='Assets/_Project/Scenes/Jehad/Jehad_V1.10.unity';add(SCENE,scene)
written=[]
for path,content in FILES.items():
    dst=ROOT/path;data=(content.rstrip()+'\n').encode()
    if not dst.exists() or dst.read_bytes()!=data:dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data);written.append(path)
report={'scene':SCENE,'scripts':scripts,'files':sorted(FILES),'secret_components':secrets,'changed_components':[51000013,61001002,61000012,51000014,*secrets],'reused_ui':'V1.9 menus/HUD/layout and V1.7 settings/factory; V1.8 focus history','reused_world':'V1.5 feedback/audio/particles/chests; V1.4 population; V1.3 flow; V1.2 pickups/landscape'}
args.manifest.parent.mkdir(parents=True,exist_ok=True);args.manifest.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'scene':SCENE,'scripts':len(scripts),'changed_components':report['changed_components'],'written':len(written)}))
