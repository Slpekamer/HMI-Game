# Independent actual-host responsive review

## Result
Projection/lifecycle checks pass; current extreme-portrait camera framing is NOT accepted as a quality solution.

Fresh isolated hmi-host-responsive-ni88f7s4 built/imported and headless PASS 292. Native X11/llvmpipe PASS 299, process exit 0, six distinct PNGs at actual requested dimensions. Final repeated 1280 capture overwrites the same named first frame. Desktop inventory confirms Godot exited. Driver V-Sync warning retained; this is not a performance benchmark or Android run.

## Checked
Production Start uses a fresh isolated persistence directory and enters real Combat before the fixture pauses it. Resize preserves simulation/world identity, run token, wave, tick, seed and player identity. Tests cover viewport Expand, stable screen/world basis, release of held joystick on changed layout, real enemy-only bars with current generation, full bar viewport clipping, inverse mapping under transformed CanvasLayer, live modal preservation across rotation, and layout release after host free. Programmatic input is not a physical gesture test.

## Actual visual findings
- 1280x720: main hero and enemy bars readable, original framing retained.
- 720x1280: actual full portrait framebuffer, no letterbox substitution. UI fits but hero becomes substantially smaller amid a wide arena overview.
- 540x1280: hero silhouette only about 30 physical pixels tall, smaller companions and excessive empty floor; finite floor plane edges reveal purple clear background at upper right and bottom. This is a concrete visual blocker, despite mathematical coverage success.
- Logged nominal vertical 1.62m world segment (1.2m model × host scale 1.35) is 48.68 logical pixels landscape, 27.38 portrait; at 540 physical width the latter is about 20.54 physical pixels. This is a projection diagnostic, not an exact rendered silhouette measure.
- Enemy bar anchors and full rectangles remain aligned in tested views; this does not prove all animation poses, crowds or device safe areas.

## Required next decision
Do not adopt the current tall-portrait all-world-coverage formula as final. Choose an explicit framing/fallback policy retaining meaningful character/action readability and adequate peripheral threats. Do not change gameplay collision/scale to hide a camera issue. Avoid claiming an arbitrarily complex following solver is approved. Actual normal input/rotation and Android screenshots remain separate gates.

Evidence: host-responsive-{first,second}.log (first is a TEST assembly-access compile failure, preserved), host-responsive-native-native.log, host-responsive-native-world-*.png, source SHA table. No production source was edited by this review.

Read-only baseline source preservation check: all 16 original Core C# files, 4 persistence C# files, 11 content JSON files and 10 GLBs are byte-identical to restored BUG4 baseline. Build/import artifacts and extracted texture files are excluded; no full-directory equality is claimed.
