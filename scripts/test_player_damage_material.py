"""Isolated strict preparation and separately labelled behavioral execution; never masks import red."""
import argparse,os,shutil,subprocess,json,hashlib
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('phase',choices=['prepare','behavior']);ap.add_argument('--work',required=True);args=ap.parse_args()
root=Path(__file__).resolve().parents[1];work=Path(args.work).resolve();project=work/'project';evidence=work/'evidence';evidence.mkdir(parents=True,exist_ok=True)
engine=os.environ['GODOT_NET'];env=os.environ.copy();env['XDG_CONFIG_HOME']=str(work/'editor-config');env['XDG_DATA_HOME']=str(work/'build-data')
for n in ['ENEMY_DAMAGE_C','READY_STRAFE_PILOT','READY_MELEE_PRIORITY','DAMAGE_KEYFRAMES','DAMAGE_SHORT360','DAMAGE_FIRST_HEADLESS','READY_OUTPUT']:env.pop(n,None)
def run(name,cmd,e=env,marker=None):
 p=subprocess.run(cmd,cwd=project,env=e,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=300);(evidence/(name+'.log')).write_text(p.stdout)
 good=p.returncode==0 and not any(s in p.stdout for s in ['ERROR:','FATAL','Unhandled exception','ObjectDB instances leaked','resources still in use']) and (marker is None or marker in p.stdout)
 print(name,'exit',p.returncode,'clean',good,flush=True);return good
if args.phase=='prepare':
 if project.exists():raise SystemExit('Refusing overwrite of previous project/evidence')
 shutil.copytree(root,project,ignore=shutil.ignore_patterns('.git','.godot','bin','obj','builds','exports','android','__pycache__'))
 for name in ['PlayerDamageMaterialProbe','PlayerDamageFirstDrawProbe','PlayerDamageHostProbe']:
  (project/('src/Adapters/Godot/'+name+'.cs')).write_text((root/('tests/Presentation/'+name+'.cs.template')).read_text())
  (project/('scenes/'+name+'.tscn')).write_text('[gd_scene load_steps=2 format=3]\n[ext_resource type="Script" path="res://src/Adapters/Godot/'+name+'.cs" id="1"]\n[node name="'+name+'" type="Node3D"]\nscript = ExtResource("1")\n')
 (evidence/'authored-source-sha256.json').write_text(json.dumps({str(p.relative_to(project)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((project/'src').rglob('*.cs')) if 'bin' not in p.parts and 'obj' not in p.parts},indent=2)+'\n')
 if not run('build',['dotnet','build','HmiGame.csproj','-m:1','-v','minimal']):raise SystemExit(1)
 ok=run('strict-import',[engine,'--headless','--editor','--path',str(project),'--import']);(evidence/'strict-preparation.json').write_text(json.dumps({'strictImportPassed':ok})+'\n');raise SystemExit(0 if ok else 1)
expected=json.loads((evidence/'authored-source-sha256.json').read_text())
for p,h in expected.items():
 if hashlib.sha256((project/p).read_bytes()).hexdigest()!=h:raise SystemExit('source changed after compile: '+p)
results={}
for name,marker in [('PlayerDamageMaterialProbe','PLAYER_DAMAGE_MATERIAL_PASS'),('PlayerDamageFirstDrawProbe','NATURAL_PLAYER_EXPIRE_PASS'),('PlayerDamageHostProbe','PLAYER_DAMAGE_HOST_PASS')]:
 e=env.copy();e['XDG_DATA_HOME']=str(work/('data-'+name));e['READY_OUTPUT']=str(evidence/name);Path(e['READY_OUTPUT']).mkdir(exist_ok=True);e['PLAYER_FIRST_HEADLESS']='1'
 results[name]=run(name,[engine,'--headless','--path',str(project),'res://scenes/'+name+'.tscn'],e,marker)
(evidence/'separate-behavior-results.json').write_text(json.dumps({'note':'Independent compiled behavior, does not clear strict import failure','results':results},indent=2)+'\n')
raise SystemExit(0 if all(results.values()) else 1)
