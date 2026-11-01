"""Read-only runtime GLB contract gate. Supports an explicit single-hero pilot, never implies batch completion."""
from pathlib import Path
import argparse, hashlib, io, json, struct
from PIL import Image

IDS=('orbit','crit','honey','summon','bounce')
def check(path):
    data=path.read_bytes()
    magic,version,length=struct.unpack_from('<III',data)
    assert magic==0x46546C67 and version==2 and length==len(data),'Invalid GLB header'
    chunks={};offset=12
    while offset<len(data):
        size,kind=struct.unpack_from('<II',data,offset);offset+=8
        assert offset+size<=len(data),'Truncated GLB chunk'
        assert kind not in chunks,'Duplicate GLB chunk'
        chunks[kind]=data[offset:offset+size];offset+=size
    doc=json.loads(chunks[0x4E4F534A]);binary=chunks.get(0x004E4942,b'')
    assert not doc.get('extensionsRequired'),'Required custom glTF extensions not supported'
    assert all(not b.get('uri') for b in doc.get('buffers',[])),'External buffer forbidden'
    meshes=doc.get('meshes',[]);assert meshes,'No meshes'
    mesh_counts=[];morphs=set()
    for mesh in meshes:
        names=mesh.get('extras',{}).get('targetNames',[]);morphs.update(names)
        assert len(names)==len(set(names)),'Duplicate targetNames'
        assert all(v==0 for v in mesh.get('weights',[])),'Nonzero default mesh morph weights'
        triangles=surfaces=0
        for primitive in mesh['primitives']:
            assert not any(k.startswith('COLOR_') for k in primitive['attributes']),'Texture-only food PBR contract forbids legacy vertex color multiplication'
            assert 'TEXCOORD_0' in primitive['attributes'] and not any(k.startswith('TEXCOORD_') and k!='TEXCOORD_0' for k in primitive['attributes']),'Single baked atlas requires only TEXCOORD_0'
            material=doc.get('materials',[])[primitive.get('material',0)] if doc.get('materials') else {}
            if 'normalTexture' in material:
                assert 'TANGENT' in primitive['attributes'],'Baked normal map requires exported tangents consistent with the bake'
                tangent=doc['accessors'][primitive['attributes']['TANGENT']]
                assert tangent.get('type')=='VEC4' and tangent.get('componentType')==5126 and tangent['count']==doc['accessors'][primitive['attributes']['POSITION']]['count'],'Invalid exported tangent accessor'
            assert primitive.get('mode',4)==4,'Only triangle surfaces supported'
            count=doc['accessors'][primitive['indices']]['count'] if 'indices' in primitive else doc['accessors'][primitive['attributes']['POSITION']]['count']
            assert count%3==0,'Triangle index count not divisible by three'
            triangles+=count//3;surfaces+=1
            assert len(primitive.get('targets',[]))==len(names),'Morph names/targets mismatch'
            assert not any(k.startswith('JOINTS_') and k!='JOINTS_0' for k in primitive['attributes']),'More than four bone influences'
        mesh_counts.append((triangles,surfaces))
    nodes=doc.get('nodes',[]);roots=set(doc['scenes'][doc.get('scene',0)].get('nodes',[]));active=set();stack=list(roots)
    while stack:
        index=stack.pop();assert index not in active,'Repeated/cyclic scene node'
        assert 0<=index<len(nodes),'Invalid scene node'
        active.add(index);stack.extend(nodes[index].get('children',[]))
    for index in active:
        assert all(v==0 for v in nodes[index].get('weights',[])),'Nonzero default node morph weights'
    mesh_instances=[nodes[i]['mesh'] for i in active if 'mesh' in nodes[i]]
    assert mesh_instances,'No active scene mesh instances'
    triangles=sum(mesh_counts[i][0] for i in mesh_instances);surfaces=sum(mesh_counts[i][1] for i in mesh_instances)
    unreachable_meshes=sorted(set(range(len(meshes)))-set(mesh_instances))
    assert triangles<=30000,('Scene-instance triangle budget',triangles)
    assert surfaces<=4,('Total surface budget incl props',surfaces)
    assert {'Blink','Smile','Surprise'}<=morphs,('Required facial channels missing',sorted(morphs))
    skin_indices={nodes[i]['skin'] for i in active if 'skin' in nodes[i]}
    joints=set(j for i in skin_indices for j in doc['skins'][i]['joints'])
    assert 1<=len(joints)<=32,('Skeleton joint budget',len(joints))
    materials=doc.get('materials',[]);assert len(materials)<=4
    for material in materials:
        assert material.get('alphaMode','OPAQUE')=='OPAQUE','Opaque contract violated'
        assert 'baseColorTexture' in material.get('pbrMetallicRoughness',{}),'Runtime hero requires baked base-color atlas'
        texture_infos=[material.get(k) for k in ('normalTexture','occlusionTexture','emissiveTexture')] + [material.get('pbrMetallicRoughness',{}).get(k) for k in ('baseColorTexture','metallicRoughnessTexture')]
        for info in texture_infos:
            if info is not None:
                assert info.get('texCoord',0)==0 and info.get('extensions',{}).get('KHR_texture_transform',{}).get('texCoord',0)==0,'All baked material textures must use UV0'
                textures=doc.get('textures',[]);texture_index=info['index']
                assert 0<=texture_index<len(textures),'Invalid material texture index'
                texture=textures[texture_index];samplers=doc.get('samplers',[])
                assert 'sampler' in texture and 0<=texture['sampler']<len(samplers),'Non-tiling atlas requires explicit sampler'
                sampler=samplers[texture['sampler']]
                assert sampler.get('wrapS',10497)==33071 and sampler.get('wrapT',10497)==33071,'Non-tiling food atlas must clamp both edges'
                assert 0<=texture.get('source',-1)<len(doc.get('images',[])),'Invalid embedded texture source' 
    images=[]
    for entry in doc.get('images',[]):
        assert 'uri' not in entry and 'bufferView' in entry,'Embed textures in GLB'
        view=doc['bufferViews'][entry['bufferView']];start=view.get('byteOffset',0);raw=binary[start:start+view['byteLength']]
        with Image.open(io.BytesIO(raw)) as im:
            im.load();assert max(im.size)<=1024,('Texture budget',im.size)
            images.append({'size':list(im.size),'decoded_rgba_bytes':im.width*im.height*4,'sha256':hashlib.sha256(raw).hexdigest()})
    assert 1<=len(images)<=3,('One atlas set basecolor/normal/ORM',len(images))
    clips={}
    for animation in doc.get('animations',[]):
        name=animation.get('name','').split('/')[-1]
        assert name!='Dodge','Dodge forbidden'
        if name not in ('Idle','Run','Attack'):continue
        assert name not in clips,('Ambiguous clip',name)
        channels=animation['channels'];targets=set()
        for channel in channels:
            target=channel['target'];assert target['path']!='weights','Body clip must not write facial channels'
            assert target['node'] not in roots,'Model root motion channel forbidden'
            targets.add(target['node'])
        assert len(targets)>=2,('Root-only or insufficient articulation',name)
        clips[name]={'target_nodes':len(targets),'channels':len(channels)}
    assert set(clips)=={'Idle','Run','Attack'},('Missing body clip',clips)
    return {'path':str(path),'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),'triangles':triangles,'surfaces':surfaces,'mesh_instances':len(mesh_instances),'unreachable_meshes':unreachable_meshes,'materials':len(materials),'bones':len(joints),'morphs':sorted(morphs),'textures':images,'clips':clips}

def main():
    p=argparse.ArgumentParser();p.add_argument('directory',type=Path);p.add_argument('--hero',choices=IDS,help='Explicit pilot only; omission requires all five');p.add_argument('--output',type=Path);a=p.parse_args()
    heroes=(a.hero,) if a.hero else IDS
    result={'scope':'single pilot only' if a.hero else 'all five heroes','assets':[check(a.directory/f'hero_{hero}.glb') for hero in heroes],'limits':'Static glTF contract only; actual imported axis/pose/morph fidelity and inner skeleton root drift, native gameplay, draw calls, GPU memory and mobile performance are separate gates.'}
    if a.output:a.output.write_text(json.dumps(result,indent=2))
    print('FOOD_HERO_GLTF_CONTRACT_PASS',json.dumps(result))
if __name__=='__main__':main()
