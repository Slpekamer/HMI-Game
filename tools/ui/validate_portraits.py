"""Read-only approved-food concept atlas gate; no image pixels are edited."""
from pathlib import Path
from PIL import Image
import hashlib,json,sys
root=Path(__file__).resolve().parents[2];folder=root/'content/ui/portraits'
manifest=json.loads((folder/'manifest.json').read_text());assert manifest['format']=='approved-turnaround-atlas-v1'
records=[];expected={'hero_'+x for x in ('orbit','crit','honey','summon','bounce')};assert {r['id'] for r in manifest['portraits']}==expected
for row in manifest['portraits']:
    path=folder/row['file'];assert path.name==row['id']+'.png'
    assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'],('Approved source changed',row['id'])
    with Image.open(path) as source:
        source.load();width,height=source.size;assert [width,height]==row['image_size']
        x,y,w,h=row['region_xywh'];assert x>=0 and y>=0 and w>0 and h>0 and x+w<=width and y+h<=height
        assert (x,y,h)==(0,0,700) and w==(750 if row['id']=='hero_bounce' else 710)
        assert source.crop((x,y,x+w,y+h)).getbbox(),'Empty portrait region'
        text=(folder/row['texture']).read_text();assert 'AtlasTexture' in text and row['file'] in text
        assert 'region = Rect2('+', '.join(map(str,row['region_xywh']))+')' in text
        records.append({'id':row['id'],'sha256':row['sha256'],'size':[width,height],'region':row['region_xywh'],'decoded_rgba_bytes':width*height*4})
result={'passed':True,'portraits':records,'total_full_atlas_decoded_rgba_bytes':sum(x['decoded_rgba_bytes'] for x in records),'limits':'Non-destructive cream-background concept atlas, not transparent cutout. Atlas region does not reduce full texture memory; actual native framing/identity and GPU/device memory are separate gates.'}
if len(sys.argv)>1:Path(sys.argv[1]).write_text(json.dumps(result,indent=2))
print('PORTRAIT_ASSETS_PASS',json.dumps(result))
