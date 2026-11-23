"""Static wiring checks only; run the supplied acceptance harness in Unity for engine behaviour."""
from pathlib import Path
import argparse,json,re,hashlib,yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp

ROOT=Path(__file__).resolve().parents[3]
parser=argparse.ArgumentParser();parser.add_argument('--manifest',type=Path,required=True);parser.add_argument('--baseline',type=Path);parser.add_argument('--report',type=Path,required=True);args=parser.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];gate=ROOT/m['gate_prefab']
paths=[p for p in (ROOT/'Assets').rglob('*') if p.is_file() and ('/V1_3/' in str(p) or p.name.startswith('Jehad_V1.3.'))]
metas={}
for p in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(p.read_text())['guid'];assert g not in metas,('duplicate GUID',g)
    metas[g]=p.with_suffix('')
for p in paths:
    if p.suffix!='.meta':assert Path(str(p)+'.meta').exists(),('missing metadata',p)
extra={'3826a2c2a5d73a14e92af1648366817b':('prefab','5973304569983579869'),'fe008e0334370dc42a2159149a49dfb3':('cs','11500000'),'9ed6550c8b5a0f44f87461d512129dc2':('asset','11400000')}
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.2.unity').read_text()))
documents={};document_count=0
for p in [scene,gate]:
    text=p.read_text();ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids)),('duplicate file ID',p)
    normal=re.sub(r'^%.*\n','',text,flags=re.M);normal=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',normal,flags=re.M)
    docs=list(yaml.safe_load_all(normal));assert len(docs)==len(ids)
    documents[p]={int(fid):doc for fid,doc in zip(ids,docs)};document_count+=len(docs)
    for line in text.splitlines():
        if 'guid:' not in line:
            for fid in re.findall(r'fileID: (-?\d+)',line):assert fid=='0' or fid in ids,('dangling local reference',p,line)
    for fid,g in re.findall(r'fileID: (-?\d+), guid: ([0-9a-f]{32})',text):
        assert g in metas or g in inherited or g in extra,('unknown GUID',p,g)
        if g in extra:assert fid==extra[g][1],('wrong known external object ID',fid,g)
        target=metas.get(g)
        if target is not None and target.suffix in ['.prefab','.mat']:
            if fid=='100100000' and target.suffix=='.prefab':continue
            assert re.search(r'^--- !u!\d+ &'+fid+r'(?:\s|$)',target.read_text(),re.M),('bad cross-asset file ID',target,fid)
    for fid,d in documents[p].items():
        if 'Transform' in d:
            for child in d['Transform'].get('m_Children',[]):
                assert documents[p][child['fileID']]['Transform']['m_Father']['fileID']==fid,('bad child parent',p,fid)
        for obj in d.values():
            if isinstance(obj,dict) and obj.get('m_GameObject',{}).get('fileID',0):
                go=documents[p][obj['m_GameObject']['fileID']]['GameObject']
                assert any(c['component']['fileID']==fid for c in go['m_Component']),('unattached component',p,fid)

docs=documents[scene]
assert len([x for x in docs.values() if 'PrefabInstance' in x])==3
assert len([x for x in docs.values() if 'AudioListener' in x])==1
assert docs[1395263158]['GameObject']['m_Layer']==8
assert docs[1395263170]['MonoBehaviour']['_mode']==0
assert docs[1395263170]['MonoBehaviour']['m_GameObject']['fileID']==1395263158
assert docs[31000400]['GameObject']['m_TagString']=='EditorOnly' and not docs[31000401]['Transform']['m_Children']
assert docs[31000200]['GameObject']['m_Layer']==15 and docs[31000203]['BoxCollider']['m_IsTrigger']==1
assert docs[31000203]['BoxCollider']['m_Size']=={'x':24,'y':6,'z':24}
volume=docs[31000201]['Transform']['m_LocalPosition'];size=docs[31000203]['BoxCollider']['m_Size']
entry=docs[31000131]['Transform']['m_LocalPosition'];retry=docs[31000111]['Transform']['m_LocalPosition']
assert all(abs(entry[k]-volume[k])<size[k]/2 for k in 'xyz')
assert all(abs(retry[k]-volume[k])<size[k]/2 for k in 'xyz')
assert retry['z']>88+.2+.5,'Retry point overlaps closing gate'
assert docs[31000012]['MonoBehaviour']['_gameplayInput']==[{'fileID':1395263160}]
respawn=docs[31000012]['MonoBehaviour']['_onRespawned']['m_PersistentCalls']['m_Calls']
assert len(respawn)==1 and respawn[0]['m_Target']['fileID']==1395263160 and respawn[0]['m_MethodName']=='ResetMotion'
assert documents[gate][10000]['GameObject']['m_IsActive']==0

csharp=Parser(Language(tree_sitter_c_sharp.language()));scripts=sorted((ROOT/'Assets/_Project/Scripts').glob('**/V1_3/*.cs'))
for p in scripts:
    assert not csharp.parse(p.read_bytes()).root_node.has_error,('C# syntax error',p)
    assert re.search(r'\bclass\s+'+re.escape(p.stem)+r'\b',p.read_text()),('class/file mismatch',p)
for d in docs.values():
    if 'MonoBehaviour' not in d:continue
    obj=d['MonoBehaviour'];target=metas.get(obj['m_Script']['guid'])
    if target not in scripts:continue
    fields=set(re.findall(r'\[SerializeField(?:,[^\]]*)?\]\s*private\s+[\w<>\[\]]+\s+(_\w+)',target.read_text()))
    for key in obj:
        if not key.startswith('m_'):assert key in fields,('field is not serialized by the C# class',target,key)

changed=[];baseline_files=0
if args.baseline:
    baseline=json.loads(args.baseline.read_text());baseline_files=len(baseline)
    for path,h in baseline.items():
        p=ROOT/path;assert p.exists(),('deleted baseline file',path)
        if hashlib.sha256(p.read_bytes()).hexdigest()!=h:changed.append(path)
    assert all(p=='docs/reference/contract-changes.md' for p in changed),('changed pre-existing checkpoint',changed)
report={'result':'PASS','scene':m['scene'],'csharp_syntax_files':len(scripts),'parsed_unity_documents':document_count,'world_prefabs_reused':['V1.2 main landscape','V1.2 boss arena'],'new_gate_prefab':m['gate_prefab'],'single_player_health_relay':True,'arena_entry_and_retry_inside_trigger':True,'retry_clear_of_gate':True,'respawn_callback':'JehadFlowExplorerV13.ResetMotion','review_harness':'EditorOnly root, no gameplay children','baseline_files_checked':baseline_files,'modified_baseline_files':changed,'unity_editor_compile_playtest':'NOT RUN — no Unity Editor available','runtime_navmesh_and_ui_verification':'NOT RUN — use FlowReviewHarnessV13 in Unity'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
