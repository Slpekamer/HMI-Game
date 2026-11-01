# Independent offline fur source review

Read-only Blender4.3.2 reopen with `--disable-autoexec`; no save operations. Exact files/hashes and limits in r5-consistency-summary.json.

- Original frozen source: real native hair counts matched, no animation, but small closed nose mesh had inward winding. This was reported rather than called green.
- Distinct corrected source: all finite checks and closed-mesh orientation checks pass;10native CURVES objects,537200curves,2686000points; no armature/actions/drivers/NLA; source bytes unchanged after inspection.
- Plane, smile and tongue are open surfaces: no global closed-volume/outward certification is claimed for them.
- Earlier four-view source vs later corrected side source is NOT exact same geometry data. Mesh vertex positions/materials/world transforms match; some face tessellation differs; tail fur has a different point layout. Nine other grooms match. Numeric per-index tail difference is not a physical translation of the whole tail.
- Recommend render final views by reopening one immutable corrected source and moving only camera, rather than rerunning procedural geometry generation.

These checks prove neither plush aesthetic fidelity nor Godot/mobile suitability. User approval remains pending. Scripts and reports are technical evidence only; production code/runtime/assets were not changed.

## Canonical corrected source — 2026-10-31 03:45 UTC
The artist created a distinct canonical .blend directly from the original four-view source, reversing only nose face winding. Independent reopen PASSED. SHA256a810230dc84f7a20664157be2c4ddd516f30a2fadccc6d842002c864d5193242. All10 groom fingerprints now exactly match the original four-view source; every other mesh fingerprint matches except the intended nose winding. Counts and no-animation checks pass, source bytes unchanged. See offline-fur-canonical-integrity.json.

The earlier side-run source/comparison remains historical evidence and is superseded, not silently called consistent. First four reviewed preview images precede the nose-normal correction (same vertex shape/exact groom); additional side is rendered from saved canonical source, not regenerated geometry. Generator rebuilds are not promised byte/groom deterministic. This closes source consistency, NOT owner aesthetic approval or game/mobile compatibility.
