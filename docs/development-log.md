# 开发日志

所有变更均需留痕；可按可审查的变更批次记录，不必逐行记账。
状态及验证须忠于实际；下述初始验证由工程创建阶段提供，不代表本次重新执行。

## CHG-0001 — 空白 Godot 工程与开发规范基线

- 日期：2026-10-30。
- 需求：建立空白 Godot 工程；为未来 Unity / 团结迁移、全面 ECS 和全过程留痕制定规范。
- 范围：`project.godot`、`scenes/main.tscn`、`.gitignore`、`README.md`；
  `agent.md`、`AGENTS.md` 及本目录的记录模板。未实现玩法或 ECS，未安装 C# 工具链。
- 基线：Godot `4.6.3.stable.official.7d41c59c4` 标准版，Compatibility；主场景仅空 `Node3D`。
- 设计：核心与引擎适配隔离；未来纯 C# 路线须先完成 .NET 工具链与目标平台/双引擎验证。
- 决策来源：用户明确要求 Unity 迁移兼容、全面 ECS 和设计/代码/功能/架构等全过程留痕；
  具体工程约束整理于 `../agent.md`，不是已经通过验证的迁移实现。
- 创建阶段验证：工程根目录执行 `godot --headless --editor --path . --import`，退出码 0；
  `godot --headless --path . --quit-after 5`，退出码 0（工具可执行文件名依安装位置而异）。
- 未验证：图形窗口/GPU、导出包、Android/其他平台、C#、Unity / 团结兼容性和实际迁移。
- 提交/PR：发布时通过提交消息 `CHG-0001` 关联；尚不填写未知哈希。
- 遗留：实际开发前确认未来 Unity / 团结版本、目标平台及 C# 可行性；本次不开展玩法。

## CHG-0002 — 隔离 C# 工具链与 Linux / Android 构建验证

- 日期 / 状态：2026-10-30；隔离探针验证完成，游戏项目未转换。
- 需求与目的：按授权准备后续 C# 开发依赖并验证基础编译/导出能力，保留开发全过程证据。
- 设计与边界：全部安装、探针源码、缓存、模板和产物位于仓库外；系统标准版 Godot 保留。
  探针仅调用纯 C# 加法函数，不是 ECS 或游戏功能；不改变既定迁移路线。
- 仓库变更：`../README.md`、`../agent.md`、本日志和新增 [toolchain.md](toolchain.md)；仅文档。
- 验证：Godot .NET 4.6.3 + SDK 10.0.403；核心 `netstandard2.1` / C# 8，宿主 `net10.0`。
  编译零警告/零错误；headless 宿主与 Linux 导出运行均输出 `BOUNDARY_PROBE=42`、退出码 0。
  Android ARM64 Gradle 调试 APK 导出退出码 0；v2 签名、16 KiB 对齐及程序集/原生库检查通过。
- 修复与差异：隔离探针补齐 solution、开启 ETC2/ASTC；按实际 4.6.3 模板要求补装 SDK 36、
  build-tools 36.1.0、NDK 29.0.14206865；没有擅自升级模板或隐藏其警告。
- 证据：仓库外验证目录的 `README.md`、`manifest.json` 与 `logs/`；脱敏摘要、版本和摘要值见工具链记录。
- 未验证与风险：没有设备/模拟器/GPU/正式签名/商店发布/Unity/团结/iOS/Web 验证；
  Android C# 仍属实验支持，模板 AGP/SDK 及引擎退出诊断见工具链记录。
- 提交/PR：通过提交消息 `CHG-0002` 关联，不填写未产生的哈希。

## CHG-0003 — 技能扩展解耦与防回归开发要求

- 日期 / 状态：2026-10-30；规范已补充，未实现技能或玩法代码。
- 需求与目的：用户要求代码注意解耦，使后续新技能易于加入，并避免原有功能回退或 bug 回归。
- 变更：`../agent.md` 第 3.4 节和本日志；保留 CHG-0002 工具链记录及其验证边界。
- 设计与决策：采用独立技能定义/配置、稳定接口、可组合 ECS 数据/系统及显式注册；
  禁止按具体技能堆积中央分支和跨技能硬编码调用；继续隔离引擎适配层。
- 质量门槛：明确事件契约、确定的时间/种子/顺序、生命周期清理、效果叠加与冲突策略；
  合并前要求旧功能基线、新技能、新旧交互与边界场景的实际回归证据；涉及数据时验证向后兼容和回退。
- 验证：已检查文档 UTF-8、Markdown 围栏配对、尾随空白、关联章节与既有记录保留；仅文档变更。
- 未执行/不适用：未运行技能回归或编译游戏代码；当前没有技能实现，不能据此宣称玩法已经通过测试。
- 风险与边界：规范用于降低回归风险，不保证零 bug；不提前建设框架，不新增依赖，不改变工具链或引擎配置。
- 提交/PR：通过提交消息 `CHG-0003` 关联，发布前不填未知哈希。

## CHG-0004 — 完整设计归档、扩展约束与规范入口合并

- 日期 / 状态：2026-10-30；完成文档整理与源内容核对，未实现游戏玩法。
- 需求与目的：用户要求将实际 HTML 的完整设计转为 AI 可读文档，补充技能树、道具机制、角色扩展的解耦 tips，
  完成后提交；另要求合并大小写近似的规范文件，避免多入口混淆。
- 来源：实际 v0.9 `dist/index.html`；其 Site 源提交与 SHA-256 记录在 [game-design.md](game-design.md)。
  Site 提交不是本游戏仓库提交；原型演示不等于 Godot 实现或最终产品规格。
- 变更：新增 `game-design.md`、`design/catalog-v0.9.md`、`design/catalog-v0.9.json`、
  `design/simulation-v0.9.md`、`design/html-v0.9-reference.md`；更新 `../README.md`、本日志。
- 规范合并：实际原文件为 `agent.md`（完整规则）与 `AGENTS.md`（入口），未发现单数大写 `AGENT.md`。
  完整规则及新增设计入口/第 3.4 节扩展要求合并到 [../AGENTS.md](../AGENTS.md)，删除冗余 `agent.md`。
  本日志早期条目中的旧文件名是历史记录，不再表示当前入口；内容与历史可由 Git 追溯。
- 设计覆盖：五角色、全部 21 张卡、4 道具、6 类敌人提案、成长/经济/碰撞/伙伴/拾取/波次规则、
  美术资产定位、交互状态、原型参数、已确认方向/提案/未实现边界，以及旧方向替代关系。
- 解耦与质量：沿用纯核心 ECS/引擎适配边界，补充角色/技能树/道具稳定定义与 ID、注册与效果接口、
  事件/schema 版本、内容组合回归矩阵；保留旧功能基线及回退要求，不提前搭建大型框架。
- 验证：独立执行原 HTML 模型并比对 JSON，21 卡与 4 道具逐对象完全一致；原 HTML SHA-256 一致；
  三个内联 JS 块均在参考附录完整保留；HTML 解析得到的 244 个非样式/脚本文字片段全部可在附录找到。
  已人工核对主设计与模拟说明的规则、提案状态和未实现声明，并检查 Markdown/JSON 格式及本地链接。
- 未执行/不适用：本次不重跑引擎/设备验证，不将 HTML 制作阶段的测试摘要声称为本仓库 CI；
  未提交图片 base64、大型产物、SDK、签名材料或任何游戏实现。
- 提交/PR：通过提交消息 `CHG-0004` 关联；未填写尚未产生的仓库哈希。

## CHG-0005 — 归档最新局内与 UI 概念图

- 日期 / 状态：2026-10-30；完成参考素材整理与本地核验，待发布者核验实际 Git 提交。
- 需求与目的：用户要求把最新几张概念图上传 Git 作为参考依据；仅参考资产与文档变更，不实现游戏。
- 变更：新增 `art-reference/` 下四张原始 PNG、README 索引和 `manifest.json`；更新根 README 与 `game-design.md` 的入口/最新方向说明。
- 设计记录：战斗采用最新 combat-v03，远镜头大地图、连续无方格地面、伙伴无血条且怪物不攻击伙伴；保留蓝色棉队、亮黄色蜜团、机械入侵者。用户随后明确通过四张图的 UI 提案并要求一并提交；横屏/摇杆/闪避按钮视觉布局已接受，闪避行为参数与图示数值仍待确认。
- 来源：用户审阅的本次实际 AI 生图产物；Library 原身份、图版本、日期、尺寸与 SHA-256 逐项记录在 manifest。旧 combat-v01/v02 不复制，其他三张保持 v01。
- 验证：在 staging 目录用 Python/Pillow 检查四 PNG 可解码及 1672×941 尺寸；逐字节比对原图与副本并核对 SHA-256、字节数；检查索引/Markdown 本地链接与 JSON，人工复核最新战斗图。总计 8,912,487 字节，原分辨率未改动。
- 未执行 / 不适用：未运行 Godot、游戏回归或设备性能测试；图片不是实机截图，不证明 UI 或机制实现。未更新 Site/HTML、项目配置、游戏场景或脚本。
- 提交 / PR：以提交消息 `CHG-0005` 追溯，远端提交与 CI 状态由发布者实际核验，不预填未知哈希。

## 后续条目模板

```markdown
## <ID> — <标题>
- 日期 / 状态：
- 需求与目的：<来源、验收标准或功能记录链接>
- 设计与决策：<理由、备选方案、ADR；不适用则说明>
- 变更：<实际路径与内容，包括文档/配置/依赖>
- 验证：<代码基线、环境、工作目录、实际命令/输入、结果及证据>
- 未执行/失败/阻塞：<范围、原因与下一步条件>
- 迁移影响与风险：
- 提交/PR：<真实链接或 ID 反查方式>
```

## CHG-0006 / FEAT-0001 — Recovery and portable playable slice (2026-10-31)

- User authorized an isolated development branch, parallel original Blender assets and code, tested incremental commits, ECS architecture, and a decision log for morning review. No main merge.
- A cloud reset destroyed the unpublished implementation, tests, exports and local evidence. Remote staging retained only four changed files; the development branch remained at baseline. The previous M1 is not represented as published or recoverable byte-for-byte.
- Reconstructed portable C# ECS and Godot input/presentation adapter using retained contracts/source and authoritative design; recorded ADR-0001 before reconstructed implementation. Added DEC-001…017 review log and art appendix.
- Fresh core/host build, headless import and actual managed smoke passed with zero compile warnings/errors at the first recovered build. Behavioral regression and native UI checks remain separate gates and are recorded in milestone evidence when executed.
- First immutable WIP source backup saved to Library before publication. WIP backups explicitly do not certify tests. Future release snapshots must include exact SHA256 manifests and fresh evidence.

- Native recovery QA found selection portraits too dark with the low-energy arena lighting applied to their isolated preview worlds. Restored preview-only directional0.8/ambient0.4; arena remains0.25/0.12. Fresh final preview verification pending. M1 evidence wording refers to an adjacent source manifest; actual publication manifest was external to the evidence directory, so interpret it as the release archive's `publication-manifest.json`, not a missing evidence file.

- Native first recovered wave reached12coins and correctly produced trait/base/risk choices. Review found card descriptions overflowing their panels; wrap mode is now initialized before assigning label size, with explicit detail areas for full item downsides. Retest pending; do not call the earlier screen visually passed.
- Independent host review found keyboard focus could escape the codex into background shop actions. Added explicit background focus/button lock and restore, plus centralized close synchronization. Native repeated-open/Tab/Enter/focus-loss checks are required before final signoff.

- Final selection fidelity review identified the key light behind model fronts. Rotated only the isolated portrait key by180degrees (yaw−28→152), preserving energy and all arena/gameplay settings. New native portrait/input followup pending; old selection screenshot is superseded for this visual check only.
- Aggregate import caught evidence images labeled PNG that were actually JPEG from the native screenshot service. Corrected extensions without altering image bytes and excluded docs from Godot import with `.gdignore`; rerun core/build/import/smoke passed. This was a packaging/evidence import failure, not a gameplay exception; it was not suppressed.

- Fresh Linux export and actual standalone smoke passed twice, including a repeat-export regression with deliberate JSON and scene sentinels under `builds/`. Engine resource probing confirmed those prior outputs were excluded while gameplay JSON remained packed. Repository preset now uses `builds/HmiGame.x86_64`; all 197 exported dependency files are required for distribution. No Android result is claimed after reset.

- Final native followup passed corrected portrait palette, mouse joystick right/up movement and recenter, mouse dodge cooldown while moving, actual defeat with5kills/5coins, and restart to100HP/0coins/wave1. Independent review and actual JPEG screenshots are retained. Native full six-wave victory, hardware touch, resizing and audio remain unrun; no device certification is claimed.


## CHG-0007 / FEAT-0001 — Tested reconstructed desktop slice

Final clean source-snapshot aggregate passed 538,761 assertions, zero-warning core/host builds, asset validation, clean import and managed smoke. Independent native review passed selection/palette, keyboard and mouse movement, moving dodge, upgrades/shop ledger, next-wave buffs, modal/focus interruptions, defeat and reset. Fresh Linux first/repeated exports and actual standalone smoke passed; packed-resource sentinels verified output exclusion. Native full victory, hardware touch, audio, resize, Android/device and Unity remain unrun. Exact source/art/evidence are packaged for durable delivery before Git publication; no main merge.

## CHG-0008 — Decision audit reconciliation (documentation only)

After durable M2 delivery, corrected stale initial test-pending labels in the decision audit to match the actual final evidence. Added a Chinese per-decision review table using the same DEC-001–024 IDs and linking the existing Chinese art appendix. User approval remains pending; no runtime source, balance or frozen M2 artifact changed.

## CHG-0009 / FEAT-0001 — Post-release native full-run evidence

Independent reviewer completed a normal-control honey/seed1030 six-wave victory with actual card/shop choices:136 kills/earned,42 spent,94 remaining. No cheats or runtime source changes. Added the actual screenshot and exact choice/purchase path as supplemental evidence; immutable M2 is unchanged. This expands coverage for one representative native path, not all-build balance/device certification.

- Full-run followup also checked victory/selection at four desktop window sizes up to1359×750; readable and unclipped. No smaller-phone/portrait claim. Supplemental evidence is versioned separately from the immutable M2 archive.

## CHG-0010 — Android packaging followup

Using an isolated frozen-M2 source copy, fresh Android managed publish and approved Gradle export produced an ARM64 debug APK. Signature, ZIP alignment, package metadata, assembly/data/native payload and permissions were inspected; no device run was performed. Preserved the optional debug export preset and sanitized evidence as documentation, without changing delivered M2 runtime or default Linux preset. Original98MB APK storage succeeded but native attachment delivery exceeded the size limit; delivery packaging is separately verified and must not be called delivered prematurely.

## CHG-0011 — Export notice preservation

Caught missing font license in the binary export before APK delivery. Added complete runtime notices and explicit export inclusion, verified actual Linux PCK and corrected Android r2 APK bytes, and introduced a notice/signing-field packaging regression. Removed transient debug signing settings from the documentation preset before any artifact save. Runtime C# and gameplay/assets are unchanged; this corrects distribution metadata rather than changing the frozen M2 archive. Android device tests remain unrun.

## CHG-0012 / FEAT-0002 — Owner-requested visual correction (in progress)
2026-10-31: owner rejected APK UI fidelity, static movement and model detail. Started isolated revision from frozen M3; prior functional tests are not visual acceptance. Re-viewed all four approved references. UI, refined animated art and thin host presentation proceed separately. Owner subsequently withdrew dodge; remove all active input/core/UI paths, not merely hide button. See FEAT-0002 and DEC-027–029. No new release or visual pass claimed; actual screenshots/video and regressions pending. Frozen M1–M3 publication remains independent.

### CHG-0012 follow-up — Visual gate failed by owner
2026-10-31 02:24 UTC: owner rejected the revised model screenshot again. Current source retains technical UI/no-dodge/animation changes and passing tests, but model visual acceptance FAILED. Candidate APK is local only, not promoted or delivered. Next art scope restricted to one blue hero static multi-view shape approval before animation/other heroes. See DEC-033; do not reinterpret earlier functional greens as art acceptance.

## CHG-0013 / BUG-0001 — Repair presentation generation ownership
Actual normal native wave2 testing exposed an actor→pickup slot-reuse bug despite core/clip/layout greens. Added an isolated actual-engine regression, demonstrated failure before fix and pass after clearing the old presentation object on every generation replacement. Verified inverse transition, stale generation events, expanded-companion wave transition, pause and restart. Full aggregate passed538343 assertions plus engine lifecycle on the corrected source. A shared-editor Android-setting shutdown diagnostic initially blocked the aggregate; validation now isolates editor config per run instead of ignoring errors. Native replay is still required; rejected model visuals and held r3 APK are NOT promoted by this fix.

### FEAT-0002 / DEC-035 — Single-blue study delivered; approval pending
2026-10-31 02:50 UTC: parent delivered actual Blender front, side and three-quarter views of the isolated blue棉队 v03 r9 study (message Sentinel_85b28d6ec5e88191ba4159e4a7ba0a21). These are directional shape-study renders, NOT game screenshots or approved final art. Remaining ear/scarf/plush-material gaps were disclosed. Await explicit owner shape feedback/approval; do not animate, integrate or propagate to other heroes. No runtime changes. Git commit request remains separately pending formal publication approval; no remote commit claimed here.

Delivered image provenance: front libfile_b9da9a1978108191854bf459eb70e4d6; side libfile_6713bd75b9a88191beb3f45322db03f9; three-quarter libfile_25447a86a95c8191903b5d10133f168f. Study source backup libfile_d475670ce3f081919e7182628f7bce96 version2 remains separate from rejected runtime candidate.

### FEAT-0002 / DEC-036 — New cover reference requires actual fluffy detail
2026-10-31 02:54 UTC: owner supplied a new cover PNG (Library libfile_5a793255fd848191ba7a414a9bb29252) and explicitly requires matching its fluffy appearance with preserved detail. A smooth-toy approximation is not an acceptable substitute. Art worker owns supported materialization/pixel inspection and one blue棉队 fur-detail study. The image is not yet approved runtime material or evidence of engine compatibility. Existing single-character shape/material approval gate remains: no animation, integration or other heroes before explicit owner approval. UI/core functional evidence remains distinct from art acceptance.

### FEAT-0002 — Whole plush-toy requirement clarified
2026-10-31 03:34 UTC: art worker relayed owner's “要像个毛绒玩具”. Treat overall stuffed shape, soft transitions and material as the requirement, not merely adding strands to rejected plastic geometry. No runtime changes or gate relaxation. Lead is preparing a read-only integrity check of the frozen offline study after release; aesthetic approval remains the owner's decision.

### FEAT-0002 — Independent offline fur integrity and consistency
2026-10-31 03:45 UTC: lead reopened immutable Blender study read-only. Detected/reported inward nose winding and different tail-groom placement between separately generated view runs; artist repaired normals and created canonical source directly from the first four-view .blend. Final canonical SHAa810230dc84f7a20664157be2c4ddd516f30a2fadccc6d842002c864d5193242 passes finite/closed-normal checks,10grooms/537200curves/2686000points matching manifest, no animation/armatures. All10 groom fingerprints exactly match first four-view source, only nose winding differs. Ground/smile/tongue are open surfaces, not falsely certified closed. Source unchanged by checks. Evidence under FEAT-0002; owner plush-aesthetic approval still pending, no runtime integration.

### FEAT-0002 — Canonical plush candidate durably preserved
2026-10-31 03:49 UTC: offline single-blue candidate source/five views/review reports saved to Library libfile_5f7a68f81f3c8191a241abfd0a2149c3 version2, SHA3c758f7d9871cf47c733fa2d94a8a1384a0605c5eb151e9e1e1a236f47e9689d. Await owner appearance approval; no runtime source/model changes. Parent holds preview image IDs; image delivery/approval is not inferred by the implementation lead.

### FEAT-0002 / DEC-037 — Owner approves Cotton appearance, requests animation/VFX preview
2026-10-31 03:50 UTC: parent relayed explicit owner approval of the single Cotton r5 shape/coat and request for animation/VFX preview. This lifts the appearance gate ONLY for this character's offline animation study. Art worker owns actual Blender Idle/Run/Attack/Summon with restrained particles, no Dodge. No other heroes authorized through this approval. Animation/VFX review is now pending; game integration, runtime fur representation and mobile feasibility remain separate unpassed gates. Existing rejected runtime batch is not approved retroactively.

### 2026-10-31 05:32 UTC — Cotton offline preview preserved, owner review pending
Actual offline animation/VFX MP4 and all144 frames completed by art worker, with persistent native fur binding and independent saved-source/transition/stance checks. Final source Library libfile_1b02d587ce3c819180692055eceea78f v6; MP4 libfile_ea98c16f76f88191b22c6291e1f93fc8 v0; contact sheet libfile_6f7c7b61b7748191a18d6cc3991ecd07 v0. Exact hashes and remaining gates recorded in docs/evidence/FEAT-0002/owner-review-gate.md. No runtime code or model import performed; user animation approval and runtime/mobile feasibility remain separate pending gates.

### 2026-10-31 06:11 UTC — FEAT-0003 isolated 2D selection revision
Owner rejects stiff Cotton motion and requests 2D selection art. Created HMI-Game-2d-selection from prior tested visual revision, preserving frozen M1–M3 and the prior tree. Added feature/test plan before implementation; selection portraits now TextureRects, combat HUD previews lazy/cleaned, selection 3D pass disabled. Host builds with0warnings/errors; final assets/import/aggregate/native visual gates pending. Pure core unchanged. DEC-039/040 record scope and rendering tradeoffs; no remote commit claim.

### 2026-10-31 06:30 UTC — FEAT-0003 scoped technical gates passed
Fresh full aggregate passed538343 core assertions plus new actual-engine selection/resource-lifecycle probe; no core changes. Independent native review passed all five portraits, identity/summary/badge, keyboard focus/Enter, real3D combat restoration, pause/restart and repeated-run/modal behavior. Actual screenshots and source hashes are preserved under docs/evidence/FEAT-0003. Owner visual acceptance, new exports and real-device performance remain unrun/pending; old pink in-combat HUD model crop remains disclosed. Final source snapshot is separate from frozen M1–M3 and prior visual revision. No remote publication verified.

## FEAT-0004 / CHG-0014 — Recover tested source and isolate food-hero integration

- 2026-10-31: owner authorized five food heroes and game integration. Recovered Library v1 archive with exact SHA256 and clean ZIP CRC. Preserved immutable recovery baseline; created separate local `dev/food-heroes-20261031` history. The baseline import commit is provenance, not a new validation or remote publication claim.
- Read AGENTS, existing ECS/presentation contracts and tests. Added ADR-0002 and runtime contract before adapter changes. Core and gameplay data remain unchanged; no Dodge or new mechanics.
- Toolchain restored separately from official pinned Godot.NET4.6.3/SDK10.0.403. Initial adapter compilation passed with zero warnings/errors; full fresh aggregate and new lifecycle/morph tests are pending.
- Runtime hero assets are not yet delivered. Existing assets remain only to exercise the adapter baseline; no claim that they represent the new food heroes. Native visual, export, mobile and Unity gates remain unrun for this increment.

### FEAT-0004 stage1 gate (17:13 UTC)
- Fresh full aggregate passed on final adapter: core538343, synthetic actual-engine presenter482, contract-unit8, host scope/catchup/pickup/wave2/scene-exit and selection regressions.
- Independent native old-asset movement/pause/defeat/restart reset passed; environment dummy audio/VSync warnings disclosed. This does not accept new food assets or close-up motion quality.
- Original event run/wave/tick is preserved in pending gestures; no re-stamping at render. Core/gameplay bytes unchanged.
- New runtime food GLBs remain outside this milestone; first Kaotuan import trial is isolated and artistic motion defects are being corrected separately.
- Local stage1 commit is authorized after these gates; remote publication requires publisher verification.

### FEAT-0004 / CHG-0015 — B1 food portrait and delivery-contract increment (17:37 UTC)
- Local commits `02d88e3` and `cdfe21c` add five approved food-character AtlasTexture front views, display-only names, safe inner margins and required-model fail-fast. Stable IDs, all Core source and mechanical content fields are unchanged. Missing resources no longer become a silent sphere.
- Fresh aggregate passed: Core 538343, presentation 482, static contract 9 fixtures, host lifecycle and selection identity/seed/focus/restart checks. Independent final native padding screenshot is preserved under `docs/evidence/FEAT-0004/stageB-preasset/`.
- Immutable B1 snapshot and manifest were created outside the work tree; durable source is Library `libfile_9b02ddeab7b48191a715fcaf9e2ff637` version 1. No remote publication is claimed. This follow-up log records the already-frozen increment without mutating its manifest.
- **Old in-game 3D heroes remain temporary.** B1 is not five-food-model completion. Kaotuan r6/r8 structural imports passed, but actual-engine forehead normal artifacts and Smile crease blocked asset integration. Removing COLOR attributes did not fix the visible artifacts; this failed hypothesis is retained.

### FEAT-0004 / BUG-0004 — Runtime baked-normal tangent mismatch (17:40 UTC, candidate)
- Same-source r9 explicitly exports tangent and morph-tangent data. Independent actual Godot comparison shows forehead dark lines disappear while fine normal detail remains; Smile crease persists and is a separate shape/normal issue.
- Tangent seams increase vertex count from 25046 to 25093. All 89547 indexed triangle corners retain byte-identical non-tangent positions, normals, UVs, joints, weights and morph attributes; animation, node, inverse-bind and texture bytes also match. Do not incorrectly claim accessor-array byte identity across the split.
- Added normalTexture → explicit matching float VEC4 tangent contract, with positive/negative tests: 11 static unit fixtures pass; r8 fails the new gate, r9 passes. Actual engine import of r9 passes, but final full asset visual/morph gate remains blocked. No new GLB has been copied into the game.

### BUG-0004 correction — 17:49 UTC
The r9 visual-fix attribution above is **withdrawn**. Independent reviewer rechecked the recorded frames: the apparently clean r9 frame had Smile≈0.991, whereas the failing frame had Smile=1.0. A strict r9/r10/r11 × neutral/Blink1/Smile1 matrix shows the forehead lines in all full-weight Blink/Smile cases; neutral is clean. r10's long Smile crease is independently repaired. Preserve this history rather than hide the failed comparison.

GLB base+delta normals remain unit length. Actual imported foreground head normals/tangents are unit length and preserve handedness at all three morph extremes; there is no observed forehead vector cancellation in the accessible arrays. Further renderer/import/mesh-localization diagnosis is pending. The official historical Godot issue [42620](https://github.com/godotengine/godot/issues/42620) describes a similar full-weight normal-map symptom on Godot 3, but it does not prove the cause or a fix on this project's Godot 4.6.3. No engine binary, lighting or facial maximum has been altered to hide the issue.

An optional skeleton-bake geometry probe failed under headless dummy rendering because its skin was not registered, then passed on the actual native rendering path. Separate CPU reconstruction of imported skin/bone matrices also passes finite/ground checks at 18 sampled clip positions. These scopes are distinct; neither certifies all collision/intersection poses or mobile rendering.

### FEAT-0004 / BUG-0005 — Atlas channel and imported instance regressions (18:01 UTC)
- Actual engine rendering exposed Juangjuan r2 using the source parametric UV as default instead of its baked atlas. Added a single-UV0/material-texCoord0 contract and positive/negative fixtures. Thirteen structural tests pass; actual r2 correctly fails and r3 passes. r3 still has separate facial visual defects and is not integrated.
- Extended the production-asset import probe to instantiate two copies of the actual PackedScene, mutate only one facial state, verify no cross-instance leak, queue-free and recreate with neutral weights. This passes Kaotuan r14/r15 and Juangjuan r3, not a claim about the three undelivered heroes.
- Kaotuan r14's precise triangle partition removes the unaffected forehead from the morph mesh. The independent equal-weight five-state native matrix now clears the forehead/long-crease defects without changing light or capping weights. The first Blender separation candidate r12 failed the region goal because floating-point residuals around 1e-7 m selected the forehead; its normals also changed. r14 preserves exact original indexed-corner base attributes. r15 compacts only referenced rows and remains byte-identical when expanded to all indexed base/morph corners. Dynamic facial vertices fall to 1396; total stored vertices are 25136, triangles 29849, two surfaces. This is a resource count, not a measured frame-rate result.
- Six zero base tangents on boot/bell remain a separate local correction candidate; final gameplay motion/appearance and performance gates remain pending. No food GLB has been copied into the product tree.

### FEAT-0004 / BUG-0006 — Food hero HUD portrait headroom (18:10 UTC, pending native recheck)
- Actual Kaotuan r16 host screenshot exposes the toast crown clipped by the old bust camera. The asset remains at the same scale and collision size; only the HUD camera moves to a level 0.88 m look height with a 0.86 m orthographic span.
- Added actual-engine projection assertions for the 1.2 m contracted height plus 4 cm idle allowance, horizontal head envelope, all five selections and repeated start/restart cleanup. The isolated selection probe passes. This is a framing envelope regression, not proof that every future accessory/animation fits.
- Original candidate remains frozen while independent normal-play wave tests run. A separate r16 HUD candidate is running the aggregate; actual new pixels remain pending before local commit.
- Terminal candidate aggregate passed: Core 538343, presentation 482, contract 13 and build/import/smoke/host/selection gates. Independent actual `r16-hud-fixed.jpg` now shows the complete toast crown with margin. This clears the HUD framing bug only; normal Kaotuan play attempts ended in first-wave defeat, so wave-two native validation remains pending. Side-view texture seam investigation is also separate.
- Main tree (still legacy in-game hero assets) also reran the entire aggregate successfully after the same camera/test changes. Both scopes are logged separately under `docs/evidence/FEAT-0004/hud-framing`; no new food model is hidden in this UI-only commit.

### FEAT-0004 / BUG-0007 — Non-tiling atlas sampler contract (18:19 UTC)
- Actual raycast/UV diagnosis found an outer-atlas seam sampling across the default repeated boundary. Added explicit clamp-to-edge on both axes, resolved texture/image references and four negative fixtures; 17 structural tests pass. Existing non-clamped Buding r2 correctly fails the new gate.
- Independently compared Kaotuan r16→r21, Juanjuan r3→r6 and Buding r2→r3: all BIN bytes remain identical. JSON changes only sampler wrap fields and buffer length metadata accounting for existing padding (+1/+0/+3 bytes respectively). All three new candidates pass static and actual imported clip/morph/instance-isolation probes.
- Actual visual result is partial, not a blanket fix: Buding's fixed waist line is gone; Kaotuan's side line is reduced but not entirely gone; Juanjuan's mouth line is unchanged. These assets remain isolated candidates. No lighting change, re-bake or expression cap was used to conceal the residuals.

### FEAT-0004 B2a — First two runtime food heroes (18:35 UTC)
- Replaced only `hero_summon.glb` (Kaotuan r23) and `hero_honey.glb` (Buding r3), retaining stable skill IDs and all Core/gameplay bytes. Other three in-game heroes remain legacy pending their gates. Resource/provenance manifests state the mixed scope.
- Final main-tree aggregate passes: Core 538343, presentation 482, contract 17, actual per-food imported clip/root/morph/instance-isolation tests, lifecycle, selection, build/import/layout/smoke. `verify.sh` now runs the actual food probe for each staged food asset even without an all-five completion manifest.
- Independent normal Buding run passed wave1 (19 coins), wave2 (41 coins), free choices, gloves 5 coins with duplicate prevention, wave3 (100 HP/36 coins), restart and clean Kaotuan model/trait/state switch. r23's final host screenshot and movement/HUD check passed after effective-content-equivalent compaction. This does not establish all-five balance, audio, device GPU performance or commercial readiness.
- Native/source derivatives are preserved separately with exact Library identities. Included four portable postprocess tools and dependency version record, not giant Blender binaries. Independently rebuilt both from raw exports: Buding whole-file SHA matches; Kaotuan JSON values and BIN match, serialization order differs. This distinction is retained in evidence.
- The 211-texel coverage correction cleared Kaotuan's remaining side seam under controlled actual rendering. r23 has no geometry/rig/morph changes from the visually cleared r22. Generated extracted atlas PNGs are ignored; actual imported r23 pixels exactly match the embedded authority.

### FEAT-0004 B2 — All five runtime food heroes (18:44 UTC)
- Added the remaining Feiling r2, Juanjuan r9 and Cuilan r2 derivatives with exact per-asset native/high-source/Library provenance. Added the all-five manifest so aggregate validation requires the complete roster. No Core, CoreTests or gameplay catalog bytes changed from B1 (explicit Git diff exit 0).
- Re-ran final main aggregate successfully. Independent production imports test real clips, fixed Root, facial extremes and per-instance state/free/recreate. The native review's four adapter and five model hashes exactly match the final source tree; source-match evidence is stored.
- Actual final host cycle across all five clears model/identity/reset/leaked-trait state. Exact scope, prior multi-wave economy evidence and remaining presentation/performance limits are listed in FEAT-0004; no single one-second hero visit is represented as a completed wave or peak attack test.
- Rebuilt all five from declared raw exports using the committed portable chain: four whole-file SHAs match, Kaotuan JSON values/BIN match with only serialization-order differences. High-density Blender sources remain durable external artifacts, not repository noise.
- Documentation-only boundary correction (18:47): the plan now separates completed scoped B2 integration from unfinished B3 resource-performance/high-rate animation work. The obsolete initial “four heroes still being modeled” risk is retained as history and followed by the current state; no implementation or test result changed.

### FEAT-0005 C1 — Compiled schema2 skill graph (2026-10-31)
- Recorded ADR-0003 before implementation; retained the coordinator's preparation documents as design provenance, not executed-test evidence.
- Added compiled all/any ranked prerequisites and exclusive groups, joint assignment reachability, bounded load work, conservative per-assignment stat bounds and terminal finite-pool validation. Legacy parent syntax/empty-parent acceptance and the production catalog remain unchanged.
- Added public typed card availability; runtime queries use compiled indices and existing ECS progression command authority. No adapter/UI or new trigger implementation is included.
- Fresh final pure-core suite: PASS 538393, with frozen-DLL legacy fingerprint matching four compatibility paths. Maximum synthetic load and zero-allocation warm queries are measured in FEAT-0005/evidence; device/Unity/visual checks remain unrun for this stage.

### FEAT-0006 C2 — Eight differentiated deeper branches (2026-10-31)
- Recorded ADR-0004 before changing production content. Added three paired exclusive decisions and two cross-trait fusions using existing typed stat domains only; original21 cards/four items/five heroes are semantically unchanged.
- Preserved exact prior schema1 bytes in a test fixture. The complete legacy suite still runs on it, while production-vs-fixture comparison prevents a self-referential compatibility claim.
- Fresh PASS546285 includes independent prerequisite/tradeoff checks and250 synthetic finite selection-state runs. All eight new IDs appeared in offers, counts12/5/4/5/9/5/1/1; these are not real combat-survival or balance results.
- Retained the first failed test expectation (movement risk multiplier omitted), corrected only the expected composed delta, then re-ran all tests. Core source remained unchanged from C1; UI/device/Unity validation remains separate.

### FEAT-0006 C2 UI — Authoritative graph codex (reconstructed 2026-10-31)
- Isolated presentation reads core availability/current levels and renders named ALL/ANY/rank/exclusive constraints without duplicating acquisition rules. Legacy effects remain verbatim; no Core/catalog rule change.
- Approved actual card geometry and line-spacing fix preserves full descriptions; a real-control probe complements the independent33-string font gate.
- Preserved the prior font subset and added runtime glyph coverage with unchanged OFL notice. Desktop fallback is not proof of packaged coverage.
- Workspace reset occurred before the original CUI commit; this reconstruction was first backed up as explicitly unvalidated WIP. Fresh final test/native evidence, not historical counts, is required before commit; see docs/evidence/C2-UI.

- Fresh reconstruction closure: formatter594, core546285, complete aggregate and actual-control23 passed; independent native fixture24 plus sampled codex glyph/constraint, close/pause, real OS focus-loss and restart gates passed on matching source hashes. Full evidence and explicit fusion-art/unit/device limitations are retained in docs/evidence/C2-UI. No inherited historical green claim.

### FEAT-0008 DUI — Explicit endless continuation and temporary-slot presentation
- Integrated read-only frozen D2 core/content/tests 05c96916 on CUI 0298212. Explicit scoped Continue/Finish intents preserve finite completion and ledger; endless intermissions expose voluntary finish. No UI gameplay calculations.
- Typed slots retain all three categories, authoritative enabled state and full descriptions; temporary duration and pending/active names are visible. Codex now explains RiskLimit and catalogs temporary contracts separately from permanent ranks.
- Fresh formatter655, core624431, existing aggregate, codex23, endless37 and39-string typography gates pass. Exact source provenance and testing limits are in docs/evidence/DUI; native acceptance remains pending until recorded there.

- DUI scoped closure: independent native fixture42 and actual Continue→wave7 passed; normal Buding1030 cleared five waves then died in wave six (115−33=82). Ten-to-thirty-wave enjoyment and manual Between Finish remain unverified. Fixture XServer BadValue is retained; normal Dummy-audio launch did not reproduce it. No balance change or blanket native-zero-error claim.

### BUG-0002 — Synchronize ECS health limit at timed stat expiry
- Independent reproduction showed a1-second custom shield expired Stats.MaxHealth125→100 while the Health component stayed125. Shipped duration0 shield was unaffected, but the accepted data contract allowed this mismatch.
- Timed stat expiry now shares a health-limit synchronization helper with contract expiry: update maximum from final stats and clamp current downward, never heal on maximum restoration.
- Fresh own/independent PASS624451,20 focused new assertions, exact-source match. Covered pause boundary, injured/no-heal cases and timed shield with active temporary protection. No E checkpoint changes are included.

### FEAT-0009 — Prepared checkpoint Core
- Recorded ADR-0007 before implementing the explicit pre-init boundary. Added Prepared tokens, pure capture/validation, transactional existing-instance restore and one canonical Begin path. Legacy APIs wrap the same preparation/initialization flow and preserve golden behavior.
- Host-injected catalog hash plus store UUID/descriptor sequence binds detached DTOs. Restore validates complete staging resources/systems before swapping, increments run scope and preserves generation baselines; failed validation leaves exact authority and system-storage identity unchanged.
- Fresh own/independent exact-source PASS624665 includes all-six-contract pending-item reconstruction, five heroes, duplicate/stale token rejection, malformed DTOs, history/economy constraints and numeric handle limits. Retained the first inactive-offer-index mismatch; canonical prepared cleanup fixed it before the final run.
- Pure validation deliberately allocates staging memory (50 calls:6.50ms/9,440,216B in this run); combat warm ticks remain0B. File I/O, JSON bridge, Godot lifecycle and platform crash durability are separate integration gates, not claimed by this milestone.

### E host integration — in progress, 2026-10-31 20:21 UTC
- Combined frozen DUI, exact Core810ad65 (including timed-health fix), B3 reference hygiene and B4 bounded attack gestures in a separate local branch. Frozen milestone roots/Library identities remain unchanged. B4 ADR renumbered to0008 here only.
- Added separate net10 persistence project referencing engine-independent Core; Godot references the adapter, never the reverse. E1 physical directory creation is now lazy under the writer acquisition so permission errors reach explicit recovery UI instead of failing scene construction.
- Invocation-scoped `-m:1` avoids verified MSBuild out-of-process named-pipe permission failure in this environment. No global permission, proxy, trust or security configuration changed. Toolchain diagnostic evidence is separate.
- Core624665, Store111, Session144 and UI scoped gates are evidence for their own layers, not final host acceptance. First actual-engine host fixture passed22 checks; extended failure/UI gates and full aggregate remain pending.
- Real normal-run evidence does not establish10–30 wave endless fun/balance. No Unity, Android/device or production crash/power-loss certification is claimed.

## E integrated milestone — 2026-10-31 20:58 UTC
Unified aggregate completed exit0 after preserving and correcting the production catalog-hash seam and two legacy fixture assumptions. See `features/E-host-integration.md` and `evidence/E-Integration` for exact suites/source hashes; actual three-process resume/close and injected-failure UI are independent evidence. This local milestone includes B3 reference hygiene, B4 attack coalescing, timed-health regression and prepared checkpoint Core/store/session/host. It excludes environment/camera experiments and makes no remote, mobile, audio or long-endless-experience claim.

## BUG-0003 — 2026-10-31 21:14 UTC
Real third-run fractional HP exposed mixed HUD rounding. Separate one-expression presentation fix plus raw-bar/fractional regression and native screenshot passed full aggregate. E source/export remain immutable. Third normal run won finite six waves and died in endless wave7; this is not10–30-wave experience acceptance.

## PERF-0006 — Opaque menu renderer suppression (2026-10-31 22:06 UTC)
- Runtime delta is one known-page boolean expression in GameUi; HP camera/Core/persistence/input remain unchanged. No camera-trial code is imported.
- Actual native 16-pair RGBA comparison is exact, independently decoded; renderer counters avoid637 draws per pair. This does not claim mobile/frame-time benefit or lower combat cost.
- Normal production wave 1→free pick→shop purchase→wave 2→pause/resume/restart passes. New structural144-check probe preserves live actor/AnimationPlayer identity, clock and UI lifecycle and is wired into the aggregate.
- Full evidence hashes and one representative pair stay in Git; all 32 original A/B PNGs remain in the separate durable evidence artifact. See feature/evidence PERF-0006 for limits and retained fixture-red history.

## BUG-0004 — Save notice top-gap candidate (2026-10-31 22:30 UTC)
One presentation rectangle changes; full persistent text/font and authoritative persistence unchanged. Rejected left-top experiment preserved separately. Fresh real-control text/geometry gate and13 native pairs pass sampled scope; dense legal-grid diagnostic bounds remaining risk. Final aggregate exit0, residual-pixel and real-main success-state/pause/restart gates passed. Parent approved adoption; no camera-trial code included. Commit/export remain separate.
