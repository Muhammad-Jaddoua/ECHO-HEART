"""Static current-pause relic snapshot checks. Native Unity behavior remains separate."""
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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.25.unity').read_text()))
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

assert len(docs)==136
obj=docs[61001012]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.PauseRelicReviewV126')
assert meta[obj['m_Script']['guid']]==ROOT/m['scripts']['PauseRelicReviewV126']
assert obj['_flow']=={'fileID':31000011} and obj['_menus']=={'fileID':31000016}
assert meta[docs[31000011]['MonoBehaviour']['m_Script']['guid']]==ROOT/m['extended_source']
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert [x['component']['fileID'] for x in docs[61001000]['GameObject']['m_Component']]==[61001001,*range(61001002,61001013)]
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.25.unity').read_text();current=text.replace('Jehad V1.26','Jehad V1.25')
current=re.sub(r'^--- !u!114 &61001012\n.*?(?=^--- !u!|\Z)','',current,flags=re.M|re.S).replace('  - component: {fileID: 61001012}\n','')
assert current==previous,'Unintended scene/runtime/menu change'
assert m['new_runtime_components']==0
flow=(ROOT/m['extended_source']).read_text()
new='''            if (_ready && !_transitioning && _policy.Pause())
            {
                // A pickup can precede the counter's next poll; publish a current pause summary.
                if (_relics != null) _relics.Refresh(false);
                PublishState();
            }
'''
old='            if (_ready && !_transitioning && _policy.Pause()) PublishState();\n'
assert flow.count(new)==1
original_flow=flow.replace(new,old)
assert hashlib.sha256(original_flow.encode()).hexdigest()=='3cce2f8a464964d7711059ae451187d8590fbb332c4ce25099e4bd072b8cdca9','Unintended original flow source changes'
assert not parser.parse(flow.encode()).root_node.has_error
# Parse the actual Pause method and prove the refresh precedes state publication in its accepted branch.
queue=[parser.parse(flow.encode()).root_node];pause=None
while queue:
    node=queue.pop();queue.extend(node.children)
    if node.type=='method_declaration' and node.child_by_field_name('name').text.decode()=='Pause':pause=node
assert pause is not None
pause_source=flow.encode()[pause.start_byte:pause.end_byte].decode()
assert pause_source.index('_policy.Pause()')<pause_source.index('_relics.Refresh(false)')<pause_source.index('PublishState();')
counter=(ROOT/'Assets/_Project/Scripts/World/Jehad/V1_3/RelicCounterV13.cs').read_text()
assert 'if (!force && count == _published) return;' in counter
assert '_nextRefresh = Time.unscaledTime + 0.1f;' in counter
menu=(ROOT/'Assets/_Project/Scripts/UI/Jehad/V1_17/FlowMenuV117.cs').read_text()
assert '_views.PauseStats.text="Relics  "+_flow.Relics.Collected+' in menu
native=(ROOT/m['scripts']['PauseRelicReviewV126']).read_text()
assert '#if UNITY_EDITOR || DEVELOPMENT_BUILD' in native and 'finally' in native
assert 'counter.enabled = false;' in native and 'counter.enabled = counterWasEnabled;' in native
assert '_flow.Player.Teleport(target.transform.position' in native and 'target.Collected' in native
assert 'Require(counter.Collected == 0, "Fixture did not retain the stale pre-poll count.");' in native
assert 'countEvents == 1 && countAtPause == 1' in native and 'VisibleState == GameState.Paused' in native
assert 'PauseSummary().StartsWith("Relics  1 / "' in native
assert 'GameEvents.CollectiblesChanged += counted;' in native and 'GameEvents.CollectiblesChanged -= counted;' in native
assert 'GameEvents.StateChanged += stateChanged;' in native and 'GameEvents.StateChanged -= stateChanged;' in native
assert all(x not in native for x in ('GameEvents.Raise','SendMessage','GetField(','BindingFlags','Collected = true'))

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
        data=original_flow.encode() if path==m['extended_source'] else (ROOT/path).read_bytes()
        assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==sha,('Unintended original source change',path)
        verified_remote_sources+=1
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('Deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert set(changed)<={m['extended_source'],'docs/reference/contract-changes.md'},changed
output=args.compiled_test_output.read_text();match=re.search(r'PASS: (\d+) assertions',output);assert match,output
assert int(match[1])==445
report={'result':'PASS','scene':m['scene'],'scene_yaml_documents':len(docs),'new_csharp_syntax_files':len(m['scripts']),'extended_csharp_syntax_files':1,'compiled_tests':{'flow_policy_regression':{'result':'PASS','assertions':445,'output':output.strip(),'scope':'Actual unchanged SessionPolicyV13/CoreTypes; excludes Unity flow side effects and counter timing'}},'namespace_audit':audit,'scene_matches_v125_after_removing_editoronly_review_and_version_labels':True,'original_flow_restored_after_reversing_pause_refresh':True,'accepted_pause_refreshes_counter_before_publishing_state':True,'unchanged_counts_keep_original_no_duplicate_event_guard':True,'counter_poll_pickup_menu_timer_and_other_state_methods_preserved':True,'shared_flow_applies_snapshot_fix_to_earlier_scenes':True,'native_review_uses_real_pickup_with_counter_polling_temporarily_held_and_restored':True,'project_sources_compared_to_remote_after_reversing_pause_edit':verified_remote_sources,'new_runtime_components':0,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_trigger_pickup_pause_summary_event_order_native_review_playtest':'NOT RUN — no Unity Editor available; pure flow policy tests do not establish snapshot behavior'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

