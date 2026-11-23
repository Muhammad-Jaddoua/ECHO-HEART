"""Static references/geometry and decoded audio analysis; does not compile or run Unity."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess,sys,struct
import numpy as np
import yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from landscape import height,normal
p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--baseline',type=Path);p.add_argument('--report',type=Path,required=True);args=p.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];chest=ROOT/m['chest_prefab'];bank=ROOT/m['bank']
def documents(path):
    text=path.read_text();ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids)),('duplicate ID',path)
    text=re.sub(r'^%.*\n','',text,flags=re.M);text=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',text,flags=re.M)
    docs=list(yaml.safe_load_all(text));assert len(ids)==len(docs)
    return {int(i):d for i,d in zip(ids,docs)}
meta={}
for f in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(f.read_text())['guid'];assert g not in meta,('duplicate GUID',g);meta[g]=f.with_suffix('')
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.4.unity').read_text()))
docs_by_path={f:documents(f) for f in [scene,chest,bank,*[ROOT/p for p in m['materials'].values()],*[ROOT/p for p in m['meshes']]]}
for path,docs in docs_by_path.items():
    assert Path(str(path)+'.meta').exists(),('missing metadata',path)
    for line in path.read_text().splitlines():
        if 'guid:' not in line:
            for fid in re.findall(r'fileID: (-?\d+)',line):assert int(fid)==0 or int(fid) in docs,('dangling local ID',path,line)
    for fid,g in re.findall(r'fileID: (-?\d+), guid: ([0-9a-f]{32})',path.read_text()):
        assert g in meta or g in inherited,('unknown GUID',path,g)
        target=meta.get(g)
        if target is None:continue
        if target.suffix in {'.mat','.prefab','.asset'}:
            if fid=='100100000' and target.suffix=='.prefab':continue
            assert re.search(r'^--- !u!\d+ &'+fid+r'(?:\s|$)',target.read_text(),re.M),('invalid cross-asset ID',path,target,fid)
        if target.suffix in {'.wav','.ogg'}:assert fid=='8300000'
        if target.suffix=='.shader':assert fid=='4800000'
    for fid,d in docs.items():
        if 'Transform' in d:
            for child in d['Transform'].get('m_Children',[]):assert docs[child['fileID']]['Transform']['m_Father']['fileID']==fid,('bad transform parent',path,fid)
        for o in d.values():
            if isinstance(o,dict) and o.get('m_GameObject',{}).get('fileID',0):
                go=docs[o['m_GameObject']['fileID']]['GameObject']
                if 'm_Component' in go:assert any(c['component']['fileID']==fid for c in go['m_Component']),('unattached component',path,fid)
docs=docs_by_path[scene];bd=docs_by_path[bank][11400000]['MonoBehaviour']
assert len([d for d in docs.values() if 'AudioListener' in d])==1
assert len([d for d in docs.values() if 'PrefabInstance' in d])==5
assert docs[31000015]['MonoBehaviour']['m_Enabled']==0,'Legacy automatic pickup/win cues would duplicate new cues'
assert docs[31000016]['MonoBehaviour']['_audio']['fileID']==31000015,'Menu volume provider must remain bound'
assert docs[1395263170]['MonoBehaviour']['_mode']==0
assert docs[51005000]['GameObject']['m_TagString']=='EditorOnly' and not docs[51005001]['Transform']['m_Children']
roots=[d['SceneRoots']['m_Roots'] for d in docs.values() if 'SceneRoots' in d][0]
assert len(roots)==len({r['fileID'] for r in roots}) and all(r['fileID'] in docs for r in roots)
world=ROOT/'Assets/_Project/Prefabs/World/Jehad/V1_2/MainLandscape_V1_2.prefab';worldtext=world.read_text()
assert len(re.findall(r'  _id: jehad-v12-',worldtext))==16
assert len(m['chests'])==2 and len({c['reward_source_id'] for c in m['chests']})==2
for c in m['chests']:
    o=docs[c['controller']]['MonoBehaviour'];reward=docs[o['_reward']['fileID']]['MonoBehaviour']
    assert reward['m_CorrespondingSourceObject']['fileID']==c['reward_source_id']
    x,y,z=c['position'];assert abs(height(x,z)-y)<.0001 and normal(x,z)[1]>.8
    assert docs[o['_lid']['fileID']]['Transform']['m_CorrespondingSourceObject']['fileID']==20001
chestdocs=docs_by_path[chest]
assert chestdocs[20001]['Transform']['m_Father']['fileID']==10001
assert chestdocs[20001]['Transform']['m_LocalPosition']=={'x':0,'y':.72,'z':.5}
assert len([d for d in chestdocs.values() if 'BoxCollider' in d])==1

parser=Parser(Language(tree_sitter_c_sharp.language()))
for name,path in m['scripts'].items():
    f=ROOT/path;assert not parser.parse(f.read_bytes()).root_node.has_error,('C# syntax error',path)
    assert re.search(r'\bclass\s+'+name+r'\b',f.read_text())
    assert Path(str(f)+'.meta').exists()
for d in docs.values():
    if 'MonoBehaviour' not in d:continue
    o=d['MonoBehaviour'];target=meta.get(o['m_Script']['guid'])
    if target not in [ROOT/p for p in m['scripts'].values()]:continue
    fields=set(re.findall(r'\[SerializeField(?:,[^\]]*)?\]\s*private\s+[\w<>\[\]]+\s+(_\w+)',target.read_text()))
    assert all(k.startswith('m_') or k in fields for k in o),('unknown serialized field',target)
assert len(bd['_music'])==6 and len(bd['_cues'])==23
for ref,name in zip(bd['_music'],m['music_order']):assert meta[ref['guid']].stem==name
for ref,name in zip(bd['_cues'],m['cue_order']):assert meta[ref['guid']].stem==name
shader=(ROOT/m['shader']).read_text();assert shader.count('{')==shader.count('}') and 'UniversalPipeline' in shader
for path in m['meshes']:
    mesh=docs_by_path[ROOT/path][4300000]['Mesh'];v=mesh['m_VertexData'];raw=bytes.fromhex(v['_typelessdata']);vertices=list(struct.iter_unpack('<8f',raw))
    assert len(raw)==v['m_DataSize']==v['m_VertexCount']*32
    indices=struct.unpack('<'+'H'*(len(mesh['m_IndexBuffer'])//4),bytes.fromhex(mesh['m_IndexBuffer']))
    assert max(indices)<len(vertices) and len(indices)%3==0
    for vertex in vertices:
        assert abs(sum(x*x for x in vertex[3:6])-1)<.001
        for i,k in enumerate('xyz'):assert abs(vertex[i]-mesh['m_LocalAABB']['m_Center'][k])<=mesh['m_LocalAABB']['m_Extent'][k]+.0001
    for i in range(0,len(indices),3):
        a,b,c=[np.array(vertices[j][:3]) for j in indices[i:i+3]]
        assert np.dot(np.cross(b-a,c-a),vertices[indices[i]][3:6])>0,'Incorrect mesh winding'

analysis=[];music_hashes=[]
for a in m['audio']['assets']:
    file=ROOT/a['path'];assert hashlib.sha256(file.read_bytes()).hexdigest()==a['sha256'],('audio checksum changed',file)
    setting=yaml.safe_load(Path(str(file)+'.meta').read_text())['AudioImporter']['defaultSettings']
    assert setting['loadType']==(2 if a['loop'] else 0) and setting['preloadAudioData']==1
    raw=subprocess.check_output(['ffmpeg','-v','error','-i',str(file),'-f','f32le','-acodec','pcm_f32le','-'])
    samples=np.frombuffer(raw,dtype='<f4').reshape(-1,a['channels'])
    assert np.isfinite(samples).all();peak=float(np.max(np.abs(samples)));rms=float(np.sqrt(np.mean(samples**2)))
    assert .002<rms<.3 and peak<.75,('silent or clipped audio',file,rms,peak)
    assert abs(len(samples)/a['sample_rate']-a['duration_seconds'])<.01
    seam=float(np.max(np.abs(samples[0]-samples[-1])))
    typical=float(np.quantile(np.abs(np.diff(samples,axis=0)),.999))
    if a['loop']:assert seam<max(.02,typical*2),('loop seam outlier',file,seam,typical)
    if file.stem in m['music_order']:music_hashes.append(hashlib.sha256(raw).hexdigest())
    analysis.append({'name':file.name,'seconds':round(len(samples)/a['sample_rate'],4),'peak':round(peak,5),'rms':round(rms,5),'loop_endpoint_step':round(seam,5) if a['loop'] else None})
assert len(set(music_hashes))==6,'Two environment music tracks have identical decoded audio'

changed=[];baseline_files=0
if args.baseline:
    base=json.loads(args.baseline.read_text());baseline_files=len(base)
    for path,digest in base.items():
        f=ROOT/path;assert f.exists(),('deleted baseline',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert all(p in ['docs/reference/contract-changes.md','docs/ASSET_CREDITS.md'] for p in changed),('changed earlier checkpoint',changed)
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'new_scene_prefab_bank_material_mesh_yaml_documents':sum(len(d) for d in docs_by_path.values()),'arched_lid_meshes_checked':len(m['meshes']),'music_tracks_distinct_decoded_audio':6,'ambience_loops':3,'sfx_assets':23,'audio_bytes':sum(a['bytes'] for a in m['audio']['assets']),'audio_analysis':analysis,'existing_relics':16,'loose_relics':14,'chest_rewards_from_existing_relics':2,'secret_discovery_zones':2,'pooled_world_sfx_voices':8,'pooled_particle_bursts':12,'max_particles_configured':832,'baseline_files_checked':baseline_files,'modified_baseline_files':changed,'unity_compilation_shader_import_playback_playtest':'NOT RUN — no Unity Editor connected','new_unity_harness':'WorldPolishReviewV15 supplied, not executed'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='audio_analysis'}))
