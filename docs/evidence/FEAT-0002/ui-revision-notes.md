# UI revision: reference alignment, not visual acceptance

2026-10-31. User rejected the APK's appearance. Earlier functional testing did not establish reference fidelity or user acceptance. This revision is isolated from frozen M2/M3.

## Inspected inputs and changes

All four actual approved PNG references in `docs/art-reference` were reopened before implementation. The reference's dodge button is superseded by the user's latest explicit removal request; no replacement action is added.

- Character selection uses five full-height live model portraits, a selected badge, colored rounded frames, a bottom selected-character summary, and a clear primary start action.
- The title hierarchy uses plum text, cream outlined plaques, larger headline type, and layered card edges rather than the previous flat lavender panels.
- The initial text-free procedural candy-village SVG was too flat in actual review. It is retained as source/fallback; the current candidate uses a newly generated original text-free candy-city raster supplied by the integration lead and inspected before integration. No concept screenshot is used as fake game UI. The real interactive controls and live model previews are rendered over the decorative image.
- Upgrades retain three authoritative categories. Larger illustrations, colored category tabs, readable descriptions and explicit selection state remain separate interactive controls.
- Shop keeps four data-driven items, the real earned/spent/remaining ledger, item price pills and disabled stock/funds states. It does not copy the reference's sample prices or balance.
- Combat has a compact hero portrait/HP group, separate wave and timer pills, coin counter, joystick and companion controls. Dodge control, hint and callback are removed.
- Named Idle model animations are played in isolated portrait viewports when available. The old unanimated v01 models remain an interim input, not a completed animation feature.

## Boundaries and risks

No gameplay decisions were moved into UI. Existing callback capture, codex modal background lock, close handling and repeated transitions are retained. Public HUD signature removes the obsolete dodge argument and adds an explicit authoritative `hasCompanions` presentation flag; capability is not inferred from localized text. Final integration must test those flows again.

The compound mechanism/item illustrations remain original stylized vectors rather than the reference's plush mechanism artwork. The new raster backdrop improves atmospheric depth, but only actual native comparison can judge the composition. Larger live portraits expose model fidelity limitations; refined v02 models are a separate art delivery. Visible live portrait viewports require performance measurement on the final candidate and real devices. Readability or a successful build alone is not a visual approval.

## Evidence so far

- Initial revised host build: 0 warnings, 0 errors.
- Godot resource import completed.
- Actual managed headless smoke: `HMI_SMOKE phase=Combat wave=1 entities=16 tick=57`.
- First native iteration found genuine clipping defects: textured style-box default content margins inflated button minimum sizes; narrow ornate frames crossed text; long category titles exceeded tabs. These were not accepted.
- Second candidate uses explicit texture/content margins, clean short capsules, short category titles, emboldened typography and 12 compound mechanism/item illustrations.
- Automated actual scene layout checks: selection and combat have zero visible buttons outside the 1280×720 base viewport. This checks geometry, not text or visual quality.
- Current 21 authored SVGs passed hash/XML/no-external-resource validation; decorative raster hash, PNG dimensions and provenance also validated.
- Second native comparison still found squashed legacy square button artwork and overlapping long shop descriptions. Both remained blockers, not accepted polish.
- Third candidate replaces button artwork with purpose-built wide 320×80 assets, allocates 135px shop detail space and 124px card detail space, and removes redundant same-ink text outlining. Wrapped labels receive their final width before their text, preventing stale minimum-height inflation.
- Actual engine font geometry test `tools/ui/check_catalog_text.gd` passes all 25 catalog descriptions: 21 cards and 4 items. Maximum required heights are 120px for risk.bounty and 113px for tonic. No descriptions or downsides were shortened.
- Companion tray now uses actual shooter/support model portraits without HP bars; preview viewports render WhenVisible.
- Third native visual/input review and animation video/export gates remain pending.

## Latest user review: models rejected again

At approximately 02:25 UTC, the implementation lead relayed the user's rejection of the refined model screenshot: “这模型好丑啊”. The current art is **not visually accepted**. UI functional/layout evidence may still be preserved, but must not be presented as character-model approval or approval for another APK delivery. Further character work is gated on a single blue hero's shape, face and proportions reviewed from front, side and three-quarter views before animation/roster propagation. UI runtime source is frozen; no further UI feature changes are being made in this increment.

## Final UI-specific observation (02:26 UTC)

The independent reviewer captured actual `final-selection.jpg`, `final-combat.jpg`, `final-upgrades.jpg`, and `final-shop-before-pick.jpg` in `/workspace/shared/hmi-visual-review`. The UI author also inspected the actual upgrade/shop images. The sampled risk card's complete downside, all four shop descriptions including tonic expiry and item effect scope, and the full-height buttons are visible without the earlier collisions. The blue hero HUD face crop was corrected in the actual combat screen. All-five HUD portrait proof was deferred after the user's model rejection; it is not claimed passed.

**This is not an overall release gate.** The same native run found a blocking wave-two `ObjectDisposedException` in `CharacterPresentation.Snapshot`, outside this UI-owned change set; the integration lead is responsible for lifecycle repair and a fresh regression. No final all-green or visual acceptance is claimed. Current character art remains explicitly rejected by the user.
