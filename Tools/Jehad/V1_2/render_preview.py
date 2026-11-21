"""Blender geometry previews, not Unity screenshots. Run with Blender --python ... -- WORK OUT."""
import bpy,json,math,sys,re
from pathlib import Path
from mathutils import Vector

args=sys.argv[sys.argv.index('--')+1:];work=Path(args[0]);out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
root=Path(__file__).resolve().parents[3]
d=json.loads((work/'preview-data.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials={};meshes={}
def material(path):
    if path in materials:return materials[path]
    mat=bpy.data.materials.new(Path(path).stem);mat.use_nodes=True;nodes=mat.node_tree.nodes;bsdf=nodes.get('Principled BSDF')
    bsdf.inputs['Roughness'].default_value=.88
    if path==d['forest_material']:
        image=nodes.new('ShaderNodeTexImage');image.image=bpy.data.images.load(d['texture'],check_existing=True)
        mat.node_tree.links.new(image.outputs['Color'],bsdf.inputs['Base Color'])
    elif path.endswith('ValleyWater.mat'):
        uv=nodes.new('ShaderNodeTexCoord');sep=nodes.new('ShaderNodeSeparateXYZ');ramp=nodes.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].color=(.015,.32,.48,1);ramp.color_ramp.elements[1].position=.97;ramp.color_ramp.elements[1].color=(.78,.96,.91,1)
        mid=ramp.color_ramp.elements.new(.73);mid.color=(.035,.58,.64,1)
        mat.node_tree.links.new(uv.outputs['UV'],sep.inputs[0]);mat.node_tree.links.new(sep.outputs['X'],ramp.inputs[0]);mat.node_tree.links.new(ramp.outputs['Color'],bsdf.inputs['Base Color'])
        noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=.9
        bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.15;bump.inputs['Distance'].default_value=.12
        mat.node_tree.links.new(uv.outputs['Generated'],noise.inputs['Vector']);mat.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);mat.node_tree.links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])
        bsdf.inputs['Roughness'].default_value=.23;bsdf.inputs['Metallic'].default_value=.15
    else:
        text=(root/path).read_text();v=re.search(r'- _BaseColor: \{r: ([^,]+), g: ([^,]+), b: ([^,]+)',text)
        rgb=tuple(map(float,v.groups())) if v else (.5,.6,.5);bsdf.inputs['Base Color'].default_value=(*rgb,1)
    materials[path]=mat;return mat

for a in d['details']:
    key=(a['mesh'],tuple(a['materials']))
    if key not in meshes:
        data=d['meshes'][a['mesh']];me=bpy.data.meshes.new(Path(a['mesh']).stem);faces=[];ids=[]
        for i,(name,ix) in enumerate(data['groups'].items()):
            me.materials.append(material(a['materials'][i]));faces.extend(ix[j:j+3] for j in range(0,len(ix),3));ids.extend([i]*(len(ix)//3))
        me.from_pydata(data['positions'],[],faces);me.update()
        uv=me.uv_layers.new(name='UVMap');uvs=data.get('uvs',[(0,0)]*len(data['positions']))
        for loop in me.loops:uv.data[loop.index].uv=uvs[loop.vertex_index]
        for poly,mi in zip(me.polygons,ids):poly.material_index=mi;poly.use_smooth=True
        me.normals_split_custom_set([data['normals'][loop.vertex_index] for loop in me.loops])
        meshes[key]=me
    obj=bpy.data.objects.new(a['name'],meshes[key]);bpy.context.collection.objects.link(obj)
    x,y,z=a['pos'];obj.location=(x,-z,y);obj.rotation_euler=(math.pi/2,0,-math.radians(a['yaw']));obj.scale=a['scale']

def shape(a,offset=(0,0,0)):
    if a['shape']=='invisible_box':return
    x,y,z=[v+b for v,b in zip(a['pos'],offset)];sx,sy,sz=a['size']
    if a['shape']=='sphere':bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=(x,-z,y))
    else:bpy.ops.mesh.primitive_cube_add(size=1,location=(x,-z,y))
    obj=bpy.context.object;obj.name=a['name'];obj.dimensions=(sx,sz,sy);obj.rotation_euler[2]=-math.radians(a.get('yaw',0))
    obj.data.materials.append(material('Assets/_Project/Art/Materials/Jehad/V1_2/'+a.get('material','Stone')+'.mat'))
for a in d['shapes']:shape(a)
for a in d['arena_shapes']:shape(a,d['arena_offset'])
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=False
scene.render.resolution_x=1600;scene.render.resolution_y=1050;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='JPEG';scene.render.image_settings.quality=91
world=scene.world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.73,.9,1);world.node_tree.nodes['Background'].inputs[1].default_value=.45
bpy.ops.object.light_add(type='SUN',location=(40,70,130));sun=bpy.context.object;sun.rotation_euler=(.40,-.55,-.4);sun.data.energy=2.5;sun.data.angle=.18
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam
shots=[('Overview',(200,275,235),(0,-5,18),'ORTHO',305),('Lake',(101,60,25),(35,-8,10),'PERSP',25),('Mountain',(-104,-104,67),(10,22,7),'PERSP',30)]
if len(args)>2:shots=[s for s in shots if s[0] in args[2:]]
for name,loc,target,kind,lens in shots:
    cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type=kind
    if kind=='ORTHO':cam.data.ortho_scale=lens
    else:cam.data.lens=lens
    scene.render.filepath=str(out/('Jehad_V1.2_'+name+'.jpg'));bpy.ops.render.render(write_still=True)
