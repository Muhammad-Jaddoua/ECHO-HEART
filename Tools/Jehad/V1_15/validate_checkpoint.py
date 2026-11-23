"""Static health-HUD and inherited-menu checks. Native Unity behavior remains separate."""
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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.14.unity').read_text()))
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

assert len(docs)==122
bindings=[(31000016,'FlowMenuV114','FlowMenuV115'),(31000402,'FlowReviewHarnessV114','FlowReviewHarnessV115'),(61001002,'UiReviewHarnessV114','UiReviewHarnessV115')]
for fid,_,name in bindings:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
menu=docs[31000016]['MonoBehaviour'];review=docs[61001002]['MonoBehaviour']
assert menu['_preferences']['fileID']==61000013 and review['_preferences']==menu['_preferences']
assert menu['_settings']['fileID']==61000011 and review['_settings']==menu['_settings']
assert review['_menu']['fileID']==31000016 and docs[31000402]['MonoBehaviour']['_menus']['fileID']==31000016
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert docs[31000400]['GameObject']['m_TagString']=='EditorOnly' and not docs[31000401]['Transform']['m_Children']
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.14.unity').read_text();current=text.replace('Jehad V1.15','Jehad V1.14')
def block(source,fid):return re.search(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',source,re.M|re.S).group()
for fid,old,new in bindings:
    before=block(previous,fid);after=block(current,fid)
    old_script=re.search(r'^  m_Script: .*$',before,re.M).group()
    normalized=re.sub(r'^  m_Script: .*$',lambda _:old_script,after,flags=re.M).replace('.'+new,'.'+old)
    assert normalized==before,('unexpected changed serialized fields',fid)
    current=current.replace(after,before)
assert current==previous,'Unintended scene/world/gameplay changes'

renames=[('FlowMenuV115','FlowMenuV114'),('MenuScreensV115','MenuScreensV114'),('UiCancelV115','UiCancelV114'),('UiHudV115','UiHudV19'),('FlowReviewHarnessV115','FlowReviewHarnessV114'),('UiReviewHarnessV115','UiReviewHarnessV114')]
for new,area in [('FlowMenuV115','UI'),('MenuScreensV115','UI'),('UiCancelV115','UI'),('FlowReviewHarnessV115','Testing'),('UiReviewHarnessV115','Testing')]:
    old=dict(renames)[new];original=(ROOT/f'Assets/_Project/Scripts/{area}/Jehad/V1_14/{old}.cs').read_text();normalized=(ROOT/m['scripts'][new]).read_text()
    if new=='UiReviewHarnessV115':
        begin=normalized.index('        [ContextMenu("UI review/Run vitality HUD checks');end=normalized.index('        [ContextMenu("UI review/Run preference storage check',begin)
        normalized=normalized[:begin]+normalized[end:]
    for a,b in renames:normalized=normalized.replace(a,b)
    normalized=normalized.replace('V1.15 UI needs','V1.14 UI needs').replace('V1.15 menus require','V1.14 menus require')
    assert normalized==original,('inherited menu/harness behavior changed',new)

hud=(ROOT/m['scripts']['UiHudV115']).read_text();model=(ROOT/m['scripts']['HealthBarV115']).read_text();old_hud=(ROOT/'Assets/_Project/Scripts/UI/Jehad/V1_9/UiHudV19.cs').read_text()
# Unchanged HUD methods keep their authored geometry, power/interaction logic and safe-area fitting.
def method(source,name):
    tree=parser.parse(source.encode());queue=[tree.root_node]
    while queue:
        node=queue.pop()
        if node.type=='method_declaration' and node.child_by_field_name('name').text.decode()==name:return source.encode()[node.start_byte:node.end_byte]
        queue.extend(node.children)
    raise AssertionError(('missing method',name))
for name in ('Fit','Power','Relics','Objective','Prompt'):assert method(hud,name)==method(old_hud,name),('unrelated HUD method changed',name)
assert '_vital.Observe(current,max)' in hud and '_guardian.Observe(current,max)' in hud
assert '_healthText.text=_vital.Current' in hud and '_bossText.text=_guardian.Current' in hud
assert '"LOW VITALITY"' in hud and '_healthLabel.color=_vital.Low?' in hud
assert hud.index('ui.Rect("Health recent loss"')<hud.index('ui.Rect("Health fill"')
assert hud.index('ui.Rect("Guardian recent loss"')<hud.index('ui.Rect("Guardian fill"')
assert '206*_vital.Fraction' in hud and '536*_guardian.Fraction' in hud and '206*_vital.Trail' in hud and '536*_guardian.Trail' in hud
assert '_vital.Snap();_guardian.Snap();_root.gameObject.SetActive' in hud
assert '_reduceMotion=values.ReduceMotion' in hud and 'values.HighContrast?1f:.65f' in hud
assert 'if(rect.sizeDelta.x!=width)' in hud
assert all(code not in hud+model for code in ('GameEvents','TryApplyDamage','Health.Reset','Teleport(','PlayerPrefs','AudioSource','UnityEngine.EventSystems','GraphicRaycaster'))
assert 'HoldSeconds=.18f,TrailRate=1.4f' in model and '(long)Current*4<=Maximum' in model
assert 'if(reduceMotion){Snap();return;}' in model and 'float.IsNaN' in model and 'float.IsInfinity' in model
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert changed==['docs/reference/contract-changes.md'] or not changed,changed
compiled={}
if args.dotnet:
    for label,path in [('vitals','V1_15/Tests/VitalTests.csproj'),('preferences_regression','V1_14/Tests/PreferenceTests.csproj'),('glints_regression','V1_13/Tests/GlintTests.csproj'),('guide_regression','V1_12/Tests/GuideTests.csproj'),('guardian_regression','V1_11/Tests/GuardianTests.csproj'),('notification_regression','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'scene_yaml_documents':len(docs),'compiled_tests':compiled,'previous_scene_restored_after_reversing_three_script_bindings':True,'inherited_menu_and_harness_logic_preserved':True,'unchanged_power_relic_prompt_objective_and_layout_methods':True,'typed_menu_harness_and_preference_bindings_verified':True,'immediate_health_and_trail_layering_accessibility_guards':True,'health_presentation_contains_no_damage_reward_or_input_authority':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_render_input_health_events_harnesses_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
