"""v02 segmented toy animation: genuine glTF node animation, no root motion.
Depends on build_assets.py geometry; never mutates a frozen release.
"""
import os
os.environ['HMI_ART_OUTPUT']=os.environ.get('HMI_ART_OUTPUT',os.path.abspath(os.path.join(os.path.dirname(__file__),'../v02')))
source=open(os.path.join(os.path.dirname(__file__),'build_assets.py')).read()
exec(compile(source[:source.rfind('records=[]')], 'build_assets_geometry.py','exec'))
from mathutils import Matrix
exec(compile(open(os.path.join(os.path.dirname(__file__),'refined_geometry.py')).read(),'refined_geometry.py','exec'))
records=[]
CLIPS={'Idle':(1.6,True),'Run':(.64,True),'Attack':(.36,False)}
FPS=25

def empty(name,pos,parent=None):
 o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o)
 if parent:o.parent=parent
 o.matrix_world=Matrix.Translation(Vector(pos))
 bpy.context.view_layer.update()
 return o

def group_for(o):
 n=o.name.split('.')[0];x=o.location.x
 if abs(x)<1e-6 and len(o.data.vertices):x=sum((o.matrix_world@v.co).x for v in o.data.vertices)/len(o.data.vertices)
 side='L' if x<0 else 'R'
 if n.startswith(('Sugar cannon','Golden cannon','Cannon opening','Support gem','Gem glint','Honey wrist')):return 'ArmR'
 if n.startswith('Face ') or n.startswith('Head '):return 'Head'
 if n.startswith('Ear'):return 'Ear'+side
 if n.startswith('Hand') or n.startswith('Glove') or n.startswith('Launcher') or n.startswith('Honey dipper'):return 'Arm'+side
 if n.startswith('Foot'):return 'Foot'+side
 if n.startswith('Wing'):return 'Wing'+side
 if n in ['Foot','Mechanical boot','Ankle']:return 'Foot'+side
 if n in ['Arm','Boxing glove','Glove patch','Honey wand','Honey dipper','Candy launcher','Launcher golden rim','Launcher opening']:return 'Arm'+side
 if n in ['Long rabbit ear','Pink ear inset','Bear ear','Ear inset','Hamster ear','Pointed fox ear','Inner ear','Tall pointed ear','Candy ear']:return 'Ear'+side
 if n in ['Butterfly wing','Wing ivory inset','Wing jewel']:return 'Wing'+side
 if n in ['Cotton tail','Fox tail','Tail cream tip']:return 'Tail'
 if n in ['Oversized head','Cream face','Chocolate eye','Eye catchlight','Rosy cheek','Nose','Smile','Forehead tuft','Marshmallow fluff','Support bow']:return 'Head'
 if n in ['Plum enamel shell','Eye bezel','Eye inset','Button pupil','Cross screw','Rivet','Mouth grille','Key shaft','Key upright','Winding key bridge','Key loop','Shoulder bolt']:return 'Head'
 return 'Body'

def make_motion(clip,t,node,kind):
 # normalized local displacement in authored Blender coordinates, rotations radians.
 tau=math.tau;phase=t*tau;rot=Vector((0,0,0));delta=Vector((0,0,0));scale=Vector((1,1,1));side=-1 if node.endswith('L') else 1
 if clip=='Idle':
  if node=='Body':scale.z=1+.025*math.sin(phase);scale.x=1-.012*math.sin(phase);delta.z=.012*(1+math.sin(phase))
  if node=='Head':rot.y=.035*math.sin(phase);rot.x=.022*math.sin(phase+.4)
  if node.startswith('Arm'):rot.y=side*.035*math.sin(phase+.3)
  if node.startswith('Ear'):rot.y=side*.055*math.sin(phase+.7)
  if node.startswith('Wing'):rot.z=side*.10*math.sin(phase)
  if node=='Tail':rot.y=.08*math.sin(phase)
 elif clip=='Run':
  stride=math.sin(phase)
  if node=='Body':delta.z=.055*(1-math.cos(phase*2));rot.x=-.12;rot.y=.07*stride;scale.z=1-.025*math.cos(phase*2)
  if node=='Head':rot.x=.06;rot.y=-.065*stride;delta.z=.012*math.sin(phase*2+.3)
  if node.startswith('Foot'):rot.x=side*.42*stride;delta.y=side*.13*stride;delta.z=.11*max(0,side*stride)
  if node.startswith('Arm'):rot.x=-side*.48*stride;rot.y=side*.10
  if node.startswith('Ear'):rot.x=.18*math.sin(phase+.8);rot.y=side*.09*math.sin(phase*2)
  if node.startswith('Wing'):rot.z=side*.38*math.sin(phase*2)
  if node=='Tail':rot.y=.15*stride;rot.x=.12*math.sin(phase*2)
 elif clip=='Attack':
  # anticipation -> impact -> recovery, endpoints at neutral.
  pulse=math.sin(math.pi*t)**2;strike=math.sin(math.pi*min(1,t/.55)) if t<.55 else 0
  if node=='Body':rot.x=-.17*strike;rot.z=.16*math.sin(t*tau)*pulse;scale.z=1-.08*strike
  if node=='Head':rot.x=.12*strike;rot.z=-.08*math.sin(t*tau)*pulse
  if node.startswith('Arm'):
   rot.x=.95*strike if kind in ['bounce','honey'] else .75*strike;rot.y=side*.15*strike;delta.y=.12*strike
  if node.startswith('Foot'):rot.x=-.10*strike
  if node.startswith('Ear'):rot.x=.20*math.sin(t*tau)*pulse
  if node.startswith('Wing'):rot.z=side*.50*strike
  if node=='Tail':rot.y=.2*math.sin(t*tau)*pulse
 return delta,rot,scale

def animate_export(name,height,kind):
 global parts
 scene=bpy.context.scene;scene.render.fps=FPS
 bpy.context.view_layer.update()
 coords=[o.matrix_world@v.co for o in parts for v in o.data.vertices];low=min(v.z for v in coords);high=max(v.z for v in coords);factor=height/(high-low)
 root=empty(name,(0,0,0));nodes={}
 positions={'Body':(0,0,.48),'Head':(0,0,1.02),'ArmL':(-.32,0,.65),'ArmR':(.32,0,.65),'FootL':(-.22,0,.13),'FootR':(.22,0,.13),'EarL':(-.25,0,1.32),'EarR':(.25,0,1.32),'WingL':(-.25,-.15,.8),'WingR':(.25,-.15,.8),'Tail':(0,-.2,.45)}
 if kind=='chaser':positions.update({'Body':(0,0,.38),'Head':(0,0,.53)})
 # transform geometry to normalized root-space first; animation pivots use same normalization
 for o in parts:
  mat=o.matrix_world.copy()
  for v in o.data.vertices:v.co=(mat@v.co-Vector((0,0,low)))*factor
  o.matrix_world=Matrix.Identity(4)
 # Consolidate geometry per animated part to avoid dozens of MeshInstance nodes.
 buckets={}
 for o in parts:buckets.setdefault(assignments[o.name],[]).append(o)
 merged=[]
 for group,objects in buckets.items():
  bpy.ops.object.select_all(action='DESELECT')
  for o in objects:o.select_set(True)
  bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=group+'Mesh';merged.append(o);assignments[o.name]=group
 parts=merged
 for n,p in positions.items():nodes[n]=empty(n,(Vector(p)-Vector((0,0,low)))*factor,root)
 bpy.context.view_layer.update()
 for o in parts:
  n=group_for_geometry_name(o)
  mat=o.matrix_world.copy();o.parent=nodes[n];o.matrix_world=mat
 # actual groups need source x side saved before normalization; supplied by map
 for n in ['Head','ArmL','ArmR','WingL','WingR','Tail']:
  mat=nodes[n].matrix_world.copy();nodes[n].parent=nodes['Body'];nodes[n].matrix_world=mat
 for n in ['EarL','EarR']:
  mat=nodes[n].matrix_world.copy();nodes[n].parent=nodes['Head'];nodes[n].matrix_world=mat
 bpy.context.view_layer.update()
 baseline={n:(o.location.copy(),o.rotation_euler.copy(),o.scale.copy()) for n,o in nodes.items()}
 # Named NLA tracks aggregate same-name object actions into one GLTF clip.
 for clip,(duration,loop) in CLIPS.items():
  end=round(duration*FPS)
  for n,o in nodes.items():
   o.animation_data_create();action=bpy.data.actions.new(name=f'{name}_{n}_{clip}');o.animation_data.action=action
   for f in range(end+1):
    d,r,s=make_motion(clip,f/end,n,kind);loc,rot,scl=baseline[n];o.location=loc+d*factor;o.rotation_euler=Vector(rot)+r;o.scale=Vector((scl[i]*s[i] for i in range(3)))
    o.keyframe_insert('location',frame=f);o.keyframe_insert('rotation_euler',frame=f);o.keyframe_insert('scale',frame=f)
   for fc in action.fcurves:
    for key in fc.keyframe_points:key.interpolation='LINEAR'
   track=o.animation_data.nla_tracks.new();track.name=clip;strip=track.strips.new(clip,0,action);o.animation_data.action=None
   # Keep all strips muted during authoring neutral pose, exporter exports every NLA track.
   track.mute=True
  for n,o in nodes.items():o.location,o.rotation_euler,o.scale=baseline[n]
 scene.frame_set(0);bpy.context.view_layer.update()
 bpy.ops.object.select_all(action='DESELECT')
 root.select_set(True)
 for o in list(nodes.values())+parts:o.select_set(True)
 bpy.context.view_layer.objects.active=root
 # Blender4.3 exporter gathers even muted NLA tracks; keep authoring rest pose neutral.
 for o in nodes.values():
  for tr in o.animation_data.nla_tracks:tr.mute=True
 scene.frame_start=0;scene.frame_end=40
 bpy.ops.export_scene.gltf(filepath=f'{ROOT}/models/{name}.glb',export_format='GLB',use_selection=True,export_yup=True,export_animations=True,export_animation_mode='NLA_TRACKS',export_nla_strips=True,export_force_sampling=True)
 bpy.ops.wm.save_as_mainfile(filepath=f'{ROOT}/sources/{name}.blend')
 triangles=sum(sum(len(poly.vertices)-2 for poly in o.data.polygons) for o in parts)
 records.append({'id':name,'height_m':height,'triangles':triangles,'meshes':len(parts),'animation':'segmented node animation; no skin; root stationary','clips':{n:{'duration_seconds':d,'loop':l} for n,(d,l) in CLIPS.items()},'sha256':hashlib.sha256(open(f'{ROOT}/models/{name}.glb','rb').read()).hexdigest()})
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);parts=[]

def group_for_geometry_name(o):return assignments[o.name]

for kind in ['summon','honey','orbit','crit','bounce']:
 if os.environ.get('HMI_ART_IDS') and 'hero_'+kind not in os.environ['HMI_ART_IDS'].split(','):continue
 hero(kind);assignments={o.name:group_for(o) for o in parts};animate_export('hero_'+kind,1.2,kind)
for kind in ['shooter','support']:
 if os.environ.get('HMI_ART_IDS') and 'companion_'+kind not in os.environ['HMI_ART_IDS'].split(','):continue
 companion(kind);assignments={o.name:group_for(o) for o in parts};animate_export('companion_'+kind,.7,kind)
if not os.environ.get('HMI_ART_IDS') or 'enemy_chaser' in os.environ['HMI_ART_IDS'].split(','):
 enemy();assignments={o.name:group_for(o) for o in parts};animate_export('enemy_chaser',.9,'chaser')
json.dump({'version':'v02-animated','source':'Original segmented Blender toy models; no external assets','coordinate_system':'metres,Y-up,forward-Z,ground root','root_motion':False,'clips':CLIPS,'assets':records},open(f'{ROOT}/manifest.json','w'),indent=2)
