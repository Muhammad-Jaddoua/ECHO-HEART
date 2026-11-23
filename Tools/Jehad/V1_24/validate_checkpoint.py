"""Static title session-cue cleanup checks. Native Unity behavior remains separate."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
import yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--baseline',type=Path);p.add_argument('--report',type=Path,required=True);p.add_argument('--namespace-report',type=Path,required=True);p.add_argument('--base-tree-json',type=Path);args=p.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];text=scene.read_text()
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids))
normal=re.sub(r'^%.*\n','',text,flags=re.M);normal=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',normal,flags=re.M)
docs={int(fid):d for fid,d in zip(ids,yaml.safe_load_all(normal))}
meta={}
for f in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(f.read_text())['guid'];assert g not in meta,('duplicate GUID',g);meta[g]=f.with_suffix('')
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.23.unity').read_text()))
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
for fid,name in [(51000023,'TitleAudioCleanupV124'),(61001011,'TitleAudioReviewV124')]:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
bridge=docs[51000023]['MonoBehaviour'];review=docs[61001011]['MonoBehaviour']
assert bridge['_flow']==review['_flow']=={'fileID':31000011}
assert bridge['_audio']==review['_audio']=={'fileID':51000011}
assert review['_bank']==docs[51000011]['MonoBehaviour']['_bank']
assert review['_arena']=={'fileID':31000202} and review['_arenaEntryPoint']=={'fileID':31000131} and review['_heart']=={'fileID':31000302}
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert [x['component']['fileID'] for x in docs[51000000]['GameObject']['m_Component']]==[51000001,*range(51000011,51000024)]
assert [x['component']['fileID'] for x in docs[61001000]['GameObject']['m_Component']]==[61001001,*range(61001002,61001012)]
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.23.unity').read_text();current=text.replace('Jehad V1.24','Jehad V1.23')
for fid in [51000023,61001011]:
    current=re.sub(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)','',current,flags=re.M|re.S)
    current=current.replace('  - component: {fileID: '+str(fid)+'}\n','')
assert current==previous,'Unintended scene, audio, world or menu binding change'

audio=(ROOT/m['extended_source']).read_text()
hook='''        public void StopSessionStinger()
        {
            // Optional scene hook; regional music and world SFX keep their existing owners.
            if (_stinger != null) _stinger.Stop();
        }
'''
assert audio.count(hook)==1
original_audio=audio.replace(hook,'')
assert hashlib.sha256(original_audio.encode()).hexdigest()=='f255204b78ffe54d17538fea123020224bc1ec753de2b370a93c86ea87c40981','Unintended original audio source changes'
assert not parser.parse(audio.encode()).root_node.has_error
runtime=(ROOT/m['scripts']['TitleAudioCleanupV124']).read_text()
assert 'GameEvents.StateChanged += StateChanged;' in runtime and 'GameEvents.StateChanged -= StateChanged;' in runtime
assert 'StateChanged(_flow.State);' in runtime
assert 'if (state == GameState.Title && _audio != null) _audio.StopSessionStinger();' in runtime
assert 'DefaultExecutionOrder(115)' in runtime and 'DisallowMultipleComponent' in runtime
assert all(x not in runtime for x in ('GameState.Dead','GameState.Complete','GameState.Paused','GameEvents.Raise','RetryRequested','void Update','void LateUpdate','new GameObject','AddComponent','PlayWorld','PlayStinger','SetMusic','SetSfx'))
native=(ROOT/m['scripts']['TitleAudioReviewV124']).read_text()
assert '#if UNITY_EDITOR || DEVELOPMENT_BUILD' in native and 'finally' in native and '_flow.ReturnToTitle()' in native
assert 'DefeatPlayer(); RequireCue(stinger, WorldCueV15.Death); ReturnToTitleAndCheck(stinger);' in native
assert '_flow.Retry();' in native and 'RequireCue(stinger, WorldCueV15.Restore); ReturnToTitleAndCheck(stinger);' in native
assert '_arena.Boss.GetComponent<Health>()' in native and '_flow.HeartUnlocked' in native and '_heart.TryInteract()' in native
assert '.Stop(' not in native and 'GameEvents.Raise' not in native and 'PlayStinger(' not in native and '.Play(' not in native,'Review must use original lifecycle owners'
assert '_audio.VoiceCapacity == 8' in native and 'tracks == 6' in native and 'sources[i].isPlaying == playing[i]' in native
callers=[]
for f in (ROOT/'Assets/_Project/Scripts').rglob('*.cs'):
    if f==ROOT/m['extended_source']:continue
    if 'StopSessionStinger(' in f.read_text():callers.append(f.relative_to(ROOT).as_posix())
assert callers==[m['scripts']['TitleAudioCleanupV124']],callers
for earlier in (ROOT/'Assets/_Project/Scenes/Jehad').glob('*.unity'):
    if earlier!=scene:assert docs[51000023]['MonoBehaviour']['m_Script']['guid'] not in earlier.read_text(),('Bridge leaked into earlier scene',earlier)

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
        data=original_audio.encode() if path==m['extended_source'] else (ROOT/path).read_bytes()
        assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==sha,('Unintended earlier source change',path)
        verified_remote_sources+=1
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('Deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert set(changed)<={m['extended_source'],'docs/reference/contract-changes.md'},changed
report={'result':'PASS','scene':m['scene'],'scene_yaml_documents':len(docs),'new_csharp_syntax_files':len(m['scripts']),'extended_csharp_syntax_files':1,'namespace_audit':audit,'scene_matches_v123_after_removing_two_components_and_version_labels':True,'original_audio_restored_after_removing_optional_stinger_hook':True,'only_new_scene_bridge_calls_hook_on_title_entry':True,'music_world_voices_and_audio_options_not_written_by_hook':True,'native_review_uses_real_death_retry_guardian_defeat_and_heart_completion':True,'earlier_scenes_lack_bridge_and_keep_original_routing':True,'project_sources_compared_to_remote_after_reversing_extension':verified_remote_sources,'runtime_and_editoronly_review_bindings_verified':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'pure_gameplay_suites':'NOT RERUN — no pure policy or original method-body changes','unity_compile_stinger_audio_death_retry_heart_completion_native_review_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

