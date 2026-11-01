# UX-0003 — Visual ground-plane alignment

Scoped candidate after tested UX-0002 commit87640db. The actor-root/sole reference is Y0, but the80×.3×80 floor Box center-.2 put its top at-.05. This contributes a real5cm visual gap; it is not proof of Android shadow flicker or all gait sliding.

Only production changes:
1. Floor centerY -.2→-.15, giving topY0.
2. Decorative petal instance centerY -.035→+.006, giving bottom+.001/top+.011 with unchanged .01height.

Honey disk stays center+.015/height.01/top+.020. Initial C trial petalcenter+.015 was rejected: honey/petal top faces coincided, producing moving triangle/stripe artifacts in an actual targeted same-camera fixture. D lowers only the petals, leaving9mm top-surface separation. C/D120-frame traces match, videos fully decode, independent continuous frames56–61 show C artifacts and D uniform surfaces. Durable study: Library libfile_e09ff0b2bd7481919270da984ef49b99 v0.

No Core collisions, wall geometry, character models, movement speed, Run clips, shadow mode/bias or camera changes. This does not claim physically locked feet or Android shadow repair. Existing tiny animation penetration/floating point tolerances need separate pose evidence.

Passed: final actual scene geometry headless95 plus real-renderer977 checks of all220 transforms; complete coherent aggregate exit0; normal Buding1030 honey/natural-drop/foot/wall readability, restart and actual Alt+F4 exit0 without ERROR/FATAL. Limited sampled visual evidence, not all poses or Android. Headless dummy MultiMesh data is not accepted as proof of real renderer instance transforms; if needed the instance-value gate runs with an actual renderer and reports that distinction.
