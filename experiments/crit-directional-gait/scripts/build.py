"""Blender background, CPU <=2 threads. One bounded pose candidate, no search."""
import bpy, math, json, hashlib, argparse, sys
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
p=argparse.ArgumentParser();p.add_argument('--source',required=True);p.add_argument('--out',required=True);args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
R=Path(args.out);[ (R/x).mkdir(parents=True,exist_ok=True) for x in ['source','runtime','evidence','previews'] ]
bpy.ops.wm.open_mainfile(filepath=args.source)
s=bpy.context.scene;rig=bpy.data.objects['FoodRig'];mesh=bpy.data.objects['JuanjuanMesh']
rig.animation_data.action=bpy.data.actions['Idle'];s.frame_set(1);bpy.context.view_layer.update()
ready={p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones}
report={'source_sha256':hashlib.sha256(Path(args.source).read_bytes()).hexdigest(),'design':{'span_m':.060,'lift_m':.040,'pelvis_down_m':.018,'samples_per_clip':25,'upper_world_down_m':.018,'period_export_s':25/30,'stance_fraction':.6,'R_phase_at_start':.6,'L_phase_at_start':.1,'fixed_bone_length':True,'no_target_clamping':True},'rig':{},'clips':{}}
footverts={};cuffverts={}
for side in ['L','R']:
 name='Foot_'+side;g=mesh.vertex_groups[name];vv=[v for v in mesh.data.vertices if any(w.group==g.index and w.weight>.001 for w in v.groups)];footverts[side]=[v.index for v in vv];cuffverts[side]=[v.index for v in vv if not any(w.group==g.index and w.weight>.999 for w in v.groups)]
 report['rig'][side]={'weighted_foot_vertices':len(vv),'mixed_cuff_vertices':len(cuffverts[side]),'thigh_length':rig.data.bones['Thigh_'+side].length,'shin_length':rig.data.bones['Shin_'+side].length,'ankle_rest':list(rig.data.bones[name].head_local)}
# Include mesh edges incident to cuff vertices: a bounded metric, not proof of self-intersection absence.
cuff=set(cuffverts['L']+cuffverts['R']);edges=[(e.vertices[0],e.vertices[1]) for e in mesh.data.edges if any(v in cuff for v in e.vertices)]
restlen=[(mesh.data.vertices[a].co-mesh.data.vertices[b].co).length for a,b in edges]
def solve(side,target,rot):
 a=rig.pose.bones['Thigh_'+side];b=rig.pose.bones['Shin_'+side];f=rig.pose.bones['Foot_'+side];start=a.head.copy();v=target-start;dist=v.length;l1=a.bone.length;l2=b.bone.length
 assert abs(l1-l2)+1e-5<dist<l1+l2-1e-5,('UNREACHABLE',side,dist,l1+l2)
 direction=v.normalized();along=(l1*l1-l2*l2+dist*dist)/(2*dist);height=math.sqrt(l1*l1-along*along);pole=Vector((0,1,0));perp=(pole-direction*pole.dot(direction)).normalized();knee=start+direction*along+perp*height
 for pb,head,tail in [(a,start,knee),(b,knee,target)]:
  q=(pb.bone.tail_local-pb.bone.head_local).rotation_difference(tail-head)@pb.bone.matrix_local.to_quaternion();pb.matrix=Matrix.Translation(head)@q.to_matrix().to_4x4();bpy.context.view_layer.update()
 f.matrix=Matrix.Translation(target)@rot.to_matrix().to_4x4();bpy.context.view_layer.update()
 return {'reach_margin_m':l1+l2-dist,'fold_margin_m':dist-abs(l1-l2),'ankle_error_m':(f.head-target).length,'knee':list(knee),'target':list(target)}
for clip in ['StrafeR','Backpedal']:
 a=bpy.data.actions.new(clip);a.use_fake_user=True;a['loop']=True;rig.animation_data.action=a;s.render.fps=30;frames=[]
 for fr in range(1,26):
  s.frame_set(fr)
  for pb in rig.pose.bones:pb.location,pb.rotation_quaternion,pb.scale=[v.copy() for v in ready[pb.name]]
  rig.pose.bones['Pelvis'].location.y-=.018;bpy.context.view_layer.update();legs={}
  for side,off in [('R',.6),('L',.1)]:
   phase=((fr-1)/24+off)%1
   if phase<=.6:travel=.030-.060*phase/.6;lift=0.;arch=0.
   else:
    u=(phase-.6)/.4;smooth=u*u*(3-2*u);travel=-.030+.060*smooth;arch=math.sin(math.pi*u)**2;lift=.040*arch
   # Native front is +Y. Strafe right +X; backpedal -Y, never reversed Run.
   rest=rig.data.bones['Foot_'+side];target=rest.head_local.copy();target.x+=travel if clip=='StrafeR' else 0;target.y+=-travel if clip=='Backpedal' else 0
   yaw=math.radians(-10)*arch if clip=='StrafeR' else 0.;pitch=math.radians(10)*arch if clip=='Backpedal' else 0.
   rot=Quaternion((0,0,1),yaw)@Quaternion((1,0,0),pitch)@rest.matrix_local.to_quaternion()
   # Solve ankle height from actual rest boot vertices transformed by foot rigid frame.
   inv=rest.matrix_local.inverted();lowest=min((rot@(inv@mesh.data.vertices[i].co)).z for i in footverts[side]);target.z=-lowest+lift
   legs[side]=solve(side,target,rot);legs[side].update({'phase':phase,'lift_m':lift,'sole_target_m':lift})
  for pb in rig.pose.bones:
   if pb.name=='Root':continue
   for path in ['location','rotation_quaternion','scale']:pb.keyframe_insert(path,frame=fr,group=pb.name)
  ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh();co=[mesh.matrix_world@v.co for v in m.vertices];assert all(math.isfinite(x) for v in co for x in v)
  for side in legs:legs[side]['sole_actual_m']=min(co[i].z for i in footverts[side]);legs[side]['cuff_bounds']=[[min(co[i][axis] for i in cuffverts[side]),max(co[i][axis] for i in cuffverts[side])] for axis in range(3)]
  ratios=[(co[a]-co[b]).length/length for (a,b),length in zip(edges,restlen) if length>1e-6]
  frames.append({'frame':fr,'legs':legs,'model_min_z':min(v.z for v in co),'cuff_edge_ratio_min':min(ratios),'cuff_edge_ratio_max':max(ratios)})
  ev.to_mesh_clear()
 for fc in a.fcurves:
  for k in fc.keyframe_points:k.interpolation='LINEAR'
 report['clips'][clip]=frames
rig.animation_data.action=None
for pb in rig.pose.bones:pb.location=(0,0,0);pb.rotation_quaternion=(1,0,0,0);pb.scale=(1,1,1)
s.frame_set(1);bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(R/'source/juanjuan-directional-candidate.blend'))
(R/'evidence/pose-checks.json').write_text(json.dumps(report,indent=2))
bpy.ops.object.select_all(action='DESELECT')
for n in ['JuanjuanMesh','FoodRig','Socket_Hand_L','Socket_Hand_R','Socket_FX']:bpy.data.objects[n].select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.gltf(filepath=str(R/'runtime/carrier.glb'),export_format='GLB',use_selection=True,export_yup=True,export_animations=True,export_animation_mode='ACTIONS',export_skins=True,export_morph=True,export_morph_normal=True,export_morph_tangent=True,export_tangents=True,export_vertex_color='NONE',export_all_vertex_colors=False,export_active_vertex_color_when_no_material=False,export_morph_animation=False,export_materials='EXPORT',export_extras=True)
assert hashlib.sha256(Path(args.source).read_bytes()).hexdigest()==report['source_sha256'];print('BUILD_OK')
