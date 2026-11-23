"""Called by build_world.py through Blender. Preserve UVs while reducing distant trees."""
import bpy,json,sys
from pathlib import Path

args=sys.argv[sys.argv.index('--')+1:]
models=json.loads(Path(args[0]).read_text());out={}
for name,d in models.items():
    mesh=bpy.data.meshes.new(name)
    ix=next(iter(d['groups'].values()))
    mesh.from_pydata(d['positions'],[],[ix[i:i+3] for i in range(0,len(ix),3)]);mesh.update()
    uv=mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:uv.data[loop.index].uv=d['uvs'][loop.vertex_index]
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    modifier=obj.modifiers.new('Distant silhouette','DECIMATE');modifier.ratio=.3
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    mesh=obj.data;mesh.calc_loop_triangles()
    positions=[];normals=[];uvs=[];indices=[];lookup={}
    for tri in mesh.loop_triangles:
        for li in tri.loops:
            loop=mesh.loops[li];p=mesh.vertices[loop.vertex_index].co;n=mesh.corner_normals[li].vector;t=mesh.uv_layers.active.data[li].uv
            key=tuple(round(v,7) for v in (*p,*n,*t))
            if key not in lookup:
                lookup[key]=len(positions);positions.append(list(p));normals.append(list(n));uvs.append(list(t))
            indices.append(lookup[key])
    out[name]={'positions':positions,'normals':normals,'uvs':uvs,'groups':{'forest':indices}}
    bpy.data.objects.remove(obj,do_unlink=True)
Path(args[1]).write_text(json.dumps(out))
