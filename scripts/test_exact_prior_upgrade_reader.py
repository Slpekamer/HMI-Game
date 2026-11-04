"""Build the real prior reader from an explicit immutable Git commit, then test upgrade boundaries.
Only synthetic temporary saves are used. No source constant or serialized format is rewritten.
"""
from pathlib import Path
import argparse, hashlib, json, os, re, subprocess, tempfile
p=argparse.ArgumentParser()
p.add_argument('--baseline',type=Path,required=True)
p.add_argument('--expected-sha',required=True)
a=p.parse_args()
if not re.fullmatch('[0-9a-f]{40}',a.expected_sha):p.error('A full lowercase Git commit SHA is required')
base=a.baseline.resolve()
def git(*args):return subprocess.check_output(['git','-C',str(base),*args])
if git('rev-parse',a.expected_sha+'^{commit}').decode().strip()!=a.expected_sha:p.error('Commit mismatch')
root=Path(__file__).resolve().parents[1]
work=Path(tempfile.mkdtemp(prefix='hmi-exact-prior-upgrade-'))
old=work/'old';old.mkdir()
paths=git('ls-tree','-r','--name-only',a.expected_sha,'src/Core','src/Adapters/Persistence','content/gameplay.json').decode().splitlines()
manifest={}
for name in paths:
    if not (name.startswith(('src/Core/','src/Adapters/Persistence/')) or name=='content/gameplay.json'):raise RuntimeError('Unexpected source path')
    if '..' in Path(name).parts:raise RuntimeError('Unsafe source path')
    data=git('show',a.expected_sha+':'+name)
    target=old/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
    manifest[name]=hashlib.sha256(data).hexdigest()
prior=(old/'src/Core/WaveRules.cs').read_text()
if not re.search(r'public const string Version\s*=\s*"hmi-core-wave60-preview1-2"',prior):raise RuntimeError('Not the real prior rule2 source')
if (old/'content/gameplay.json').read_bytes()!=(root/'content/gameplay.json').read_bytes():raise RuntimeError('Catalog differs; cannot claim this fixture contract matches the old reader')
reader=old/'tests/ExactPriorReader';reader.mkdir(parents=True)
(reader/'Program.cs').write_bytes((root/'tests/RuleUpgradeTests/ExactPriorReader.cs.template').read_bytes())
(reader/'ExactPriorReader.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><ProjectReference Include="../../src/Adapters/Persistence/Hmi.Persistence.csproj"/><None Include="../../content/gameplay.json" Link="gameplay.json" CopyToOutputDirectory="PreserveNewest"/></ItemGroup></Project>')
env=os.environ.copy();env.update(DOTNET_PROCESSOR_COUNT='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',MSBUILDDISABLENODEREUSE='1')
def run(label,args):
    result=subprocess.run(args,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=240)
    (work/(label+'.log')).write_text(result.stdout)
    print(label,'exit',result.returncode,'evidence',work,flush=True)
    if result.returncode:print(result.stdout);raise SystemExit(result.returncode)
    return result.stdout
run('build-reader',['dotnet','build',str(reader/'ExactPriorReader.csproj'),'-m:1','-p:UseSharedCompilation=false'])
dll=reader/'bin/Debug/net10.0/ExactPriorReader.dll'
run('build-current',['dotnet','build',str(root/'tests/RuleUpgradeTests/RuleUpgradeTests.csproj'),'-m:1','-p:UseSharedCompilation=false'])
current=root/'tests/RuleUpgradeTests/bin/Debug/net10.0/RuleUpgradeTests.dll'
evidence={'source_commit':a.expected_sha,'source_sha256':manifest,'reader_template_sha256':hashlib.sha256((reader/'Program.cs').read_bytes()).hexdigest(),'reader_dll_sha256':hashlib.sha256(dll.read_bytes()).hexdigest()}
(work/'provenance.json').write_text(json.dumps(evidence,indent=2))
output=run('upgrade-with-reader',['dotnet',str(current),'--old-reader',str(dll)])
for stage in range(7):
    if f'OLD_READER_STAGE {stage} EXACT_PRIOR_READER PASS' not in output:raise RuntimeError('Missing positive control/stage '+str(stage))
if 'OLD_READER NOT_RUN' in output or 'PASS RuleUpgrade' not in output:raise RuntimeError('Incomplete reader run')
print(output)
print('PASS exact prior source reader; synthetic upgrade protocol coverage only; evidence',work)
