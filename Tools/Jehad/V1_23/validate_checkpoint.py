"""Static player hurt feedback checks. Native Unity behavior remains separate."""
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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.22.unity').read_text()))
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

assert len(docs)==133
for fid,name in [(51000022,'PlayerHitAnchorV123'),(61001010,'PlayerHurtReviewV123')]:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
bridge=docs[51000022]['MonoBehaviour'];review=docs[61001010]['MonoBehaviour']
assert bridge['_feedback']==review['_feedback']=={'fileID':51000014}
assert review['_flow']=={'fileID':31000011}
assert review['_bank']==docs[51000011]['MonoBehaviour']['_bank']
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert [x['component']['fileID'] for x in docs[51000000]['GameObject']['m_Component']]==[51000001,*range(51000011,51000023)]
assert [x['component']['fileID'] for x in docs[61001000]['GameObject']['m_Component']]==[61001001,*range(61001002,61001011)]
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.22.unity').read_text();current=text.replace('Jehad V1.23','Jehad V1.22')
for fid in [51000022,61001010]:
    current=re.sub(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)','',current,flags=re.M|re.S)
    current=current.replace('  - component: {fileID: '+str(fid)+'}\n','')
assert current==previous,'Unintended scene, reward, audio or UI binding change'

actor_path='Assets/_Project/Scripts/World/Jehad/V1_5/ActorFeedbackV15.cs'
feedback_path='Assets/_Project/Scripts/World/Jehad/V1_5/WorldFeedbackV15.cs'
actor=(ROOT/actor_path).read_text();feedback=(ROOT/feedback_path).read_text()
guard='''            if (_feedback.LocalPlayerHitFeedback && HitFeedbackPointV123.UseActor(_health != null && _health.Team == Team.Player, (point - transform.position).sqrMagnitude))
                point = transform.position + Vector3.up;
'''
option='        public bool LocalPlayerHitFeedback { get; set; }\n'
assert actor.count(guard)==feedback.count(option)==1
original_actor=actor.replace(guard,'');original_feedback=feedback.replace(option,'')
assert hashlib.sha256(original_actor.encode()).hexdigest()=='77e8a5bfba12a241f94b016f3048c1fb7fd1350f58881bfae0519cf51c172664','Unintended actor feedback edits'
assert hashlib.sha256(original_feedback.encode()).hexdigest()=='a7bbf57f284c485e586429cca8133b0f7181c086325919d671533e24a53c0d11','Unintended world feedback edits'
for source in [actor,feedback]:assert not parser.parse(source.encode()).root_node.has_error
assert '_feedback.Audio.PlayWorld(_health.Team == Team.Player ? WorldCueV15.PlayerHurt : WorldCueV15.Hit, point);' in actor
assert '_feedback.Particles.Burst(WorldBurstV15.Hit, point);' in actor
runtime=(ROOT/m['scripts']['PlayerHitAnchorV123']).read_text();policy=(ROOT/m['scripts']['HitFeedbackPointV123']).read_text()
assert '_feedback.LocalPlayerHitFeedback = true;' in runtime and '_feedback.LocalPlayerHitFeedback = _before;' in runtime
assert 'DisallowMultipleComponent' in runtime and 'DefaultExecutionOrder(120)' in runtime
assert 'MaximumDistanceSquared = 64f' in policy and 'squaredDistance > MaximumDistanceSquared' in policy
assert all(x not in runtime+policy for x in ('GameEvents','TryApplyDamage','Teleport(','.Move(','void Update','void LateUpdate','AudioSource','new GameObject','AddComponent','PlayerPrefs'))
native=(ROOT/m['scripts']['PlayerHurtReviewV123']).read_text()
assert '#if UNITY_EDITOR || DEVELOPMENT_BUILD' in native and 'finally' in native and '_flow.ReturnToTitle()' in native
assert 'belowWorld.y = -20f' in native and '_flow.Player.Teleport(belowWorld' in native and 'health.Current == health.Max - 1' in native
assert 'RequireHurtAt(nearby)' in native and 'RequireHurtAt(player.position + Vector3.up)' in native
assert 'VoiceCapacity == 8' in native
for old in (ROOT/'Assets/_Project/Scenes/Jehad').glob('*.unity'):
    if old!=scene:assert docs[51000022]['MonoBehaviour']['m_Script']['guid'] not in old.read_text(),('Adapter leaked into earlier scene',old)

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
        data=original_actor.encode() if path==actor_path else original_feedback.encode() if path==feedback_path else (ROOT/path).read_bytes()
        assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==sha,('Unintended source change',path)
        verified_remote_sources+=1
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('Deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert set(changed)<={actor_path,feedback_path,'docs/reference/contract-changes.md'},changed
output=args.compiled_test_output.read_text();match=re.search(r'PASS: (\d+) assertions',output);assert match,output
assert int(match[1])==39
report={'result':'PASS','scene':m['scene'],'scene_yaml_documents':len(docs),'new_csharp_syntax_files':len(m['scripts']),'extended_csharp_syntax_files':2,'compiled_tests':{'player_hit_point_policy':{'result':'PASS','assertions':39,'output':output.strip(),'scope':'Actual pure C# policy; Unity APIs excluded'}},'namespace_audit':audit,'scene_matches_v122_after_removing_two_components_and_version_labels':True,'earlier_sources_restored_after_removing_optional_guard_and_property':True,'default_false_option_preserves_earlier_scenes':True,'audio_and_particle_calls_share_selected_point':True,'original_damage_health_fall_recovery_and_reward_owners_unchanged':True,'project_sources_compared_to_remote_after_reversing_optional_changes':verified_remote_sources,'runtime_and_editoronly_review_bindings_verified':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_player_hurt_fall_recovery_sound_particles_native_review_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

