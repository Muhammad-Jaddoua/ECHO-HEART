"""Deterministic native Unity assets for Jehad V1.2. Python 3.10+."""
from pathlib import Path
import json,math,re,struct,uuid,subprocess
ROOT=Path(__file__).resolve().parents[3]
SOURCE=(ROOT/'Assets/_Project/Prefabs/World/Jehad/V1_0/MainArea_V1_0.prefab').read_text()
MAT=(ROOT/'Assets/_Project/Art/Materials/Jehad/V1_0/M_Jehad_Grass_V1_0.mat').read_text().replace('M_Jehad_Grass_V1_0','M_Ground')
BASE='Assets/_Project/'
ART=BASE+'Art/Materials/Jehad/V1_2/'
WORLD=BASE+'Prefabs/World/Jehad/V1_2/'
SCENE=BASE+'Scenes/Jehad/Jehad_V1.2.unity'
NS=uuid.UUID('d3d9e1cb-68e7-4c99-b954-540e36bd74d1')
FILES={};MESHES={};DETAILS=[]
EXISTING={str(p.relative_to(ROOT)) for p in (ROOT/'Assets').rglob('*.meta')}
def guid(path):
    return uuid.uuid5(NS, path).hex

def meta(path, kind='default'):
    if kind == 'folder':
        return f'fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    importer = 'NativeFormatImporter' if kind == 'material' else 'PrefabImporter' if kind == 'prefab' else 'DefaultImporter'
    main = '  mainObjectFileID: 2100000\n' if kind == 'material' else ''
    return f'fileFormatVersion: 2\nguid: {guid(path)}\n{importer}:\n  externalObjects: {{}}\n{main}  userData: \n  assetBundleName: \n  assetBundleVariant: \n'

def add(path, content, kind='default'):
    FILES[path] = content
    if kind != 'text':
        FILES[path + '.meta'] = meta(path, kind)
    if path.startswith('Assets/'):
        parent = Path(path).parent
        while str(parent) not in ('Assets', '.'):
            folder = parent.as_posix()
            if folder + '.meta' not in EXISTING and folder + '.meta' not in FILES:
                FILES[folder + '.meta'] = meta(folder, 'folder')
            parent = parent.parent

def original_block(file_id):
    return re.search(r'^--- !u!\d+ &' + str(file_id) + r'\n.*?(?=^--- !u!|\Z)', SOURCE, re.M | re.S).group()

T_GO = original_block(10010)
T_TRANSFORM = original_block(10011)
T_RENDER = original_block(10013)
T_COLLIDER = original_block(10014)
T_MESH = original_block(10012)

def remap(text, mapping):
    return re.sub(r'\b(?:' + '|'.join(map(str, mapping)) + r')\b', lambda m: str(mapping[int(m.group())]), text)


_ids={10010:600295488,10011:600295492,10012:600295491,10013:600295489,10014:600295490}
T_GO=remap(T_GO,_ids).replace('MainArea_Floor','Ground')
T_TRANSFORM=re.sub(r'm_Father: \{[^}]+\}','m_Father: {fileID: 0}',remap(T_TRANSFORM,_ids))
T_RENDER=remap(T_RENDER,_ids).replace('03df31c8c17c57e1ac6008f80fe729d8','52727152102a22b4a82694364e2c80b0')
T_COLLIDER=remap(T_COLLIDER,_ids)
T_MESH=remap(T_MESH,_ids)
def vector(v):
    return '{x: %g, y: %g, z: %g}' % tuple(v)

def transform(tid, gid, parent, pos=(0, 0, 0), scale=(1, 1, 1), yaw=0, children=()):
    text = remap(T_TRANSFORM, {600295492: tid, 600295488: gid})
    angle = math.radians(yaw) / 2
    text = re.sub(r'm_LocalRotation: \{[^}]+\}', 'm_LocalRotation: {x: 0, y: %g, z: 0, w: %g}' % (math.sin(angle), math.cos(angle)), text)
    text = re.sub(r'm_LocalPosition: \{[^}]+\}', 'm_LocalPosition: ' + vector(pos), text)
    text = re.sub(r'm_LocalScale: \{[^}]+\}', 'm_LocalScale: ' + vector(scale), text)
    text = re.sub(r'm_LocalEulerAnglesHint: \{[^}]+\}', 'm_LocalEulerAnglesHint: ' + vector((0, yaw, 0)), text)
    text = text.replace('m_Father: {fileID: 0}', f'm_Father: {{fileID: {parent}}}')
    child_text = 'm_Children:\n' + ''.join(f'  - {{fileID: {x}}}\n' for x in children) if children else 'm_Children: []\n'
    return text.replace('m_Children: []\n', child_text)

class Area:
    def __init__(self, name):
        self.name = name
        self.next_id = 10010
        self.parts = []
        self.children = []
        self.shapes = []
        self.markers = []

    def obj(self, name, pos, size=(1, 1, 1), mat='Stone', shape='cube', collider=True, yaw=0):
        gid, tid, mid, rid, cid = range(self.next_id, self.next_id + 5)
        self.next_id += 10
        self.children.append(tid)
        mapping = {600295488: gid, 600295492: tid, 600295491: mid, 600295489: rid, 600295490: cid}
        go = remap(T_GO, mapping).replace('m_Name: Ground', 'm_Name: ' + name)
        if not collider:
            go = go.replace(f'  - component: {{fileID: {cid}}}\n', '')
        render = remap(T_RENDER, mapping)
        material_guid = guid(ART + mat + '.mat')
        render = render.replace('52727152102a22b4a82694364e2c80b0', material_guid)
        if not collider:
            render = render.replace('m_CastShadows: 1', 'm_CastShadows: 0')
        mesh = remap(T_MESH, mapping)
        if shape == 'sphere':
            mesh = mesh.replace('fileID: 10202,', 'fileID: 10207,')
        self.parts.extend([go, transform(tid, gid, 10001, pos, size, yaw), mesh, render])
        if collider:
            self.parts.append(remap(T_COLLIDER, mapping))
        self.shapes.append({'name': name, 'pos': pos, 'size': size, 'collider': collider, 'shape': shape})

    def marker(self, name, pos):
        gid, tid = self.next_id, self.next_id + 1
        self.next_id += 10
        self.children.append(tid)
        go = remap(T_GO, {600295488: gid})
        go = re.sub(r'm_Component:\n(?:  - component:.*\n)+', f'm_Component:\n  - component: {{fileID: {tid}}}\n', go)
        go = go.replace('m_Name: Ground', 'm_Name: ' + name).replace('m_Layer: 13', 'm_Layer: 0')
        self.parts.extend([go, transform(tid, gid, 10001, pos)])
        self.markers.append({'name': name, 'position': pos})

    def build(self):
        go = remap(T_GO, {600295488: 10000})
        go = re.sub(r'm_Component:\n(?:  - component:.*\n)+', 'm_Component:\n  - component: {fileID: 10001}\n', go)
        go = go.replace('m_Name: Ground', 'm_Name: ' + self.name).replace('m_Layer: 13', 'm_Layer: 0')
        return '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n' + go + transform(10001, 10000, 0, children=self.children) + ''.join(self.parts)

def mesh_asset(path, positions, normals, groups, uvs=None):
    """Uncompressed position/normal stream; material groups become submeshes."""
    uvs = uvs if uvs is not None else [(0,0)]*len(positions)
    assert len(uvs)==len(positions)
    assert len(positions)==len(normals) and 0<len(positions)<65536
    lo=[min(v[i] for v in positions) for i in range(3)]
    hi=[max(v[i] for v in positions) for i in range(3)]
    center=[(a+b)/2 for a,b in zip(lo,hi)]; extent=[(b-a)/2 for a,b in zip(lo,hi)]
    bounds='    m_Center: '+vector(center)+'\n    m_Extent: '+vector(extent)+'\n'
    indices=[]; subs=''
    for material, group in groups.items():
        assert len(group)%3==0 and all(0<=v<len(positions) for v in group)
        subs+=f'  - serializedVersion: 2\n    firstByte: {len(indices)*2}\n    indexCount: {len(group)}\n    topology: 0\n    baseVertex: 0\n    firstVertex: 0\n    vertexCount: {len(positions)}\n    localAABB:\n'+''.join('  '+l+'\n' for l in bounds.rstrip().split('\n'))
        indices.extend(group)
    channels=''
    for i in range(14):
        channels+=f'    - stream: 0\n      offset: {12 if i==1 else 24 if i==4 else 0}\n      format: 0\n      dimension: {3 if i<2 else 2 if i==4 else 0}\n'
    vbytes=b''.join(struct.pack('<8f',*(tuple(p)+tuple(n)+tuple(uv))) for p,n,uv in zip(positions,normals,uvs))
    ibytes=struct.pack('<'+'H'*len(indices),*indices)
    compressed=''
    for n in ['Vertices','UV','Normals','Tangents','Weights','NormalSigns','TangentSigns','FloatColors','BoneIndices','Triangles']:
        compressed+=f'    m_{n}:\n      m_NumItems: 0\n'
        if n in ['Vertices','UV','Normals','Tangents','FloatColors']:
            compressed+='      m_Range: 0\n      m_Start: 0\n'
        compressed+='      m_Data: \n      m_BitSize: 0\n'
    text=f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!43 &4300000
Mesh:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {Path(path).stem}
  serializedVersion: 10
  m_SubMeshes:
{subs}  m_Shapes:
    vertices: []
    shapes: []
    channels: []
    fullWeights: []
  m_BindPose: []
  m_BoneNameHashes: 
  m_RootBoneNameHash: 0
  m_BonesAABB: []
  m_VariableBoneCountWeights:
    m_Data: 
  m_MeshCompression: 0
  m_IsReadable: 1
  m_KeepVertices: 1
  m_KeepIndices: 1
  m_IndexFormat: 0
  m_IndexBuffer: {ibytes.hex()}
  m_VertexData:
    serializedVersion: 3
    m_VertexCount: {len(positions)}
    m_Channels:
{channels}    m_DataSize: {len(vbytes)}
    _typelessdata: {vbytes.hex()}
  m_CompressedMesh:
{compressed}    m_UVInfo: 0
  m_LocalAABB:
{bounds}  m_MeshUsageFlags: 0
  m_BakedConvexCollisionMesh: 
  m_BakedTriangleCollisionMesh: 
  m_MeshMetrics[0]: 1
  m_MeshMetrics[1]: 1
  m_MeshOptimizationFlags: 1
  m_StreamData:
    serializedVersion: 2
    offset: 0
    size: 0
    path: 
'''
    add(path,text)
    FILES[path+'.meta']=meta(path).replace('DefaultImporter:', 'NativeFormatImporter:').replace('  externalObjects: {}\n','  externalObjects: {}\n  mainObjectFileID: 4300000\n')
    MESHES[path]={'positions':positions,'normals':normals,'groups':groups,'bounds':[lo,hi],'uvs':uvs}
