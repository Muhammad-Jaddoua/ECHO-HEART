"""Static title vista/camera guards. Native Unity behavior remains separate."""
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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.15.unity').read_text()))
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

assert len(docs)==124
for fid,name in [(61000014,'TitleVistaV116'),(61001006,'TitleVistaReviewV116')]:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
vista=docs[61000014]['MonoBehaviour'];review=docs[61001006]['MonoBehaviour'];flow=docs[31000011]['MonoBehaviour']
assert {key:vista[key]['fileID'] for key in ('_flow','_menu','_settings','_camera')}=={'_flow':31000011,'_menu':31000016,'_settings':61000011,'_camera':546103918}
assert vista['_camera']==flow['_camera']==review['_camera']
assert vista['_focusPoint']==flow['_titleLookAt']
assert 'Transform' in docs[vista['_camera']['fileID']]
assert docs[546103920]['Camera']['m_GameObject']==docs[546103918]['Transform']['m_GameObject']
assert sum((flow['_titlePosition'][axis]-vista['_focusPoint'][axis])**2 for axis in ('x','z'))>1
assert review['_vista']['fileID']==61000014 and review['_preferences']['fileID']==61000013
for key in ('_flow','_menu','_settings'):assert review[key]==vista[key]
assert [x['component']['fileID'] for x in docs[61000000]['GameObject']['m_Component']]==[61000001,61000011,61000012,61000013,61000014]
assert [x['component']['fileID'] for x in docs[61001000]['GameObject']['m_Component']]==[61001001,61001002,61001003,61001004,61001005,61001006]
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.15.unity').read_text();current=text.replace('Jehad V1.16','Jehad V1.15')
for fid in (61000014,61001006):
    current=re.sub(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)','',current,flags=re.M|re.S)
    current=current.replace('  - component: {fileID: '+str(fid)+'}\n','')
assert current==previous,'Unintended prior scene, title pose or gameplay changes'

runtime=(ROOT/m['scripts']['TitleVistaV116']).read_text();model=(ROOT/m['scripts']['TitleVistaMotionV116']).read_text()
assert 'DefaultExecutionOrder(150)' in runtime and 'RefreshVista(Time.unscaledDeltaTime)' in runtime
assert '_menu.Page==UiPageV17.Home&&!_menu.ConfirmationOpen' in runtime
assert '_menu.VisibleState==GameState.Title' in runtime
assert '_flow.State==GameState.Title' in runtime and 'Application.isEditor||Application.isFocused' in runtime
assert 'if(_flow.State!=GameState.Title){_captured=false;_motion.Reset();return;}' in runtime
assert 'if(!reduced&&!Moving)return;' in runtime
assert '_settings.Values.ReduceMotion' in runtime
assert 'GameEvents.StateChanged+=StateChanged' in runtime and 'GameEvents.StateChanged-=StateChanged' in runtime
assert '_fixedPosition=_camera.position;_fixedRotation=_camera.rotation;' in runtime
assert '_flow.State==GameState.Title&&_camera!=null)_camera.SetPositionAndRotation(_fixedPosition,_fixedRotation)' in runtime
assert 'Quaternion.AngleAxis(offset.Yaw,Vector3.up)' in runtime and 'Quaternion.LookRotation(direction,Vector3.up)' in runtime
assert all(code not in runtime+model for code in ('GameEvents.Raise','TryApplyDamage','TryInteract(','Teleport(','SetControlsEnabled','PlayerPrefs','AudioSource','InputSystem','GraphicRaycaster','Camera.main','GameObject.Find','timeScale='))
assert 'Period=90f,YawLimit=4f,LiftLimit=2.4f,MaxStep=.1f' in model
assert 'if(reduceMotion){Reset();return Offset;}' in model and 'Math.Min(seconds,MaxStep)' in model and '%Period' in model
# Serialized runtime camera writes are limited to the guarded title method and title-only teardown.
tree=parser.parse(runtime.encode());methods={};queue=[tree.root_node]
while queue:
    node=queue.pop();queue.extend(node.children)
    if node.type=='method_declaration':methods[node.child_by_field_name('name').text.decode()]=runtime.encode()[node.start_byte:node.end_byte].decode()
assert [name for name,body in methods.items() if 'SetPositionAndRotation(' in body]==['RefreshVista','OnDisable'] or sorted(name for name,body in methods.items() if 'SetPositionAndRotation(' in body)==['OnDisable','RefreshVista']
assert len(re.findall(r'_camera\.SetPositionAndRotation\(',runtime))==2
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert changed==['docs/reference/contract-changes.md'] or not changed,changed
compiled={}
if args.dotnet:
    for label,path in [('vista','V1_16/Tests/VistaTests.csproj'),('vitals_regression','V1_15/Tests/VitalTests.csproj'),('preferences_regression','V1_14/Tests/PreferenceTests.csproj'),('glints_regression','V1_13/Tests/GlintTests.csproj'),('guide_regression','V1_12/Tests/GuideTests.csproj'),('guardian_regression','V1_11/Tests/GuardianTests.csproj'),('notification_regression','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'scene_yaml_documents':len(docs),'compiled_tests':compiled,'previous_scene_restored_after_removing_two_added_components':True,'authored_camera_and_title_focus_references_verified':True,'home_modal_focus_reduced_motion_and_title_only_camera_guards':True,'camera_write_methods_limited_to_guarded_refresh_and_teardown':True,'no_gameplay_input_reward_audio_or_preference_authority_added':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_actual_camera_framing_input_focus_harnesses_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
