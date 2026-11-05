# Floor-contact fixture: explicit fresh-rule preparation

## Requirement and root cause

The geometry/lifecycle probe must obey production save consent while retaining its existing floor, petal and HoneyDisc assertions. The unchanged probe on strict candidate `9d070f4955ea2093b5710f66149c7227b879d353` freshly built and imported without diagnostics, then failed at **Real Buding Start entered combat**. Its first private `Start` call opens fresh-rule preparation on an empty store; it does not yet start combat. Production deliberately returns to selection after explicit preparation and requires another start action.

## Narrow change

Only `tests/Presentation/FloorContactProbe.cs.template` changes. Before the original explicit starts, the fixture requests start through the production UI callback, requires the visible/enabled **确认准备新局** button, and emits its real confirmation signal. Four new assertions verify consent, the available confirmation, no automatic combat start, and selected rule-space state. An additional original `CheckFloor` invocation verifies node reuse across preparation.

Every original source line and assertion remains in its original order. Production code, save behavior, assets, the runner and all diagnostic thresholds are unchanged. Direct field writes, manually manufactured save selection and confirmation bypasses are not used. The original private `Start` calls remain geometry/lifecycle fixtures; this probe is not a substitute for full routed-input, replacement-consent or migration coverage.

## Verification, 2026-11-05

- Fresh unchanged candidate: strict build/import exit 0; runtime exit 1 at the original combat-start assertion. An initial attempt was blocked by this cloud environment's read-only default .NET runtime directory; writable process-scoped runtime/home directories resolved that setup issue without changing repository files.
- Corrected strict `scripts/test_floor_contact.py`: build/import/runtime exit 0, zero build warnings/errors, **FLOOR_CONTACT_PROBE PASS 115**, with strict diagnostic rejection unchanged.
- Architecture and whitespace checks pass. A disposable production-floor mutation from center -0.15 to -0.14 makes the unchanged strict runner fail the original floor-top assertion. [Aggregate continuation and downstream fixture results](legacy-host-fixture-consent.md) preserve later failures; this scoped pass is not a fully green historical suite.
- Headless shapes and node lifecycle only. The native 220-instance transform-storage gate is not run in this no-display environment. No visual, hardware, mobile performance, export or release claim follows.
