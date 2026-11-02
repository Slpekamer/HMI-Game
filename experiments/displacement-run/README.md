# Displacement/contact Run — isolated WIP

The prior in-place action sample was rejected for skating. Static proportions remain accepted; clearer calves are not a locomotion acceptance criterion.

This single prototype preserves Core speed, collision, root movement, genuine Attack/morph timing and static shape. At steady rendered speed4.8: alternating footfalls are1.2 world units apart (4/s); stance lasts.05s, followed by.20s whole-body flight. Each leg has.45s swing per.5s cycle. Contact hip–ankle separation is.180 world units, cosmetic body hop.050, swing-foot lift.040. Body crouch is dynamic, not a changed bind. Complete world Foot transforms and actual sole offsets define contact; no limb scaling or hidden reach clamp.

The existing render-root smoothing is retained. Phase follows actual rendered displacement, not input duration or nominal speed alone. A and B share the same rebound rig; A plays the rejected in-place Run, B enables the isolated contact component.

## Run scoped checks
Requires the repository-pinned .NET SDK and Godot4.6.3 .NET executable. Set `GODOT_BIN` explicitly if `godot` is not the .NET build. Each invocation needs a fresh `DISPLACEMENT_OUTPUT` directory.

```sh
GODOT_BIN=godot bash experiments/displacement-run/run-check.sh headless
GODOT_BIN=godot bash experiments/displacement-run/run-check.sh lifecycle
# Known failing boundary; failure is not counted as success:
GODOT_BIN=godot bash experiments/displacement-run/run-check.sh restart
# Explicit desktop capture, requires a display:
GODOT_BIN=godot bash experiments/displacement-run/run-check.sh native
```

Current reconstructed scripts and fixture are untested until new results are recorded. Prior local recordings/traces are unavailable. Restart during settling remains an explicit failure; immediate-moving Reset and arbitrary direction changes are not certified. No normal controller, adoption, release or visual acceptance is claimed. Numerical locking cannot replace original-scale, normal-speed visual judgment. Three contact tick intervals require four endpoints; three captured samples alone do not demonstrate50ms.
