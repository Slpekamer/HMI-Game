"""Same wall footprint/height contract, colored top and raised original candy detail."""
import os
os.environ['HMI_ART_OUTPUT']=os.environ.get('HMI_ART_OUTPUT',os.path.abspath(os.path.join(os.path.dirname(__file__),'../v02-props')))
src=open(os.path.join(os.path.dirname(__file__),'build_assets.py')).read();exec(compile(src[:src.rfind('records=[]')],'base','exec'))
# Preserve released wall bounds: 0.948837 x 0.762791 x 0.8 metres after explicit export normalization.
records=[]
M['blue_top']=mat('blue_top','9CD7F4',0,.48)
box('Soft candy block',(0,0,.35),(1,.8,.7),'blue',.10)
box('Blue frosting top',(0,0,.73),(1.02,.82,.08),'blue_top',.065)
ball('Candy stud',(.25,0,.81),(.12,.12,.05),'cream',16,8)
# flat raised five-point star on top, fully inside old dimensions
verts=[]
for z in [.77,.80]:
 for i in range(10):
  a=math.pi/2+i*math.pi/5;r=.17 if i%2==0 else .085;verts.append((-.13+math.cos(a)*r,math.sin(a)*r,z))
faces=[tuple(range(9,-1,-1)),tuple(range(10,20))]+[(i,(i+1)%10,(i+1)%10+10,i+10) for i in range(10)]
mesh=bpy.data.meshes.new('Sugar star');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Sugar star',mesh);bpy.context.collection.objects.link(o);finish(o,'Sugar star','yellow')
export('wall_block',.8)
json.dump({'version':'v02-prop-candidate','assets':records,'note':'same original wall dimensions, colored top for overhead camera'},open(f'{ROOT}/manifest.json','w'),indent=2)
