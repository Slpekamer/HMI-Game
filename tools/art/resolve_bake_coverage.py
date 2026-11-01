"""Resolve Blender baked sRGB edge coverage for an opaque PBR base-color atlas.
A controlled asset-specific fix: partial-alpha bake texels had RGB already
coverage-multiplied; opaque rendering ignored alpha and showed dark seams.
No shader transparency is introduced. Inspect source pixels before use.
"""
import argparse,io,json,hashlib
from pathlib import Path
import numpy as np
from PIL import Image
from runtime_postprocess import GLB
p=argparse.ArgumentParser();p.add_argument('input');p.add_argument('output');p.add_argument('--report',required=True);a=p.parse_args();g=GLB(a.input);ids=set();report=[]
for m in g.d['materials']:
 assert m.get('alphaMode','OPAQUE')=='OPAQUE'
 t=m.get('pbrMetallicRoughness',{}).get('baseColorTexture')
 if t:ids.add(g.d['textures'][t['index']]['source'])
for i in sorted(ids):
 v=g.d['bufferViews'][g.d['images'][i]['bufferView']];start=v.get('byteOffset',0);im=np.array(Image.open(io.BytesIO(g.b[start:start+v['byteLength']])).convert('RGBA'));before=im.copy();alpha=im[:,:,3];assert alpha.min()>0,'Zero-coverage pixels need explicit island padding, not division'
 mask=alpha<255;values=im[:,:,:3].astype(np.float64);values[mask]*=255./alpha[mask,None];im[:,:,:3]=np.clip(np.rint(values),0,255).astype(np.uint8);im[:,:,3]=255;assert np.array_equal(im[~mask,:3],before[~mask,:3]);b=io.BytesIO();Image.fromarray(im).save(b,format='PNG',compress_level=9);g.d['images'][i]['bufferView']=g.view(b.getvalue());report.append({'image':i,'partial_coverage_texels':int(mask.sum()),'minimum_alpha_before':int(alpha.min()),'maximum_rgb_change':int(np.abs(im[:,:,:3].astype(int)-before[:,:,:3]).max()),'all_full_coverage_rgb_unchanged':True})
sha=g.save(a.output);Path(a.report).write_text(json.dumps({'input_sha256':hashlib.sha256(g.raw).hexdigest(),'output_sha256':sha,'images':report,'method':'For alpha<255 only, RGB=round(RGB*255/alpha) in stored sRGB bake bytes; mark alpha255 for opaque atlas. Actual pixel A/B required; no geometry/UV/animation changes.'},indent=2));print(sha)
