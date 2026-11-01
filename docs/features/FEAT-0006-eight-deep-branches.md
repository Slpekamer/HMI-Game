# FEAT-0006 — Eight differentiated deeper nodes

Status: locally implemented and core-tested, prototype tuning; presentation integration pending. See ADR-0004. All effects are additive in existing typed domains; no new trigger. A radius reduction trades outward coverage for inward geometry, not unconditional DPS.

## `orbit.outer` — 外沿守望

需外轨拓展1级；每级星环半径+0.70米，近身空隙更大。与内环护身互斥，最多2级。

- All: [{"id": "orbit.wide", "rank": 1}]
- Any: []
- Group: orbit.geometry; cap: 2
- Effects: [{"stat": "OrbitRadius", "operation": "add", "value": 0.7}]

## `orbit.inner` — 内环护身

需外轨拓展1级；每级星环半径−0.55米，近身覆盖更密、外沿覆盖更小。与外沿守望互斥，最多2级。

- All: [{"id": "orbit.wide", "rank": 1}]
- Any: []
- Group: orbit.geometry; cap: 2
- Effects: [{"stat": "OrbitRadius", "operation": "add", "value": -0.55}]

## `crit.longwatch` — 长线瞄准

需远距寻路1级；每跳搜寻距离+1.20米，移动速度基准−4%。与近线游击互斥。

- All: [{"id": "crit.reach", "rank": 1}]
- Any: []
- Group: crit.mobility; cap: 1
- Effects: [{"stat": "RicochetRange", "operation": "add", "value": 1.2}, {"stat": "PlayerSpeed", "operation": "add", "value": -0.04}]

## `crit.skirmish` — 近线游击

需远距寻路1级；移动速度基准+6%，每跳搜寻距离−0.70米。与长线瞄准互斥。

- All: [{"id": "crit.reach", "rank": 1}]
- Any: []
- Group: crit.mobility; cap: 1
- Effects: [{"stat": "RicochetRange", "operation": "add", "value": -0.7}, {"stat": "PlayerSpeed", "operation": "add", "value": 0.06}]

## `bounce.longpush` — 长臂推移

需弹性填充1级；冲击范围+0.30米，击退初速度−1.00米/秒。与短距重推互斥。

- All: [{"id": "bounce.force", "rank": 1}]
- Any: []
- Group: bounce.leverage; cap: 1
- Effects: [{"stat": "ImpactRange", "operation": "add", "value": 0.3}, {"stat": "ImpactForce", "operation": "add", "value": -1}]

## `bounce.heavy` — 短距重推

需弹性填充1级；击退初速度+1.60米/秒，冲击范围−0.25米。与长臂推移互斥。

- All: [{"id": "bounce.force", "rank": 1}]
- Any: []
- Group: bounce.leverage; cap: 1
- Effects: [{"stat": "ImpactRange", "operation": "add", "value": -0.25}, {"stat": "ImpactForce", "operation": "add", "value": 1.6}]

## `fusion.sugaring` — 蜜环协防

需星环与蜂蜜领域，且蜜圈扩展或外轨拓展1级；蜂蜜半径+0.30米，星环半径−0.25米。内圈防守更密，外轨覆盖变小。

- All: [{"id": "orbit.root", "rank": 1}, {"id": "honey.root", "rank": 1}]
- Any: [{"id": "honey.wide", "rank": 1}, {"id": "orbit.wide", "rank": 1}]
- Group: none; cap: 1
- Effects: [{"stat": "HoneyRadius", "operation": "add", "value": 0.3}, {"stat": "OrbitRadius", "operation": "add", "value": -0.25}]

## `fusion.relaypush` — 弹冲接驳

需有限弹射与近距冲击、接力弹射2级；每跳搜寻距离+0.60米，击退初速度−0.60米/秒。不增加命中次数或连锁预算。

- All: [{"id": "crit.root", "rank": 1}, {"id": "bounce.root", "rank": 1}, {"id": "crit.chain", "rank": 2}]
- Any: []
- Group: none; cap: 1
- Effects: [{"stat": "RicochetRange", "operation": "add", "value": 0.6}, {"stat": "ImpactForce", "operation": "add", "value": -0.6}]


## Fresh tests and limits

`docs/evidence/C2/full-tests.log`: PASS **546285**. All legacy tests and frozen-DLL fingerprint remain on the exact schema1 fixture. Production schema2's 21 old cards, four items and five heroes are compared field-by-field against that fixture. Eight additions are tested for reachability, exact registered effects, real tradeoffs, mutual locks, ALL/ANY/rank boundaries and shared-melee stat isolation. Independent tests contribute66 assertions.

250 synthetic finite selection-state runs cover five heroes ×50 seeds. New-node offer counts in listed ID order:12,5,4,5,9,5,1,1 (42 total). Every new node appeared under these policies, but the two fusions are rare; this is not a balance/availability guarantee for real players. The probe directly completes waves and does not represent250 combat-survival sessions.

The first test run failed because the test's expected movement delta omitted the pre-existing `PlayerRiskSpeed` multiplier; the implementation and old stat order were not changed. The expectation now includes that multiplier, and the failed log is retained as `first-test-failure.log`.

Combat warm allocation remained zero in the existing honey and mixed-mechanism stress; cosmetic mixed-event drops remain2895 and do not govern damage. UI prerequisites/downside readability, Godot graphical integration, device/Unity/export and play-balance validation are not executed by this core-only stage. No new trigger, damage callback or engine dependency was added.

Rollback: revert C2 content and its production tests together before reverting schema2 support. The schema1 fixture SHA remains the prior production bytes. Do not feed schema2 content to the older schema1-only core.

Independent final snapshot also passed 546285; report/log/source hashes are in C2 evidence. Reviewer notes the fusions' rarity and short finite-run enjoyment window as an unresolved experience question, not a core validity failure.
