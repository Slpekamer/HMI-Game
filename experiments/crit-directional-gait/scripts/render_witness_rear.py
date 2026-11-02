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
bpy.ops.object.camera_add(location=(-3,-.8,1.2));cam=bpy.context.object;cam.data.type='ORTHO';cam.data.ortho_scale=.62;cam.rotation_euler=(Vector((0,0,.19))-cam.location).to_track_quat('-Z','Y').to_euler();s.camera=cam;s.render.engine='CYCLES';s.cycles.samples=16;s.cycles.use_denoising=False;s.render.threads_mode='FIXED';s.render.threads=2;s.render.resolution_x=480;s.render.resolution_y=560;s.render.resolution_percentage=100;s.view_settings.view_transform='AgX';s.view_settings.look='AgX - Medium High Contrast';s.view_settings.exposure=-.25
witnesses=[]
for clip,fr,ids in [('StrafeR',18,[4111,4127]),('Backpedal',19,[4111,4127]),('Run',18,[4111,4127]),('Run',19,[4111,4127])]:
 rig.animation_data.action=bpy.data.actions[clip];s.frame_set(fr);bpy.context.view_layer.update()
 ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh();coords=[o.matrix_world@m.vertices[i].co for i in ids]
 witnesses.append({'clip':clip,'frame':fr,'vertices':ids,'rest_length_m':(o.data.vertices[ids[0]].co-o.data.vertices[ids[1]].co).length,'posed_length_m':(coords[0]-coords[1]).length,'points_world':[list(v) for v in coords],'weights':[{o.vertex_groups[g.group].name:g.weight for g in o.data.vertices[i].groups} for i in ids]});ev.to_mesh_clear()
 s.render.filepath=R+'/previews/rear-witness-'+clip+'-'+str(fr)+'.png';bpy.ops.render.render(write_still=True)
 # Overlay spheres and a rod locate the actual edge; they are NOT asset geometry.
 marker=[]
 mat=bpy.data.materials.new('Diagnostic-magenta');mat.diffuse_color=(1,0,.5,1);mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(1,0,.5,1);bs.inputs['Emission Color'].default_value=(1,0,.5,1);bs.inputs['Emission Strength'].default_value=2
 # Move markers 0.09m camera-ward to expose the region even when the edge is buried.
 toward=(cam.location-Vector((0,0,.19))).normalized()*.09
 for v in coords:
  bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.004,location=v+toward);obj=bpy.context.object;obj.data.materials.append(mat);marker.append(obj)
 curve=bpy.data.curves.new('Diagnostic-edge','CURVE');curve.dimensions='3D';curve.bevel_depth=.0015;poly=curve.splines.new('POLY');poly.points.add(1)
 for pt,v in zip(poly.points,coords):pt.co=(*(v+toward),1)
 obj=bpy.data.objects.new('Diagnostic-edge',curve);s.collection.objects.link(obj);obj.data.materials.append(mat);marker.append(obj)
 s.render.filepath=R+'/previews/rear-witness-'+clip+'-'+str(fr)+'-region-overlay.png';bpy.ops.render.render(write_still=True)
 for obj in marker:bpy.data.objects.remove(obj,do_unlink=True)
json.dump({'overlay_note':'Magenta locator shifted camera-ward 0.09m to reveal projected region; not visible asset geometry or intersection evidence. Clean counterpart is authoritative.','witnesses':witnesses},open(R+'/evidence/rear-cuff-witness.json','w'),indent=2)
