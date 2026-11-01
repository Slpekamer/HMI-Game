# Food hero runtime asset contract v1

2026-10-31. This is an implementation contract and provisional budget, not a performance or appearance acceptance result.

| Stable hero ID | Display name | GLB basename | Existing trait |
|---|---|---|---|
| orbit | 绯绫 | hero_orbit.glb | orbit |
| crit | 卷卷 | hero_crit.glb | finite ricochet |
| honey | 布叮 | hero_honey.glb | honey zone |
| summon | 烤团 | hero_summon.glb | independent companions |
| bounce | 脆蓝 | hero_bounce.glb | impact |

- Runtime paths `content/models/<basename>`. Manifest includes source hash, export hash, source generator/version, triangles, surfaces, textures, bones, morph names, bounds and clip lengths.
- Imported glTF/Godot coordinates: meters, Y-up, forward -Z, feet-center origin at ground, identity model root. Authored Blender source may be Z-up/front +Y; exporter must convert correctly, verified in engine. Neutral maximum height 1.2 m including costume; existing visual-only host scale 1.35 retained initially, no collision radius changes.
- One AnimationPlayer, exact unique suffixes `Idle`, `Run`, `Attack` (prefix library path permitted and recorded). No Walk alias. Idle/Run loop; Attack one-shot. Suggested durations 1.6 / .64 / .36 s are authoring starting points, not forced timing. No model-root transform tracks or gameplay callbacks. An exporter may include constant skeletal Root-bone channels, but actual imported Root poses must remain unchanged; motion belongs to child bones. Articulated limbs and balanced feet, not root-only sliding.
- Morph names `Blink`, `Smile`, `Surprise`, default 0; each may exist on multiple facial meshes with coordinated definitions. All three must occur in hero asset. Body clips must not write them. Basis shape matches approved appearance. No hide-switch face swaps. No Dodge clip.
- Optional stable empty sockets: `Socket_Hand_L`, `Socket_Hand_R`, `Socket_FX`; these are visual attachments only, no collision/physics/gameplay metadata.
- Candidate LOD0 aggregate per hero including clothes/props: 15–30k triangles, <=32 deform bones, <=4 total material surfaces, <=4 skin influences/vertex. Measure imported totals; four materials is not a promise of four draw calls (passes/shadows can multiply them).
- One 1K atlas set (base color, optional normal, ORM); opaque portable PBR, no Blender procedural-only shader, strand fur, custom extensions or runtime scripts. Texture CPU/GPU memory/compression and skin/morph cost must be measured; 1K atlas alone is not an aggregate memory budget.
- 2D selection remains TextureRect-based. Use approved new-food concept front views through non-destructive AtlasTexture regions, preserving silhouettes/aspect; these have cream backgrounds and are not transparent cutouts; do not render five live 3D selection viewports. Old cast portraits are not final acceptance for new models.
- First export Kaotuan only for actual Godot import/pose/morph probe before batch. Keep editable high-resolution sources outside the runtime repository in durable Library. Reject import defects before replacing placeholders; no fallback counts as success.

## Lifecycle and fallback policy
Host consumes events after each fixed Step, rejects wrong run/wave, and binds trigger source generation before queuing a gesture. A presenter rejects duplicate same-tick triggers; newer valid triggers may restart Attack. Paused Advance consumes neither clip time nor the queued gesture. Run/wave reset or tick rollback clears queued attacks, facial timers and morph weights. Disposal is idempotent and safe even after the model is freed. Core progression never depends on clips/morph completion.

Legacy enemy/companion models may lack facial morphs during this scoped hero update. Required new hero clips/morphs are checked as part of release asset validation; optional legacy absence must not be conflated with a passing food-hero export. Missing final hero resources must fail integration rather than silently displaying old models or fallback spheres.

## Texture-only export regression gate

For this specific baked-atlas food-hero contract, runtime primitives must omit legacy `COLOR_*` vertex attributes. The high-resolution source uses color attributes during baking; accidentally exporting them again adds unused data and risks unintended atlas multiplication when a material enables vertex-color albedo. Removing them is a hygiene/contract gate, not proven attribution of the observed dark lines. This is a project contract, not a claim that vertex colors are generally invalid glTF. A deliberate future vertex-color material requires a separately reviewed contract change.

Kaotuan r6 imported correctly but actual Godot footage exposed forehead black lines and a Smile crease. The added validator rejects that export; r8 is the isolated color-attribute-removal A/B (r7 internal cleanup was incomplete), not automatically accepted. Same-light Godot footage still shows both defects in r8; vertex-color removal did not fix their visible cause. Shape normals and morph deformation remain separate visual checks.

AtlasTexture cropping retains the full source texture in memory. Current five 2027×776 sources represent 31,459,040 decoded RGBA bytes before mip/compression/driver overhead. Final device memory must be measured, not inferred from the cropped rectangle.

### Baked normal tangent gate
Normal-mapped primitives must export a float VEC4 TANGENT accessor matching the vertex count. Preserve the bake's tangent basis, including compatible morph tangent export. Explicit tangent export makes the baked basis inspectable and reproducible, but it did NOT establish a visual fix: the initial r9 comparison used Smile≈0.991 instead of 1.0. Strict equal-weight frames retain the forehead artifact. This correction supersedes the initial diagnosis; no universal GPU certification is claimed. Tangent seam splits may change accessor counts/order while indexed triangle-corner geometry remains identical. Morph facial quality is a separate gate and must still pass actual shaded views.

### Single-atlas UV channel gate
The baked food runtime mesh uses exactly one UV channel, TEXCOORD_0. All material texture slots, including any texture-transform UV override, must reference UV0. Source parametric UV layers used by procedural authoring must not survive before the baked atlas as an unintended default. Juangjuan r2 was structurally importable but visibly sampled the wrong UV set; r3 fixes the channel mapping. This is another actual-pixel gate, not something triangle counts can prove.

### Non-tiling atlas boundary sampling

Each referenced texture must resolve to an embedded image and an explicit sampler with both `wrapS` and `wrapT` equal to `CLAMP_TO_EDGE` (33071). Default glTF repeat sampling is valid generally, but not this project's non-tiling baked atlas: near UV 0/1, bilinear repeat can sample the opposite edge. This does not replace internal island padding or fix arbitrary mesh/morph shading defects. Three candidates were compared with identical BIN chunks and only sampler fields plus benign buffer-padding length metadata changes; actual Buding waist-line pixels improved, while Kaotuan and Juanjuan retain separately tracked residuals.
