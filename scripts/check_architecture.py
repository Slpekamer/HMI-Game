from pathlib import Path
root=Path(__file__).resolve().parents[1]
for p in (root/'src/Core').glob('*.cs'):
 text=p.read_text()
 for forbidden in ('using Godot','using UnityEngine','using System.IO','DateTime.Now','DateTime.UtcNow','new Random('):
  assert forbidden not in text,(p,forbidden)
project=(root/'src/Core/Hmi.Core.csproj').read_text()
assert 'netstandard2.1' in project and '8.0' in project and 'PackageReference' not in project
assert 'EnableDefaultCompileItems>false' in (root/'HmiGame.csproj').read_text()
assert (root/'src/Core/.gdignore').exists()
print('PASS architectural dependency boundary')
