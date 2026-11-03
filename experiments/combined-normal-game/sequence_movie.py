#!/usr/bin/env python3
"""Normal-path candidate: every unique 60 Hz frame, original pixels and speed."""
import argparse,json,pathlib,subprocess,tempfile
from PIL import Image,ImageDraw,ImageFont
from decode_movie import frames
p=argparse.ArgumentParser();p.add_argument('base',type=pathlib.Path);p.add_argument('output',type=pathlib.Path);p.add_argument('--font',required=True);a=p.parse_args()
n=json.loads((a.base/'B/summary.json').read_text())['frame'];assert 0<n<=1024
font=ImageFont.truetype(a.font,20);seen={};rows=[];previous=-1
with tempfile.TemporaryDirectory()as d:
 d=pathlib.Path(d)
 for index,tick,im,samples in frames(a.base/'B/capture.avi'):
  assert 0<=tick<n and tick>=previous,('stamp order',index,tick);duplicate=tick in seen;rows.append({'encoded_frame':index,'render_frame':tick,'duplicate':duplicate})
  if not duplicate:
   seen[tick]=index;out=Image.new('RGB',(1280,752),'#181818');ImageDraw.Draw(out).text((12,3),'正常游戏路径组合候选 | 脚本输入 · 原尺度 · 正常速度',font=font,fill='white');out.paste(im,(0,32));out.save(d/f'{tick:04}.png',compress_level=1)
  previous=tick
 assert sorted(seen)==list(range(n)),('missing frames',n,len(seen))
 subprocess.run(['ffmpeg','-v','error','-framerate','60','-i',str(d/'%04d.png'),'-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(a.output)],check=True)
(a.base/'frame-map.json').write_text(json.dumps({'fps':60,'frames':n,'rows':rows,'source':'B/capture.avi','mapping':[seen[i]for i in range(n)],'scope':'Normal GameHost, scripted input, controlled gait disabled. No crop, retiming or interpolation.'},indent=2)+'\n')
