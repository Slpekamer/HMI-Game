"""Only run after native run-native.sh completes all four recordings."""
import subprocess, pathlib, json, hashlib, sys
r=pathlib.Path(__file__).resolve().parent
font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
def run(args):subprocess.run(args,check=True)
def ff(*args):run(['ffmpeg','-hide_banner','-loglevel','warning','-threads','2',*map(str,args)])
proof={}
for clip in ['Backpedal-weight']:
 stamps=[]
 for mode in ['A','B']:
  folder=r/f'native-{mode}';avi=folder/'capture.avi';out=folder/'capture.mp4'
  assert (folder/'summary.json').exists(),f'Incomplete {folder}'
  log=(folder/'runtime.log').read_text();assert 'CONTROLLED_GAIT_MOVIE_DONE' in log and 'ERROR:' not in log
  probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-count_frames','-show_entries','stream=width,height,r_frame_rate,nb_read_frames,duration','-of','json',str(avi)]))
  raw=subprocess.check_output(['ffmpeg','-v','error','-threads','2','-i',str(avi),'-vf','crop=112:12:8:696,format=gray','-f','rawvideo','-'])
  size=112*12;assert len(raw)%size==0
  bits=[sum((1<<i) if raw[start+6*112+6+i*14]>128 else 0 for i in range(8)) for start in range(0,len(raw),size)]
  stamps.append(bits)
  ff('-i',avi,'-an','-c:v','libx264','-threads','2','-preset','veryfast','-crf','18','-pix_fmt','yuv420p','-y',out)
  ff('-i',out,'-f','null','-')
  proof[f'{clip}-{mode}']={'probe':probe,'frame_stamps':bits,'sha256':hashlib.sha256(out.read_bytes()).hexdigest()}
 assert stamps[0]==stamps[1],f'{clip} frame stamp mismatch'
 # Preserve all encoded frames and scene resolution: no retiming, zoom, hidden crop or slowed speed.
 left=r/'native-A/capture.mp4';right=r/'native-B/capture.mp4'
 title=f'CONTROLLED {clip} / fixed heading / original camera scale and 4.8m/s'
 f=(f"[0:v]pad=iw:ih+48:0:48:color=0x201b2c,drawtext=fontfile={font}:text='A - frozen foot-only Backpedal':x=20:y=10:fontsize=25:fontcolor=white[a];"
    f"[1:v]pad=iw:ih+48:0:48:color=0x201b2c,drawtext=fontfile={font}:text='B - one Chest / Head weight-transfer curve':x=20:y=10:fontsize=25:fontcolor=white[b];"
    f"[a][b]hstack=inputs=2,pad=iw:ih+44:0:0:color=0x201b2c,drawtext=fontfile={font}:text='{title}':x=20:y=h-34:fontsize=24:fontcolor=white[v]")
 ff('-i',left,'-threads','2','-i',right,'-filter_complex_threads','1','-filter_complex',f,'-map','[v]','-an','-c:v','libx264','-threads','2','-preset','veryfast','-crf','18','-pix_fmt','yuv420p','-y',r/f'controlled-{clip}-AB-full.mp4')
 # Sequential version is 1280px wide, keeping one full-size game frame visible on narrower displays.
 f=(f"[0:v]pad=iw:ih+48:0:48:color=0x201b2c,drawtext=fontfile={font}:text='A - foot-only Backpedal / CONTROLLED':x=20:y=10:fontsize=25:fontcolor=white[a];"
    f"[1:v]pad=iw:ih+48:0:48:color=0x201b2c,drawtext=fontfile={font}:text='B - weight transfer / same camera speed scale':x=20:y=10:fontsize=25:fontcolor=white[b];[a][b]concat=n=2:v=1:a=0[v]")
 ff('-i',left,'-threads','2','-i',right,'-filter_complex_threads','1','-filter_complex',f,'-map','[v]','-an','-c:v','libx264','-threads','2','-preset','veryfast','-crf','18','-pix_fmt','yuv420p','-y',r/f'controlled-{clip}-AB-sequential.mp4')
proof_path=r/'video-proof.json'
previous=json.loads(proof_path.read_text()) if proof_path.exists() else {}
previous.update(proof);proof_path.write_text(json.dumps(previous,indent=2))
print('POSTPROCESS_COMPLETE')
