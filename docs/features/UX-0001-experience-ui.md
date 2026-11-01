# UX-0001 — Device-feedback UI revision

Authority: 2026-11-01 owner device feedback supersedes the old static 2D-selection requirement. Work is isolated from published BUG-0004 and does not import the unaccepted wallet candidate.

## Three incremental stages
1. Home → real 3D selection. Home exposes only existing Start/Continue/instructions. Start navigates to selection without changing Core or save; actual StartRequested remains behind the existing overwrite boundary. One large real model preview switches among all five heroes, with instance-owned animation resources and a manual Idle clock. Hidden pages, modal/focus loss and destruction stop the clock. Existing stale-button and persistence-generation guards remain.
2. Simplify visual chrome/background for cards/shop/pages, keeping category hierarchy, illustrations, complete mechanic/risk/expiry text and state labels. Consistent normal/pressed/hover/focus styles; no accidental black rectangle or size jump.
3. Actual viewport/safe-area layout. Host supplies logical viewport and mapped safe rect; UI reflows HUD and narrow-screen menus, while host owns camera/world/input projection. Validate 16:9,19.5:9,4:3 and portrait/very narrow cases without claiming letterbox is responsive. No collider/Core changes.

## Ownership / API
UI worker owns GameUi, CandyTheme, preview/layout helpers and UI probes only. Lead owns GameHost/project stretch/safe-area mapping/camera/enemy bars. API additions: ShowHome(); SetLayoutMetrics(Vector2 logicalSize, Rect2 safeRect). Existing ShowSelection remains the actual chooser; restart returns to chooser for quick retry, chooser Back returns Home. Existing gameplay/persistence events remain unchanged. First stage keeps the existing1280×720 contract; the metrics setter is a compatibility seam, not a responsive-layout claim.

## Gates
- Home navigation never writes save/Core; Continue only emits after existing validated confirmation. No fabricated settings feature.
- All5 real model Idle clips advance with changing pose; no shared animation mutation, no main-world model changes; one selection SubViewport at most, zero after home/combat/page exit.
- Repeated navigation, held/drag-out/cancel/focus-loss input, stale delegates and modal focus protection.
- Current card/shop/contract/save text and ledger gates retained; old 2D zero-selection-viewport assertion is explicitly superseded, not silently dropped.
- Actual engine screenshots and short motion capture after headless gates. Device input/performance remains separate from cloud llvmpipe evidence.

## Stage 1 verification in progress (2026-11-01)

- Host build: zero warnings/errors.
- Updated selection engine probe passed all five hero identities, two repeated start/restart cycles, seed mapping, live actual Idle bone/hierarchy motion, per-preview AnimationLibrary identity, focus freeze/resume, one live selection viewport, and disposal when leaving selection. This explicitly supersedes the former user-directed 2D/zero-selection-viewport requirement, not the lifetime gate.
- Persistence UI probe passed 100 assertions after replacing the obsolete assumption that Resume lives beside the selection seed editor. Home Resume remains confirmation-only; selection replacement still locks its internal seed editor. Fewer background controls explain the changed assertion count; counts are not coverage equivalence.
- Shipped font coverage passed 577 required codepoints with no font modifications. Catalog text layout passed all 39 entries.
- Preserved red evidence: the first new selection fixture had an explicit notification integer-cast compile mistake; corrected fixture passed. The first persistence fixture queried a seed editor no longer present on Home; its replacement tests both Home and the selection editor separately.
- Opaque-page fixture exposed an inactive AnimationPlayer clock read with the existing AnimationTree mixer. Test-only update measures the real presentation clock plus exact paused bone poses rather than suppressing engine errors. Fresh rerun passed 144 checks, including exact paused bone poses and resumed presentation clock.
- Actual 3D framing, motion appearance, long-press/drag/cancel/focus button behavior, and visual acceptance are pending native review. Stage 3 actual responsive layout is not yet implemented; the metrics method currently stores the incoming contract only.

### Planned responsive composition (not implemented in stage 1)

The host supplies actual logical viewport and inset-safe rectangle. Landscape uses additional world area, anchored HUD groups, and centered menu content rather than stretching artwork. Portrait uses an independent stacked chooser, scrollable card/shop content, and a safe-bottom action row; it must not shrink the complete 1280-wide desktop screen into an unreadable phone stripe. Common modal text remains scrollable with reachable actions. Rotation should release joystick/held pointers and rebuild presentation only, never send Core or persistence intents. The existing fixed-size opaque-page coverage test must become an actual-safe-rectangle coverage test when Expand is activated. Default 1280×720 behavior is maintained until that coordinated phase.

### Stage 2 planned minimum diff

Replace full candy-city backgrounds behind decisions with the same quiet warm neutral surface used by Home. Replace ornate repeating nine-slice panel frames with flat rounded cream panels and thin category-tinted outlines; retain category hue, names, mechanic illustrations and all effect/downside text. Simplify the large title plaque into a clear title/subtitle pair. Keep existing card text rectangles/button positions unchanged in this phase so the actual-control text-fit gate remains comparable. No new raster assets or gameplay choices are introduced. Native before/after images, not the simplified code alone, decide whether the hierarchy is clearer.

## Stage1 final scoped gate (2026-11-01 07:08 UTC)
Final main source equals the independently rendered r6 production C# byte-for-byte; adding the permanent256-check ownership fixture did not change production code. Final verify.sh completed exit0 with Core624665, mixer17590, boundary100, cadence5980, enemyHP35, primitive256, lifecycle, all5preview, Codex23, checkpointHost31/store112/session and UI gates. Three independent fresh-XDG actual window closes were exit0, no ERROR/FATAL/leak; no CtrlC substitute.

Old failed exits and rejected mitigations remain in the diagnostic Library package and source plan. No universal/root-cause/device claim. Quiet pages/responsive/portrait/floor remain separate, not included in this first-stage commit. See ADR-0009.
