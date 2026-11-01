"""Run actual Godot codex regression in an isolated copied project.
Test fixture is compiled ONLY into the copy; no production test hooks or cheats.
"""
from pathlib import Path
import os,shutil,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
work=Path(tempfile.mkdtemp(prefix='hmi-codex-'))
project=work/'project'
shutil.copytree(root,project,ignore=shutil.ignore_patterns('.godot','.git','bin','obj','builds','exports','android','__pycache__','*.import','*.uid'))
probe=project/'src/Adapters/Godot/CodexProbe.cs'
probe.write_text((root/'tests/Presentation/CodexProbe.cs.template').read_text())
(project/'scenes/main.tscn').write_text('[gd_scene load_steps=2 format=3]\n[ext_resource type="Script" path="res://src/Adapters/Godot/CodexProbe.cs" id="1"]\n[node name="CodexProbe" type="Node"]\nscript = ExtResource("1")\n')
engine=os.environ['GODOT_NET']
env=os.environ.copy();env['XDG_CONFIG_HOME']=str(work/'editor-config')
Path(env['XDG_CONFIG_HOME']).mkdir()
env['XDG_DATA_HOME']=str(work/'user-data');Path(env['XDG_DATA_HOME']).mkdir()
for name,args in [('build',['dotnet','build','HmiGame.csproj','-m:1','-v','minimal']),('import',[engine,'--headless','--editor','--path',str(project),'--import']),('run',[engine,'--headless','--path',str(project)])]:
 p=subprocess.run(args,cwd=project,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=180)
 (work/(name+'.log')).write_text(p.stdout)
 print(f"Codex {name}: exit {p.returncode}; log {work/(name+chr(46)+"log")}")
 if name=="run" or p.returncode: print(p.stdout)
 if p.returncode or 'ERROR:' in p.stdout or 'Exception' in p.stdout:raise SystemExit(f'FAIL codex {name}; evidence {work}')
 if name=='run' and 'CODEX_ENGINE_PROBE PASS' not in p.stdout:raise SystemExit('Missing codex pass marker')
print('PASS isolated actual-engine codex; evidence',work)
