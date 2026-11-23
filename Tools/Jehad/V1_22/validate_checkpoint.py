"""Static namespace-fix checkpoint and scene checks. Unity compilation remains separate."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
import yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--baseline',type=Path);p.add_argument('--report',type=Path,required=True);p.add_argument('--namespace-report',type=Path,required=True);p.add_argument('--before-namespace-report',type=Path);p.add_argument('--base-tree-json',type=Path);args=p.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];text=scene.read_text()
ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids))
normal=re.sub(r'^%.*\n','',text,flags=re.M);normal=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',normal,flags=re.M)
docs={int(fid):d for fid,d in zip(ids,yaml.safe_load_all(normal))}
meta={}
for f in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(f.read_text())['guid'];assert g not in meta,('duplicate GUID',g);meta[g]=f.with_suffix('')
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.21.unity').read_text()))
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

assert len(docs)==131
previous=(ROOT/m['source_scene']).read_text()
assert text.replace('Jehad V1.22','Jehad V1.21')==previous,'Unintended scene behavior or reference change'
assert m['new_runtime_components']==m['new_scene_roots']==0
fixed=ROOT/m['fixed_source'];current=fixed.read_text();line='using Echoheart.Audio;\n'
assert current.startswith(line) and current.count(line)==1
assert hashlib.sha256(current[len(line):].encode()).hexdigest()=='371d3d506e0cb516b2421116f4b2e88fd2e98cfa3c124ddec153eb47e482e83d','Fix changes more than the missing import'
parser=Parser(Language(tree_sitter_c_sharp.language()))
assert not parser.parse(fixed.read_bytes()).root_node.has_error
bank=(ROOT/'Assets/_Project/Scripts/Audio/Jehad/V1_5/WorldAudioBankV15.cs').read_text()
assert 'namespace Echoheart.Audio' in bank and 'enum WorldCueV15' in bank
assert docs[51000021]['MonoBehaviour']['m_EditorClassIdentifier'].endswith('.AirborneFeedbackV121')
assert meta[docs[51000021]['MonoBehaviour']['m_Script']['guid']]==fixed
assert docs[61001000]['GameObject']['m_TagString']=='EditorOnly'
assert not docs[61001001]['Transform']['m_Children']
audit=json.loads(args.namespace_report.read_text())
sources=sorted((ROOT/'Assets/_Project/Scripts').rglob('*.cs'))
digest='\n'.join(p.relative_to(ROOT).as_posix()+'\0'+hashlib.sha256(p.read_bytes()).hexdigest() for p in sources)
assert audit['indexed_sources_sha256']==hashlib.sha256(digest.encode()).hexdigest(),'Namespace audit is stale for current sources'
assert audit['project_source_files_indexed']==len(sources)
assert audit['jehad_source_files_audited']==sum('/Jehad/' in p.as_posix() for p in sources)
assert audit['result']=='PASS' and not audit['missing_namespace_candidates'] and not audit['syntax_errors']
before=None
if args.before_namespace_report:
    before=json.loads(args.before_namespace_report.read_text())
    assert before['result']=='FAIL' and not before['syntax_errors']
    expected={(config,number) for config in ['Editor','Player'] for number in [73,78]}
    actual={(x['configuration'],x['line']) for x in before['missing_namespace_candidates']}
    assert actual==expected and len(before['missing_namespace_candidates'])==4
    assert all(x['path']==m['fixed_source'] and x['identifier']=='WorldCueV15' and x['candidate_namespaces']==['Echoheart.Audio'] for x in before['missing_namespace_candidates'])
    assert before['project_source_files_indexed']==audit['project_source_files_indexed'] and before['jehad_source_files_audited']==audit['jehad_source_files_audited']
verified_remote_sources=0
if args.base_tree_json:
    tree=json.loads(args.base_tree_json.read_text());assert not tree.get('truncated',False)
    expected={x['path']:x['sha'] for x in tree['tree'] if x['type']=='blob' and x['path'].startswith('Assets/_Project/Scripts/') and x['path'].endswith('.cs')}
    assert set(expected)=={p.relative_to(ROOT).as_posix() for p in sources},'Incomplete source index'
    for path,sha in expected.items():
        data=(ROOT/path).read_bytes()
        if path==m['fixed_source']:data=data[len(line.encode()):]
        assert hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==sha,('Unintended project source change',path)
        verified_remote_sources+=1
changed=[];baseline_count=0
if args.baseline:
    baseline=json.loads(args.baseline.read_text());baseline_count=len(baseline)
    for path,digest in baseline.items():
        p=ROOT/path;assert p.exists(),('Deleted earlier file',path)
        if hashlib.sha256(p.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert set(changed)<={m['fixed_source'],'docs/reference/contract-changes.md'},changed
report={'result':'PASS','scene':m['scene'],'scene_yaml_documents':len(docs),'existing_runtime_source_fixes':1,'new_runtime_components':0,'scene_matches_v121_except_version_labels':True,'source_matches_main_after_removing_single_audio_import':True,'all_project_sources_compared_to_remote_tree_except_import':verified_remote_sources,'namespace_audit':audit,'before_fix_namespace_audit':before,'single_audio_listener':True,'baseline_files_checked':baseline_count,'modified_baseline_files':changed,'pure_gameplay_suites':'NOT RERUN — source bodies and pure policies unchanged; V1.21 evidence retained separately','unity_compile_import_audio_physics_playtest_profiling':'NOT RUN — no Unity Editor available'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

