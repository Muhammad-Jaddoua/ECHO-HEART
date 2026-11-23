"""Static optional ambient-mute hooks and scene checks. Native Unity behavior remains separate."""
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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.18.unity').read_text()))
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

assert len(docs)==128
for fid,name in [(51000019,'AmbientMuteV119'),(61001008,'AmbientMuteReviewV119')]:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
bridge=docs[51000019]['MonoBehaviour'];review=docs[61001008]['MonoBehaviour'];audio_obj=docs[51000011]['MonoBehaviour']
assert bridge['_audio']['fileID']==51000011 and review['_audio']==bridge['_audio']
assert bridge['_settings']['fileID']==61000011 and review['_settings']==bridge['_settings']
assert review['_flow']['fileID']==31000011 and review['_preferences']['fileID']==61000013
assert docs[61000011]['MonoBehaviour']['_audio']==audio_obj['_volumes']
assert meta[audio_obj['m_Script']['guid']]==ROOT/m['extended_source']
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert [x['component']['fileID'] for x in docs[51000000]['GameObject']['m_Component']]==[51000001,51000011,51000012,51000013,51000014,51000015,51000016,51000017,51000018,51000019]
assert [x['component']['fileID'] for x in docs[61001000]['GameObject']['m_Component']]==[61001001,61001002,61001003,61001004,61001005,61001006,61001007,61001008]
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.18.unity').read_text();current=text.replace('Jehad V1.19','Jehad V1.18')
for fid in (51000019,61001008):
    current=re.sub(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)','',current,flags=re.M|re.S)
    current=current.replace('  - component: {fileID: '+str(fid)+'}\n','')
assert current==previous,'Unintended prior scene or gameplay changes'

runtime=(ROOT/m['scripts']['AmbientMuteV119']).read_text();audio=(ROOT/m['extended_source']).read_text()
assert not parser.parse(audio.encode()).root_node.has_error,'Extended audio C# syntax'
def method(source,name):
    queue=[parser.parse(source.encode()).root_node]
    while queue:
        node=queue.pop();queue.extend(node.children)
        if node.type=='method_declaration' and node.child_by_field_name('name').text.decode()==name:return source.encode()[node.start_byte:node.end_byte].decode()
    raise AssertionError(('missing method',name))
hook=method(audio,'SilenceMutedAmbience');helper=method(audio,'SilenceAmbient');original=audio
for body in (hook,helper):original=original.replace('        '+body+'\n','')
# Verified remote source at V1.18, including its existing Guardian resolution method.
assert hashlib.sha256(original.encode()).hexdigest()=='e5857cdbf86c009d1b7ca4ec6a0085ebfd7d41ea854bed93e73883d393a539a9','Existing audio code changed'
assert 'if (SfxVolume > 0f) return;' in hook
assert re.findall(r'SilenceAmbient\((_[a-z]+)\)',hook)==['_water','_forest','_alpine']
assert 'source.volume = 0f;' in helper and 'if (source.isPlaying) source.Stop();' in helper
assert all(word not in hook+helper for word in ('_music','_voices','_stinger','_mix','_arena','GameEvents','Play(','new '))
assert '_settings.Changed+=Changed' in runtime and '_settings.Changed-=Changed' in runtime
assert 'Changed();}' in runtime and '_audio.SilenceMutedAmbience()' in runtime
assert 'DefaultExecutionOrder(115)' in runtime and 'DisallowMultipleComponent' in runtime
assert all(word not in runtime for word in ('void Update','void LateUpdate','new GameObject','AddComponent','GameEvents.Raise','SetSfx','SetMusic','PlayerPrefs'))
callers=[]
for f in (ROOT/'Assets/_Project/Scripts').rglob('*.cs'):
    if f==ROOT/m['extended_source']:continue
    if 'SilenceMutedAmbience(' in f.read_text():callers.append(f.relative_to(ROOT).as_posix())
assert callers==[m['scripts']['AmbientMuteV119']],callers
for earlier in (ROOT/'Assets/_Project/Scenes/Jehad').glob('*.unity'):
    if earlier==scene:continue
    assert docs[51000019]['MonoBehaviour']['m_Script']['guid'] not in earlier.read_text(),('mute bridge leaked into old scene',earlier)
assert 'new AudioSource[6]' in audio and 'new AudioSource[8]' in audio
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert set(changed)<={'Assets/_Project/Scripts/Audio/Jehad/V1_5/WorldAudioV15.cs','docs/reference/contract-changes.md'},changed
compiled={}
if args.dotnet:
    for label,path in [('world_music_chest_regression','V1_5/Tests/WorldPresentationTests.csproj'),('vista_regression','V1_16/Tests/VistaTests.csproj'),('vitals_regression','V1_15/Tests/VitalTests.csproj'),('preferences_regression','V1_14/Tests/PreferenceTests.csproj'),('glints_regression','V1_13/Tests/GlintTests.csproj'),('guide_regression','V1_12/Tests/GuideTests.csproj'),('guardian_regression','V1_11/Tests/GuardianTests.csproj'),('notification_regression','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'extended_csharp_syntax_files':1,'scene_yaml_documents':len(docs),'compiled_tests':compiled,'previous_scene_restored_after_removing_two_added_components':True,'existing_audio_source_restored_exactly_after_removing_two_optional_methods':True,'earlier_scenes_have_no_ambient_mute_bridge':True,'shared_settings_volume_owner_and_pool_sizes_verified':True,'only_three_ambient_sources_touched_by_mute_hook':True,'settings_event_cleanup_and_no_per_frame_bridge_work':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_ambience_mute_unmute_music_input_harnesses_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
