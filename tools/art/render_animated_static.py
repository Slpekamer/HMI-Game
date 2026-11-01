import bpy,os,json
from mathutils import Vector
ROOT=os.path.abspath(os.environ.get('HMI_ART_OUTPUT',os.path.join(os.path.dirname(__file__),'../v01')))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.use_denoising=False;s.render.resolution_x=600;s.render.resolution_y=600;s.render.resolution_percentage=100;s.render.film_transparent=True
s.world.color=(.35,.35,.35);s.view_settings.view_transform='AgX'
def light(p,power,size):
 bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,.6))-o.location).to_track_quat('-Z','Y').to_euler()
light((3,4,5),500,4);light((-3,2,3),350,3);light((1,-4,4),600,3)
bpy.ops.object.camera_add();cam=bpy.context.object;s.camera=cam;cam.data.type='ORTHO'
manifest=json.load(open(f'{ROOT}/manifest.json'));results=[]
for a in manifest['assets']:
 if os.environ.get('HMI_ART_IDS') and a['id'] not in os.environ['HMI_ART_IDS'].split(','):continue
 before=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=f"{ROOT}/models/{a['id']}.glb");added=set(bpy.data.objects)-before
 for obj in added:
  if obj.animation_data:
   obj.animation_data.action=None
   for track in obj.animation_data.nla_tracks:track.mute=True
 bpy.context.view_layer.update()
 meshes=[o for o in added if o.type=='MESH'];coords=[o.matrix_world@v.co for o in meshes for v in o.data.vertices];lo=[min(v[i] for v in coords) for i in range(3)];hi=[max(v[i] for v in coords) for i in range(3)]
 assert abs(lo[2])<1e-5,(a['id'],lo)
 assert abs(hi[2]-a['height_m'])<.05,(a['id'],hi)
 assert all(m.use_nodes for o in meshes for m in o.data.materials)
 results.append({'id':a['id'],'roundtrip_height':hi[2],'ground_min':lo[2],'mesh_count':len(meshes),'ok':True})
 target=Vector((0,0,a['height_m']*.52));cam.data.ortho_scale=max(hi[i]-lo[i] for i in range(3))*1.6
 views=['front','threequarter','back','side','gamecamera'] if a['id'].startswith('hero_') or a['id']=='enemy_chaser' else ['threequarter']
 for view in views:
  cam.location=target+Vector({'front':(0,4,1.4),'threequarter':(2.6,4,2.5),'back':(-2.6,-4,2.5),'side':(4,0,1),'gamecamera':(0,4,5)}[view]);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
  s.render.filepath=f"{ROOT}/previews/{a['id']}_{view}.png";bpy.ops.render.render(write_still=True)
 for o in added:bpy.data.objects.remove(o,do_unlink=True)
json.dump(results,open(f'{ROOT}/roundtrip-validation.json','w'),indent=2)
