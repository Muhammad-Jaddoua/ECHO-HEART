"""Static movement cue and scene checks. Native Unity behavior remains separate."""
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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.20.unity').read_text()))
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

assert len(docs)==131
for fid,name in [(51000021,'AirborneFeedbackV121'),(61001009,'MovementAudioReviewV121')]:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
runtime_obj=docs[51000021]['MonoBehaviour'];review_obj=docs[61001009]['MonoBehaviour']
assert runtime_obj['_flow']==review_obj['_flow']=={'fileID':31000011}
assert runtime_obj['_feedback']=={'fileID':51000014} and review_obj['_presenter']=={'fileID':51000021}
assert meta[docs[51000014]['MonoBehaviour']['m_Script']['guid']]==ROOT/m['extended_source']
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert [x['component']['fileID'] for x in docs[51000000]['GameObject']['m_Component']]==[51000001,*range(51000011,51000022)]
assert [x['component']['fileID'] for x in docs[61001000]['GameObject']['m_Component']]==[61001001,*range(61001002,61001010)]
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.20.unity').read_text();current=text.replace('Jehad V1.21','Jehad V1.20')
for fid in [51000021,61001009]:
    current=re.sub(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)','',current,flags=re.M|re.S)
    current=current.replace('  - component: {fileID: '+str(fid)+'}\n','')
assert current==previous,'Unintended scene, asset or gameplay change'

runtime=(ROOT/m['scripts']['AirborneFeedbackV121']).read_text()
policy=(ROOT/m['scripts']['MovementCuePolicyV121']).read_text()
review=(ROOT/m['scripts']['MovementAudioReviewV121']).read_text()
feedback=(ROOT/m['extended_source']).read_text()
assert not parser.parse(feedback.encode()).root_node.has_error,'Extended feedback C# syntax'
original=feedback.replace('        public bool ExternalAirborneCues { get; set; }\n','')
guard='''                    if (!ExternalAirborneCues)
                    {
                        if (!grounded && _grounded) _audio.PlayWorld(WorldCueV15.Jump, position);
                        if (grounded && !_grounded) { _audio.PlayWorld(WorldCueV15.Land, position); _particles.Burst(WorldBurstV15.Dust, position - Vector3.up * 0.7f); }
                    }
'''
assert guard in original
original=original.replace(guard,'''                    if (!grounded && _grounded) _audio.PlayWorld(WorldCueV15.Jump, position);
                    if (grounded && !_grounded) { _audio.PlayWorld(WorldCueV15.Land, position); _particles.Burst(WorldBurstV15.Dust, position - Vector3.up * 0.7f); }
''')
assert hashlib.sha256(original.encode()).hexdigest()=='747d30749e2b07eaa304ee3dbcb886a85dba93c78d0b932eabc5032c888d43b1','Unintended original feedback edits'
assert 'DefaultExecutionOrder(125)' in runtime and 'DisallowMultipleComponent' in runtime
assert '_feedback.ExternalAirborneCues = true;' in runtime and '_feedback.ExternalAirborneCues = _before;' in runtime
assert '(position - _lastPosition).sqrMagnitude >= 64f' in runtime
assert '_controller.isGrounded, _controller.velocity.y, Time.deltaTime' in runtime
assert 'Time.time + .3f' in runtime and '_policy.Reset()' in runtime
assert all('GameEvents.'+name+' += '+handler in runtime and 'GameEvents.'+name+' -= '+handler in runtime for name,handler in [('RetryRequested','Retrying'),('StateChanged','StateChanged')])
assert 'GameState.Title || state == GameState.Dead || state == GameState.Complete' in runtime
assert 'GameState.Paused' not in runtime,'Pause should preserve the phase'
assert 'JumpRise = .5f' in policy and 'MinimumAirTime = .12f' in policy and 'MeaningfulFallSpeed = -3f' in policy
assert 'verticalSpeed > JumpRise' in policy and '_airTime >= MinimumAirTime' in policy and '_lowestSpeed <= MeaningfulFallSpeed' in policy
assert 'if (!playing || !Finite(seconds) || seconds <= 0f)' in policy
assert all(x not in runtime+policy for x in ('.Move(', 'InputSystem', 'Raise', 'TryApplyDamage', 'SetControlsEnabled', 'Time.timeScale =', 'new GameObject', 'AddComponent', 'AudioSource', 'PlayerPrefs'))
assert '#if UNITY_EDITOR || DEVELOPMENT_BUILD' in review and 'finally' in review and 'Destroy(fixture)' in review
assert 'GetComponent<CharacterController>()' in review and 'controller.Move(' in review
assert '_presenter.JumpRequests' in review and '_presenter.LandingRequests' in review
callers=[]
for f in (ROOT/'Assets/_Project/Scripts').rglob('*.cs'):
    if f==ROOT/m['extended_source']:continue
    if 'ExternalAirborneCues' in f.read_text():callers.append(f.relative_to(ROOT).as_posix())
assert callers==[m['scripts']['AirborneFeedbackV121']],callers
for earlier in (ROOT/'Assets/_Project/Scenes/Jehad').glob('*.unity'):
    if earlier==scene:continue
    assert docs[51000021]['MonoBehaviour']['m_Script']['guid'] not in earlier.read_text(),('adapter leaked into old scene',earlier)
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert set(changed)<={m['extended_source'],'docs/reference/contract-changes.md'},changed
compiled={}
if args.dotnet:
    for label,path in [('movement_cues','V1_21/Tests/MovementTests.csproj'),('lid_reveal_regression','V1_20/Tests/RevealTests.csproj'),('world_music_chest_regression','V1_5/Tests/WorldPresentationTests.csproj'),('vista_regression','V1_16/Tests/VistaTests.csproj'),('vitals_regression','V1_15/Tests/VitalTests.csproj'),('preferences_regression','V1_14/Tests/PreferenceTests.csproj'),('glints_regression','V1_13/Tests/GlintTests.csproj'),('guide_regression','V1_12/Tests/GuideTests.csproj'),('guardian_regression','V1_11/Tests/GuardianTests.csproj'),('notification_regression','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'extended_csharp_syntax_files':1,'scene_yaml_documents':len(docs),'compiled_tests':compiled,'previous_scene_restored_after_removing_two_components_and_version_labels':True,'original_feedback_restored_after_removing_optional_property_and_two_cue_guard':True,'earlier_scenes_have_no_adapter_and_default_false_is_legacy_compatible':True,'runtime_and_editoronly_review_bindings_verified':True,'actual_grounded_velocity_scaled_time_and_3d_discontinuity_read':True,'only_existing_jump_land_and_dust_requests_no_movement_or_input_writes':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_motion_audio_dust_pause_retry_native_review_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

