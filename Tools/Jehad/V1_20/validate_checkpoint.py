"""Static lid-synchronized chest reveal and scene checks. Native Unity behavior remains separate."""
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
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.19.unity').read_text()))
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

assert len(docs)==129
for fid,name in [(51000020,'ChestRevealGateV120'),(51005002,'WorldPolishReviewV120')]:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
gate=docs[51000020]['MonoBehaviour'];review=docs[51005002]['MonoBehaviour']
assert gate['_chests']==review['_chests']==[{'fileID':51001002},{'fileID':51002002}]
assert all(meta[docs[x['fileID']]['MonoBehaviour']['m_Script']['guid']]==ROOT/m['extended_source'] for x in gate['_chests'])
assert docs[51005000]['GameObject']['m_TagString']=='EditorOnly' and not docs[51005001]['Transform']['m_Children']
assert [x['component']['fileID'] for x in docs[51000000]['GameObject']['m_Component']]==[51000001,51000011,51000012,51000013,51000014,51000015,51000016,51000017,51000018,51000019,51000020]
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.19.unity').read_text();current=text.replace('Jehad V1.20','Jehad V1.19')
def block(source,fid):return re.search(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)',source,re.M|re.S).group()
before=block(previous,51005002);after=block(current,51005002);old_script=re.search(r'^  m_Script: .*$',before,re.M).group()
normalized=re.sub(r'^  m_Script: .*$',lambda _:old_script,after,flags=re.M).replace('.WorldPolishReviewV120','.WorldPolishReviewV15')
assert normalized==before,'World acceptance references changed'
current=current.replace(after,before).replace(block(current,51000020),'').replace('  - component: {fileID: 51000020}\n','')
assert current==previous,'Unintended scene, world, reward or gameplay changes'

runtime=(ROOT/m['scripts']['ChestRevealGateV120']).read_text();policy=(ROOT/m['scripts']['ChestRevealPolicyV120']).read_text();chest=(ROOT/m['extended_source']).read_text()
assert not parser.parse(chest.encode()).root_node.has_error,'Extended chest C# syntax'
def method(source,name):
    queue=[parser.parse(source.encode()).root_node]
    while queue:
        node=queue.pop();queue.extend(node.children)
        if node.type=='method_declaration' and node.child_by_field_name('name').text.decode()==name:return source.encode()[node.start_byte:node.end_byte].decode()
    raise AssertionError(('missing method',name))
setter=method(chest,'SetRevealAfterLid');original=chest.replace('        '+setter+'\n','')
original=original.replace('        private bool _revealAfterLid;\n','').replace('        public bool RevealAfterLid => _revealAfterLid;\n','')
original=re.sub(r'^            if \(_revealAfterLid && Opened.*\n','',original,flags=re.M)
original=original.replace('bool visible = _reward != null && ChestRevealPolicyV120.Visible(Opened, _reward.Collected, _revealAfterLid, _animation);','bool visible = Opened && _reward != null && !_reward.Collected;')
assert hashlib.sha256(original.encode()).hexdigest()=='17357cd1b18870492d40843b36a976847df715440b82eb35b7b14967ea3fc94b','Unintended original chest code changes'
assert '_revealAfterLid = enabled;' in setter and 'RevealReward();' in setter
assert 'private bool _revealAfterLid;' in chest and '_revealAfterLid =' not in chest.replace(setter,'')
assert 'Time.deltaTime * 2f' in chest and '_animation >= ChestRevealPolicyV120.ClearAt' in chest
assert '_rewardTrigger != null && !_rewardTrigger.enabled && !_reward.Collected' in chest
assert 'ClearAt=.95f' in policy and 'opened&&!collected' in policy and '!afterLid||' in policy
assert 'SetRevealAfterLid(true)' in runtime and 'SetRevealAfterLid(_before[i])' in runtime
assert 'DefaultExecutionOrder(120)' in runtime and 'DisallowMultipleComponent' in runtime
assert all(x not in runtime+policy for x in ('TryOpen(','GameEvents','Collected =','TryApplyDamage','Collider','Renderer','void Update','void LateUpdate','PlayerPrefs'))
# Original world/asset acceptance stays intact outside the lid-specific checks and exact pickup teleport.
old_review=(ROOT/'Assets/_Project/Scripts/Testing/Jehad/V1_5/WorldPolishReviewV15.cs').read_text()
new_review=(ROOT/m['scripts']['WorldPolishReviewV120']).read_text().replace('WorldPolishReviewV120','WorldPolishReviewV15')
start=new_review.index('                    Require(chest.RevealAfterLid');end=new_review.index('                    chest.ResetFor(RetryScope.MainArea)',start)
new_review=new_review[:start]+new_review[end:]
line='                    _flow.Player.Teleport(chest.Reward.transform.position, Quaternion.identity);'
i=new_review.rfind(line);assert i>=0
new_review=new_review[:i]+line.replace('position,','position + Vector3.back * .9f,')+new_review[i+len(line):]
assert new_review==old_review,'Unrelated world acceptance behavior changed'
callers=[]
for f in (ROOT/'Assets/_Project/Scripts').rglob('*.cs'):
    if f==ROOT/m['extended_source']:continue
    if 'SetRevealAfterLid(' in f.read_text():callers.append(f.relative_to(ROOT).as_posix())
assert callers==[m['scripts']['ChestRevealGateV120']],callers
for earlier in (ROOT/'Assets/_Project/Scenes/Jehad').glob('*.unity'):
    if earlier==scene:continue
    assert docs[51000020]['MonoBehaviour']['m_Script']['guid'] not in earlier.read_text(),('gate leaked into old scene',earlier)
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert set(changed)<={'Assets/_Project/Scripts/World/Jehad/V1_5/SecretChestV15.cs','docs/reference/contract-changes.md'},changed
compiled={}
if args.dotnet:
    for label,path in [('lid_reveal','V1_20/Tests/RevealTests.csproj'),('world_music_chest_regression','V1_5/Tests/WorldPresentationTests.csproj'),('vista_regression','V1_16/Tests/VistaTests.csproj'),('vitals_regression','V1_15/Tests/VitalTests.csproj'),('preferences_regression','V1_14/Tests/PreferenceTests.csproj'),('glints_regression','V1_13/Tests/GlintTests.csproj'),('guide_regression','V1_12/Tests/GuideTests.csproj'),('guardian_regression','V1_11/Tests/GuardianTests.csproj'),('notification_regression','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'extended_csharp_syntax_files':1,'scene_yaml_documents':len(docs),'compiled_tests':compiled,'previous_scene_restored_after_scoped_gate_and_review_changes':True,'original_chest_source_restored_after_removing_optional_gate_changes':True,'earlier_scenes_have_no_lid_gate_and_default_off_is_legacy_compatible':True,'authored_two_chest_and_review_references_verified':True,'scaled_lid_progress_and_original_reward_authority_preserved':True,'world_asset_acceptance_preserved_outside_reveal_checks':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_lid_animation_trigger_collection_pause_harnesses_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
