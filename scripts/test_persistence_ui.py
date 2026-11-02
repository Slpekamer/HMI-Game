"""Run actual Godot persistence-ui regression in an isolated copied project.
Test fixture is compiled ONLY into the copy; no production test hooks or cheats.
"""
from pathlib import Path
import os,shutil,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
work=Path(tempfile.mkdtemp(prefix='hmi-persistence-ui-'))
project=work/'project'
shutil.copytree(root,project,ignore=shutil.ignore_patterns('.godot','.git','bin','obj','builds','exports','android','__pycache__','*.import','*.uid'))
probe=project/'src/Adapters/Godot/PersistenceUiProbe.cs'
probe.write_text((root/'tests/Presentation/PersistenceUiProbe.cs.template').read_text())
(project/'scenes/main.tscn').write_text('[gd_scene load_steps=2 format=3]\n[ext_resource type="Script" path="res://src/Adapters/Godot/PersistenceUiProbe.cs" id="1"]\n[node name="PersistenceUiProbe" type="Node"]\nscript = ExtResource("1")\n')
# Compile the complete GameUi partial class, not the whole game assembly. This
# preserves the UI-only probe/exit path while preventing new UI partials from
# silently disappearing from this isolated project. Plain motion helper has no
# Godot script registration; no Core/Persistence/Host reference is introduced.
ui_dir=project/'src/Adapters/Godot'
compile_names=['GameUi']+sorted(p.stem for p in ui_dir.glob('GameUi.*.cs'))
compile_names+=['ResumeUi','CandyTheme','TouchJoystick','AssetVisuals','HeroSelectionPreview','PersistenceUiProbe']
if (ui_dir/'UiMotionController.cs').exists():compile_names.append('UiMotionController')
(work/'compile-files.txt').write_text('\n'.join(compile_names)+'\n')
(project/'HmiGame.csproj').write_text('<Project Sdk="Godot.NET.Sdk/4.6.3"><PropertyGroup><TargetFramework>net10.0</TargetFramework><EnableDynamicLoading>true</EnableDynamicLoading><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>'+''.join('<Compile Include="src/Adapters/Godot/'+name+'.cs" />' for name in compile_names)+'</ItemGroup></Project>')
engine=os.environ['GODOT_NET']
env=os.environ.copy();env['XDG_CONFIG_HOME']=str(work/'editor-config')
Path(env['XDG_CONFIG_HOME']).mkdir()
env['XDG_DATA_HOME']=str(work/'user-data');Path(env['XDG_DATA_HOME']).mkdir()
for name,args in [('build',['dotnet','build','HmiGame.csproj','-m:1','-v','minimal']),('import',[engine,'--headless','--editor','--path',str(project),'--import']),('run',[engine,'--headless','--path',str(project)])]:
 p=subprocess.run(args,cwd=project,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=180)
 (work/(name+'.log')).write_text(p.stdout)
 print(f"PersistenceUi {name}: exit {p.returncode}; log {work/(name+chr(46)+"log")}")
 if name=="run" or p.returncode: print(p.stdout)
 if p.returncode or 'ERROR:' in p.stdout or 'Exception' in p.stdout:raise SystemExit(f'FAIL persistence-ui {name}; evidence {work}')
 if name=='run' and 'PERSISTENCE_UI_PROBE PASS' not in p.stdout:raise SystemExit('Missing persistence-ui pass marker')
print('PASS isolated actual-engine persistence-ui; evidence',work)
