# Mobile input fixtures: explicit fresh-rule preparation

The two historical mobile adapter probes assumed the first start on empty storage entered combat. On the unchanged baseline, `test_mobile_safe_area.py` failed with `no legal point` because the joystick was never in combat; `test_mobile_motion_integration.py` failed at `actual session starts after UI motion interactions`. Their builds/imports passed. Production requires consent to prepare rule3 storage, returns to selection, then waits for another start.

Only `MobileSafeAreaProbe.cs.template` and `MobileMotionIntegrationProbe.cs.template` change. Both add actual visible/enabled fresh-rule confirmation and no-auto-start checks before the original start sequences. The motion probe uses its existing routed gamepad confirmation and additionally verifies held confirmation cannot start the returned chooser. Original navigation, motion, touch, hero, seed-focus and pause checks are unchanged. No runtime state is forced and no production, runner, diagnostic threshold or gameplay rule changes.

Fresh strict build/import/runtime results on Godot 4.6.3 Mono/.NET 10.0.403:

- MobileSafeArea: PASS 31.
- MobileMotion: PASS 34 main checks plus 4 supplemental checks, zero native captures.
- All 24 original SafeArea and 32 original Motion assertion call sites remain literally intact; declaration methods are not counted as assertions.
- In separate disposable copies, withholding confirmation fails each runner at the added preparation/no-auto-start assertion. Builds/imports succeed, runtime exits 1 as required.
- Original failures remain recorded, and Git whitespace checks pass.

These are injected headless lifecycle and input-boundary checks, not physical controller/touchscreen, Android OS delivery, native appearance or device performance certification. Other aggregate/migration failures remain separately classified.
