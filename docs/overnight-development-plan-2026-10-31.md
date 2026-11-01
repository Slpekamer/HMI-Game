# 食物英雄接入后的玩法深化计划与待拍板决定

2026-10-31 17:09 UTC。来源：用户授权今晚继续工程推进、优化、更深技能树和无尽模式，以商业化质量为目标。本文是可执行阶段计划，不是商业化完成声明。先锁当前角色集成，后扩规则；未获最终资产/视觉验收的模型不冒充发布完成。

## 阶段与停止门

|阶段|交付|必须通过|未通过时|
|---|---|---|---|
|A 当前适配基础|独立morph/动画时钟、entity/run/wave生命周期、测试|fresh aggregate +旧模型native回归|不提交“全绿”；修复后重跑|
|B2 五食物英雄资源与功能接入|5 runtime GLB、2D选人、新名字/同稳定ID、实际视频|5资产合同→全回归→实际UI/动作采样审查；18:45 scoped 通过，见 FEAT-0004|不包含 B3 性能或商业化完成声明|
|B3 资源与表现性能|真实缓存驻留/释放、帧时、合法高攻速动作可读性|尚未完成；先基线测量再修复与回归|不以理论纹理内存或软件渲染录像代替手机证据|
|C 技能树schema与深度|向后兼容schema v2、分支前置/互斥/上限、首批深层节点|旧21卡/4物品回归、非法数据负例、五角色组合矩阵、seed重放|不混入B提交，不删除旧断言|
|D 有界无尽模式|6波后明确“继续无尽”，延续钱包/构筑；有限模式仍可结束|长局/空候选/极值/预算饱和/回放/经济不变量|遇上限有确定行为，不NaN/无限刷实体/递归|
|E 存档和恢复|版本化纯数据、波间检查点、显式继续/新局|损坏/未知版本/部分写入/旧scope拒绝/暂停重入|先拒绝损坏并保留原文件，不静默重置金币|
|F 调优与内容审片|定量帧耗/内存/事件丢弃、密度/节奏、实际截图录像|同种子前后对照、移动设备另门、可回退小提交|不以软件渲染或ECS名义宣称手机性能|

所有阶段测试绿后小提交、不可变源ZIP/Library备份，再由publisher核验远端；本地commit不等于push。不付费、不公开发布、不建新账号，不扩大权限。场景和核心禁止并行改同文件；各阶段必须先记录schema/行为变化。

## 提交给协调者拍板的普通设计提案

### NDEC-001 — 先有限6波，胜利后自愿进入无尽
- 保留现有6×24秒有限模式和胜利页；新增明确继续入口，不偷偷把原通关改成永无结局。
- 继续继承seed/RNG状态、角色、卡树、金币账本和装备；不重新发起始奖励。新模式标记入存档。
- 替代：直接把所有局改无尽，容易破坏现有体验/测试。可逆：mode gate隔离。
- 待协调者确认；实施前测试多次点击/旧run命令/退出后继续无重复奖励。

### NDEC-002 — 深度先做“可理解的取舍”，而非无上限数值
- 先加每机制2条分支、每分支1个深化节点（约20新增节点，具体效果逐项审平衡），旧root和旧卡ID保留。
- schema v2新增可选prerequisites（明确all/any）、exclusiveGroup、repeatPolicy、rankCap、effectLifetime；旧parent字段按明确兼容迁移归一化，拒循环/悬空/自排斥/不可能解锁。
- 首批组合已注册stat effects；真正新触发效果另设system/effect type与预算，不在中央按卡ID写巨型switch。
- 分支侧重范围/密度、链数/距离、减速/持续、伙伴数量/专精、冲击控制/碰撞收益等取舍。不要同时把所有方向无代价堆满。
- 每tick触发预算、每来源/目标冷却、链深度与visited集合明确化；超额延迟/拒绝策略须确定可重放，权威伤害不能由被丢弃的cosmetic事件决定。

### NDEC-003 — 永久池耗尽后给明确的下波补给，不崩溃也不偷偷突破cap
- 保留trait/base/risk三槽和波末商店。每槽优先合法永久升级；池满时显式展示对应“下波限定”可重复补给。
- 候选：trait下波特性增益；base下波基础攻速；risk下波更强敌人换击杀收益。数值/作用域通过数据声明，互斥同槽、只作用下一波，不叠成永久无限乘法。
- 不用虚假的“升级成功但无效果”卡，不强制无意义回血（现波间满血），不随意增发金币破坏现有账本。
- 需要新增临时effect lifetime和offerKind；完成后必测第100/1000波满树三槽仍合法、重复选拒绝、scope到期。

### NDEC-004 — 有界压力，不依赖指数膨胀
- 初始提案：保留总entity容量512，active enemy硬上限256，伙伴仍原上限4，渲染cosmetic pool64、核心cosmetic event2048维持并诊断丢弃。
- 当前射线是即时规则+视觉段，不伪称已有真实投射物。若后续新增projectile实体，单独上限128并计入总容量，不在本阶段顺手引入。
- 难度使用有界分段/平滑曲线：HP倍率先提案最大12×、速度最大1.5×；波号在计算前限制到难度tier上限。达上限后用有限有节奏的波次变体而非继续无限数值相乘。
- 计数/金币使用明确溢出策略（checked或饱和并报告），wave上限检查；上限不是商用平衡结论。
- 先量现有impact查询最坏成本再决定空间分区。优化需同输入结果等价测试；不能只加线程/框架。

### NDEC-005 — 首版仅波间持久检查点，战斗暂停不自动序列化全部世界
- 纯C# DTO：schema/catalog版本、mode、seed与RNG状态、wave、角色稳定ID、levels/picks、装备/临时排队、wallet累计、校验信息。Godot adapter负责原子写入/替换备份，不让文件I/O进入Core。
- 波间安全点保存；战斗内退出清楚提示返回上个安全点（不声称中途无损恢复）。恢复发新runToken，旧UI/事件无效；不恢复引擎Node ID或动画状态。
- 选择理由：完整战斗快照涉及实体生成代数、碰撞冷却、AI路径、事件队列和schema迁移，仓促做风险高。先可靠checkpoint，之后单独扩精确中途恢复。
- 移动系统挂起仅pause并在安全点flush；隐私/存储失败以可见错误处理，不静默吞异常。

## 具体验证矩阵
- 核心：旧测试完整保留；新增content非法图/互斥/全池耗尽/重复命令；同seed命令流2次digest一致。
- 长局：至少1000波的无渲染推进/波间选择压力用例；显式test-only生存夹具与正常游戏分开标注，不冒称玩家实际存活1000波。累计多seed/5角色、极端合法构筑与临时效果到期组合。
- 性能：warm32/128/256/512 entity核心tick耗时和alloc；impact密集碰撞与满事件队列；UI资源创建销毁/5角色切换/内存回落；实际渲染drawcalls/triangles/texture memory/帧时间分位数。软件llvmpipe与手机GPU分开。
- 恢复：每个字段损坏/缺省/未知ID/旧版本，原子写失败、重复继续、恢复→新局→旧按钮，金币=earned-spent且不重复奖励。
- 视觉：实际游戏（不是Blender/AI宣传图）截图/短录像审角色大小、材质、姿态、攻击反馈、卡片信息、风险代价、无尽入口和暂停/继续。

## 当前风险
- 历史起点：四新英雄尚在建模、烤团 runtime 仅过导入技术门。更新至 18:45：五份最终候选均已交付并过独立实际导入/五态/动作采样及同一host短切换门；保留全过程失败证据。B3 资源性能仍未完成，侧向读形/头像配饰裁切/金属侧反射仍列后续精修。
- 主机环境多次重置，持久备份与依赖锁定优先；旧签名key丢失可能使新Android调试包不能覆盖旧包。
- 当前一个chaser敌人，长期无尽会重复；先完成稳定的模式/构筑基础，再以独立内容增量增加可辨识敌人，而非未经测试堆角色种类。
- 商业化仍缺移动设备矩阵、音效/可访问性、长期平衡、存档迁移、发行合规/商店材料；本授权是质量目标，不是本夜可自动满足全部要求。

## 17:11 UTC 协调者拍板（替代上文候选冲突项）

1. 六波后明确“完成本轮 / 继续无尽”，钱包/构筑继承，有限通关和无尽成绩分开。
2. **只有trait永久池满时可转明示补给/资源**。基础/风险不得偷偷替换类型或提供无效增益；应保持合法池并对上限提供明确回退。提议到所有可用基础/风险均封顶时显示不可选的“已达上限”，继续保留三槽且保证trait补给有效；该具体UI回退尚待协调者确认，不能称三张皆可选。
3. 总容量512/敌256/伙伴4，HP12×/速度1.5×为可配置初始上限，不是最终平衡。难度后续靠少量有门槛的组合/精英节拍/规则变体，不无限膨胀。
4. 仅波间checkpoint；显式告知退出恢复最近安全点，原子保存/schema/损坏回退，防重复领奖/复制金币。
5. 旧21卡/4物品行为必测。新增约20节点是上限目标，不为凑数加卡；优先差异化和融合。

先A/B后C。等待美术期间允许隔离文档/内容测试准备，不污染冻结集成树。1000波合成压力与真实完整局分别记录。以上普通设计可逆，供用户早晨逐项review。

### 17:12 UTC confirmed fallback
Coordinator confirms capped base/risk slots show “本类已达上限”, disabled with explanation; trait supply guarantees progress. This is late-game degraded fallback, not an ideal normal state. Test reasonable run progression does not rapidly spend most waves with only one valid choice. Repeatable meaningful risk contracts/exchanges may be a later separately validated feature, not unbounded stacking introduced just to fill three cards.

## Status boundary — 18:47 UTC
B2 is a recoverable five-hero integration milestone, not the completion of every B-stage performance task. B3 cache residency and high-rate gesture readability, plus C/D/E deeper-tree/endless/checkpoint implementation, remain open. No Android hardware, audio or commercial-quality certification is implied.

## Current scoped status — 20:58 UTC
B2 five heroes is tested; B3 reference hygiene and B4 readable high-rate gestures are integrated, not a claim of lower normal-GC residency or mobile performance. C adds8 data-driven branch/fusion nodes; D includes bounded endless and6 single-wave contracts. E now passes unified aggregate plus scoped real three-process recovery and fault UI. Earlier pending rows above are historical. Long-session gameplay/balance, hardware/audio/export and camera comfort remain separate open gates; no commercial-readiness or remote-push claim.
