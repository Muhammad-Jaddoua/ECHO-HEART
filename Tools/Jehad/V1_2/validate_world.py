"""Static asset checks and conservative terrain reachability; not a substitute for Unity."""
from pathlib import Path
import argparse,json,re,yaml,struct,math,collections,hashlib
from landscape import height,normal,SECRETS,ROUTES

ROOT=Path(__file__).resolve().parents[3]
p=argparse.ArgumentParser();p.add_argument('--work-dir',type=Path,default=ROOT/'Temp/JehadV12');args=p.parse_args();WORK=args.work_dir
manifest=json.loads((WORK/'manifest.json').read_text())
paths=[f for f in (ROOT/'Assets').rglob('*') if f.is_file() and ('/V1_2/' in f.as_posix() or '/KayKitForest/' in f.as_posix() or f.name.startswith('Jehad_V1.2.'))]
guids={};docs_by_path={};mesh_data={};numdocs=0
for file in (ROOT/'Assets').rglob('*.meta'):
    d=yaml.safe_load(file.read_text());g=d['guid'];assert g not in guids,('duplicate GUID',g)
    guids[g]=file.with_suffix('')
allowed=set(re.findall(r'guid: ([0-9a-f]{32})',(ROOT/'Assets/_Project/Scenes/Jehad/Jehad_V1.1.unity').read_text()+(ROOT/'Assets/_Project/Art/Materials/Jehad/V1_0/M_Jehad_Grass_V1_0.mat').read_text()))
for f in paths:
    if not f.name.endswith('.meta'):assert Path(str(f)+'.meta').exists(),('missing meta',str(f))
    if f.suffix not in ['.asset','.mat','.prefab','.unity']:continue
    t=f.read_text();ids=re.findall(r'^--- !u!\d+ &(-?\d+)',t,re.M);assert len(ids)==len(set(ids)),('duplicate ID',f)
    norm=re.sub(r'^%.*\n','',t,flags=re.M);norm=re.sub(r'^--- !u!\d+ &(-?\d+)(?: stripped)?',r'--- &\1',norm,flags=re.M)
    # Unity's hexadecimal byte scalars can consist solely of digits; PyYAML otherwise
    # treats tiny index buffers as integers and discards their leading zero bytes.
    norm=re.sub(r'^(\s*(?:m_IndexBuffer|_typelessdata): )([0-9a-f]+)$',r'\1"\2"',norm,flags=re.M)
    docs=list(yaml.safe_load_all(norm));assert len(ids)==len(docs)
    docs_by_path[f]={fid:d for fid,d in zip(ids,docs)};numdocs+=len(docs)
    for line in t.splitlines():
        if 'guid:' not in line:
            for fid in re.findall(r'fileID: (-?\d+)',line):assert fid=='0' or fid in ids,('dangling local reference',f,line)
    for g in re.findall(r'guid: ([0-9a-f]{32})',t):assert g in guids or g in allowed,('unknown GUID',f,g)
for f,docs in docs_by_path.items():
    for fid,g in re.findall(r'fileID: (-?\d+), guid: ([0-9a-f]{32})',f.read_text()):
        target=guids.get(g)
        if target not in docs_by_path:continue
        if fid=='100100000' and target.suffix=='.prefab':continue
        assert fid in docs_by_path[target],('unknown cross-asset ID',f,target,fid)
    for fid,doc in docs.items():
        if 'Transform' in doc:
            t=doc['Transform']
            for c in t.get('m_Children',[]):assert docs[str(c['fileID'])]['Transform']['m_Father']['fileID']==int(fid),(f,'parent mismatch',fid)
        for kind,obj in doc.items():
            if isinstance(obj,dict) and obj.get('m_GameObject',{}).get('fileID',0):
                go=docs[str(obj['m_GameObject']['fileID'])]['GameObject']
                assert any(str(c['component']['fileID'])==fid for c in go['m_Component']),('component not attached',f,fid)
        if '4300000'==fid:
            mesh=doc['Mesh'];data=mesh['m_VertexData'];vb=bytes.fromhex(data['_typelessdata']);ib=bytes.fromhex(mesh['m_IndexBuffer'])
            assert len(vb)==data['m_DataSize']==data['m_VertexCount']*32,(f,'vertex stride')
            vertices=list(struct.iter_unpack('<8f',vb));ix=struct.unpack('<'+'H'*(len(ib)//2),ib)
            assert len(ix)%3==0 and max(ix)<len(vertices)
            assert sum(s['indexCount'] for s in mesh['m_SubMeshes'])==len(ix)
            assert data['m_Channels'][4]['dimension']==2
            for v in vertices:
                assert all(math.isfinite(x) for x in v)
                assert abs(sum(x*x for x in v[3:6])-1)<.007,(f,'non-unit normal')
                for i,k in enumerate('xyz'):
                    b=mesh['m_LocalAABB'];assert abs(v[i]-float(b['m_Center'][k]))<=float(b['m_Extent'][k])+.002,(f,'bad bounds')
            mesh_data[f]={'vertices':vertices,'indices':ix,'mesh':mesh}
            if f.name.startswith(('Landscape_','WindingRoutes','LakeAndRivers')):
                for j in range(0,len(ix),3):
                    a,b,c=[vertices[k] for k in ix[j:j+3]]
                    assert (b[2]-a[2])*(c[0]-a[0])-(b[0]-a[0])*(c[2]-a[2])>0,(f,'downward surface')

# Compare the exact full-resolution OBJ corner stream against the native mesh.
for f,d in mesh_data.items():
    if '/KayKitForest/Meshes/' not in str(f) or f.stem.endswith('_LOD1'):continue
    source=ROOT/'Assets/ThirdParty/KayKitForest/Source'/(f.stem+'.obj.txt')
    vs=[];ns=[];uvs=[];vertices=[];groups={};lookup={};material='forest'
    for line in source.read_text().splitlines():
        w=line.split()
        if not w:continue
        if w[0]=='v':vs.append(tuple(map(float,w[1:4])))
        elif w[0]=='vn':ns.append(tuple(map(float,w[1:4])))
        elif w[0]=='vt':uvs.append(tuple(map(float,w[1:3])))
        elif w[0]=='usemtl':material=w[1]
        elif w[0]=='f':
            face=[]
            for word in w[1:]:
                v,t,n=(int(x)-1 for x in word.split('/'));key=(v,t,n)
                if key not in lookup:lookup[key]=len(vertices);vertices.append(vs[v]+ns[n]+uvs[t])
                face.append(lookup[key])
            for j in range(1,len(face)-1):groups.setdefault(material,[]).extend([face[0],face[j],face[j+1]])
    assert len(vertices)==len(d['vertices'])
    assert all(abs(x-y)<2e-6 for a,b in zip(vertices,d['vertices']) for x,y in zip(a,b)),('changed OBJ data',f)
    assert tuple(i for g in groups.values() for i in g)==d['indices']
    lod=mesh_data[f.with_name(f.stem+'_LOD1.asset')] if f.stem.startswith('Tree') else None
    if lod:assert len(lod['indices'])<len(d['indices'])*.5,(f,'ineffective LOD')

main_docs=docs_by_path[ROOT/manifest['main_prefab']]
foliage=[x['MonoBehaviour'] for x in main_docs.values() if 'MonoBehaviour' in x and x['MonoBehaviour'].get('m_EditorClassIdentifier','').endswith('InstancedFoliageV12')]
assert len(foliage)==1
total=0
for b in foliage[0]['_batches']:
    assert 0<len(b['_positions'])<=256 and len(b['_positions'])==len(b['_rotations'])==len(b['_scales'])
    total+=len(b['_positions']);bounds=b['_bounds']
    mesh=mesh_data[guids[b['_mesh']['guid']]]
    # Check the instance bounds against each actual rotated mesh vertex.
    for p,q,s in zip(b['_positions'],b['_rotations'],b['_scales']):
        assert abs(sum(float(v)**2 for v in q.values())-1)<.0001
        c=1-2*q['y']**2;sn=2*q['y']*q['w']
        for v in mesh['vertices']:
            xyz=(p['x']+s*(c*v[0]+sn*v[2]),p['y']+s*v[1],p['z']+s*(-sn*v[0]+c*v[2]))
            assert all(abs(xyz[i]-bounds['m_Center'][k])<=bounds['m_Extent'][k]+.003 for i,k in enumerate('xyz')),('clipped foliage bounds',p)
assert total==manifest['ground_cover_instances']
relics=[x['MonoBehaviour'] for x in main_docs.values() if 'MonoBehaviour' in x and x['MonoBehaviour'].get('m_EditorClassIdentifier','').endswith('HiddenRelicV12')]
assert len(relics)==16 and len({r['_id'] for r in relics})==16
lodgroups=[x['LODGroup'] for x in main_docs.values() if 'LODGroup' in x]
assert len(lodgroups)==len(manifest['trees'])
assert len([x for x in main_docs.values() if 'MeshCollider' in x])==len(manifest['mesh_colliders'])==11
scene=(ROOT/manifest['scene']).read_text();assert scene.count('AudioListener:\n')==1 and scene.count('\nPrefabInstance:\n')==2

# Terrain-only walking graph with 0.5 m obstacle clearance. Bridges and jump physics
# are separately marked for Unity review; shallow rivers can also be crossed on foot.
obstacles=[s for s in manifest['shapes'] if s['collider'] and s['name']!='MainArea_Floor']
def free(ix,iz):
    x,z=ix*2,iz*2;y=height(x,z)
    if abs(x)>116 or abs(z)>116 or normal(x,z)[1]<.72:return False
    return not any(s['pos'][1]-s['size'][1]/2<y+1.8 and s['pos'][1]+s['size'][1]/2>y+.3 and abs(x-s['pos'][0])<s['size'][0]/2+.5 and abs(z-s['pos'][2])<s['size'][2]/2+.5 for s in obstacles)
valid={(x,z) for x in range(-58,59) for z in range(-58,59) if free(x,z)}
start=(0,-50);assert start in valid
seen={start};q=collections.deque([start])
while q:
    x,z=q.popleft()
    for dx,dz in [(1,0),(-1,0),(0,1),(0,-1)]:
        p=(x+dx,z+dz)
        if p in valid and p not in seen and abs(height(p[0]*2,p[1]*2)-height(x*2,z*2))<=1.8:
            seen.add(p);q.append(p)
unreachable=[]
for name,x,z,description in SECRETS:
    if not any(math.hypot(a*2-x,b*2-z)<=2.1 for a,b in seen):unreachable.append(name)
assert not unreachable,('unreachable secret approaches',unreachable)
assert (21,44) in seen,'Boss entrance is unreachable'
report={'result':'PASS','world_m':[240,240],'main_area_m2':57600,'ratio_to_v11':57600/4896,'maximum_elevation_m':manifest['peak_height'],'secret_pickups':16,'terrain_reachable_secret_approaches':16,'tree_lod_groups':len(lodgroups),'foliage_instances':total,'foliage_batches':len(foliage[0]['_batches']),'mesh_colliders':11,'native_meshes':len(mesh_data),'parsed_yaml_documents':numdocs,'source_geometry_and_uvs':'exact full-resolution OBJ corner/index comparison passed','foliage_bounds':'all transformed vertices fit each batch bound','unity_editor_compile_shader_compile_playtest':'NOT RUN — no connected Unity Editor','navmesh_bake':'NOT RUN','gpu_profile':'NOT RUN'}
(WORK/'validation.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
