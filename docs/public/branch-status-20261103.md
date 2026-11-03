# Branch status and succession
Snapshot: 2026-11-03 04:13 UTC. This index records current evidence and ownership boundaries; it does not merge, delete, rename, or rewrite any branch. Historical documents remain historical and may still say "pending" after a later review. A published checkpoint, passing scoped test, user-approved sample, and production integration are separate states.

## Baselines and absorbed work
- `main` remains at `61fd0a0`. It is an ancestor of the rule-3 integration snapshot; it has not received the later experimental samples.
- `dev/gummy-playable-20261030` (`f4aa1e4`) is an ancestor of `wip/rule3-integration-20261102` (`a838e35`). The integration is the later development baseline, not a release certification.
- `wip/stationary-launch-facing-20261102` and integration currently point to the exact same `a838e35` commit: stopped-launch-facing work is already present there. This does not solve moving-fire direction changes.
- `wip/mobile-ui-20261101`: selected implementation absorbed into integration. `src/Adapters/Godot/TouchJoystick.cs` and both MobileMotionIntegration/MobileSafeArea probe templates have identical Git blobs in both snapshots. The branch history diverges; this is content reuse, not a claim that its entire branch was merged or every hardware/UI test passed.
- `wip/feedback-observations-20261101`: the read-only `src/Core/CombatObservation.cs` blob is identical in integration. Related systems/tests evolved. The branch history diverges; do not re-merge it wholesale.
- `staging/m1-core-20261030`: historical staging checkpoint. Its history diverges from integration. Later development continues through dev and integration; wholesale absorption of this staging branch is not established by this audit.

## Rejected or returned experiments retained as archives
- `wip/impact-surface-rejected-20261102`: nearly invisible extra surface-impact geometry; simpler hurt feedback retained.
- `wip/ready-melee-aim-rejected-20261102`: weak gait readability and excessive whole-body target switching; not adopted.
- `wip/common-melee-fan-rejected-20261102`: attack shape not readable at normal scale.
- `wip/directional-gait-study-20261102`: side/back gait readability gain too marginal, especially backpedal. Successor investigation uses displacement-driven contact/flight.
- `wip/backpedal-weight-study-20261102`: torso-weight pilot had insufficient visible benefit; not the basis for declaring backward gait accepted.
- `wip/proportion-gait-skating-rejected-20261102`: static proportions accepted, animation rejected for skating and stride/speed mismatch. The displacement Run experiment succeeded this motion approach; the rejected animation remains archived.
- `wip/combat-vfx-20261101`: historical effects implementation is not wholly absorbed. Its CombatFeedbackVisuals/CombatSkillBodies experiments are not the current integration renderer. Integration instead includes rule-3 projectile flight and bounded hurt feedback; old effects must not be reintroduced merely because their branch still exists.

## Sample approved, awaiting production integration
- `wip/displacement-run-recovered-20261102` (`5c9251e`): user-approved forward Run sample. Displacement-based gait follows the rejected skating study. Approval is scoped to the reviewed sample, not a complete locomotion/aiming controller.
- `wip/displacement-backpedal-20261102` (`2474f6f`): user-approved backward sample. Continues displacement-based gait; production transitions and full moving-fire behavior remain separate work.
- `wip/displacement-sidehop-20261102` (`77c2b5c`): rightward synchronized side-hop sample approved on 2026-11-03. Leftward extension continues on the lateral branch below; approval does not automatically cover that extension.
- `wip/shadow-split-study-20261103` (`ad4aefb`): shadow B visual style approved on 2026-11-03. Harder shadows are an explicit appearance tradeoff. Bounded frozen landscape/portrait coverage is not played-route, all-camera, or Android/8155 certification. Runtime remains isolated and has not been merged into integration.

## Currently under validation
- `wip/displacement-lateral-20261103` (`e03e787`): successor to the approved rightward side-hop, extending the fixed-heading diagnostic to leftward movement. Missing StrafeL base-clip lookup was repaired by explicitly reusing the frozen StrafeR comparison clip. The ordinary start collided with an interior wall; the first relocated start collected haste and failed the fixed-speed landing gate. Both failures are retained. The latest bounded start avoids those fixture interactions and adds a native capture helper; fresh runtime/visual validation is pending at this checkpoint. Collision response and production direction transitions are not certified.
- `wip/ui-lifecycle-20261103` (`9594e8b`): successor investigation for outstanding mobile/UI lifecycle and teardown uncertainty, based on integration. First batch repairs only the test wave bound using WaveRules.DurationTicks. The strict responsive runner still fails on the known editor-shutdown diagnostic despite import exit zero. Fresh lifecycle checks, intermittent teardown reproduction, and native/device validation remain open.

## Evidence and interpretation
The remote branch refs and full file trees were read for this snapshot. Git compare confirms main/dev ancestry and staging/mobile-ui/feedback/combat-vfx divergence relative to integration. Exact blob reuse supports the narrow content-absorption statements above. User review status is recorded without private conversations, recordings, or operational logs. Recent publication checks found no Actions runs, check runs, or commit statuses for lateral, UI lifecycle, and shadow approval checkpoints; absence is not CI success. Existing scoped test claims retain their original limits.

## Remote head inventory
These are frozen observed heads, not promises that active branches will remain unchanged. The documentation branch itself is intentionally excluded.

| Branch | Observed commit |
| --- | --- |
| `dev/gummy-playable-20261030` | `f4aa1e452ecfab70bd235d635ced0bbfd3b1a629` |
| `main` | `61fd0a0724d5310e28a40430d1a1f6f00407136a` |
| `staging/m1-core-20261030` | `5208d5618674ac14492e9b4e138cc0008e4b10e9` |
| `wip/backpedal-weight-study-20261102` | `e682d82cf5b4e8f95f78adbdee5ab4386dabc11e` |
| `wip/combat-vfx-20261101` | `ce973f3f3e40bd49cc785c5e114543d4dda3bb5c` |
| `wip/common-melee-fan-rejected-20261102` | `e4e7e6351e8de39c3376594968022f661e189d12` |
| `wip/directional-gait-study-20261102` | `89f8b98a7f5bae2e9fcb93ae0d9a415b3a731d35` |
| `wip/displacement-backpedal-20261102` | `2474f6fe268bce4d1b44955e36f04d675e744e7c` |
| `wip/displacement-lateral-20261103` | `e03e787b930eb6b3a85e3b67e2ea6b08521a7407` |
| `wip/displacement-run-recovered-20261102` | `5c9251e34507a9292112a2a1ed835d8b1c2ee85f` |
| `wip/displacement-sidehop-20261102` | `77c2b5cdb12d1c196ee33a9f6b1da916b38b3f38` |
| `wip/feedback-observations-20261101` | `36c84628bbdd5b3bac4b40e352b5abc6a40af6b2` |
| `wip/impact-surface-rejected-20261102` | `a00bd51ae38632d67410df06b25b80be17480637` |
| `wip/mobile-ui-20261101` | `8650d6701d004909fe269bab3ecfcc39ffb0f1a4` |
| `wip/proportion-gait-skating-rejected-20261102` | `fb060866fccf28c31d68c2905249017a2da11e94` |
| `wip/ready-melee-aim-rejected-20261102` | `dab90e6b37a53d1f8417abed57df3d8fa90ba1b3` |
| `wip/rule3-integration-20261102` | `a838e3592f92b3de6507de710d56f5abe7513e7f` |
| `wip/shadow-split-study-20261103` | `ad4aefb5ff19b049e81b4aade9eb70111acee228` |
| `wip/stationary-launch-facing-20261102` | `a838e3592f92b3de6507de710d56f5abe7513e7f` |
| `wip/ui-lifecycle-20261103` | `9594e8b673debbe798777f2728067b2e6e59e82d` |
