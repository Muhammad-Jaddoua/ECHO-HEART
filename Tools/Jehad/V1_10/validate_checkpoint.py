"""Static UI/scene checks, and reference-canvas geometry invariants. Unity behavior remains separate."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
import yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--baseline',type=Path);p.add_argument('--report',type=Path,required=True);p.add_argument('--dotnet',type=Path);args=p.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];text=scene.read_text()
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids))
normal=re.sub(r'^%.*\n','',text,flags=re.M);normal=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',normal,flags=re.M)
docs={int(fid):d for fid,d in zip(ids,yaml.safe_load_all(normal))}
meta={}
for f in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(f.read_text())['guid'];assert g not in meta,('duplicate GUID',g);meta[g]=f.with_suffix('')
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.9.unity').read_text()))
for line in text.splitlines():
    if 'guid:' not in line:
        for fid in re.findall(r'fileID: (-?\d+)',line):assert int(fid)==0 or int(fid) in docs,('dangling local ID',line)
for fid,g in re.findall(r'fileID: (-?\d+), guid: ([0-9a-f]{32})',text):
    assert g in meta or g in inherited,('unknown GUID',g)
    target=meta.get(g)
    if target is not None and target.suffix in {'.prefab','.asset','.mat'}:
        if fid=='100100000' and target.suffix=='.prefab':continue
        assert re.search(r'^--- !u!\d+ &'+fid+r'(?:\s|$)',target.read_text(),re.M),('invalid source component',target,fid)
for fid,d in docs.items():
    if 'Transform' in d:
        for child in d['Transform'].get('m_Children',[]):assert docs[child['fileID']]['Transform']['m_Father']['fileID']==fid
    for value in d.values():
        if isinstance(value,dict) and value.get('m_GameObject',{}).get('fileID',0):
            go=docs[value['m_GameObject']['fileID']]['GameObject']
            if 'm_Component' in go:assert any(c['component']['fileID']==fid for c in go['m_Component'])
assert len([d for d in docs.values() if 'AudioListener' in d])==1
assert len([d for d in docs.values() if 'PrefabInstance' in d])==5

expected={51000013:'WorldToastV110',61000012:'WorldNoticesV110',61001002:'UiReviewHarnessV110',51004002:'SecretDiscoveryV110',51004102:'SecretDiscoveryV110'}
assert m['secret_components']==[51004002,51004102]
for fid,name in expected.items():
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
assert docs[51000013]['MonoBehaviour']['_flow']['fileID']==31000011
assert docs[51000013]['MonoBehaviour']['_settings']['fileID']==61000011
assert docs[51000014]['MonoBehaviour']['_toast']['fileID']==0,'Old feedback must not also publish text'
observer=docs[61000012]['MonoBehaviour']
assert observer['_flow']['fileID']==31000011 and observer['_relics']['fileID']==31000014 and observer['_toast']['fileID']==51000013
assert [x['fileID'] for x in observer['_chests']]==[51001002,51002002]
assert docs[61001002]['MonoBehaviour']['_toast']['fileID']==51000013
assert docs[61001002]['MonoBehaviour']['_menu']['fileID']==31000016
assert docs[31000016]['MonoBehaviour']['m_EditorClassIdentifier'].endswith('FlowMenuV19')
assert docs[31000402]['MonoBehaviour']['m_EditorClassIdentifier'].endswith('FlowReviewHarnessV19')
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
for fid in m['secret_components']:
    obj=docs[fid]['MonoBehaviour'];assert obj['_toast']['fileID']==51000013 and obj['_feedback']['fileID']==51000014
assert all(name not in text for name in ('Echoheart.UI.WorldToastV15','Echoheart.UI.WorldToastSkinV19','Echoheart.World.SecretDiscoveryV15'))
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.9.unity').read_text();current=text.replace('Jehad V1.10','Jehad V1.9')
def component(content,fid):return re.search(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',content,re.M|re.S).group()
def common(block):return re.sub(r'guid: [0-9a-f]{32}','guid: SCRIPT',block)
for fid in m['changed_components']:
    old=component(previous,fid);new=component(current,fid)
    if fid==51000013:
        assert common(new.replace('WorldToastV110','WorldToastV15').replace('  _settings: {fileID: 61000011}\n',''))==common(old)
    elif fid==61001002:
        assert common(new.replace('UiReviewHarnessV110','UiReviewHarnessV19').replace('  _toast: {fileID: 51000013}','  _toastSkin: {fileID: 61000012}'))==common(old)
    elif fid in m['secret_components']:
        assert common(new.replace('SecretDiscoveryV110','SecretDiscoveryV15').replace('  _toast: {fileID: 51000013}\n',''))==common(old)
    elif fid==51000014:assert new.replace('  _toast: {fileID: 0}','  _toast: {fileID: 51000013}')==old
    elif fid==61000012:
        assert set(observer)=={'m_ObjectHideFlags','m_CorrespondingSourceObject','m_PrefabInstance','m_PrefabAsset','m_GameObject','m_Enabled','m_EditorHideFlags','m_Script','m_Name','m_EditorClassIdentifier','_flow','_relics','_toast','_chests'}
    current=current.replace(new,old)
assert current==previous,'Unintended gameplay/scene changes'
parser=Parser(Language(tree_sitter_c_sharp.language()))
for name,path in m['scripts'].items():
    f=ROOT/path;assert not parser.parse(f.read_bytes()).root_node.has_error,('C# syntax',f)
    assert re.search(r'\bclass\s+'+name+r'\b',f.read_text()) and Path(str(f)+'.meta').exists()
for d in docs.values():
    if 'MonoBehaviour' not in d:continue
    obj=d['MonoBehaviour'];target=meta.get(obj['m_Script']['guid'])
    if target not in [ROOT/p for p in m['scripts'].values()]:continue
    fields=set(re.findall(r'\[SerializeField(?:,[^\]]*)?\]\s*private\s+[\w<>\[\]]+\s+(_\w+)',target.read_text()))
    assert all(k.startswith('m_') or k in fields for k in obj),('unserialized field',target)

secret=(ROOT/m['scripts']['SecretDiscoveryV110']).read_text().replace('SecretDiscoveryV110','SecretDiscoveryV15').replace('using Echoheart.UI;\n','')
secret=re.sub(r'^.*\[SerializeField\] private WorldToastV110 _toast;\n','',secret,flags=re.M)
secret=re.sub(r'^.*if \(_toast != null\) _toast.Show\(.*\n','',secret,flags=re.M)
assert secret==(ROOT/'Assets/_Project/Scripts/World/Jehad/V1_5/SecretDiscoveryV15.cs').read_text(),'Secret rules changed beyond added presentation'
toast=(ROOT/m['scripts']['WorldToastV110']).read_text();notices=(ROOT/m['scripts']['WorldNoticesV110']).read_text()
assert 'Time.unscaledDeltaTime,Playing' in toast and '_group.blocksRaycasts=false' in toast and 'GraphicRaycaster' not in toast
assert 'UiLayoutV19.Surface' in toast and 'UiLayoutV19.SafeArea' in toast and '820,104' in toast
assert 'state==GameState.Title||state==GameState.Dead||state==GameState.Complete' in toast
assert 'state==GameState.Playing&&_resetting' in notices and 'Capture();_resetting=false;' in notices
assert '_toast.Dismiss("boss:start")' in notices
assert all('GameEvents.Raise' not in source for source in (toast,notices)),'Presentation must not publish progression'
assert all('PlayerPrefs' not in (ROOT/path).read_text() for path in m['scripts'].values())
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert changed==['docs/reference/contract-changes.md'] or not changed,changed
compiled={}
if args.dotnet:
    for label,path in [('notification','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'scene_yaml_documents':len(docs),'compiled_tests':compiled,'single_notification_presenter':True,'old_feedback_text_disabled_only':True,'secret_rule_source_preserved':True,'gameplay_scene_preserved_beyond_declared_bindings':True,'menu_hud_audio_particles_chests_preserved':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_render_input_audio_display_harnesses':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
