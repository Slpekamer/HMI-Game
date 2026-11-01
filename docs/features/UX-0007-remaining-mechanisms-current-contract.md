# 剩余技能 VFX：当前只读观测接口接线合同

**审计日期：2026-11-01。范围：只读后续设计，不改 Core / VFX / Host，不启动原生窗口。**

## 0. 当前基线与完成边界

- OBS-0001 已完成 Core scoped 验证并本地提交：`e1b4cdb3c9fae052550e208ba7b955de81beef06`；Library `libfile_ee0021b37df481918d80c480d8983547` v1。
- 本次直接读取 `HMI-OBS0001-source-scoped-tested.zip` 的 `git-commit.raw` 并按 Git 对象头重算，得到上述精确 commit；将其中六个事件/系统关键文件逐 SHA 与本次只读工作源核对，全部一致。旁附 `remaining-mechanisms-source-proof.json`，不是仅沿用聊天中的版本名。
- 原 `HMI-Game-vfx-study-r1/docs/features/UX-0007-readable-skill-vfx-contract.md` 的“metadata 未批准/未实现”及“Orbit/Honey 只能通用命中、支援/墙反弹完全无通知”已被 **OBS-0001 当前接口**替代。原文仍作为历史样板合同保留，本任务不修改它。
- **当前接线上下文仅 BasicMelee + Ricochet**。以下 Orbit / Honey / 射手伙伴 / 支援伙伴 / WallBounce / Blast / Impact 是待分步实现与视觉验收的映射，不是“全部技能视觉已完成”。OBS 的 authority parity / 存档兼容通过不等于表现、Android、手柄或设备帧率通过。
- 无新增声音；不借本次合同恢复已撤下的音频实验。新出手主体仍须保留简约读性，不以数量/光照/满屏闪掩盖信息不足。

## 1. 必须使用的当前 API 与消费顺序

```csharp
// GameSimulation.CombatObservationSchemaVersion == 1
// 在每一次真实 sim.Step() 返回后读取，而非只读 catchup 最后一个 tick：
for (int i = 0; i < sim.EventCount; i++)
    ConsumeLegacy(sim.GetEvent(i), i);  // GameEvent.Mechanism 已显式给出
for (int i = 0; i < sim.ObservationCount; i++)
    ConsumeObservation(sim.GetObservation(i), i);
// 状态型黏附：只对当前活着的完整 enemy ID 查询。
sim.TryGetEnemyMotionObservation(id, out EnemyMotionObservation sample);
```

- `GameEvent.Mechanism`：`Unknown, BasicMelee, Orbit, Ricochet, Honey, CompanionShot, Impact, ImpactBlast, EnemyContact`。按 **Kind + Mechanism** 双重匹配，不再按伤害值、距离、Source==player 猜机制。未知值退为通用可读 cue 或不显示可选技能体，不重新解释成某个已知技能。
- 旧 `GameEvent` 保存原 kind / Source / Target / From / To / Value / Tick / RunToken / Wave；**没有全局跨通道序号、攻击 transaction ID、命中的轨道糖果编号或墙法线**。新机制标签不是这些缺失数据的替代品。
- `CombatObservation` 独立 kind 为 `SupportHealApplied / SupportBuffApplied / WallBounce`，带完整 Source / Target、位置、Value、Tick、RunToken、Wave。
- 两通道的 index 各自从零开始，去重键须含 **channel + run + wave + tick + original index**。不能只用 `(tick,index)`，否则旧事件 0 会吞新通知 0。
- 旧 2048 事件缓冲和新 512 通知缓冲互不占槽，均可能丢 newest；`DroppedEvents` 与 `DroppedObservations` 分开观察。新通知 dropped 计数在 Prepare / Begin / Restore scope 重置，不能跨 scope 简单相减得到负数。满池只降表现，不能变成规则 early return，也不能补造已丢的命中或支援接收者。
- 两通道没有共同发出顺序：先读旧通道再读新通道只是适配器遍历顺序，**不是把“支援先后/反弹先后”重新排入 Core**。同 tick 视觉可并存；不得借视觉排序推迟或重复 HP / buff / 伤害。
- 状态和事件均需验证 run/wave；绑定 actor 还需完整 index+generation。目标已同 tick 销毁时，真实事件可在不可变 `To` 生成短独立效果，但不能染新占槽 actor。Source 已不存在时可用事件 `From`，不要绑定同 slot 的新 Source。

## 2. 每机制接线映射

### 2.1 环绕糖果 Orbit

|项目|当前真实来源 / 最小映射|
|---|---|
|持续主体|当前 Combat 且 `GetStat(OrbitEnabled)>0`；按实际 `OrbitCount/OrbitRadius` 与权威 `WaveSeconds` 定位。角度公式为 `WaveSeconds * 2.1 + j * 2π / count`，中心为当前玩家位置。当前内容最多四颗，但适配器仍要明确自身 pool 上限并记录异常/降级，不擅改 Core count。|
|实际命中|`GameEvent.Kind==Damage && Mechanism==Orbit`。Source 是玩家完整 ID；Target/To 是受击敌人及当时位置；Value 是已走 DamageResolution 的伤害量（含现有 Power 增幅），不是固定 2.1。|
|时间|只有真实满足糖果距离/LOS 且该敌 OrbitCooldown 允许才出 Damage；敌共用 .3s 冷却。不得给所有糖果各自伪造命中节拍。|
|最小形状|持续的独立圆润糖果实体，避免仅画整圈线；命中时 Target 处小圆瓣/糖屑形接触 cue，颜色之外用圆瓣轮廓区别。默认不加每颗长尾和独立灯光。|
|缺数据 / 禁止|Damage 不含 **哪一颗糖果** 命中；From 通常是玩家位置，不是糖果中心。不能反查最近糖果就声称其击中，不画错误的玩家→目标射线，不把整 OrbitRadius 圆盘当持续伤害区。可短亮 Target 的 Orbit-family 命中体，不能保证逐糖果命中对应。|
|状态清理|pause 时冻结主体角度/淡出时钟；恢复用同权威 WaveSeconds，不自由旋转补帧。玩家 scope/波次/角色切换或退出 Combat 清持续体与 ID 绑定；移动只读快照，不写 Transform。|

### 2.2 蜂蜜 Honey：池、伤害脉冲、真实黏附分开

|项目|当前真实来源 / 最小映射|
|---|---|
|持续蜜池|Combat + `HoneyEnabled` +真实 `HoneyRadius`。完整生效半径应首帧存在，后续只作表面/边缘小幅流动，不做“慢慢铺到某敌后才生效”的假时间线。|
|真实伤害|`Damage + Mechanism.Honey`，Source=player，Target/To=实际受击敌。Core HoneyTimer 约 .6s一次，且逐敌要求半径+LOS；不是每帧 DoT。|
|确认的脉冲分组|同 `(run,wave,tick,Source)` 收到一条或多条 Honey Damage，说明本 tick 确实发生至少一次有效蜂蜜命中。最多可合并一个很弱的池面响应，目标命中仍按实际事件/预算。没有通知时，不按 `WaveSeconds %.6` 猜全池伤害脉冲。|
|真实慢速状态|对活敌完整 ID 调 `TryGetEnemyMotionObservation`；返回 true 且 `HoneyMultiplier < 1`、`UsedKnockMovement==false`，表示 **本 tick Movement 确实用了这个慢速乘数**。可显示极小脚底黏附印/短黏丝；不得直接按敌在池内判 slow。|
|历史语义|此样本不是下一 tick 的预测。Movement 后同 tick 的 Impact 可能新开 knock，样本仍诚实保留先前已用的 slow；死亡/复用 ID getter 会拒绝。Knock 分支记录 multiplier=1，即便人在池内。Slow 本身没有 LOS 条件，而 Honey Damage 有 LOS，二者不能合并成“所有黏附者都受伤”。|
|最小形状|低矮扁平蜜池+少量圆润边瓣，受击目标小琥珀滴；已用 slow 的目标可选短小黏附印。不同于 Orbit 的悬空实体和 Blast 的短放射碎片。不增加每敌常驻大圈。|
|缺数据 / 禁止|无公开 HoneyTimer 倒计时/空场 HoneyPulse 通知，不能画精确倒计时或无目标打击；不能把样本丢失/无查询结果视为“肯定减速”。直接移除或淡退该可选状态印。高度层必须重新核蜜池/花瓣/新特效，避免已有共面问题重现。|
|清理|pause 保持有效同 tick 状态及表面位置，但不推进流动/淡出；恢复第一新 sample 取代旧状态。newrun/wave/restore/离开 Combat 清池和所有黏附绑定；sample 失效/敌死亡/代数变化移除对应印。|

### 2.3 射手伙伴 CompanionShot

|项目|当前真实来源 / 最小映射|
|---|---|
|出手|`Ray + Mechanism.CompanionShot`；Source 为真正射手伙伴的完整 ID，From 为其发射时位置，Target/To 为目标。无需再按 Source 类型或 Ray.Value 猜归属。|
|命中|`Damage + Mechanism.CompanionShot`；Value 是真实 DamageResolution 应用量。旧 Ray.Value 仍为发出事件原值 2.7，即便 buff 后 Damage 输入为 3.2，不能由 Ray.Value 推断当前buff或实际损血。|
|时间|现有射手 cooldown .95s，伤害同 tick 结算，且射程/LOS/锚点规则已在 Core 做完。动画表现不延迟下一发、不创建新 projectile collision。|
|最小形状|伙伴自身位置的小糖籽出手体/短口部膨缩，加一颗可辨切面糖籽的**瞬时命中印**或完整短残迹；首帧作用终点已经出现。不同于 Ricochet 多段晶链，射手是一段、来源独立，不能仅换颜色却从英雄发出。|
|缺数据 / 禁止|没有飞行实体、速度、到达时间或角色枪口骨契约。不要做伤害之后才“飞到”敌人的慢球造成可躲窗口错觉；不猜某个骨名后写原骨架。若 Source actor 已销毁，From 独立 cue 可保留，不绑定复用 slot。没有确切攻击transaction ID，多次同 Source/Target/tick 也按原事件 index，不用几何合并掉必要去重语义。|
|清理|与 transient pool 相同：pause停时钟，scope切换清出手体/残迹，释放 actor 绑定不修改共享材质。伙伴始终无受伤 cue/血条/伤害接收机制。|

### 2.4 支援伙伴 Support：真实施放与实际增益区别

|项目|当前真实来源 / 最小映射|
|---|---|
|玩家治疗|新通道 `SupportHealApplied`；Source=支援伙伴，Target=player，From/To 为实际赋值时位置，Value=`clamp后HP - 原HP`，包括满血的 **0**。现规则满足范围就算 used，原 cooldown=1.8s。|
|伙伴增益|新通道 `SupportBuffApplied`；Source=支援伙伴，Target=实际范围内其他伙伴，Value=被赋值的 buff时长1.5秒。是刷新，不是新叠层。|
|来源合并|同 `(run,wave,tick,Source)` 多个实际通知可合一个 source 小花瓣施放 cue，再在已收到的各 Target 展示对应小反应。只合 source 外观；Target事件原 index仍去重，缺失通知不能补出其他接收者。|
|最小形状|Source 一次圆润软糖花瓣开合；Value>0 的 Heal 在 player 处短小向上糖滴/微光点；Value==0 允许 source 轻施放识别，但**不出现+HP、回血闪、填条动画**。Buff 目标用一次短星/叶形刷新印，不用伤害色。|
|缺数据 / 禁止|公开 EntityView 仍无当前 BuffTime。通知 Value 是赋值时长，不是读取时的精确remaining：同 tick 后续伙伴循环可能已经减去一个FixedDelta，pause也影响衰减。因此本阶段做短“刷新发生”cue，不给持续buff倒计时/精确状态徽章承诺。不能按 HP 增加反推来源，不能按固定1.8s自行施放，无通知不假造支援。|
|清理|通知不进入checkpoint；restore不重放。pause冻结已存在的短cue，scope/newrun/退出清接收者表与group；actor死亡/世代变化不能把buff印贴给新slot。Source合并不能改变实际治疗、BuffTime或cooldown。|

### 2.5 撞墙 WallBounce

|项目|当前真实来源 / 最小映射|
|---|---|
|触发|新通道 `WallBounce`，只在原 Movement knock 分支 `hit!=0 && WallCooldown<=0`、原反射/计数/上限逻辑之后发。Source==Target==反弹敌人完整ID；From==To==该步最终碰撞约束后的位置；Value=原bounce ordinal（1..3）。|
|时间|是**已经发生**的墙接触/反射通知。第三次原逻辑可能将 KnockTime/Velocity归零，但仍真实触发了这次分支；不得因此继续画出后续飞行。|
|最小形状|敌脚/实体附近一簇紧凑扁平糖晶接触闪/短压扁印，位置取事件 To；只作视觉，不推动 actor。|
|缺数据 / 禁止|没有墙ID、准确表面接触点、碰撞轴、法线、入射/反射速度。事件位置是**敌中心**，不能称精确墙面火花，也不能从相邻帧方向变化猜向量。宜使用不承诺方向的短接触簇；若将来要贴墙定向喷溅，先补正式观测数据，不做位置近似冒充实测。普通追击撞墙不会发此通知，不能补画成技能反弹。|
|清理|事件独立池允许目标已同tick死亡后在To保留短cue；不染/抖新代数 actor。pause冻结、run/wave/退出清池；Value只是ordinal，不是伤害值/新反弹次数指令。|

### 2.6 撞敌终末爆圈 Blast / ImpactBlast

|项目|当前真实来源 / 最小映射|
|---|---|
|主体事件|`Blast + Mechanism.ImpactBlast`；Source是击退中的撞击敌人、Target是碰撞另一敌人，From==To==爆圈中心，Value为实际半径（当前2.2m）。不要因Source不是玩家而丢弃。|
|实际命中|`Damage + Mechanism.ImpactBlast`，现 Damage Source仍为player；这是终末damage-only，不产生新的冲量。中心半径内多个真实Damage依次发出。|
|最小形状|中心低矮切面糖晶绽开，辅以一层短圆形范围轮廓，首帧覆盖已结算的真实半径，随后只碎片收缩/alpha退。命中敌人独立使用该机制的短接触cue。|
|缺数据 / 禁止|没有Blast transaction ID，且Blast.Source与Damage.Source不同。多个Blast同tick时不可按“最近中心”将Damage硬归入某一个Blast实例；主体按每个真实Blast事件生成，命中按已标机制但不强关联的Damage生成。不能画递归连锁/二次击退/冲锋，也不做延迟扩大的伤害前沿。|
|规则细节|当前爆圈伤害只按半径，未做逐目标LOS检查；不能套用Impact的LOS规则去少报/改伤害。VFX只显示实际事件，不由外观回算伤害对象。|
|清理|已死Source/Target仍可在事件To出短主体；不要求Alive才允许绘制，也不修改清理时序。pause/scopes按统一池规则，原HP/death提示优先于爆圈附属碎片。|

### 2.7 窄扇冲击 Impact

|项目|当前真实来源 / 最小映射|
|---|---|
|出手|`Impact + Mechanism.Impact`；Source=player，Target=原选择敌，From=出手位置，To=From+forward*真实range，Value=range（当前由Stat决定），不是伤害。|
|真实命中|`Damage + Mechanism.Impact`，逐敌通过实际range、半角 `acos(.9009689)`≈25.7°及LOS才应用；存活者随后由Core赋权威knock。仅收到Impact而缺Damage，不补造目标命中/击退。|
|最小形状|带实体厚度的窄楔软糖压片/拳端扁平冲击体；作用端首帧就到事件To，消散只收薄/淡出。不要把已判定的短冲击改成慢前沿。相较近战糖壳弧必须有明确窄楔方向形状，而不是半圆AOE。|
|缺数据 / 禁止|没有每敌“已成功赋knock”的独立通知（致死Damage后会跳过赋knock）；Damage并不自动保证还活着/被击退。跟随权威实体位置，不由VFX强行位移或摇骨。To只是作用方向终点，不是选中敌的当前位置。不要填充被墙挡住区域并暗示它正在造成伤害；先用窄主体/稀疏边翼，不承诺完整无障碍命中面。|
|后续关联|WallBounce与Blast已有各自真实通知，不能从Impact出现就预排这些后续效果。未触发时不画；触发后各自独立按真实事件生成。|
|清理|pause冻结，scope/角色切换清主体；与旧Attack表现互不改变时钟/伤害节奏，不能重新启动或延迟Core攻击。|

## 3. 统一生命周期与预算（实施约束，不是性能结果）

1. **一个全局 transient 预算**，不是每机制再开48池：沿当前48 logical slots / 每render最多12 ordinary cues，8步catchup共享该render额度，不因新增通道每Step重置。新通知只允许消耗同一可选视觉预算。
2. 主角真实受伤保留独立优先；敌死亡高于出手尾迹/蜜池小波纹；满池先舍弃低优先尾迹/次要碎片，绝不排队补播旧攻击。HP/存档/输入和Core事件读取不受预算影响。
3. 持续体单独有界：Orbit最多按当前内容4颗、Honey1池；可选黏附印建议最多8个可见目标，**只降可见提示，不改变减速**。这是待像素/计数测量的起点，不能称已验证最佳值或所有受影响敌人均有印。重复刷新使用同slot，不累加mesh。
4. 一个逻辑slot内所有主体/连接/碎片要统计原生节点、triangles和draws；禁止用“48slot”掩盖每slot几十个粒子。先复用已审owned geometry/material池，不为每击重复建资源、不Dispose共享GD.Load/getter资源。
5. `Unknown`、丢事件或sample失效时保守退为通用短命中/无可选状态，不猜技能或补延迟攻击。记录每秒spawn/drop和峰值，不把16ms采样/llvmpipe当手机性能。
6. 低动态默认：不全屏闪，不每击震屏，不加动态灯/阴影投射，不改camera/authority Transform。降低动态模式禁附属转动/回弹，只保短静态形状+淡出；不能关掉必要HP、受伤方向与死亡识别。
7. ordinary pause 不推进任何新VFX clock、不消费积压补播；不重复读取同tick同index。显式resize只重投影已有cue，不重发命中。run/wave/restore/返回首页/结束页/owner退出，幂等清所有group、actor绑定、池、持续体及旧scope callbacks。
8. 两通道索引、缓冲与观察样本全为临时输出，不能保存到checkpoint再重放，也不反向发Core command。

## 4. 建议分步门（未执行）

|增量|首要反例 / 实际审查|
|---|---|
|Orbit + Honey持续体与tag命中|同tick重叠Damage仍各自机制明确；Orbit不猜糖果编号；墙另一侧Honey slow与Honey damage区别；knock穿蜜区sample=1；缩放/灰度/降低动态；蜜池/花瓣共面与敌条遮挡。|
|独立射手 + 支援|同tick多个伙伴；Source离场/slot复用；buff射手Ray.Value2.7但Damage不同；满血SupportHeal=0无回血UI；多recipient只一个source动作；pause后buff刷新cue不变成倒计时假状态。|
|Impact → WallBounce / Blast|Impact空事件或致死不假knock；普通墙路由不假反弹；第三次真实bounce停下；敌同tick死亡后To cue；多Blast无事务ID不近邻归因；无二次冲量/递归。|
|统一密集与生命周期|原通道/新通道同index不误去重；2048/512掉包；单render8step；满48/12预算player优先；newrun/restore/暂停第一帧与正常退出；同权威trace与弱动态实际影片。|

每一步先独立事件/状态边界测试，再同相机单机制/叠加密集真实渲染与正常输入。当前这份审计没有执行这些新增视觉门，不应将表中建议变为已交付事实。
