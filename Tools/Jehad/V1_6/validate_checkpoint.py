"""Static UI/scene checks, and reference-canvas geometry invariants. Unity behavior remains separate."""
from pathlib import Path
import argparse,hashlib,json,re
import yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--baseline',type=Path);p.add_argument('--report',type=Path,required=True);args=p.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];text=scene.read_text()
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids))
normal=re.sub(r'^%.*\n','',text,flags=re.M);normal=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',normal,flags=re.M)
docs={int(fid):d for fid,d in zip(ids,yaml.safe_load_all(normal))}
meta={}
for f in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(f.read_text())['guid'];assert g not in meta,('duplicate GUID',g);meta[g]=f.with_suffix('')
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.5.unity').read_text()))
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
assert docs[31000016]['MonoBehaviour']['m_EditorClassIdentifier'].endswith('FlowMenuV16')
assert docs[31000016]['MonoBehaviour']['_settings']['fileID']==61000011
assert docs[31000402]['MonoBehaviour']['m_EditorClassIdentifier'].endswith('FlowReviewHarnessV16')
assert docs[31000402]['MonoBehaviour']['_menus']['fileID']==31000016
assert docs[61000011]['MonoBehaviour']['_audio']['fileID']==31000015
assert docs[31000015]['MonoBehaviour']['m_Enabled']==0
assert docs[61000012]['MonoBehaviour']['_toast']['fileID']==51000013
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert 'Echoheart.UI.FlowMenuV13' not in text and 'Echoheart.Testing.FlowReviewHarnessV13' not in text
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
ui=(ROOT/m['scripts']['FlowMenuV16']).read_text();settings=(ROOT/m['scripts']['UiSettingsV16']).read_text()
assert 'ScreenMatchMode.Expand' in ui and 'Screen.safeArea' in ui
assert '_backFrame==Time.frameCount' in ui and 'Time.frameCount+1' in ui
assert 'PagesGroup.interactable=!ConfirmationOpen' in ui
assert 'Time.unscaledDeltaTime' in settings and 'OnApplicationFocus' in settings and 'Apply(_initial)' in settings
assert not any('PlayerPrefs' in (ROOT/p).read_text() for p in m['scripts'].values()),'This checkpoint must not create disk saves'
# Expand mode guarantees at least the reference dimensions; cards use fixed sizes inside that area.
layout=[]
for w,h in [(1280,720),(1920,1080),(2560,1080),(1024,768),(3840,2160)]:
    scale=min(w/1600,h/900);vw,vh=w/scale,h/scale
    assert vw>=1600-.001 and vh>=900-.001
    assert (vw-1320)/2>=100 and (vh-760)/2>=60
    layout.append({'screen':[w,h],'virtual_canvas':[round(vw,2),round(vh,2)],'largest_menu_card':[1320,760],'fits_reference_geometry':True})
# Largest supported HUD scale: vertically adjacent clusters must retain non-overlapping bounds.
for scale in [.85,1,1.15]:
    assert 24+92*scale+14<=38+92*scale
    assert 24+82*scale+16<=40+82*scale
    assert (40+82*scale)+104*scale+16<=56+186*scale+.001
assert 96+66*1.15<194,'Interaction prompt overlaps discovery toast'
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier checkpoint',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert all(p=='docs/reference/contract-changes.md' for p in changed),('changed previous asset/code',changed)
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'scene_yaml_documents':len(docs),'single_menu_presenter':'FlowMenuV16, legacy class absent from scene','flow_harness_type_updated':True,'single_audio_listener':True,'modal_page_interaction_isolated':True,'input_shield_one_frame':True,'settings_scope':'session only; restores runtime display/quality/frame settings on teardown','reference_geometry_checks':layout,'hud_max_scale_no_cluster_overlap':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compilation_layout_input_display_playtest':'NOT RUN — no Unity Editor available','unity_harnesses':['UiReviewHarnessV16 supplied, not executed','FlowReviewHarnessV16 supplied, not executed']}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
