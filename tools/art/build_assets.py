"""Original project procedural geometry generator. Binary identity across toolchain versions is not guaranteed."""
import bpy, math, json, os, hashlib
from mathutils import Vector
ROOT=os.path.abspath(os.environ.get('HMI_ART_OUTPUT',os.path.join(os.path.dirname(__file__),'../v01')))
for d in ['models','sources','previews']: os.makedirs(f'{ROOT}/{d}',exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def mat(name,h,metal=0,rough=.45):
 m=bpy.data.materials.new(name);m.diffuse_color=(*[(lambda c:c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4)(int(h[i:i+2],16)/255) for i in (0,2,4)],1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=m.diffuse_color;p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;return m
M={k:mat(k,h,me,r) for k,h,me,r in [('cream','FFF2D9',0,.6),('ink','24152F',0,.22),('white','FFFFFF',0,.24),('pink','F08CAF',0,.5),('blue','6AA8E8',0,.5),('blue_dark','3477BE',0,.45),('purple','A885D6',0,.48),('orange','F58955',0,.5),('yellow','FFDB39',0,.45),('honey','E99516',0,.25),('plum','442B59',.5,.32),('brass','D9A84D',.7,.3),('steel','82758F',.65,.4),('brown','865133',0,.55)]}
parts=[]
def finish(o,name,ma):
 o.name=name;o.data.materials.append(M[ma]);parts.append(o)
 for p in o.data.polygons:p.use_smooth=True
 return o
def ball(name,p,s,ma,seg=16,rings=8):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=p);o=bpy.context.object;o.scale=s;return finish(o,name,ma)
def box(name,p,s,ma,bevel=.08):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 b=o.modifiers.new('Soft manufactured corners','BEVEL');b.width=bevel;b.segments=2;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=b.name);return finish(o,name,ma)
def cyl(name,p,r,depth,ma,axis='Z',verts=16):
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=p);o=bpy.context.object
 if axis=='Y':o.rotation_euler[0]=math.pi/2
 if axis=='X':o.rotation_euler[1]=math.pi/2
 return finish(o,name,ma)
def torus(name,p,r,th,ma,axis='Z'):
 bpy.ops.mesh.primitive_torus_add(major_radius=r,minor_radius=th,major_segments=16,minor_segments=6,location=p);o=bpy.context.object
 if axis=='Y':o.rotation_euler[0]=math.pi/2
 return finish(o,name,ma)
def face(z=1.02,y=.38,size=1):
 ball('Cream face',(0,y-.02,z-.06),(.36*size,.09,.29*size),'cream')
 for x in [-.16,.16]:
  ball('Chocolate eye',(x*size,y+.075,z+.005),(.073*size,.038,.09*size),'ink')
  ball('Eye catchlight',(x*size-.018*size,y+.108,z+.033),(.019*size,.013,.024*size),'white',12,6)
  ball('Rosy cheek',(x*1.38*size,y+.075,z-.105),(.064*size,.023,.032*size),'pink',12,6)
 ball('Nose',(0,y+.09,z-.07),(.035*size,.023,.026*size),'ink',12,6)
 ball('Smile',(0,y+.08,z-.13),(.045*size,.024,.035*size),'ink',12,6)
def body(color):
 ball('Soft body',(0,0,.48),(.35,.26,.43),color);ball('Cream tummy',(0,.225,.46),(.23,.06,.28),'cream');ball('Oversized head',(0,0,1.02),(.46,.36,.42),color)
 for x in [-.22,.22]:
  ball('Foot',(x,.08,.105),(.17,.23,.105),color);ball('Arm',(x*1.65,.04,.51),(.135,.15,.24),color)
 face()
 for x in [-.12,0,.12]:
  o=ball('Forehead tuft',(x,.21,1.32-abs(x)*.3),(.09,.07,.14),color,12,6);o.rotation_euler[1]=x*2
def hero(kind):
 color={'summon':'blue','honey':'yellow','orbit':'purple','crit':'orange','bounce':'pink'}[kind];body(color)
 if kind=='summon':
  for x,a in [(-.24,-.16),(.25,.6)]:
   o=ball('Long rabbit ear',(x,-.015,1.64),(.145,.13,.43),color);o.rotation_euler[1]=a
   o=ball('Pink ear inset',(x,.108,1.65),(.085,.035,.31),'pink');o.rotation_euler[1]=a
  ball('Cotton tail',(0,-.29,.42),(.17,.17,.17),'cream')
  for x in [-.1,.1]:
   o=ball('Neckerchief bow',(x,.34,.68),(.14,.065,.08),'blue_dark');o.rotation_euler[1]=x*3
  ball('Bow knot',(0,.405,.68),(.055,.04,.055),'blue')
 elif kind=='honey':
  for x in [-.33,.33]:
   ball('Bear ear',(x,0,1.32),(.17,.13,.18),color);ball('Ear inset',(x,.115,1.33),(.09,.025,.1),'pink')
  cyl('Honey backpack',(0,-.34,.58),.27,.48,'brown');torus('Honey jar lip',(0,-.34,.82),.24,.04,'honey');ball('Honey lid',(0,-.34,.83),(.25,.25,.055),'honey')
  for x in [-.27,.27]:box('Backpack strap',(x,.16,.59),(.045,.05,.42),'brown',.018)
  cyl('Honey wand',(.5,.13,.72),.035,.46,'brown');ball('Honey dipper',(.5,.13,.97),(.11,.1,.14),'honey')
 elif kind=='bounce':
  for x in [-.32,.32]:
   ball('Hamster ear',(x,0,1.31),(.15,.12,.15),color);ball('Ear inset',(x,.105,1.32),(.08,.025,.085),'cream')
  for x in [-.5,.5]:
   ball('Boxing glove',(x,.16,.62),(.26,.24,.28),'pink');ball('Glove patch',(x,.37,.64),(.15,.035,.16),'cream')
 elif kind=='crit':
  for x in [-.28,.28]:
   bpy.ops.mesh.primitive_cone_add(vertices=4,radius1=.25,radius2=.035,depth=.54,location=(x,0,1.48));finish(bpy.context.object,'Pointed fox ear','orange');ball('Inner ear',(x,.14,1.45),(.09,.03,.19),'cream')
  o=ball('Fox tail',(.34,-.34,.56),(.24,.2,.45),'orange');o.rotation_euler[1]=-.65
  ball('Tail cream tip',(.55,-.34,.83),(.17,.17,.18),'cream');cyl('Candy launcher',(.4,.34,.58),.13,.52,'brown','Y');torus('Launcher golden rim',(.4,.61,.58),.13,.033,'honey','Y');cyl('Launcher opening',(.4,.615,.58),.09,.014,'ink','Y')
 elif kind=='orbit':
  for x in [-.28,.28]:
   o=ball('Tall pointed ear',(x,-.02,1.39),(.15,.12,.3),'purple');o.rotation_euler[1]=x*.9;ball('Inner ear',(x,.10,1.4),(.075,.025,.18),'cream')
  for side in [-1,1]:
   for z,s in [(.88,.35),(.42,.25)]:
    o=ball('Butterfly wing',(side*.45,-.19,z),(.4,.075,s),'purple',12,6);o.rotation_euler[1]=side*.5
    ball('Wing ivory inset',(side*.58,-.105,z),(.17,.025,s*.65),'cream',12,6);ball('Wing jewel',(side*.72,-.08,z),(.055,.028,.065),'pink',12,6)
def enemy():
 ball('Plum enamel shell',(0,0,.53),(.4,.34,.37),'plum')
 for x in [-.21,.21]:
  box('Mechanical boot',(x,.055,.07),(.23,.3,.14),'brass',.04);cyl('Ankle',(x,0,.19),.065,.18,'steel')
 for x in [-.16,.16]:
  cyl('Eye bezel',(x,.312,.58),.105,.05,'brass','Y');cyl('Eye inset',(x,.343,.58),.07,.02,'ink','Y')
 cyl('Button pupil',(-.16,.36,.58),.03,.025,'brass','Y')
 for a in [0,math.pi/2]:
  o=box('Cross screw',(.16,.36,.58),(.10,.02,.026),'brass',.005);o.rotation_euler[1]=a
 for x in [-.29,0,.29]:ball('Rivet',(x,.22,.76),(.034,.028,.034),'brass',12,6)
 box('Mouth grille',(0,.34,.4),(.16,.025,.04),'steel',.01)
 cyl('Key shaft',(0,-.36,.7),.045,.28,'brass','Y');box('Key upright',(0,-.5,.79),(.055,.055,.22),'brass',.015);box('Winding key bridge',(0,-.5,.88),(.3,.05,.065),'brass',.02)
 for x in [-.13,.13]:torus('Key loop',(x,-.5,.97),.09,.025,'brass','Y')
 for x in [-.41,.41]:ball('Shoulder bolt',(x,0,.45),(.075,.1,.075),'brass',12,6)
def companion(kind):
 body('orange' if kind=='shooter' else 'cream')
 if kind=='shooter':
  for x in [-.25,.25]:ball('Candy ear',(x,0,1.37),(.1,.1,.16),'orange')
  cyl('Sugar cannon',(0,.49,.62),.17,.4,'brown','Y');torus('Golden cannon rim',(0,.7,.62),.17,.04,'honey','Y');cyl('Cannon opening',(0,.72,.62),.12,.01,'ink','Y')
 else:
  for i in range(9):
   a=i*math.tau/9;ball('Marshmallow fluff',(.38*math.cos(a),.04,1.02+.35*math.sin(a)),(.14,.29,.14),'cream',12,6)
  ball('Support bow',(0,0,1.48),(.12,.09,.08),'blue');ball('Support gem',(.47,.13,.62),(.17,.06,.25),'purple',8,4);box('Gem glint',(.47,.195,.62),(.05,.015,.18),'white',.01)
def export(name,height):
 global parts
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 coords=[o.matrix_world@v.co for v in o.data.vertices];lo=Vector(tuple(min(v[i] for v in coords) for i in range(3)));hi=Vector(tuple(max(v[i] for v in coords) for i in range(3)));factor=height/(hi.z-lo.z)
 for v in o.data.vertices:v.co=(o.matrix_world@v.co-Vector((0,0,lo.z)))*factor
 o.location=(0,0,0);o.rotation_euler=(0,0,0);o.scale=(1,1,1)
 tri=o.modifiers.new('Export triangulation','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
 bpy.ops.export_scene.gltf(filepath=f'{ROOT}/models/{name}.glb',export_format='GLB',use_selection=True,export_yup=True,export_animations=False)
 bpy.ops.wm.save_as_mainfile(filepath=f'{ROOT}/sources/{name}.blend')
 records.append({'id':name,'height_m':height,'dimensions_blender_xyz':list(o.dimensions),'triangles':len(o.data.polygons),'materials':sorted(set(m.name for m in o.data.materials)),'sha256':hashlib.sha256(open(f'{ROOT}/models/{name}.glb','rb').read()).hexdigest(),'runtime_path':f'content/models/{name}.glb','source_generator':'tools/art/build_assets.py'})
 bpy.data.objects.remove(o,do_unlink=True);parts=[]
records=[]
for k in ['summon','honey','orbit','crit','bounce']:hero(k);export('hero_'+k,1.2)
enemy();export('enemy_chaser',.9)
for k in ['shooter','support']:companion(k);export('companion_'+k,.7)
box('Marshmallow wall',(0,0,.4),(1,.8,.8),'blue',.1);box('Icing top',(0,0,.81),(1.02,.82,.10),'cream',.08);export('wall_block',.8)
ball('Candy core',(0,0,.18),(.18,.18,.18),'pink')
for x in [-.25,.25]:
 bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=.14,radius2=.045,depth=.19,location=(x,0,.18),rotation=(0,math.pi/2 if x<0 else -math.pi/2,0));finish(bpy.context.object,'Candy wrapper','cream')
export('candy_pickup',.3)
json.dump({'version':'v01.1-recovered-20261031','author':'Original procedural Blender models authored for HMI-Game','license':'Original project assets; no third-party source assets','blender':bpy.app.version_string,'coordinate_system':'glTF Y-up, forward -Z, metre units, origin ground center','animation':'Unrigged; adapter handles presentation-only squash/bob','assets':records},open(f'{ROOT}/manifest.json','w'),indent=2)
