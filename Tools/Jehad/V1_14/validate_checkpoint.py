"""Static preference/menu scene checks. Native Unity behavior remains separate."""
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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.13.unity').read_text()))
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
bindings=[(31000016,'FlowMenuV19','FlowMenuV114'),(31000402,'FlowReviewHarnessV19','FlowReviewHarnessV114'),(61001002,'UiReviewHarnessV110','UiReviewHarnessV114')]
for fid,_,name in bindings+[(61000013,'','PreferenceStoreV114')]:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
store=docs[61000013]['MonoBehaviour'];menu=docs[31000016]['MonoBehaviour'];review=docs[61001002]['MonoBehaviour']
assert store['_settings']['fileID']==61000011 and store['_flow']['fileID']==31000011
assert menu['_preferences']['fileID']==61000013 and review['_preferences']==menu['_preferences']
assert menu['_settings']==store['_settings'] and review['_settings']==store['_settings']
assert review['_menu']['fileID']==31000016 and docs[31000402]['MonoBehaviour']['_menus']['fileID']==31000016
assert [x['component']['fileID'] for x in docs[61000000]['GameObject']['m_Component']]==[61000001,61000011,61000012,61000013]
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert docs[31000400]['GameObject']['m_TagString']=='EditorOnly' and not docs[31000401]['Transform']['m_Children']
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.13.unity').read_text();current=text.replace('Jehad V1.14','Jehad V1.13')
def block(source,fid):return re.search(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',source,re.M|re.S).group()
for fid,old,new in bindings:
    before=block(previous,fid);after=block(current,fid)
    old_script=re.search(r'^  m_Script: .*$',before,re.M).group()
    normalized=re.sub(r'^  m_Script: .*$',lambda _:old_script,after,flags=re.M).replace('.'+new,'.'+old)
    normalized=normalized.replace('  _preferences: {fileID: 61000013}\n','')
    assert normalized==before,('unexpected changed component fields',fid)
    current=current.replace(after,before)
current=current.replace(block(current,61000013),'').replace('  - component: {fileID: 61000013}\n','')
assert current==previous,'Unintended prior scene/gameplay changes'

runtime=(ROOT/m['scripts']['PreferenceStoreV114']).read_text()
codec=(ROOT/m['scripts']['PreferenceCodecV114']).read_text()
assert 'DefaultExecutionOrder(-210)' in runtime
assert 'DefaultExecutionOrder(-220)' in (ROOT/'Assets/_Project/Scripts/UI/Jehad/V1_7/UiSettingsV17.cs').read_text()
assert '_settings.Set(saved)' in runtime and '_settings.Changed+=Changed' in runtime and '_settings.Changed-=Changed' in runtime
assert 'PlayerPrefs.SetString(StorageKey,payload);PlayerPrefs.Save()' in runtime and 'Echoheart.Jehad.AudioInterface.v1' in runtime
assert '_flow.State!=GameState.Playing' in runtime and '_save.Due(Time.unscaledTime)' in runtime
assert 'GameEvents.StateChanged+=StateChanged' in runtime and 'GameEvents.StateChanged-=StateChanged' in runtime
assert all(x in runtime for x in ('OnApplicationFocus','OnApplicationPause','OnApplicationQuit','_save.Acknowledge(payload)','_failed=true','PlayerPrefs.DeleteKey(StorageKey)'))
assert all(x not in runtime+codec for x in ('GameEvents.Raise','TryApplyDamage','Teleport(','ResetFor(','CurrentRetryScope','Collectibles','DraftResolution','QualitySettings','Screen.SetResolution','DeleteAll','BinaryFormatter'))
assert 'parts.Length!=5' in codec and 'parts[0]!="1"' in codec and 'text.Length>128' in codec
assert 'CultureInfo.InvariantCulture' in codec and '.Sanitized()' in codec and 'Delay=.5f' in codec
# The inherited menu logic must differ only in typed names and preference status bindings.
pairs=[('FlowMenuV114','FlowMenuV19','UI/V1_9'),('MenuScreensV114','MenuScreensV19','UI/V1_9'),('UiCancelV114','UiCancelV19','UI/V1_9'),('FlowReviewHarnessV114','FlowReviewHarnessV19','Testing/V1_9'),('UiReviewHarnessV114','UiReviewHarnessV110','Testing/V1_10')]
for new,old,where in pairs:
    area,version=where.split('/');original=(ROOT/f'Assets/_Project/Scripts/{area}/Jehad/{version}/{old}.cs').read_text();normalized=(ROOT/m['scripts'][new]).read_text()
    if new=='FlowMenuV114':
        for line in ('        [SerializeField] private PreferenceStoreV114 _preferences;\n','        public string PreferenceStatus => _preferences!=null?_preferences.Status:"Changes last for this session.";\n','            if(_preferences!=null)_preferences.StatusChanged+=PreferencesChanged;\n','            if(_preferences!=null)_preferences.StatusChanged-=PreferencesChanged;\n'):normalized=normalized.replace(line,'')
        normalized=normalized.replace('_views.SettingsStatus.text=PreferenceStatus;','_views.SettingsStatus.text="Changes last for this session.";').replace('V1.14 UI needs','V1.9 UI needs').replace('V1.14 menus require','V1.9 menus require')
    if new=='MenuScreensV114':normalized=normalized.replace('_owner.PreferenceStatus','"Changes last for this session."')
    if new=='UiReviewHarnessV114':
        normalized=normalized.replace('        [SerializeField] private PreferenceStoreV114 _preferences;\n','')
        begin=normalized.index('        [ContextMenu("UI review/Run preference storage check');end=normalized.index('        private static void Require',begin)
        normalized=normalized[:begin]+normalized[end:]
    for a,b in [('FlowMenuV114','FlowMenuV19'),('MenuScreensV114','MenuScreensV19'),('UiCancelV114','UiCancelV19'),('FlowReviewHarnessV114','FlowReviewHarnessV19'),('UiReviewHarnessV114','UiReviewHarnessV110')]:normalized=normalized.replace(a,b)
    assert normalized==original,('inherited behavior changed',new)
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert changed==['docs/reference/contract-changes.md'] or not changed,changed
compiled={}
if args.dotnet:
    for label,path in [('preferences','V1_14/Tests/PreferenceTests.csproj'),('glints_regression','V1_13/Tests/GlintTests.csproj'),('guide_regression','V1_12/Tests/GuideTests.csproj'),('guardian_regression','V1_11/Tests/GuardianTests.csproj'),('notification_regression','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'scene_yaml_documents':len(docs),'compiled_tests':compiled,'previous_scene_restored_after_reversing_scoped_bindings_and_added_store':True,'inherited_menu_and_harness_logic_preserved':True,'settings_menu_store_harness_references_verified':True,'settings_awake_before_load_before_menu_awake':True,'audio_interface_only_codec_and_menu_idle_save_guards':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_render_input_playerprefs_restart_harnesses_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
