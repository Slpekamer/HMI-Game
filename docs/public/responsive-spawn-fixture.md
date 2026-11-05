# Responsive Host fixture: wait for real spawn activation

## Cause and preserved purpose

After real fresh-rule preparation was added, the strict responsive Host runner passed combat start but failed the original `actual combat contains visible enemy bars` assertion. The fixture immediately paused tick-zero combat. Current `WaveRules.SpawnPreviewTicks` is 60; initial enemies exist only as pending reservations until `SpawnReservations.ActivateDue` runs at their scheduled activation tick. Correct production therefore has no enemy health bars at tick zero. The test's purpose is to verify real enemy-bar projection and authority preservation across viewport changes, not to remove the preview delay.

## Narrow fixture change

Only `HostResponsiveProbe.cs.template` changes. Before the original pause/resize sequence, it asserts tick zero, pending reservations, no live enemies, the complete preview duration and zero rendered enemy bars. It then advances exactly the existing `WaveRules.SpawnPreviewTicks` using real `GameSimulation.Step`, normal Host event consumption and presentation rendering. It requires combat at the actual first activation tick with live enemies before beginning the unchanged resize assertions.

No tick, health, entity or spawn state is overwritten for this new setup. No production system, bar visibility logic, camera contract, timeout, error filter or original assertion changes. All original source lines remain in order. The later already-existing controlled corner-positioning/camera fixtures remain explicitly synthetic and are not natural gameplay evidence.

## Fresh verification

- Strict `scripts/test_host_responsive.py`: build/import/runtime exit 0, zero build warnings/errors; **HOST_RESPONSIVE_PROBE PASS 298**.
- At tick 60, ten actual enemies are live and nine bars are initially visible. The unchanged physical-resize routes check four to nine visible bars, depending on framing, plus generation identity, projected rectangles, pause invariance, modal continuity, camera restore and teardown.
- Disposable no-enemy negative control: after all activation preconditions pass, remove the activated enemies only in the temporary fixture. Build/import exit 0; runtime exits 1 at the original `actual combat contains visible enemy bars` assertion. This is counterevidence, not a shipped state manipulation.
- The preceding tick-zero failure and negative-control logs are retained. Git whitespace and original-line preservation checks pass.

Headless projection/lifecycle only: no native pixels, touchscreen/physical-controller delivery, hardware performance or Android acceptance. [Earlier consent-stage failures](legacy-host-fixture-consent.md) remain historical evidence; this fix does not resolve the separate historical migration suites.
