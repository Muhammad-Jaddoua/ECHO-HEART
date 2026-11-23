"""Static open-title composition and typed integration checks. Native Unity behavior remains separate."""
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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.16.unity').read_text()))
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
bindings=[(31000016,'FlowMenuV115','FlowMenuV117'),(31000402,'FlowReviewHarnessV115','FlowReviewHarnessV117'),(61001002,'UiReviewHarnessV115','UiReviewHarnessV117'),(61000014,'TitleVistaV116','TitleVistaV117'),(61001006,'TitleVistaReviewV116','TitleVistaReviewV117')]
for fid,_,name in bindings:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
menu=docs[31000016]['MonoBehaviour'];review=docs[61001002]['MonoBehaviour'];vista=docs[61000014]['MonoBehaviour'];vista_review=docs[61001006]['MonoBehaviour']
assert menu['_settings']['fileID']==61000011 and menu['_preferences']['fileID']==61000013
assert review['_menu']['fileID']==31000016 and vista['_menu']==review['_menu']==vista_review['_menu']
assert docs[31000402]['MonoBehaviour']['_menus']==review['_menu']
assert vista_review['_vista']['fileID']==61000014 and vista['_camera']==vista_review['_camera']==docs[31000011]['MonoBehaviour']['_camera']
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert docs[31000400]['GameObject']['m_TagString']=='EditorOnly' and not docs[31000401]['Transform']['m_Children']
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.16.unity').read_text();current=text.replace('Jehad V1.17','Jehad V1.16')
def block(source,fid):return re.search(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',source,re.M|re.S).group()
for fid,old,new in bindings:
    before=block(previous,fid);after=block(current,fid)
    old_script=re.search(r'^  m_Script: .*$',before,re.M).group()
    normalized=re.sub(r'^  m_Script: .*$',lambda _:old_script,after,flags=re.M).replace('.'+new,'.'+old)
    assert normalized==before,('unexpected changed serialized fields',fid)
    current=current.replace(after,before)
assert current==previous,'Unintended scene, world or gameplay changes'

def method(source,name):
    queue=[parser.parse(source.encode()).root_node]
    while queue:
        node=queue.pop();queue.extend(node.children)
        if node.type=='method_declaration' and node.child_by_field_name('name').text.decode()==name:return source.encode()[node.start_byte:node.end_byte].decode()
    raise AssertionError(('missing method',name))
renames=[('FlowMenuV117','FlowMenuV115'),('MenuScreensV117','MenuScreensV115'),('UiCancelV117','UiCancelV115'),('UiReviewHarnessV117','UiReviewHarnessV115'),('FlowReviewHarnessV117','FlowReviewHarnessV115'),('TitleVistaV117','TitleVistaV116'),('TitleVistaReviewV117','TitleVistaReviewV116')]
pairs=[('FlowMenuV117','UI','V1_15'),('MenuScreensV117','UI','V1_15'),('UiCancelV117','UI','V1_15'),('UiReviewHarnessV117','Testing','V1_15'),('FlowReviewHarnessV117','Testing','V1_15'),('TitleVistaV117','UI','V1_16'),('TitleVistaReviewV117','Testing','V1_16')]
for new,area,version in pairs:
    old=dict(renames)[new];original=(ROOT/f'Assets/_Project/Scripts/{area}/Jehad/{version}/{old}.cs').read_text();normalized=(ROOT/m['scripts'][new]).read_text()
    if new=='FlowMenuV117':
        normalized=normalized.replace('        private UiRoundedV16 _shade;\n','').replace(';_shade=_ui.Paint(_overlay',';_ui.Paint(_overlay')
        normalized=re.sub(r'^            _shade.color=new Color.*\n','',normalized,flags=re.M)
        normalized=normalized.replace('V1.17 UI needs','V1.15 UI needs').replace('V1.17 menus require','V1.15 menus require')
    if new=='MenuScreensV117':normalized=normalized.replace(method(normalized,'BuildTitle'),method(original,'BuildTitle'))
    if new=='UiReviewHarnessV117':
        start=normalized.index('        [ContextMenu("UI review/Run open title layout checks');end=normalized.index('        [ContextMenu("UI review/Run vitality HUD checks',start)
        normalized=normalized[:start]+normalized[end:]
    for a,b in renames:normalized=normalized.replace(a,b)
    assert normalized==original,('unrelated inherited behavior changed',new)

views=(ROOT/m['scripts']['MenuScreensV117']).read_text();flow_code=(ROOT/m['scripts']['FlowMenuV117']).read_text();title=method(views,'BuildTitle')
assert 'Title=_ui.Rect("Title",parent,0,0,1320,760' in title and 'Title=Card(' not in title
assert '_ui.Panel("Title menu card",Title,0,0,600,760' in title
assert '_ui.Panel("Borrowed powers",Title,760,-560,510,140' in title
assert 'Power sigils' not in title and '192,new Color' not in title
buttons=re.findall(r'_ui.Button\(Title,"([^"]+)",([\d.]+),(-?[\d.]+),([\d.]+),.*?\);',title)
assert [row[0] for row in buttons]==['Begin adventure','Settings','Controls','Credits','Quit']
for caption,x,y,w in buttons:
    call=re.search(r'_ui.Button\(Title,"'+re.escape(caption)+r'".*?\);',title).group()
    height=re.search(r',(true|false),([\d.]+)\);$',call);h=float(height[2]) if height else 62
    x,y,w=float(x),float(y),float(w)
    assert x>=0 and x+w<=600 and y<=0 and -y+h<=760,('button outside menu panel',caption)
labels=re.findall(r'_ui.Label\(Title,"(?:\\.|[^"\\])*",([\d.]+),(-?[\d.]+),([\d.]+),([\d.]+),',title)
assert len(labels)==4
for values in labels:
    x,y,w,h=map(float,values);assert x>=0 and x+w<=600 and y<=0 and -y+h<=760,('label outside menu panel',values)
assert 760+510<=1320 and 560+140<=760 and 600<760
open_fraction=1-(600*760+510*140)/(1320*760);assert open_fraction>.45
assert 1320/2>600 and 760/2<560,'Logical center focal region is covered by title panels'
assert '_route.State==GameState.Title&&_route.Page==UiPageV17.Home&&!ConfirmationOpen ? .16f : .48f' in flow_code
assert '_shade=_ui.Paint(_overlay,new Color(.004f,.025f,.02f,.48f),0,true)' in flow_code
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert changed==['docs/reference/contract-changes.md'] or not changed,changed
compiled={}
if args.dotnet:
    for label,path in [('vista_regression','V1_16/Tests/VistaTests.csproj'),('vitals_regression','V1_15/Tests/VitalTests.csproj'),('preferences_regression','V1_14/Tests/PreferenceTests.csproj'),('glints_regression','V1_13/Tests/GlintTests.csproj'),('guide_regression','V1_12/Tests/GuideTests.csproj'),('guardian_regression','V1_11/Tests/GuardianTests.csproj'),('notification_regression','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'scene_yaml_documents':len(docs),'compiled_tests':compiled,'previous_scene_restored_after_reversing_five_script_bindings':True,'inherited_behavior_preserved_outside_title_build_shade_and_review_action':True,'typed_menu_vista_harness_and_preference_bindings_verified':True,'title_buttons_and_text_within_left_panel':True,'title_logical_area_without_large_panels_fraction':round(open_fraction,4),'central_logical_focal_region_not_covered_by_title_panels':True,'home_and_modal_backdrop_guards':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_render_font_fit_input_framing_harnesses_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
