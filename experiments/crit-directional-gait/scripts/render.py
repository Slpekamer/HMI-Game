import bpy,json,sys,argparse
p=argparse.ArgumentParser();p.add_argument('--out',required=True);args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
from mathutils import Vector
R=args.out
bpy.ops.wm.open_mainfile(filepath=R+'/source/juanjuan-directional-candidate.blend');s=bpy.context.scene;rig=bpy.data.objects['FoodRig'];o=bpy.data.objects['JuanjuanMesh']

for a in list(bpy.data.objects):
 if a.type in ['LIGHT','CAMERA']:bpy.data.objects.remove(a,do_unlink=True)
s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[0].default_value=(.75,.71,.65,1);s.world.node_tree.nodes['Background'].inputs[1].default_value=.22
for loc,power,size in [((-2,3,3),270,2),((2,1,2),110,2),((0,-2,2),150,2)]:
 bpy.ops.object.light_add(type='AREA',location=loc);a=bpy.context.object;a.data.energy=power;a.data.size=size;a.rotation_euler=(Vector((0,0,.6))-a.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2,4,2.7));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=1.48;cam.rotation_euler=(Vector((.025,0,.6))-cam.location).to_track_quat('-Z','Y').to_euler();s.camera=cam;s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=False;s.render.threads_mode='FIXED';s.render.threads=2;s.render.resolution_x=480;s.render.resolution_y=560;s.render.resolution_percentage=100;s.view_settings.view_transform='AgX';s.view_settings.look='AgX - Medium High Contrast';s.view_settings.exposure=-.25
for clip in ['StrafeR','Backpedal']:
 for name,fr in [('support',1),('lead-lift',6),('lead-contact',11),('follow-lift',18)]:
  rig.animation_data.action=bpy.data.actions[clip];s.frame_set(fr);bpy.context.view_layer.update();s.render.filepath=R+'/previews/'+clip+'-'+name+'.png';bpy.ops.render.render(write_still=True)
