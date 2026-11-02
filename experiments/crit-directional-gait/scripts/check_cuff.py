import bpy,sys,argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--out',required=True);args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);R=Path(args.out)
bpy.ops.wm.open_mainfile(filepath=str(R/'source/juanjuan-directional-candidate.blend'));s=bpy.context.scene;r=bpy.data.objects['FoodRig'];o=bpy.data.objects['JuanjuanMesh']
foot=set(g.index for g in o.vertex_groups if g.name.startswith('Foot_'));mixed=set(v.index for v in o.data.vertices if any(g.group in foot and .001<g.weight<.999 for g in v.groups));edges=[tuple(e.vertices) for e in o.data.edges if any(v in mixed for v in e.vertices)];lengths=[(o.data.vertices[a].co-o.data.vertices[b].co).length for a,b in edges];report={}
r.animation_data.action=bpy.data.actions['Idle'];s.frame_set(1);bpy.context.view_layer.update()
upper=['Chest','Head','Scarf','UpperArm_L','Forearm_L','Hand_L','UpperArm_R','Forearm_R','Hand_R']
ready={n:r.pose.bones[n].matrix_basis.copy() for n in upper};head={n:r.pose.bones[n].head.copy() for n in upper};root=r.pose.bones['Root'].matrix.copy()
parity={'upper_local_max_error':0.,'upper_world_down_error_m':0.,'root_max_error':0.}
for name in ['Idle','Run','StrafeR','Backpedal']:
 r.animation_data.action=bpy.data.actions[name];frames=[]
 for i in range(97):
  f=1+i/4;s.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();
  if name in ['StrafeR','Backpedal']:
   parity['upper_local_max_error']=max(parity['upper_local_max_error'],max(abs(r.pose.bones[n].matrix_basis[i][j]-ready[n][i][j]) for n in upper for i in range(4) for j in range(4)))
   parity['upper_world_down_error_m']=max(parity['upper_world_down_error_m'],max(abs((r.pose.bones[n].head-head[n])[i]-(-.018 if i==2 else 0)) for n in upper for i in range(3)))
   parity['root_max_error']=max(parity['root_max_error'],max(abs(r.pose.bones['Root'].matrix[i][j]-root[i][j]) for i in range(4) for j in range(4)))
  ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=ev.to_mesh();coords=[o.matrix_world@v.co for v in m.vertices];assert all(math.isfinite(x) for v in coords for x in v);delta=[((coords[a]-coords[b]).length/l,l,(coords[a]-coords[b]).length,a,b) for (a,b),l in zip(edges,lengths) if l>1e-6];worst=max(delta)
  frames.append({'frame':f,'min_z':min(v.z for v in coords),'cuff_max_ratio':worst[0],'cuff_max_ratio_rest_edge_m':worst[1],'cuff_max_ratio_posed_edge_m':worst[2],'cuff_max_ratio_vertex_pair':worst[3:],'cuff_min_ratio':min(x[0] for x in delta),'cuff_max_edge_extension_m':max(x[2]-x[1] for x in delta)});ev.to_mesh_clear()
 report[name]={'samples':frames,'minimum_z':min(x['min_z'] for x in frames),'maximum_cuff_ratio':max(x['cuff_max_ratio'] for x in frames),'maximum_cuff_edge_extension_m':max(x['cuff_max_edge_extension_m'] for x in frames)}
(R/'evidence/cuff-interpolation.json').write_text(json.dumps(report,indent=2));print(json.dumps({k:{p:v for p,v in x.items() if p!='samples'} for k,x in report.items()}))

assert max(parity.values())<1e-5,parity
(R/'evidence/upper-root-parity.json').write_text(json.dumps(parity,indent=2));print(parity)
