"""Strict actual-engine migration Host/UI probe in isolated source + per-case XDG.
Controlled rule2/rule1 fixtures, never actual user saves. Cold cases use separate Godot
processes, preserving only their temporary XDG files. No diagnostic whitelist.
"""
from pathlib import Path
import os, shutil, subprocess, tempfile, json, hashlib
root=Path(__file__).resolve().parents[1]
work=Path(tempfile.mkdtemp(prefix='hmi-rule-upgrade-host-'))
project=work/'project'
shutil.copytree(root,project,ignore=shutil.ignore_patterns('.godot','.git','bin','obj','builds','exports','android','__pycache__','*.import','*.uid'))
(project/'src/Adapters/Godot/RuleUpgradeHostProbe.cs').write_text((root/'tests/Presentation/RuleUpgradeHostProbe.cs.template').read_text())
(project/'scenes/main.tscn').write_text('[gd_scene load_steps=2 format=3]\n[ext_resource type="Script" path="res://src/Adapters/Godot/RuleUpgradeHostProbe.cs" id="1"]\n[node name="RuleMigrationProbe" type="Node"]\nscript = ExtResource("1")\n')
engine=os.environ['GODOT_NET']; env=os.environ.copy()
env['XDG_CONFIG_HOME']=str(work/'editor-config');env['XDG_DATA_HOME']=str(work/'build-data')
for n in ('XDG_CONFIG_HOME','XDG_DATA_HOME'):Path(env[n]).mkdir(parents=True)
(work/'source-sha256.json').write_text(json.dumps({str(p.relative_to(project)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((project/'src').rglob('*.cs'))},indent=2))
def run(name,args,environment=env):
 p=subprocess.run(args,cwd=project,env=environment,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=240)
 (work/(name+'.log')).write_text(p.stdout)
 print(f'{name} exit={p.returncode} evidence={work}',flush=True)
 if p.returncode or any(s in p.stdout for s in ('ERROR:','FATAL','Unhandled exception','ObjectDB instances leaked','resources still in use')):
  print(p.stdout);raise SystemExit(f'FAIL {name}; retained {work}')
 return p.stdout
run('build',['dotnet','build','HmiGame.csproj','-m:1','-v','minimal'])
run('import',[engine,'--headless','--editor','--path',str(project),'--import'])
for group,case in [('prior', 'prior'), ('pad','pad'), ('preserve', 'preserve'), ('legacy', 'legacy'), ('closed', 'closed'), ('pendingstart', 'pendingstart'), ('fresh', 'fresh'), ('blocked', 'blocked'), ('stale', 'stale'), ('lostack', 'lostack'), ('source-close','source-close'), ('source-temp','source-temp'), ('target-close','target-close'), ('target-advance','target-advance'), ('selected-active','selected-active'), ('selected-closed','selected-closed'), ('selected-pending','selected-pending'), ('cold-up', 'pending-up-setup'), ('cold-up', 'pending-up-resume'), ('cold-new', 'pending-new-setup'), ('cold-new', 'pending-new-resume')]:
 e=env.copy();e['XDG_DATA_HOME']=str(work/('data-'+group));Path(e['XDG_DATA_HOME']).mkdir(exist_ok=True)
 out=run(group+'-'+case,[engine,'--headless','--path',str(project),'--','--rule-host-case='+case],e)
 if 'RULE_UPGRADE_HOST PASS ' not in out:raise SystemExit('Missing host pass marker')
print('PASS strict controlled rule migration Host/UI and separate-process restart; evidence',work)
