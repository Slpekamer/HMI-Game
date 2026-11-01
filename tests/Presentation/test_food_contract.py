"""Synthetic JSON/GLB contract-unit fixtures, not importable production asset or visual evidence."""
import copy,importlib.util,io,json,struct,tempfile,unittest
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('contract',root/'tools/art/validate_food_heroes.py');contract=importlib.util.module_from_spec(spec);spec.loader.exec_module(contract)
class ContractTests(unittest.TestCase):
 def fixture(self):
  image=Image.new('RGB',(1,1),(255,255,255));stream=io.BytesIO();image.save(stream,format='PNG');binary=stream.getvalue()
  doc={'asset':{'version':'2.0'},'scene':0,'scenes':[{'nodes':[0]}],'nodes':[{'children':[1,2]},{'mesh':0,'skin':0},{}],'skins':[{'joints':[1,2]}],'meshes':[{'extras':{'targetNames':['Blink','Smile','Surprise']},'weights':[0,0,0],'primitives':[{'attributes':{'POSITION':0,'JOINTS_0':1,'TEXCOORD_0':0},'targets':[{}, {}, {}]}]}],'accessors':[{'count':3},{'count':3}],'materials':[{'pbrMetallicRoughness':{'baseColorTexture':{'index':0}}}],'textures':[{'source':0,'sampler':0}],'samplers':[{'wrapS':33071,'wrapT':33071}],'images':[{'bufferView':0}],'bufferViews':[{'byteLength':len(binary)}],'buffers':[{'byteLength':len(binary)}],'animations':[{'name':name,'channels':[{'target':{'node':1,'path':'rotation'}},{'target':{'node':2,'path':'rotation'}}]}for name in ['Idle','Run','Attack']]}
  return doc,binary
 def run_fixture(self,mutate=None):
  doc,binary=self.fixture()
  if mutate:mutate(doc)
  raw=json.dumps(doc).encode();raw+=b' '*((-len(raw))%4);binary+=b'\0'*((-len(binary))%4)
  data=struct.pack('<III',0x46546C67,2,12+8+len(raw)+8+len(binary))+struct.pack('<II',len(raw),0x4E4F534A)+raw+struct.pack('<II',len(binary),0x004E4942)+binary
  with tempfile.TemporaryDirectory() as t:
   path=Path(t)/'fixture.glb';path.write_bytes(data);return contract.check(path)
 def test_valid_contract_shape(self):self.assertEqual(self.run_fixture()['triangles'],1)
 def test_mesh_weights(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['meshes'][0].update(weights=[1,0,0]))
 def test_node_weights(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['nodes'][1].update(weights=[0,.5,0]))
 def test_duplicate_morph_names(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['meshes'][0]['extras'].update(targetNames=['Blink','Blink','Surprise']))
 def test_instance_triangle_budget(self):
  def mutate(d):d['accessors'][0]['count']=60000;d['nodes'][2].update(mesh=0,skin=0)
  with self.assertRaises(AssertionError):self.run_fixture(mutate)
 def test_scene_cycle(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['nodes'][2].update(children=[0]))
 def test_body_morph_track(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['animations'][0]['channels'][0]['target'].update(path='weights'))
 def test_extra_source_uv(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['meshes'][0]['primitives'][0]['attributes'].update(TEXCOORD_1=0))
 def test_material_wrong_uv(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['materials'][0]['pbrMetallicRoughness']['baseColorTexture'].update(texCoord=1))
 def test_implicit_repeat_sampler(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['textures'][0].pop('sampler'))
 def test_repeated_atlas_axis(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['samplers'][0].update(wrapT=10497))
 def test_invalid_texture_index(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['materials'][0]['pbrMetallicRoughness']['baseColorTexture'].update(index=3))
 def test_invalid_texture_source(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['textures'][0].update(source=7))
 def test_normal_without_tangent(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['materials'][0].update(normalTexture={'index':0}))
 def test_normal_with_tangent(self):
  def mutate(d):
   d['materials'][0]['normalTexture']={'index':0}
   d['accessors'].append({'count':3,'type':'VEC4','componentType':5126})
   d['meshes'][0]['primitives'][0]['attributes']['TANGENT']=2
  self.assertEqual(self.run_fixture(mutate)['triangles'],1)
 def test_legacy_vertex_color(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['meshes'][0]['primitives'][0]['attributes'].update(COLOR_0=0))
 def test_root_track(self):
  with self.assertRaises(AssertionError):self.run_fixture(lambda d:d['animations'][0]['channels'][0]['target'].update(node=0))
if __name__=='__main__':unittest.main()
