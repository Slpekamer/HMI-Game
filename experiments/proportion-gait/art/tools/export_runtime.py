"""Re-export the packed review source with Blender4.3.2. No production installation.
blender -b -t 2 --python tools/export_runtime.py -- --source source/juanjuan-proportion-motion-r1.blend --output build/candidate.glb
"""
import argparse,sys
from pathlib import Path
import bpy
p=argparse.ArgumentParser();p.add_argument('--source',required=True);p.add_argument('--output',required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);src=Path(a.source).resolve();out=Path(a.output).resolve();assert src!=out;out.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(src));r=bpy.data.objects['FoodRig'];r.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1);bpy.context.view_layer.update();bpy.ops.object.select_all(action='DESELECT')
for n in ['JuanjuanMesh','FoodRig','Socket_Hand_L','Socket_Hand_R','Socket_FX']:bpy.data.objects[n].select_set(True)
bpy.context.view_layer.objects.active=r
bpy.ops.export_scene.gltf(filepath=str(out),export_format='GLB',use_selection=True,export_yup=True,export_animations=True,export_animation_mode='ACTIONS',export_skins=True,export_morph=True,export_morph_normal=True,export_morph_tangent=True,export_tangents=True,export_vertex_color='NONE',export_morph_animation=False,export_materials='EXPORT',export_extras=True,export_frame_range=False,export_force_sampling=True,export_optimize_animation_size=False)
