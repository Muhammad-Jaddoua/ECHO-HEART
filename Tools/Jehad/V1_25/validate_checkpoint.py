"""Static count-text cache checks. Unity behavior/profiling remain separate."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
import yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--baseline',type=Path);p.add_argument('--report',type=Path,required=True);p.add_argument('--namespace-report',type=Path,required=True);p.add_argument('--base-tree-json',type=Path);p.add_argument('--compiled-test-output',type=Path,required=True);args=p.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];text=scene.read_text()
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids))
normal=re.sub(r'^%.*\n','',text,flags=re.M);normal=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',normal,flags=re.M)
docs={int(fid):d for fid,d in zip(ids,yaml.safe_load_all(normal))}
meta={}
for f in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(f.read_text())['guid'];assert g not in meta,('duplicate GUID',g);meta[g]=f.with_suffix('')
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.24.unity').read_text()))
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

assert len(docs)==135
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.24.unity').read_text()
assert text.replace('Jehad V1.25','Jehad V1.24')==previous,'Unintended scene or component binding change'
assert m['added_components']==[] and m['new_scene_roots']==0
toast_path='Assets/_Project/Scripts/UI/Jehad/V1_10/WorldToastV110.cs'
menu_path='Assets/_Project/Scripts/UI/Jehad/V1_17/FlowMenuV117.cs'
toast=(ROOT/toast_path).read_text();menu=(ROOT/menu_path).read_text()
toast_field='        private readonly CountTextV125 _pendingCountText=new CountTextV125(""," more",true);\n'
menu_field=r'        private readonly CountTextV125 _displayCountdownText=new CountTextV125("Reverting in "," seconds.\nChoose Revert if the image is difficult to see.",false);'+'\n'
assert toast.count(toast_field)==menu.count(menu_field)==1
toast_use='_count.text=_pendingCountText.Format(_queue.PendingCount);'
old_toast='_count.text=_queue.PendingCount>0?_queue.PendingCount+" more":"";'
menu_use='_views.ModalBody.text=_displayCountdownText.Format(Mathf.CeilToInt(_settings.PreviewSeconds));'
old_menu=r'_views.ModalBody.text="Reverting in "+Mathf.CeilToInt(_settings.PreviewSeconds)+" seconds.\nChoose Revert if the image is difficult to see.";'
assert toast.count(toast_use)==menu.count(menu_use)==1
original_toast=toast.replace(toast_field,'').replace(toast_use,old_toast)
original_menu=menu.replace(menu_field,'').replace(menu_use,old_menu)
assert hashlib.sha256(original_toast.encode()).hexdigest()=='0e97b698f58e760efe969195f93bdac376e03e1a738124a8868559175b2a77ed','Unintended toast source changes'
assert hashlib.sha256(original_menu.encode()).hexdigest()=='8ab14ad14e3caffb95f9f3a37f3ac4edb51b5c2c8f642b06c4e9ea2ce05e2fe5','Unintended menu source changes'
for source in [toast,menu]:assert not parser.parse(source.encode()).root_node.has_error
cache=(ROOT/m['scripts']['CountTextV125']).read_text()
assert 'if (_ready && _value == value) return _text;' in cache
assert '_hideNonPositive && value <= 0 ? string.Empty : _prefix + value + _suffix' in cache
assert all(x not in cache for x in ['UnityEngine','GameEvents','MonoBehaviour','Dictionary','List<','Time.','PlayerPrefs'])
assert 'Mathf.CeilToInt(_settings.PreviewSeconds)' in menu and '_queue.Tick(Time.unscaledDeltaTime,Playing)' in toast

audit=json.loads(args.namespace_report.read_text());sources=sorted((ROOT/'Assets/_Project/Scripts').rglob('*.cs'))
digest='\n'.join(p.relative_to(ROOT).as_posix()+'\0'+hashlib.sha256(p.read_bytes()).hexdigest() for p in sources)
assert audit['indexed_sources_sha256']==hashlib.sha256(digest.encode()).hexdigest(),'Namespace audit is stale'
assert audit['result']=='PASS' and not audit['missing_namespace_candidates'] and not audit['syntax_errors']
assert audit['project_source_files_indexed']==len(sources) and audit['jehad_source_files_audited']==sum('/Jehad/' in p.as_posix() for p in sources)
verified_remote_sources=0
if args.base_tree_json:
    tree=json.loads(args.base_tree_json.read_text());assert not tree.get('truncated',False)
    expected={x['path']:x['sha'] for x in tree['tree'] if x['type']=='blob' and x['path'].startswith('Assets/_Project/Scripts/') and x['path'].endswith('.cs')}
    assert set(expected)|set(m['scripts'].values())=={p.relative_to(ROOT).as_posix() for p in sources}
    for path,sha in expected.items():
        data=original_toast.encode() if path==toast_path else original_menu.encode() if path==menu_path else (ROOT/path).read_bytes()
        assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==sha,('Unintended earlier source change',path)
        verified_remote_sources+=1
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('Deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert set(changed)<={toast_path,menu_path,'docs/reference/contract-changes.md'},changed
output=args.compiled_test_output.read_text();match=re.search(r'PASS: (\d+) assertions',output);assert match,output
allocation=json.loads(next(line for line in output.splitlines() if line.startswith('{')))
assert allocation['iterations']==10000
assert allocation['cached_display_bytes']==allocation['cached_pending_bytes']==0
assert allocation['previous_display_expression_bytes']>0 and allocation['previous_pending_expression_bytes']>0
report={'result':'PASS','scene':m['scene'],'scene_yaml_documents':len(docs),'new_csharp_syntax_files':len(m['scripts']),'extended_csharp_syntax_files':2,'compiled_tests':{'count_text_cache':{'result':'PASS','assertions':int(match[1]),'output':output.strip(),'scope':'Actual CountTextV125 source, C# 9.0/.NET 8; Unity APIs excluded'}},'managed_allocation_microbenchmark':allocation,'namespace_audit':audit,'scene_matches_v124_except_version_labels':True,'earlier_ui_sources_restored_after_reversing_two_fields_and_format_calls':True,'original_wording_queue_timer_input_display_and_gameplay_owners_preserved':True,'shared_ui_renderers_use_same_cache_in_earlier_scenes':True,'project_sources_compared_to_remote_after_reversing_cache_call_sites':verified_remote_sources,'new_components':0,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_ui_rendering_native_allocations_frame_times_playtest_profiling':'NOT RUN — no Unity Editor available; managed formatter result is not a Unity performance measurement'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

