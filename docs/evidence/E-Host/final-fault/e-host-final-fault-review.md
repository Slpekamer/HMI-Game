# Final host fault/hash gate
2026-10-31 20:42 UTC. Read alongside e-host-native-independent-review.md.

- Exact production copied-source positive: build/import/run exit0, CHECKPOINT_HOST_PROBE PASS31.
- Negative control changes only GameHost catalog SHA expression by removing ToLowerInvariant. Build/import0, run1 at first pre-injection production assertion, line34: actual host catalog hash agrees with Core codec and production store. This is an intentional regression test, not an unreported product failure.
- Positive fixture native (official binary, separate native-user-data/native-editor-config, Dummy audio): PASS33, two actual PNGs independently viewed. Closed failure warns that ended state has not persisted and force exit may retain old safe point; only Retry. First snapshot failure explicitly offers whole-run-unsaved/no recovery, not a misleading previous checkpoint promise. Both messages fit; duplicate unsaved paragraph is minor polish.
- Inventory before launch confirmed benchmark window absent; inventory after native terminal pass confirmed fixture window absent. Desktop released.
- Source hashes: e-host-final-fault-source-sha256.txt. Logs: e-host-hash-{negative,positive}-control.log and e-host-error-native.log. Pixels: e-host-error-{close-failed,first-save-failed}.png.

No gameplay/source writes to main tree. Scope is actual host with injected storage errors, not a real disk power-loss trial. Native three-process real save/restore/acknowledged-death restart evidence remains separate. Final aggregate/package authority stays with codelead.
