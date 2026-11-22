"""Static scene/source/terrain checks. Unity NavMesh, physics and AI execution are separate acceptance."""
from pathlib import Path
import argparse,collections,hashlib,json,math,re,sys
import yaml
from tree_sitter import Language,Parser
import tree_sitter_c_sharp

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'V1_2'))
from landscape import height,normal
p=argparse.ArgumentParser();p.add_argument('--manifest',type=Path,required=True);p.add_argument('--baseline',type=Path);p.add_argument('--report',type=Path,required=True);args=p.parse_args()
m=json.loads(args.manifest.read_text());scene=ROOT/m['scene'];paths=[scene,*[ROOT/p for p in m['source_prefabs'].values()]]

def documents(path):
    text=path.read_text();ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids)),('duplicate file ID',path)
    text=re.sub(r'^%.*\n','',text,flags=re.M);text=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',text,flags=re.M)
    docs=list(yaml.safe_load_all(text));assert len(docs)==len(ids)
    return {int(i):d for i,d in zip(ids,docs)}

meta={}
for file in (ROOT/'Assets').rglob('*.meta'):
    g=yaml.safe_load(file.read_text())['guid'];assert g not in meta,('duplicate GUID',g)
    meta[g]=file.with_suffix('')
inherited=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.3.unity').read_text()))
external={
 '04075f042e34a72459794ff02e05bec1':11500000,
 'f13a87f93b657ea4fa8c7e65c57452a6':2098280771408410335,
 'b5ad6b658f8ab36478e9790043520db8':6657649362964423119,
 '86f0ddf849670194c8bc4284a118971e':5712756238236456871}
all_docs={path:documents(path) for path in paths}
for path,docs in all_docs.items():
    assert Path(str(path)+'.meta').exists(),('missing metadata',path)
    for line in path.read_text().splitlines():
        if 'guid:' not in line:
            for fid in re.findall(r'fileID: (-?\d+)',line):assert int(fid)==0 or int(fid) in docs,('dangling local ref',path,line)
    for fid,g in re.findall(r'fileID: (-?\d+), guid: ([0-9a-f]{32})',path.read_text()):
        assert g in meta or g in inherited or g in external,('unknown GUID',path,g)
        if g in external:assert int(fid)==external[g],('wrong external component',path,g,fid)
        target=meta.get(g)
        if target is not None and target.suffix in {'.prefab','.mat'}:
            if fid=='100100000' and target.suffix=='.prefab':continue
            assert re.search(r'^--- !u!\d+ &'+fid+r'(?:\s|$)',target.read_text(),re.M),('unknown prefab/material object',path,target,fid)
    for fid,d in docs.items():
        if 'Transform' in d:
            for child in d['Transform'].get('m_Children',[]):assert docs[child['fileID']]['Transform']['m_Father']['fileID']==fid,('parent mismatch',path,fid)
        for obj in d.values():
            if isinstance(obj,dict) and obj.get('m_GameObject',{}).get('fileID',0):
                go=docs[obj['m_GameObject']['fileID']]['GameObject'];assert any(c['component']['fileID']==fid for c in go['m_Component']),('unattached component',path,fid)
    roots=[d['SceneRoots']['m_Roots'] for d in docs.values() if 'SceneRoots' in d]
    if roots:
        for root in roots[0]:assert root['fileID'] in docs

docs=all_docs[scene]
assert len([x for x in docs.values() if 'PrefabInstance' in x])==3
assert len([x for x in docs.values() if 'AudioListener' in x])==1
assert docs[1395263170]['MonoBehaviour']['_mode']==0
assert docs[1395263158]['GameObject']['m_Layer']==8
assert docs[41001000]['GameObject']['m_TagString']=='EditorOnly' and not docs[41001001]['Transform']['m_Children']
population=docs[41000012]['MonoBehaviour'];nav=docs[41000011]['MonoBehaviour']
assert population['_navigation']['fileID']==41000011 and population['_flow']['fileID']==31000011
assert nav['_worldRoot']['fileID']==20000001
assert nav['_sourcePoints']==[{'fileID':s['point']} for s in m['sources']]
assert len(population['_sources'])==6
assert collections.Counter(s['power'] for s in m['sources'])=={'Sword':2,'Bow':2,'Fire':2}
assert nav['_arenaExclusion']=={'m_Center':{'x':42,'y':30,'z':102},'m_Extent':{'x':17,'y':50,'z':17}}
for s,ref in zip(m['sources'],population['_sources']):
    pos=docs[s['point']]['Transform']['m_LocalPosition'];assert abs(pos['y']-s['position'][1])<.0001
    assert ref['_point']['fileID']==s['point'] and meta[ref['_prefab']['guid']]==ROOT/s['prefab'] and ref['_prefab']['fileID']==10002
    assert pos['z']<85 and math.hypot(pos['x'],pos['z']+100)>12
for path in list(all_docs)[1:]:
    d=all_docs[path];root=d[10000]['GameObject'];spawner=d[10002]['MonoBehaviour']
    assert root['m_IsActive']==0 and spawner['m_Enabled']==0
    assert spawner['_spawnPoint']['fileID']==10001 and spawner['_countsTowardCap']==1
    assert spawner['_spawnOnStart']==1 and spawner['_resetOnBossArenaRetry']==0
    assert spawner['_respawnDelay']==6 and spawner['_minPlayerDistance']==12
    assert not any('BoxCollider' in x or 'CapsuleCollider' in x or 'NavMeshAgent' in x for x in d.values())

cs=Parser(Language(tree_sitter_c_sharp.language()))
for name,path in m['scripts'].items():
    file=ROOT/path;assert not cs.parse(file.read_bytes()).root_node.has_error,('C# syntax error',file)
    assert re.search(r'\bclass\s+'+name+r'\b',file.read_text()) and Path(str(file)+'.meta').exists()
for d in docs.values():
    if 'MonoBehaviour' not in d:continue
    o=d['MonoBehaviour'];target=meta.get(o['m_Script']['guid'])
    if target not in [ROOT/x for x in m['scripts'].values()]:continue
    fields=set(re.findall(r'\[SerializeField(?:,[^\]]*)?\]\s*private\s+[\w<>\[\]]+\s+(_\w+)',target.read_text()))
    assert all(k.startswith('m_') or k in fields for k in o),('unserialized field',target)

# Conservative solid-box clearance from the actual reused prefab, including parent rotation/scale.
world=documents(ROOT/'Assets/_Project/Prefabs/World/Jehad/V1_2/MainLandscape_V1_2.prefab')
transforms={fid:d['Transform'] for fid,d in world.items() if 'Transform' in d}
by_go={t['m_GameObject']['fileID']:fid for fid,t in transforms.items()}
def vec(d):return tuple(float(d[k]) for k in 'xyz')
def rotate(v,q):
    x,y,z=v;qx,qy,qz,qw=(q[k] for k in 'xyzw');tx=2*(qy*z-qz*y);ty=2*(qz*x-qx*z);tz=2*(qx*y-qy*x)
    return (x+qw*tx+qy*tz-qz*ty,y+qw*ty+qz*tx-qx*tz,z+qw*tz+qx*ty-qy*tx)
def to_world(fid,v):
    t=transforms[fid];scaled=tuple(a*b for a,b in zip(v,vec(t['m_LocalScale'])));v=tuple(a+b for a,b in zip(rotate(scaled,t['m_LocalRotation']),vec(t['m_LocalPosition'])))
    return to_world(t['m_Father']['fileID'],v) if t['m_Father']['fileID'] else v
boxes=[]
for d in world.values():
    if 'BoxCollider' not in d:continue
    c=d['BoxCollider']
    if not c['m_Enabled'] or c['m_IsTrigger']:continue
    size=vec(c['m_Size']);center=vec(c['m_Center']);tid=by_go[c['m_GameObject']['fileID']]
    corners=[to_world(tid,tuple(center[i]+sign[i]*size[i]/2 for i in range(3))) for sign in [(x,y,z) for x in [-1,1] for y in [-1,1] for z in [-1,1]]]
    boxes.append((tuple(min(p[i] for p in corners) for i in range(3)),tuple(max(p[i] for p in corners) for i in range(3))))
def clearance(x,z):
    y=height(x,z)
    return min((math.hypot(max(a[0]-x,0,x-b[0]),max(a[2]-z,0,z-b[2])) for a,b in boxes if a[1]<y+1.8 and b[1]>y+.3),default=100)
for s in m['sources']:
    x,y,z=s['position'];assert normal(x,z)[1]>.9 and clearance(x,z)>1.2,('unsafe source terrain/clearance',s)
def free(x,z):return abs(x)<=116 and abs(z)<=116 and normal(x,z)[1]>.72 and clearance(x,z)>.5
valid={(x,z) for x in range(-116,117,2) for z in range(-116,117,2) if free(x,z)}
seen={(0,-100)};queue=collections.deque(seen);assert (0,-100) in valid
while queue:
    x,z=queue.popleft()
    for dx,dz in [(2,0),(-2,0),(0,2),(0,-2)]:
        p=(x+dx,z+dz)
        if p in valid and p not in seen and abs(height(*p)-height(x,z))<=1.8:seen.add(p);queue.append(p)
assert all((s['position'][0],s['position'][2]) in seen for s in m['sources']),'A source has no conservative terrain approach'

changed=[];baseline_files=0
if args.baseline:
    baseline=json.loads(args.baseline.read_text());baseline_files=len(baseline)
    for path,digest in baseline.items():
        f=ROOT/path;assert f.exists(),('deleted baseline file',path)
        if hashlib.sha256(f.read_bytes()).hexdigest()!=digest:changed.append(path)
    assert all(path=='docs/reference/contract-changes.md' for path in changed),('changed earlier checkpoint',changed)
report={'result':'PASS','scene':m['scene'],'new_csharp_syntax_files':len(m['scripts']),'new_scene_and_prefab_yaml_documents':sum(len(d) for d in all_docs.values()),'sources':6,'source_prefabs':3,'powers_per_location':['Sword','Bow','Fire'],'source_initially_inactive':True,'enemy_cap_configured':6,'conservative_terrain_approaches':6,'solid_box_colliders_checked':len(boxes),'source_clearances_m':{s['name']:round(clearance(s['position'][0],s['position'][2]),3) for s in m['sources']},'boss_surface_overlap_excluded':True,'baseline_files_checked':baseline_files,'modified_baseline_files':changed,'unity_compile_playtest_runtime_navmesh':'NOT RUN — no Unity Editor available','unity_acceptance_harness':'PopulationReviewV14 context menu; supplied, not executed'}
args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
