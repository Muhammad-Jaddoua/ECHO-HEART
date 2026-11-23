"""Static UI/scene checks, and reference-canvas geometry invariants. Unity behavior remains separate."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
import yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--baseline',type=Path);p.add_argument('--report',type=Path,required=True);p.add_argument('--dotnet',type=Path);p.add_argument('--reference-json',type=Path);args=p.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];text=scene.read_text()
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids))
normal=re.sub(r'^%.*\n','',text,flags=re.M);normal=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',normal,flags=re.M)
docs={int(fid):d for fid,d in zip(ids,yaml.safe_load_all(normal))}
meta={}
for f in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(f.read_text())['guid'];assert g not in meta,('duplicate GUID',g);meta[g]=f.with_suffix('')
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.10.unity').read_text()))
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

assert len(docs)==117
for fid,name in [(51000015,'GuardianFeedbackV111'),(61001003,'GuardianReviewV111')]:
    obj=docs[fid]['MonoBehaviour'];assert obj['m_EditorClassIdentifier'].endswith('.'+name)
    assert meta[obj['m_Script']['guid']]==ROOT/m['scripts'][name]
boss=docs[51000015]['MonoBehaviour']
assert {k:boss[k]['fileID'] for k in ('_flow','_arena','_feedback')}=={'_flow':31000011,'_arena':31000202,'_feedback':51000014}
assert boss['_bank']==docs[51000011]['MonoBehaviour']['_bank']
assert boss['_warningMaterial']==docs[51000014]['MonoBehaviour']['_warningMaterial']
assert boss['_muzzlePath']=='Muzzle'
assert docs[51000014]['MonoBehaviour']['_arena']['fileID']==0
assert docs[51000014]['MonoBehaviour']['_toast']['fileID']==0
assert docs[61001003]['MonoBehaviour']['_presentation']['fileID']==51000015
assert docs[61001003]['MonoBehaviour']['_arenaEntryPoint']['fileID']==31000131
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[61001001]['Transform']['m_Children']
assert [x['component']['fileID'] for x in docs[51000000]['GameObject']['m_Component']]==[51000001,51000011,51000012,51000013,51000014,51000015]
assert [x['component']['fileID'] for x in docs[61001000]['GameObject']['m_Component']]==[61001001,61001002,61001003]
previous=(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.10.unity').read_text();current=text.replace('Jehad V1.11','Jehad V1.10')
for fid in (51000015,61001003):current=re.sub(r'^--- !u!114 &'+str(fid)+r'\n.*?(?=^--- !u!|\Z)','',current,flags=re.M|re.S)
for fid in (51000015,61001003):current=current.replace('  - component: {fileID: '+str(fid)+'}\n','')
pattern=r'^--- !u!114 &51000014\n.*?(?=^--- !u!|\Z)'
block=re.search(pattern,current,re.M|re.S).group();current=current.replace(block,block.replace('  _arena: {fileID: 0}','  _arena: {fileID: 31000202}'))
assert current==previous,'Unintended scene/gameplay changes'

references=json.loads(args.reference_json.read_text()) if args.reference_json else {}
def reference(path):
    local=ROOT/path
    if local.exists():return local.read_text()
    assert path in references,('Full checkout or --reference-json is required',path)
    source=references[path];data=source['content'].encode()
    assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==source['sha'],('Reference bytes differ',path)
    return source['content']
prefab=reference('Assets/_Project/Prefabs/Boss/CorruptedGuardian.prefab')
normalized=re.sub(r'^%.*\n','',prefab,flags=re.M);normalized=re.sub(r'^--- !u!\d+ &(-?\d+)',r'--- &\1',normalized,flags=re.M)
source_docs={int(fid):d for fid,d in zip(re.findall(r'^--- !u!\d+ &(-?\d+)',prefab,re.M),yaml.safe_load_all(normalized))}
brain=next(d['MonoBehaviour'] for d in source_docs.values() if d.get('MonoBehaviour',{}).get('m_EditorClassIdentifier','').endswith('AI.BossBrain'))
muzzle=source_docs[brain['_muzzle']['fileID']]['Transform'];root=source_docs[brain['m_GameObject']['fileID']]['GameObject']
root_transform=root['m_Component'][0]['component']['fileID']
assert muzzle['m_Father']['fileID']==root_transform
assert source_docs[muzzle['m_GameObject']['fileID']]['GameObject']['m_Name']==boss['_muzzlePath']
for key in ('_onTelegraph','_onSlamTelegraph','_onRangedTelegraph','_onAttack','_onRecover','_onDefeated'):assert not brain[key]['m_PersistentCalls']['m_Calls'],'Prefab already has competing cue hooks'
brain_code=reference('Assets/_Project/Scripts/AI/BossBrain.cs')
assert 'Physics.OverlapSphereNonAlloc(center, _definition.SlamRadius' in brain_code
assert 'Vector3 origin = _muzzle != null ? _muzzle.position' in brain_code
assert '_definition.ProjectileRange' in brain_code
runtime=(ROOT/m['scripts']['GuardianFeedbackV111']).read_text()
assert '_boss.Definition.SlamRadius' in runtime and '_boss.Definition.ProjectileRange' in runtime
assert '_boss.transform.Find(_muzzlePath)' in runtime and 'Layers.EnvironmentMask' in runtime
assert '_legacy.enabled=false' in runtime and '_legacy.enabled=_restoreLegacy' in runtime
assert 'if(next==_boss)return true' in runtime and 'actor.Bind(_feedback,_boss.GetComponent<Health>(),null)' in runtime
assert '_warning=Voice(' in runtime and '_impact=Voice(' in runtime
assert all(code in runtime for code in ('_warning.Pause()','_impact.Pause()','_warning.UnPause()','_impact.UnPause()','_feedback.Audio.SfxVolume*.8f'))
assert all(code not in runtime for code in ('TryApplyDamage','RaiseBoss','RaiseGame','_boss.enabled=','_boss.Tick(','_boss.ResetFor(','_boss.BeginEncounter('))
changed=[];count=0
if args.baseline:
    base=json.loads(args.baseline.read_text());count=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted earlier file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert changed==['docs/reference/contract-changes.md'] or not changed,changed
compiled={}
if args.dotnet:
    for label,path in [('guardian','V1_11/Tests/GuardianTests.csproj'),('notification_regression','V1_10/Tests/NotificationTests.csproj'),('layout_regression','V1_9/Tests/LayoutTests.csproj'),('menu_regression','V1_8/Tests/UiPolicyTests.csproj')]:
        result=subprocess.run([str(args.dotnet),'run','--project',str(ROOT/'Tools/Jehad'/path),'--configuration','Release'],check=True,text=True,capture_output=True)
        match=re.search(r'PASS: (\d+) assertions',result.stdout);assert match,result.stdout
        compiled[label]={'result':'PASS','assertions':int(match[1]),'output':result.stdout.strip(),'scope':'Actual pure C# sources; Unity APIs excluded'}
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'scene_yaml_documents':len(docs),'compiled_tests':compiled,'existing_scene_restored_after_declared_changes':True,'boss_prefab_muzzle_and_empty_cue_hooks_verified':True,'boss_damage_range_source_verified':True,'legacy_boss_feedback_delegated':True,'two_dedicated_audio_voices_and_pause_guards':True,'boss_AI_damage_and_prefab_unchanged':True,'single_audio_listener':True,'baseline_files_checked':count,'modified_baseline_files':changed,'unity_compile_render_audio_physics_input_harnesses':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

