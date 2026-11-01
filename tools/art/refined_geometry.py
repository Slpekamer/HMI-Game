"""Original v02 sculptural toy upgrade: volumetric face/paws/accessories; no textures."""
# Executed into build_animated_assets.py after base geometry definitions.
_base_ball=ball

def ball(name,p,s,ma,seg=24,rings=12):return _base_ball(name,p,s,ma,seg,rings)
M['iris']=mat('iris','684E99',0,.24);M['mouth']=mat('mouth','7D314D',0,.5);M['blush']=mat('blush','F9A2B8',0,.6)
M['iris_amber']=mat('iris_amber','6F3E26',0,.24);M['iris_blue']=mat('iris_blue','315E9A',0,.24);M['wing_membrane']=mat('wing_membrane','CBB0EF',0,.52)
FACE_KIND='summon'
M['cream_warm']=mat('cream_warm','FFE1B1',0,.65);M['seam']=mat('seam','BAD8F7',0,.68)

def stroke(name,points,radius,ma):
 curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=3;curve.bevel_depth=radius;curve.bevel_resolution=2
 sp=curve.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
 for b,p in zip(sp.bezier_points,points):b.co=p;b.handle_left_type='AUTO';b.handle_right_type='AUTO'
 o=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object
 # curves generated in world coordinates; side classifier uses weighted mesh center instead of location
 return finish(o,name,ma)

def star(name,p,r,depth,ma):
 verts=[]
 for y in [-depth/2,depth/2]:
  for i in range(10):
   a=math.pi/2+i*math.pi/5;rr=r if i%2==0 else r*.48;verts.append((p[0]+math.cos(a)*rr,p[1]+y,p[2]+math.sin(a)*rr))
 faces=[tuple(range(9,-1,-1)),tuple(range(10,20))]+[(i,(i+1)%10,(i+1)%10+10,i+10) for i in range(10)]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
 b=o.modifiers.new('Rounded star','BEVEL');b.width=.015;b.segments=2;bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.modifier_apply(modifier=b.name);return finish(o,name,ma)

def tapered_ear(name,centers,radii,ma):
 # Closed rounded ear with a curved centerline, not a low-poly cone.
 verts=[];sides=16
 for j,((x,y,z),(rx,ry)) in enumerate(zip(centers,radii)):
  tangent=(Vector(centers[min(j+1,len(centers)-1)])-Vector(centers[max(0,j-1)])).normalized()
  u=Vector((tangent.z,0,-tangent.x)).normalized();v=tangent.cross(u).normalized()
  for i in range(sides):
   a=i*math.tau/sides;point=Vector((x,y,z))+u*(rx*math.cos(a))+v*(ry*math.sin(a));verts.append(tuple(point))
 faces=[]
 for j in range(len(centers)-1):
  for i in range(sides):k=j*sides+i;kn=j*sides+(i+1)%sides;faces.append((k,kn,kn+sides,k+sides))
 faces+=[tuple(range(sides-1,-1,-1)),tuple((len(centers)-1)*sides+i for i in range(sides))]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);sub=o.modifiers.new('Soft ear surface','SUBSURF');sub.levels=1;bpy.ops.object.modifier_apply(modifier=sub.name);return finish(o,name,ma)

def face(z=.98,y=.37,size=1):
 # Two cheek/eye lobes merge into a soft muzzle instead of a flat cream disk.
 ball('Face unified ivory mask',(0,.305,z-.045),(.385,.095,.275),'cream')
 for x in [-.17,.17]:
  ball('Face muzzle cheek',(x*.64,.346,z-.195),(.195,.072,.105),'cream')
  ball('Face dark eye rim',(x,.392,z+.015),(.096,.040,.119),'ink')
  ball('Face luminous iris',(x,.424,z-.012),(.071,.021,.085),'iris_blue' if FACE_KIND=='summon' else 'iris_amber' if FACE_KIND in ['crit','honey','bounce'] else 'iris')
  ball('Face pupil',(x,.444,z+.006),(.048,.013,.072),'ink')
  ball('Face primary catchlight',(x-.023,.46,z+.050),(.024,.012,.031),'white',16,8)
  ball('Face tiny catchlight',(x+.023,.453,z-.034),(.010,.008,.013),'white',12,6)
  ball('Face blush',(x*1.5,.372,z-.125),(.070,.026,.039),'blush')
 ball('Face nose left',(-.014,.450,z-.122),(.027,.024,.020),'ink',16,8);ball('Face nose right',(.014,.450,z-.122),(.027,.024,.020),'ink',16,8)
 ball('Face happy mouth',(0,.438,z-.190),(.070,.022,.044),'mouth')
 ball('Face pink tongue',(0,.461,z-.207),(.028,.009,.013),'pink',16,8)

def body(color):
 ball('Soft body',(0,0,.43),(.33,.26,.34),color)
 ball('Cream tummy',(0,.235,.43),(.22,.055,.255),'cream')
 ball('Oversized head',(0,0,.98),(.48,.36,.43),color)
 for x in [-.21,.21]:
  ball('Foot',(x,.09,.105),(.175,.22,.105),color)
  ball('Foot sole',(x,.245,.080),(.12,.027,.065),'cream_warm')
  for dx in [-.055,0,.055]:ball('Foot toe',(x+dx,.259,.132),(.029,.028,.027),'cream',12,6)
  ball('Arm',(x*1.72,.035,.48),(.13,.14,.22),color)
  ball('Hand palm',(x*1.86,.102,.345),(.126,.13,.115),color)
  for dx in [-.05,0,.05]:ball('Hand finger',(x*1.86+dx,.200,.355),(.033,.032,.045),'cream',12,6)
 face()
 # Three elongated, overlapping swept tufts rather than forehead beads.
 for x,z,r in [(-.13,1.285,-.35),(0,1.33,.10),(.13,1.29,.35)]:
  o=ball('Face forehead curl',(x,.215,z),(.105,.10,.145),color);o.rotation_euler[1]=r

def hero(kind):
 global FACE_KIND
 FACE_KIND=kind
 color={'summon':'blue','honey':'yellow','orbit':'purple','crit':'orange','bounce':'pink'}[kind];body(color)
 if kind=='summon':
  # One long upright, one visibly flopped ear preserving reference asymmetry.
  tapered_ear('Ear upright',[(-.25,0,1.23),(-.31,0,1.48),(-.32,0,1.73),(-.24,0,1.91),(-.19,0,1.94)],[(.12,.11),(.14,.12),(.13,.11),(.08,.08),(.015,.025)],color)
  o=ball('Ear upright pink inset',(-.29,.108,1.65),(.074,.025,.21),'pink');o.rotation_euler[1]=.10
  tapered_ear('Ear floppy',[(.25,0,1.24),(.31,0,1.48),(.43,.025,1.65),(.61,.04,1.66),(.72,.08,1.55),(.73,.10,1.48)],[(.13,.11),(.14,.12),(.15,.12),(.14,.12),(.10,.10),(.025,.04)],color)
  o=ball('Ear floppy pink inset',(.47,.13,1.57),(.22,.026,.062),'pink');o.rotation_euler[1]=-.20
  ball('Cotton tail',(0,-.30,.37),(.165,.15,.16),'cream')
  for x in [-.1,.1]:
   o=ball('Neckerchief bow',(x,.29,.64),(.14,.06,.075),'blue_dark');o.rotation_euler[1]=x*3
  ball('Bow knot',(0,.36,.64),(.05,.04,.05),'blue')
  for side in [-1,1]:
   for j in range(2):
    o=ball('Head blue ribbon leaf',(side*(.43+j*.065),.10,.90+j*.07),(.095,.035,.045),'blue_dark');o.rotation_euler[1]=side*(-.65+j*.6)
  star('Face forehead sugar star',(0,.303,1.285),.055,.025,'cream')
 elif kind=='honey':
  for x in [-.35,.35]:ball('Ear bear',(x,0,1.30),(.17,.13,.17),color);ball('Ear pink inset',(x,.112,1.31),(.092,.026,.095),'pink')
  cyl('Honey backpack',(0,-.36,.56),.255,.47,'brown',verts=24)
  for z in [.37,.76]:torus('Honey barrel hoop',(0,-.36,z),.255,.026,'honey')
  ball('Honey lid',(0,-.36,.80),(.26,.26,.055),'honey')
  for i in range(6):
   a=i*math.tau/6;ball('Honey drip',(.23*math.cos(a),-.36+.23*math.sin(a),.75),(.05,.05,.11 if i%2 else .075),'honey',16,8)
  for x in [-.27,.27]:box('Backpack strap',(x,.18,.51),(.045,.05,.39),'brown',.018)
  cyl('Honey wand',(.48,.12,.64),.032,.44,'brown')
  for z in [.85,.89,.93]:cyl('Honey dipper ring',(.48,.12,z),.095,.025,'honey',verts=24)
  ball('Honey dipper cap',(.48,.12,.96),(.075,.075,.035),'honey')
  torus('Honey wrist cuff',(.39,.1,.37),.12,.025,'pink')
 elif kind=='bounce':
  for x in [-.33,.33]:ball('Ear hamster',(x,0,1.30),(.15,.12,.15),color);ball('Ear cream inset',(x,.105,1.31),(.08,.025,.083),'cream')
  for x in [-.49,.49]:
   ball('Boxing glove',(x,.15,.53),(.25,.235,.27),'pink');ball('Glove cream patch',(x,.367,.55),(.15,.025,.16),'cream');star('Glove star',(x,.401,.55),.09,.025,'cream_warm')
   ball('Glove thumb',(x*.75,.28,.43),(.09,.09,.12),'pink')
  for x in [-.09,.09]:ball('Neck pink bow',(x,.28,.63),(.12,.05,.06),'pink')
 elif kind=='crit':
  ball('Face fox snout',(0,.385,.815),(.17,.10,.095),'cream')
  for o in parts:
   if o.name.startswith(('Face nose','Face happy mouth','Face pink tongue')):o.location.y+=.055
  for side in [-1,1]:
   x=side*.29;tapered_ear('Ear pointed fox',[(x,0,1.22),(x+side*.035,0,1.42),(x+side*.055,0,1.62),(x+side*.045,0,1.70)],[(.18,.13),(.14,.10),(.07,.055),(.012,.014)],'orange')
   o=ball('Ear ivory fox inset',(x+side*.035,.112,1.45),(.075,.025,.18),'cream');o.rotation_euler[1]=side*.13
  tapered_ear('Fox tail',[(.17,-.27,.26),(.35,-.37,.40),(.55,-.39,.62),(.60,-.38,.84)],[(.12,.11),(.20,.16),(.23,.18),(.17,.13)],'orange')
  tapered_ear('Tail cream tip',[(.55,-.39,.66),(.60,-.38,.84),(.57,-.37,.99),(.49,-.36,1.06)],[(.22,.17),(.17,.13),(.09,.08),(.012,.014)],'cream')
  cyl('Candy launcher',(.40,.34,.46),.125,.46,'brown','Y',24)
  for y in [.17,.35,.57]:torus('Launcher brass ring',(.40,y,.46),.128,.025,'honey','Y')
  cyl('Launcher opening',(.40,.585,.46),.087,.012,'ink','Y',24)
  ball('Launcher candy sight',(.4,.32,.62),(.05,.06,.045),'pink',16,8)
 elif kind=='orbit':
  for side in [-1,1]:
   x=side*.27;o=ball('Ear moth',(x,-.02,1.34),(.15,.12,.27),'purple');o.rotation_euler[1]=side*.25
   ball('Ear cream inset',(x,.095,1.36),(.075,.025,.16),'cream')
   for z,s,xw in [(.86,.34,.52),(.44,.27,.46)]:
    o=ball('Butterfly wing',(side*xw,-.20,z),(.40,.07,s),'purple');o.rotation_euler[1]=side*(.55 if z>.6 else -.55)
    o=ball('Wing ivory inset',(side*(xw+.025),-.126,z),(.27,.022,s*.72),'wing_membrane');o.rotation_euler[1]=side*(.55 if z>.6 else -.55)
    for j in range(3):ball('Wing lilac jewel',(side*(xw+.11+j*.06),-.096,z+(j-1)*.095),(.05,.025,.06),'pink',16,8)
   for dz in [-.16,0,.16]:stroke('Wing vein',[(side*.28,-.105,.67),(side*.49,-.095,.80+dz/2),(side*.72,-.10,.87+dz)],.012,'purple')
  star('Face moth star',(0,.305,1.27),.055,.025,'cream_warm')
