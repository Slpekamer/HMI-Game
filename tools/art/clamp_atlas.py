import argparse,json,hashlib
from pathlib import Path
from runtime_postprocess import GLB
p=argparse.ArgumentParser();p.add_argument('input');p.add_argument('output');p.add_argument('--report',required=True);a=p.parse_args();g=GLB(a.input);old=[]
for i,s in enumerate(g.d.get('samplers',[])):old.append({'index':i,'old':dict(s)});s['wrapS']=33071;s['wrapT']=33071
assert old
sha=g.save(a.output);Path(a.report).write_text(json.dumps({'input_sha256':hashlib.sha256(g.raw).hexdigest(),'output_sha256':sha,'changed_samplers':old,'new_wrap':'CLAMP_TO_EDGE 33071, both axes','reason':'Packed atlas islands reach UV edges; repeat wrapping samples opposite image edge under bilinear filtering. No image/UV/geometry/morph/animation edits.'},indent=2));print(sha)
