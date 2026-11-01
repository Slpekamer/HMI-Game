# R8 floor-contact geometry gate

Owned additions only: tests/Presentation/FloorContactProbe.cs.template and scripts/test_floor_contact.py. No Core/production/verify changes by this worker.

- Fresh isolated build/import/headless run: exit0, PASS95, no ERROR/FATAL. Actual floor top0, four petal batches/mesh heights, real Buding HoneyDisc center.015/height.01/top.020/radius, initial noncombat and opaque Home/Selection, restart preserves floor/batch node IDs and heights.
- Independent actual desktop native execution of that exact built/imported fixture: PASS977, exit0, no ERROR/FATAL. All220 actual MultiMesh stored transforms printed once and checked again after restart: petal center.006/bottom.001/top.011, positive separation from ground0 and actual HoneyDisc top.020. Reviewer confirmed no Godot process remained.
- Native runner first launch lacked `python`; retry with python3 then encountered NU1900 audit-index build blockage. Both failed launch/build logs retained by reviewer. No audit disabling or network change. Native then ran exact previously built headless fixture with fresh XDG paths; probe/DLL hashes checked before execution.

Headless dummy rendering cannot validate MultiMesh transform storage. Default runner explicitly prints that native instance gate is deferred; --native performs it without numerical fallback. Results establish geometry/storage/lifecycle only, not shadows, rendered appearance, all animation poses, normal gameplay, or Android.

Final source hashes: floor-contact-source-sha256.txt. Headless log: floor-contact-headless-final.log. Native log: /workspace/shared/hmi-experience-review/floor-contact-native.log. Exact isolated project: /workspace/scratch/80f5bc988ef7/godot-toolchain-verification/tools/tmp/hmi-floor-contact-wuugfn80/project.
