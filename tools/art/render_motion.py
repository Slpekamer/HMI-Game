import bpy,os,math,json
from mathutils import Vector
ROOT=os.path.abspath(os.environ.get('HMI_ART_OUTPUT',os.path.join(os.path.dirname(__file__),'../v02')))
name=os.environ.get('HMI_ART_ID','hero_summon');bpy.ops.wm.open_mainfile(filepath=f'{ROOT}/sources/{name}.blend')
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=16;s.cycles.use_denoising=False;s.render.resolution_x=480;s.render.resolution_y=480;s.render.resolution_percentage=100;s.render.film_transparent=False;s.world.color=(.28,.25,.22);s.view_settings.view_transform='AgX'
# actual ground contact, not a backdrop pretending to be geometry
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.005));plane=bpy.context.object
m=bpy.data.materials.new('Preview cream ground');m.diffuse_color=(.68,.58,.45,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=m.diffuse_color;plane.data.materials.append(m)
def light(p,power,size):
 bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((0,0,.6))-o.location).to_track_quat('-Z','Y').to_euler()
light((3,4,5),500,4);light((-3,2,3),350,3);light((1,-4,4),600,3)
bpy.ops.object.camera_add(location=(2.6,4,2.6));cam=bpy.context.object;s.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=1.65;cam.rotation_euler=(Vector((0,0,.62))-cam.location).to_track_quat('-Z','Y').to_euler()
base={o.name:(o.location.copy(),o.rotation_euler.copy(),o.scale.copy()) for o in bpy.data.objects if o.animation_data and o.animation_data.nla_tracks}
for clip,count in [('Idle',40),('Run',16),('Attack',9)]:
 if os.environ.get('HMI_CLIPS') and clip not in os.environ['HMI_CLIPS'].split(','):continue
 path=f'{ROOT}/previews/motion/{name}/{clip}';os.makedirs(path,exist_ok=True)
 for name2,pose in base.items():
  o=bpy.data.objects[name2];o.location,o.rotation_euler,o.scale=pose
  for track in o.animation_data.nla_tracks:track.mute=track.name!=clip
 for f in range(count):
  s.frame_set(f);s.render.filepath=f'{path}/{f:03}.png';bpy.ops.render.render(write_still=True)
