"""Run: blender -b SOURCE.blend --python export_runtime_glb.py -- --output raw.glb.
Read-only native source export; no .blend save. Requires baked skinned source,
not high-poly procedural authoring source. Actual engine QA remains mandatory.
"""
import argparse,sys
from pathlib import Path
import bpy
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--rig',default='FoodRig');a=p.parse_args();rig=bpy.data.objects[a.rig];assert rig.type=='ARMATURE'
meshes=[o for o in bpy.data.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)];assert meshes,'No baked skinned runtime mesh found'
for ob in meshes:
 for n in [x.name for x in ob.data.color_attributes]:ob.data.color_attributes.remove(ob.data.color_attributes[n])
 if ob.data.shape_keys:
  for k in ob.data.shape_keys.key_blocks:k.value=0
rig.animation_data.action=None
for pbone in rig.pose.bones:pbone.location=(0,0,0);pbone.rotation_quaternion=(1,0,0,0);pbone.scale=(1,1,1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for ob in meshes:ob.select_set(True)
for ob in bpy.data.objects:
 if ob.type=='EMPTY' and ob.name.startswith('Socket_') and ob.parent==rig:ob.select_set(True)
bpy.context.view_layer.objects.active=rig;bpy.context.view_layer.update();Path(a.output).parent.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.gltf(filepath=str(Path(a.output).resolve()),export_format='GLB',use_selection=True,export_yup=True,export_animations=True,export_animation_mode='ACTIONS',export_skins=True,export_morph=True,export_morph_normal=True,export_morph_tangent=True,export_tangents=True,export_morph_animation=False,export_materials='EXPORT',export_extras=True,export_vertex_color='NONE',export_all_vertex_colors=False,export_active_vertex_color_when_no_material=False)
