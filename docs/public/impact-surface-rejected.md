# Target-surface impact reaction — completed, rejected

## Intent and scope
The first physical-point symbol was mostly hidden by the victim. This experiment allowed a cosmetic offset to the actual victim's camera-facing rigid-mesh surface while leaving physical impact and authoritative damage unchanged. Only real Impact transitions could create it; targets were addressed by full identity, not by the nearest visible object.

## Implementation
`ProjectileImpactSurface` casts a camera-parallel ray against the selected victim's own rigid head mesh, with a bounded shared-mesh cache. It stores a target-local attachment for the short symbol lifetime rather than repeating a full triangle scan on every update. Normal depth remains enabled. The physical-to-visual displacement is guarded conservatively against scene walls and bounded relative to the target; this is not a guarantee of full GPU visibility.

The scan budget is at most two candidate attempts and 4,096 triangle visits per render, with eight cached meshes. The measured head mesh contains 2,412 triangles, so a second full scan in the same render is rejected rather than partially tested. Ambiguous neighboring recipient bounds suppress the symbol instead of retargeting it. Invalid or missing targets fall back conservatively or clear the reaction. Identity, scope and rate limits remain bounded.

An eight-slot impact pool uses short lifetime, per-victim/global onset limits and dark intervals. Repeated hits do not indefinitely extend an active symbol. The physical impact coordinate and the visual surface coordinate are kept distinct.

## Result and decision
Two natural first-impact samples rendered successfully with matching framebuffer stamps and real target bindings. Nevertheless, normal-scale images showed only tiny fragmented highlights; the existing enemy warm coloration carried nearly all of the readable feedback. Comparison against the earlier physical-point variant changed only 29 and 52 pixels in the relevant world region. These were different presentation binaries with matching recorded authority, not a claim of universal causal isolation.

On the software-rendered desktop sample, initial cache preparation was about 3.81 ms and one 2,412-triangle scan about 0.72–0.77 ms. These are local instrumentation timings, not mobile performance estimates. The visible benefit did not justify this additional complexity. **Do not adopt this experiment.** Retain the improved projectile bodies and simpler existing hurt feedback instead. No further geometry or color sweep is proposed by this archive.

No full wall-occlusion native negative case or dense-scene readability acceptance is claimed from the two images. No large burst, white flash or depth override was used to conceal the failure.
