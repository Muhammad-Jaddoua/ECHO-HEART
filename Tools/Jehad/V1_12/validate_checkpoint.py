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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.11.unity').read_text()))
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

assert len(docs)==119
for fid,name in [(51000016,'ObjectiveGuideV112'),(61001004,'ObjectiveGuideReviewV112')]:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
guide=docs[51000016]['MonoBehaviour'];review=docs[61001004]['MonoBehaviour']
assert {k:guide[k]['fileID'] for k in ('_flow','_arena','_settings','_camera','_guardianEntry','_heart')}=={'_flow':31000011,'_arena':31000202,'_settings':61000011,'_camera':546103920,'_guardianEntry':31000131,'_heart':31000301}
assert 'Camera' in docs[guide['_camera']['fileID']]
assert 'Transform' in docs[guide['_guardianEntry']['fileID']] and 'Transform' in docs[guide['_heart']['fileID']]
assert guide['_guardianEntry']==docs[31000402]['MonoBehaviour']['_arenaEntryPoint']
assert guide['_heart']['fileID']==docs[51000014]['MonoBehaviour']['_heart']['fileID']
assert review['_guide']['fileID']==51000016 and review['_heart']['fileID']==31000302 and review['_arenaEntry']['fileID']==31000131
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert [x['component']['fileID'] for x in docs[51000000]['GameObject']['m_Component']]==[51000001,51000011,51000012,51000013,51000014,51000015,51000016]
assert [x['component']['fileID'] for x in docs[61001000]['GameObject']['m_Component']]==[61001001,61001002,61001003,61001004]
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.11.unity').read_text();current=text.replace('Jehad V1.12','Jehad V1.11')
for fid in (51000016,61001004):
    current=re.sub(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)','',current,flags=re.M|re.S)
    current=current.replace('  - component: {fileID: '+str(fid)+'}\n','')
assert current==previous,'Unintended existing scene/gameplay changes'
runtime=(ROOT/m['scripts']['ObjectiveGuideV112']).read_text()
assert 'ObjectiveGuidePolicyV112.Select' in runtime and 'ObjectiveGuidePolicyV112.Place' in runtime
assert '_settings.Values.Hints' in runtime and 'GameEvents.PromptChanged+=Prompt' in runtime
assert 'WorldToScreenPoint' in runtime and 'ScreenPointToLocalPointInRectangle' in runtime and 'screen.z<=0' in runtime
assert 'UiLayoutV19.Surface' in runtime and 'UiLayoutV19.SafeArea' in runtime
assert 'group.blocksRaycasts=false' in runtime and 'GraphicRaycaster' not in runtime
assert 'if(kind!=_shownKind)' in runtime and 'if(metres!=_lastMetres)' in runtime
assert 'DefaultExecutionOrder(120)' in runtime and '200,64' in runtime
assert all(code not in runtime for code in ('GameEvents.Raise','TryApplyDamage','TryInteract(','EnterArena(','CompleteFromHeart(','Teleport(','PlayerPrefs','HiddenRelic','SecretChest'))
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert changed==['docs/reference/contract-changes.md'] or not changed,changed
compiled={}
if args.dotnet:
    for label,path in [('guide','V1_12/Tests/GuideTests.csproj'),('guardian_regression','V1_11/Tests/GuardianTests.csproj'),('notification_regression','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'scene_yaml_documents':len(docs),'compiled_tests':compiled,'existing_scene_restored_after_removing_two_added_components':True,'authored_camera_entrance_heart_references_verified':True,'hints_prompt_and_camera_projection_guards':True,'input_transparent_marker':True,'no_progression_or_reward_calls_in_runtime':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_render_camera_input_harnesses':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

