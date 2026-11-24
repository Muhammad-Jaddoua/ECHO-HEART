"""Combined Jehad source, model, asset and scene checks. Unity execution remains separate."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
import yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--baseline',type=Path);p.add_argument('--report',type=Path,required=True);p.add_argument('--namespace-report',type=Path,required=True);p.add_argument('--base-tree-json',type=Path);p.add_argument('--regression-report',type=Path,required=True);args=p.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];text=scene.read_text()
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids))
normal=re.sub(r'^%.*\n','',text,flags=re.M);normal=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',normal,flags=re.M)
docs={int(fid):d for fid,d in zip(ids,yaml.safe_load_all(normal))}
meta={}
for f in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(f.read_text())['guid'];assert g not in meta,('duplicate GUID',g);meta[g]=f.with_suffix('')
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.26.unity').read_text()))
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

assert len(docs)==137
obj=docs[61001013]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.JehadFinalReviewV127')
assert meta[obj['m_Script']['guid']]==ROOT/m['scripts']['JehadFinalReviewV127']
assert obj['_flow']=={'fileID':31000011} and obj['_menus']=={'fileID':31000016} and obj['_feedback']=={'fileID':51000014}
assert obj['_bank']==docs[51000011]['MonoBehaviour']['_bank']
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert [x['component']['fileID'] for x in docs[61001000]['GameObject']['m_Component']]==[61001001,*range(61001002,61001014)]
assert m['new_runtime_components']==0
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.26.unity').read_text();current=text.replace('Jehad V1.27','Jehad V1.26')
current=re.sub(r'^--- !u!114 &61001013\n.*?(?=^--- !u!|\Z)','',current,flags=re.M|re.S).replace('  - component: {fileID: 61001013}\n','')
assert current==previous,'Unintended scene, runtime owner or asset binding changes'
feedback_path='Assets/_Project/Scripts/World/Jehad/V1_5/WorldFeedbackV15.cs'
notices_path='Assets/_Project/Scripts/World/Jehad/V1_10/WorldNoticesV110.cs'
feedback=(ROOT/feedback_path).read_text();notices=(ROOT/notices_path).read_text()
old='                _collected[i] = relic.Collected;'
new='                if (_flow.State != GameState.Paused || _refresh) _collected[i] = relic.Collected;'
assert feedback.count(new)==1
original_feedback=feedback.replace(new,old)
original_notices=notices.replace('A hidden relic waits inside.','A hidden relic has been revealed.')
assert notices.count('A hidden relic waits inside.')==1
assert hashlib.sha256(original_feedback.encode()).hexdigest()=='a5c8e0435c62787585935bc92df117b7626a3ea31dec7a335a0525988d03c8f4'
assert hashlib.sha256(original_notices.encode()).hexdigest()=='67ec717839c3d1df2ca8dbe32332121e4f28b9998ef9d3cb06091d14cf075017'
for source in [feedback,notices]:assert not parser.parse(source.encode()).root_node.has_error
assert 'if (!_refresh && Playing && relic.Collected && !_collected[i])' in feedback
assert '_audio.PlayWorld(WorldCueV15.Relic, relic.transform.position);' in feedback and '_particles.Burst(WorldBurstV15.Relic, relic.transform.position);' in feedback
native=(ROOT/m['scripts']['JehadFinalReviewV127']).read_text()
assert '#if UNITY_EDITOR || DEVELOPMENT_BUILD' in native and 'finally' in native
assert 'counter.enabled = false; _feedback.enabled = false;' in native
assert '_feedback.enabled = feedbackWasEnabled;' in native and 'counter.enabled = counterWasEnabled;' in native
assert '_flow.Player.Teleport(target.transform.position' in native and 'target.Collected' in native
assert 'countEvents == 1 && countAtPause == 1' in native and 'PauseSummary().StartsWith("Relics  1 / "' in native
assert 'RelicVoiceBindings() == 1' in native and 'RelicVoiceBindings() == 0' in native
assert 'yield return null; yield return null;' in native
assert all(x not in native for x in ['GameEvents.Raise','Collected = true','SendMessage','GetField(','BindingFlags'])

readme_section='''## Jehad world, UI and audio checkpoint

The combined review scene is [`Jehad_V1.27.unity`](Assets/_Project/Scenes/Jehad/Jehad_V1.27.unity).
It contains Jehad's world, collectibles/secrets, flow, menus, music, SFX and VFX
with the movement-only review player. Native Unity acceptance, profiling and
production-player integration remain pending.

Contribution, validation evidence and integration details: [`docs/jehad/README.md`](docs/jehad/README.md).

'''
readme=(ROOT/'README.md').read_text();assert readme.count(readme_section)==1
original_readme=readme.replace(readme_section,'')
assert hashlib.sha256(original_readme.encode()).hexdigest()=='eb0a5a7e06b3c1ace5862d2c286feb920071f296984407c66b0e8a75362378dc'

audit=json.loads(args.namespace_report.read_text());sources=sorted((ROOT/'Assets/_Project/Scripts').rglob('*.cs'))
digest='\n'.join(p.relative_to(ROOT).as_posix()+'\0'+hashlib.sha256(p.read_bytes()).hexdigest() for p in sources)
assert audit['indexed_sources_sha256']==hashlib.sha256(digest.encode()).hexdigest(),'Namespace audit is stale'
assert audit['result']=='PASS' and not audit['missing_namespace_candidates'] and not audit['syntax_errors']
assert audit['project_source_files_indexed']==len(sources) and audit['jehad_source_files_audited']==sum('/Jehad/' in p.as_posix() for p in sources)
regression=json.loads(args.regression_report.read_text());assert regression['result']=='PASS'
labels={'flow','world_audio_chests','menu','layout','notifications','guardian_feedback','objectives','relic_glints','preferences','vitality','title_vista','chest_reveal','movement_cues','hurt_position','counter_text'}
assert set(regression['compiled_tests'])==labels and regression['suite_count']==15
assert regression['total_assertions']==sum(x['assertions'] for x in regression['compiled_tests'].values())==9436
for test in regression['compiled_tests'].values():
    assert test['result']=='PASS' and test['input_sha256']
    for path,sha in test['input_sha256'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha,('Stale compiled-test input',path)
benchmark=regression['managed_counter_microbenchmark']
assert benchmark['cached_display_bytes']==benchmark['cached_pending_bytes']==0 and benchmark['iterations']==10000

# Metadata/header checks establish authored asset integrity, not appearance, audio quality or import success.
import wave,struct
bank_path=meta[obj['_bank']['guid']]
bank_text=re.sub(r'^%.*\n','',bank_path.read_text(),flags=re.M)
bank_text=re.sub(r'^--- !u!\d+ &\d+',r'---',bank_text,flags=re.M)
bank=next(yaml.safe_load_all(bank_text))['MonoBehaviour']
assert len(bank['_music'])==6 and len(bank['_cues'])==23
clips=bank['_music']+bank['_cues']+[bank['_water'],bank['_forest'],bank['_alpine']]
assert len({clip['guid'] for clip in clips})==32
formats={'wav':0,'ogg_vorbis':0}
for clip in clips:
    target=meta[clip['guid']];assert target.exists()
    if target.suffix=='.wav':
        with wave.open(str(target),'rb') as audio:assert audio.getnframes()>0 and audio.getframerate()>0 and audio.getnchannels() in [1,2]
        formats['wav']+=1
    elif target.suffix=='.ogg':
        data=target.read_bytes()[:512];assert data.startswith(b'OggS')
        header=data.find(b'\x01vorbis');assert header>=0
        assert struct.unpack_from('<I',data,header+7)[0]==0 and data[header+11] in [1,2] and struct.unpack_from('<I',data,header+12)[0]>0
        formats['ogg_vorbis']+=1
    else:raise AssertionError(('Unexpected audio format',target))
assert formats=={'wav':23,'ogg_vorbis':9}
for pack in ['KenneyNatureKit','KayKitForest']:
    assert (ROOT/'Assets/ThirdParty'/pack/'LICENSE.txt').exists()
credits=(ROOT/'docs/ASSET_CREDITS.md').read_text();assert 'Kenney' in credits and 'Kay Lousberg' in credits and 'CC0' in credits
chests=docs[51000020]['MonoBehaviour']['_chests'];assert chests==[{'fileID':51001002},{'fileID':51002002}]
assert 'Echoheart.Testing.JehadFlowExplorerV13' in text,'Production player status needs reassessment'

verified_remote_sources=0;remote_paths=set()
if args.base_tree_json:
    tree=json.loads(args.base_tree_json.read_text());assert not tree.get('truncated',False)
    remote_paths={x['path'] for x in tree['tree'] if x['type']=='blob'}
    expected={x['path']:x['sha'] for x in tree['tree'] if x['type']=='blob' and x['path'].startswith('Assets/_Project/Scripts/') and x['path'].endswith('.cs')}
    assert set(expected)|set(m['scripts'].values())=={p.relative_to(ROOT).as_posix() for p in sources}
    for path,sha in expected.items():
        data=original_feedback.encode() if path==feedback_path else original_notices.encode() if path==notices_path else (ROOT/path).read_bytes()
        assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==sha,('Unintended earlier source change',path)
        verified_remote_sources+=1
    readme_blob=next(x['sha'] for x in tree['tree'] if x['path']=='README.md')
    data=original_readme.encode();assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==readme_blob
    assert not any(p.startswith('Assets/_Project/Scripts/Player/') and p.endswith('.cs') for p in remote_paths),'Reassess production-player integration status'

doc_paths=['docs/jehad/README.md','docs/jehad/CONTRIBUTION.md','docs/jehad/ACCEPTANCE.md','docs/jehad/INTEGRATION.md','docs/jehad/PERFORMANCE.md','docs/phase3/final_report.md','docs/jehad/versions/Jehad_V1.27.md','Tools/Jehad/V1_27/README.md']
expected_report='docs/jehad/versions/Jehad_V1.27.validation.json'
for path in doc_paths:
    f=ROOT/path;assert f.exists(),('Missing handoff document',path)
    for link in re.findall(r'\[[^\]]+\]\(([^)]+)\)',f.read_text()):
        if link.startswith(('http:','https:','#')):continue
        raw=link.split('#',1)[0];target=(f.parent/raw).resolve()
        rel=target.relative_to(ROOT).as_posix()
        assert target.exists() or rel in remote_paths or rel==expected_report,('Broken documentation link',path,link)
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,sha in base.items():
        f=ROOT/path;assert f.exists(),('Deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=sha:changed.append(path)
    assert set(changed)<={feedback_path,notices_path,'README.md','docs/reference/contract-changes.md'},changed
report={'result':'PASS','scene':m['scene'],'scene_yaml_documents':len(docs),'compiled_regression':regression,'namespace_audit':audit,'scene_matches_v126_after_removing_editoronly_review_and_labels':True,'original_world_sources_restored_after_reversing_paused_snapshot_and_copy_changes':True,'readme_change_limited_to_jehad_checkpoint_section':True,'pickup_feedback_held_only_during_pause_unless_retry_snapshot_requested':True,'authored_audio_headers_and_guid_references_checked':32,'audio_header_formats':formats,'authored_music_tracks':6,'authored_sfx_clips':23,'authored_ambient_loops':3,'nature_pack_license_files_and_credits_present':True,'authored_chest_refs_checked':2,'review_capsule_confirmed':True,'handoff_documents_and_links_checked':doc_paths,'project_sources_compared_to_remote_after_reversing_edits':verified_remote_sources,'baseline_files_checked':count,'modified_baseline_files':changed,'new_runtime_components':0,'single_audio_listener':True,'release_status':'INTEGRATION CANDIDATE — native acceptance and production player pending','unity_compile_import_native_actions_physics_render_audio_mix_gpu_cpu_frame_times_playtests_build_video_release':'NOT RUN / NOT PROVIDED — no Unity Editor connected; model/source evidence is not final-build evidence'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'result':'PASS','scene':m['scene'],'suites':15,'assertions':9436,'namespace_scripts':audit['jehad_source_files_audited'],'audio_clips_checked':32,'unity_runtime':'NOT RUN'}))

