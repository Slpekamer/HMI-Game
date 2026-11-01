# Exclusive primitive-wrapper lifecycle trial

Copy-only candidate; no production files changed. Only AssetVisuals.Box and Disc acquire newly created mesh/material wrappers with `using`, assign them to the MeshInstance3D native owners, and deterministically release those managed wrappers. The shared Material factory, all other call sites, GD.Load/PackedScene caches, animation, Theme, and Core remain unchanged. No forced GC.

## Actual ownership gate

Three fresh headless processes each passed 256 assertions with no ERROR and exit 0. An explicit primitive owner retains the same native mesh/material IDs and properties after the original wrappers are disposed. Actual candidate factories preserve mesh sizes, material colors, Disc alpha/unshaded flags; two Box instances own distinct resources, mutations do not cross instances, and freeing one leaves the other usable. Inspection-acquired wrappers are released by the probe. This is native identity/property evidence, not rendered pixel evidence.

## Contemporaneous exit comparison

Same current-r1 source snapshot, A original versus B only the two factory changes, each built once. Nongenerated C# comparison confirms AssetVisuals.cs is the only A/B source difference. For each entry point, ABBA × 5 uses ten fresh user-data processes per variant:

| Entry point | Original A fatal | Candidate B fatal | Behavior assertions |
| --- | ---: | ---: | --- |
| tools/ui/check_layout.gd -- --smoke | 0/10 | 0/10 | All bounds checks passed |
| Original full CodexProbe | 6/10 | 0/10 | All 23 checks passed before exit |

The layout control did not reproduce its previously observed failure in this run; do not claim this comparison proves a layout fix. The Codex result supports this scoped candidate but is not universal reliability, native visual, or full-aggregate acceptance. All prior rejected style/fixture experiments and their red logs remain unchanged in the separate earlier diagnosis package.

Evidence: primitive-wrapper-validity-*, primitive-wrapper-abba-results.json, primitive-wrapper-abba-projects.json, forty individual layout/Codex logs, and primitive-wrapper-AssetVisuals.diff. No public upstream submission.
