# HTML v0.9 原文与规则源摘录（只读参考）

本文件来自已交付 HTML 的实际内容，不是引擎实现代码。静态文字保留全部章节；动态卡片、敌人详情、模拟公式与 UI 由下方完整脚本补齐。移除 CSS 和图片 base64 以便 AI 阅读，原图资产另见主设计文档。不要直接把此 JS 当作正式 ECS 架构。

## 静态页面全文

软糖突围 — 一场软乎乎的硬仗● 软糖突围 / CONCEPT玩法循环玩法动画五位角色卡片图鉴道具汇总 ↗

HMI-Game · 新方向概念讨论稿 v0.9
五个起点，一万种甜头。

软糖突围 · 守护糖果世界选一个糖果角色，把每波三选一，变成自己的战斗配方。

[概念图片替代文字] 原创宣传概念图：紫色环绕法师、橙色硬糖狐、亮黄色蜜罐熊、蓝色召唤兔及伙伴、粉色软垫仓鼠在糖果玩具场迎战

AI 生成宣传概念图 · 用于讨论未来美术风格，不是游戏截图，也不代表已实现的画质或玩法。角色名与设定仍为提案。
认识五位角色 ↓ 试试波末三选一 ↓

01 / The promise
可爱是外壳，有选择才耐玩。

你已提出的方向
波次肉鸽 + 欢乐美术

类似《土豆兄弟》的关卡循环：每关有怪物，关后选择增益，再进入下一关。美术借鉴《糖豆人》的明快、圆润与玩具感，而非复制角色或资产。

我补充的核心提案
不是一条解法，是五个起点，多种成长路线

环绕糖果、弹射投射、蜂蜜领域、玩具召唤与冲击近战可以独立成立，也可以交叉搭配。五位角色拥有相同基础近战，特性在其上叠加。敌人碰撞与墙面反弹主要服务冲击路线，不是其他流派的必经机制。

暂不做的事情
先让单人一局成立

不沿用赛车玩法，不先做多人联机、复杂技能栏、装备稀有度海洋。第一版只证明：走位舒服、选择有感、失败还想重开。

02 / One more wave
紧张一分钟，开心选一次。

建议完整一局 8 波、约 10–15 分钟；第 4 波精英，第 8 波 Boss。所有时长与数值都只是设计占位，尚未经过实测。
01 战斗
02 收集
03 三选一
04 补给 / 下一波
05 结算 / 重来

下方「玩法演示」用代码展示机制与融合；此处只切换波次循环说明。
波内击杀获得金币；波末先做一次免费三选一，再用本局金币购买道具。两者分开：免费成长不花钱，商店也不会代替选牌。未花金币可留到后续波次，不涉及真实货币。

03 / Five ways to survive
五位糖果伙伴，只是五种不同的起点。

五种流派成为五个角色的初始机制，而不是互斥职业。五位角色都带同一套短距离基础近战；初始特性叠加在普攻之上，之后可以融合其他机制。以下角色与平衡数值均为概念提案。动画只演示图鉴中标明可抽取的节点；下列远期进化方向并不等于已经进入候选池。

01 / 环绕法师 · 中距离贴边
星绒 · 糖果星环
薰衣草紫 / 蝶翼小兽
基础：保留统一基础近战，另有糖果绕身旋转，持续命中经过轨道的怪物。敌人太近会钻进内圈，因此需要围着怪群移动，而非一直贴脸。

成长 → 进化：糖果数量 / 转速 / 轨道半径 →「双层糖环」，内外两圈交替覆盖，限制同目标命中频率。

选择：扩圈更安全但内圈更空；加密更稳却放弃爆发。蜂蜜减速可延长敌人停留在轨道上的时间。

02 / 弹射射手 · 找角度穿怪
脆跳 · 跳跳糖连射
珊瑚橙 / 硬糖小狐
基础：保留统一基础近战，额外向目标射出糖粒；第一次命中后，只能反射到有限距离内的另一名敌人。附近没有合格目标就结束，不隔空跳怪。

成长 → 进化：弹射根节点 →「多跳一次」或「搜寻更远」。这两个分支可兼修；后续再研究终端分裂，当前动画不伪装成已实现的分裂弹。

选择：追加次数适合密集怪群；扩大搜索距离能连接分散目标，但不增加跳数。每条弹射链不会重复命中同一敌人，仍受次数、距离与墙体遮挡约束；单 Boss 无第二目标时不会凭空追加伤害。

03 / 蜂蜜术士 · 控路线与驻留
蜜团 · 蜜涡领域
亮柠檬黄 / 蜜罐小熊
基础：保留统一基础近战，额外拥有蜂蜜持续伤害圈与轻减速；与糖果轨道接触命中不同。

成长 → 进化：范围 / 持续伤害 / 减速强度 →「流蜜回廊」，移动时短暂留下蜜路。

选择：当前图鉴扩圈不会自动降低伤害；未来可另设“更大面积、较低密度”的互换分支。蜜路只是远期提案，减速有上限，不完全定身。要把怪引入领域，又不能被远程怪逼停。

04 / 玩具召唤师 · 守护与调度
棉队 · 糖果伙伴
晴空蓝 / 棉花糖小兔
基础：保留统一基础近战，额外召唤独立小糖果伙伴：硬糖射手负责攻击；棉花糖护卫承伤并给邻近伙伴短时护盾；蜂蜜伙伴减速敌人。伙伴拥有独立的地图位置、目标与攻击节奏：可选择自主游走索敌，或驻守指定位置。主角走开时伙伴不会贴身跟随；改变阵地需要明确召回 / 重新部署。

成长 → 进化：数量 / 自主索敌范围 / 支援效率 →「队长玩偶」，2 个普通伙伴合并成 1 个强化队长。后续可试能力融合：硬糖 + 蜂蜜成为减速射手，但牺牲一个独立单位，避免无代价叠加。

选择：自主游走追击散怪，或驻守形成安全阵地；星环玩家可主动把敌人引到伙伴火力区。攻击、增益、减益占用同一组召唤位，上限 4 个；同类护盾 / 减速不无限叠加。伙伴的活动范围、攻击射程与驻守点绑定，不以主角为圆心；后续可用生命、持续时间或部署冷却限制全图无风险输出。

05 / 软垫斗士 · 进退与爆发
砰砰 · 抱枕冲击
莓果粉 / 软垫仓鼠
基础：保留与所有角色相同的短距离基础近战；额外的小扇形冲击负责定向击退，不再是大范围扇形清屏。

成长 → 双专精：被击退的敌人撞到另一敌人时触发一次小范围冲击；撞墙则反弹。进化可选增加精准推力或反弹效率，而不是无限扩大扇形。

选择：利用墙体、缺口和怪物排列制造碰撞；击退也可能把怪推出蜂蜜区或糖环。碰撞有冷却与次数上限，Boss 抗硬控，不能无限弹射。

混搭，而不是全部都要
初始机制不是职业锁

星环 + 蜂蜜：减速帮助轨道命中，但都怕远程压力。

召唤 + 弹射：小队牵制、投射清群；蜂蜜伙伴也能帮星环留住目标。投资分散会延后任一终极进化。

冲击 + 蜂蜜：可把怪推回蜜区，也可能推离；位置判断决定收益。

本版取消旧的 3 机制位提案：五种起始特性都能跨角色取得，但每次选择仍只获得一张卡。第一格从满足前置的“新特性 / 分支升级”中随机抽取；铺开五条路线意味着放弃深度升级，不等于更强。图鉴与抽卡共用同一候选目录，不出现重复根节点或满级废牌。
演示目标：站在开阔场地时，五条路线都能完成战斗。墙面是冲击流的战术工具，而不是其他角色的通关门票；开阔地仍可凭统一普攻与各自特性完成战斗。首版先做一个角色与两种可融合机制，验证三选一质量；完整五角色是后续目标，不一次塞进所有内容。

PLAY THE IDEA / SVG CONCEPT
让机制动起来，看见每一次选择。

代码驱动的玩法示意：英雄按固定路线移动，发条敌人接近、预警、受击与重新入场。英雄是按角色配色绘制的抽象标记，不替代上方角色美术。不是可玩的 Godot 游戏，也不代表实际 AI、帧率或数值平衡。

星绒 · 环绕
脆跳 · 弹射
蜜团 · 蜂蜜
棉队 · 伙伴
砰砰 · 冲击

伙伴自主游走
伙伴驻守原地
召回并重新部署 ↺

播放示意 ▶
演示快进结束本波 →

已暂停 · 点击播放，不会自动开始。

固定路线穿过中间通道 · 自动演示机械敌人受击后退场，再从边缘重新入场
紫环：接触命中橙线：弹射路径 / 淡橙圈：命中点搜索范围亮黄蜜圈：持续伤害 + 减速独立伙伴：独立游走 / 驻守短白弧：统一近战 · 窄粉扇：特殊击退虚线箭头：敌方冲锋预警六角道具：临时增益 / 自动靠近拾取

与下方构筑联动：融合会增加可见机制；稳健牌只强化基础近战攻速、基础近战范围或最大生命，不加速专属机制。风险牌可提高敌方生命并增加实际击杀金币，或以生命 / 敌速为代价换取不同收益。动画只示意这些差异，不把视觉特效当作真实 DPS 测试。切角色会重置整套构筑；所有角色共有同伤害、同射程、同冷却的基础近战，初始特性在其上叠加。砰砰额外窄扇击退：敌撞敌产生小范围伤害，撞墙反弹；每次击退有碰撞与反弹上限。地图道具只临时生效，不占波末机制位。击杀金币可在波末补给阶段消费；只有真实击破事件发币。重绘、暂停、修改卡牌不重复发币，动画没有可刷奖励的重播按钮。数值仅为演示占位。伙伴拥有独立位置、索敌范围与冷却；驻守不跟随英雄，只有“召回并重新部署”会移动部署点。每波演示24秒，暂停不计时；快进结束本波不发额外金币。下一波重置演示生命与敌人，临时商店物品在下一波生效；金币余额由波末商店管理。切角色须重开整局并清空经济。训练示意的生命归零后自动补满并继续，不代表正式游戏无敌或复活规则。使用“减少动态效果”时保持暂停，仍可手动播放。

04 / One catalog, many routes · v0.9
战斗赚钱，波末补给，一局构筑才真正连起来。

① 随机特性的获取或升级，② 只强化共同普攻与生命的基础词条，③ 规则级收益与明确代价。角色只是起点，砰砰也能抽到脆跳的射击。

① 特性：跨流派，也能纵向成长
先检查前置与等级上限，再从全部可用节点抽一张。反射次数和反射搜寻范围是两条可并修的分支，远处目标不会被无条件串起来。

② 基础：只改共同底座
生命、默认近战攻速、默认近战范围。不会顺手提高星环频率、弹射距离或伙伴射速。

③ 风险：改变一条规则
更硬的敌人换更多金币、脆弱的身体换更强普攻、敌我同时加速。收益、代价与可升级次数在选前可见。

演示流程：共 6 个压缩战斗波，每波最多 24 秒；第 1–5 波结束后各有一次免费三选一和金币商店，第 6 波直接结算。实际游戏仍是另行讨论的 8 波提案。演示下一波会恢复生命，便于比较机制；不代表正式游戏的治疗规则。所有价格与效果都是示意参数。0 枚本局可用金币查看道具图鉴
等待本波结束
构筑试选
卡片树 / 图鉴
道具图鉴 / 商店

星绒 · 星环
脆跳 · 弹射
蜜团 · 领域
棉队 · 召唤
砰砰 · 冲击

抽牌种子应用并重开整局
换种子重开
同角色 + 同种子 + 同选择路径 = 同一组牌；等权抽取示意，非最终掉落权重

有效最大生命基础近战攻速基础近战范围单次击杀金币

重开整局 · 清空金币 ↺

上方 SVG 与这里共用一份本局金币。选牌免费，买道具另收费；查看图鉴、切换标签和暂停不会改变金币。只有重开整局、换角色或换种子才清空本局进度。上列基础词条不含道具临时加成，实际战斗状态见演示。

机制位修订：取消旧稿「最多 3 个机制」限制，本候选池共 5 个根机制，均可跨角色获得且不重复抽到已满级节点。多学机制不保证最强：也会稀释单条树的成长机会。图鉴与抽牌使用同一份前置、等级、效果数据。
读树方法：根节点 → 分支节点。紫色表示已拥有，白色表示可获取 / 可升级，虚线表示尚缺前置；已满级会标注。此版所有兄弟分支都能并修，没有互斥分支。不是强行补满的二叉树：召唤当前只有一条扩编分支；更多路线以后再讨论。
节点 ID 固定；通常要求父节点 1 级；「终段接力」明确要求接力弹射 2 级。卡片标注的单位、等级上限与倍率均为可验证的示意参数，不是测定后的游戏平衡。
本局钱包 · 本地状态
0 枚

只有波末补给阶段可兑换。每阶段每种道具限购 1 件；免费卡与商店是两个独立选择。余额可以留到下一次补给，最终结算后不转入新局。

生效与消耗：道具都从下一波生效。绒垫护符与轻击汽水在入场时消耗；汽水只持续 10 秒战斗时间。鞋垫、护套属于本局装备，最多各 2 件。战斗中不能购物；未购买、金币不足、售罄或已达上限都不会扣款。

05 / The clockwork intruders
糖果世界的守护者，对上发条入侵者。

以封面原有的深色球形发条小怪为设计种子，而不是另一群糖果生物。4 种普通单位、1 个精英、1 个 Boss；每张为独立 AI 概念图。点击查看攻击、预警与应对，均是设计提案而非已实现玩法。
阵营设定提案 · 发条远征队：来自糖果世界之外的机械工坊，试图把柔软、自由生长的糖果乐园改造成按齿轮节拍运转的装配场。五位主角守护家园。它们不是邪恶糖果，也不是写实军队，而是一群固执、古怪、会咔哒作响的玩具机械入侵者。
[概念图片替代文字] 发条豆兵独立概念插图：延续封面原有小怪：深梅紫球形金属壳、黄铜纽扣眼与背部发条钥匙。
普通 / 追击
发条豆兵

延续封面原有小怪：深梅紫球形金属壳、黄铜纽扣眼与背部发条钥匙。
查看攻击 / 预警 / 应对 ↓

[概念图片替代文字] 铆钉哨兵独立概念插图：球形搪瓷壳与短气压发射管，保留纽扣眼和黄铜铆钉。
普通 / 远程
铆钉哨兵

球形搪瓷壳与短气压发射管，保留纽扣眼和黄铜铆钉。
查看攻击 / 预警 / 应对 ↓

[概念图片替代文字] 弹簧冲车独立概念插图：低矮轮式撞车，厚金属保险杠与外露弹簧形成明确朝向。
普通 / 冲锋
弹簧冲车

低矮轮式撞车，厚金属保险杠与外露弹簧形成明确朝向。
查看攻击 / 预警 / 应对 ↓

[概念图片替代文字] 校时技师独立概念插图：机械调谐天线与小型工具臂，轮廓区别于直接攻击单位。
普通 / 支援
校时技师

机械调谐天线与小型工具臂，轮廓区别于直接攻击单位。
查看攻击 / 预警 / 应对 ↓

[概念图片替代文字] 铁壳重锤独立概念插图：叠层金属甲壳与巨大圆头锤臂，保留圆润玩具比例而非真实战械。
精英 / 重击
铁壳重锤

叠层金属甲壳与巨大圆头锤臂，保留圆润玩具比例而非真实战械。
查看攻击 / 预警 / 应对 ↓

[概念图片替代文字] 总发条长独立概念插图：大型机械统领，中央钟盘与主发条、黄铜齿轮边框构成身份，不用糖罐或王冠糖果。
Boss / 节拍压场
总发条长

大型机械统领，中央钟盘与主发条、黄铜齿轮边框构成身份，不用糖罐或王冠糖果。
查看攻击 / 预警 / 应对 ↓
全体统一规则：用剪影、动作方向与节奏表达意图，颜色只是辅助；预警必须早于伤害。限制同屏支援、冲锋与远程威胁的重叠，避免满屏红圈或特效淹没角色。敌方统一使用深梅紫搪瓷金属、黄铜铆钉、齿轮、钥匙与机械刻度；我方使用软绒、软糖和明亮色彩。保持形状与材质的阵营差异，不只依赖红蓝配色。

06 / Art direction
像拆开一盒新玩具，不是换皮糖豆人。

[概念图片替代文字] 五角色概念阵容，从左至右为星绒、脆跳、蜜团、棉队及糖果伙伴、砰砰

AI 生成角色阵容概念 · 左至右：星绒 / 脆跳 / 蜜团 / 棉队与糖果伙伴 / 砰砰。以宣传图为身份参考，保持配色、轮廓与道具一致；非最终建模规范。视觉关键词
短绒 × 半透软糖 × 哑光软胶大轮廓 / 小表情 / 克制高光

角色配色：星绒薰衣草紫、脆跳珊瑚橙、蜜团亮柠檬黄、棉队晴空蓝、砰砰莓果粉。蜜团采用明亮柠檬黄主体与暖色阴影，不用暗褐黄覆盖主体。五位角色使用不同耳形、体形与道具识别：蝶翼小兽的环、狐的发射器、熊的蜜罐、兔的伙伴、仓鼠的拳套。颜色辅助识别，但不能只靠颜色区分。验收建议：缩到手机战斗尺寸仍可凭轮廓认角色；关闭炫光仍能看清攻击范围；角色亮度高于地面而低于危险预警。明亮不等于刺眼，背景比角色低对比，危险信号保留专属色与形状。
镜头：俯视 / 轻等距 3D（提案）看清四周威胁；不让高台或装饰遮挡主角。材质与反馈：绒毛轮廓、糖体透光、软胶回弹概念图可有丰富细节；游戏内缩小为清楚色块与短促轮廓闪光。持续伤害用低亮度地面纹样，不能遮住预警。场地：一座软垫玩具箱少量内部墙体留出通道，不能封死路线；冲击流可借墙反弹。地图道具提供可见倒计时的临时增益，到期消失，与波末永久构筑分开。风险契约可改变敌人耐久、移速或击破金币；击杀金币在波末兑换道具；临时道具与本局装备分开标注，不涉及真实付款。取舍：可读性胜过物理真实动画弹性与规则击退分离；移动端不靠海量刚体或高粒子密度撑场面。

07 / Small first, fun first
先做一小局，验证它到底好不好玩。

完整 8 波是方向，不是首版承诺。第一块垂直切片应更小，能让人亲手判断走位和增益是否有趣。

层级先做什么如何判断有效

P0 · 可玩闭环1 场地 / 1 主角 / 3 基础敌人 / 1 精英3–5 波，失败重试与胜利结算一轮从开始到结算无阻断；每次受伤能解释原因

P0 · 构筑验证先做 9–12 个候选增益，覆盖特性树 / 基础词条 / 风险契约三类波末免费三选一 + 小规模道具商店3 类选择都能在不同局面被合理选中；扩机制不成为必选

P1 · 体验打磨触屏手感、攻击预警、声音、反馈强度开关真机测试输入与拥挤场景；帧率目标在设备范围确定后制定

后续再议完整 8 波 / Boss / 五角色与更多融合 / 道具扩展基础闭环足够有趣后再扩量，不用内容掩盖手感问题

与现有工程的关系（仅概念，不是实现报告）
当前讨论对应现有 Godot 空项目的新玩法方向；本页没有修改游戏仓库，也没有实现战斗系统。若未来迁移 Unity / 纯 C#，可以再讨论 ECS 分层：角色、敌人、投射物作为实体；位置、生命、击退等组件；移动、攻击、波次与增益系统。架构选择应服务于可玩验证，不作为当前前置条件。

软糖突围 / 原创概念讨论稿 v0.9 · 2026.10非已实现游戏 · 无外部资源依赖 · 所有平衡数值待验证



## 全部 inline JavaScript

### 脚本 1

```javascript
(function(){
'use strict';
const $=id=>document.getElementById(id),NAMES={orbit:'糖果星环',crit:'跳跳糖弹射',honey:'蜂蜜领域',summon:'糖果伙伴',bounce:'抱枕冲击'},COLORS={orbit:'#a885d6',crit:'#f58955',honey:'#ffdb39',summon:'#6aa8e8',bounce:'#e887ad'};
const layers={walls:$('gp-walls'),pickups:$('gp-pickups'),zones:$('gp-zones'),tele:$('gp-telegraphs'),enemies:$('gp-enemies'),shots:$('gp-projectiles'),hero:$('gp-hero'),fx:$('gp-effects')};
let config={character:'orbit',mechanisms:['orbit'],damage:1,health:1,speed:1,basicSpeed:1,basicRange:1,traits:{orbitCount:2,orbitRadius:76,ricochetBounces:1,ricochetRange:120,honeyRadius:103,honeySlow:.45,summonCount:1,impactRange:68,impactForce:220},risks:{enemyHealth:1,coinMultiplier:1,basicDamage:1,playerHealth:1,enemySpeed:1,playerSpeed:1}},playing=false,frame=0,last=0,time=0,clock=0,kills=0,coins=0,hero={x:400,y:235,hp:100,flash:0},enemies=[],shots=[],effects=[],timers={crit:0,honey:0,summon:0,bounce:0},charge={phase:'idle',at:3,target:null,enemy:0},uiTime=0,companionMode='roam',companions=[],pickups=[],buffs={power:0,haste:0},basicCooldown=0,pairCooldowns=new Map(),economyActive=false,runState={phase:"combat",runToken:"standalone",wave:1,balance:0},waveEnded=false,shopItems={shield:false,tonic:false,shoes:0,gloves:0};
const WAVE_SECONDS=24;
const BASIC=Object.freeze({range:46,damage:1.5,cooldown:.72}),IMPACT=Object.freeze({range:68,halfAngle:Math.PI/7,damage:2.1,cooldown:1.4,velocity:220});
const WALLS=[{x:215,y:138,w:26,h:190},{x:563,y:138,w:26,h:190}];
const cap=(v,lo,hi)=>Math.max(lo,Math.min(hi,v)),dist=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y),has=k=>config.mechanisms.includes(k),f=v=>Number(v).toFixed(1);
const starts=[[110,105],[690,100],[705,340],[105,350],[390,90],[660,235],[400,380],[140,245]];
function resetScene(){time=0;clock=0;kills=0;coins=0;waveEnded=false;hero={x:480,y:235,hp:100*config.health,flash:0};enemies=starts.map((p,i)=>({x:p[0],y:p[1],hp:enemyMaxHP(),alive:true,respawn:0,flash:0,hitAt:-2,id:i,life:0,vx:0,vy:0,knockUntil:0,bounces:0,blastUsed:false,wallAt:-1}));shots=[];effects=[];basicCooldown=0;pairCooldowns.clear();buffs={power:0,haste:0};pickups=[{kind:'power',x:475,y:257,ready:0},{kind:'haste',x:345,y:214,ready:0}];timers={crit:0,honey:0,summon:0,bounce:0};charge={phase:'idle',at:3,target:null,enemy:0};last=0;uiTime=0;resetCompanions();render();}
function resetCompanions(){companions=has('summon')?Array.from({length:config.traits.summonCount},(_,i)=>({id:i===0?'shooter':'companion'+i,role:i%2===0?'shooter':'shield',x:435+(i%2)*90,y:280+Math.floor(i/2)*40,ax:435+(i%2)*90,ay:280+Math.floor(i/2)*40,range:i%2===0?125:76,leash:i%2===0?140:125,cooldown:.15+i*.25,buffedUntil:0})):[];updateCompanionControls();}
function updateCompanionControls(){$('gp-companion-controls').hidden=!has('summon');$('gp-roam').setAttribute('aria-pressed',String(companionMode==='roam'));$('gp-station').setAttribute('aria-pressed',String(companionMode==='station'));$('gp-companion-description').textContent=companionMode==='station'?'驻守：伙伴保持当前地图坐标，英雄会独自走开；只能攻击自身射程内目标。护盾伙伴仅支援身边单位。':'游走：伙伴自主索敌，在各自部署点附近寻找同侧可达目标；不隔墙追击，也不绕着英雄转。虚线圈是活动边界，细实线圈是自身射程。';}
function setCompanionMode(mode){companionMode=mode==='station'?'station':'roam';updateCompanionControls();render();}
function redeployCompanions(){companions.forEach((p,i)=>{p.x=cap(hero.x+(i?38:-38),65,735);p.y=cap(hero.y+40,70,395);p.ax=p.x;p.ay=p.y;p.cooldown=.2;p.buffedUntil=0;});shots=shots.filter(s=>!s.source);render();$('gp-companion-description').textContent='已明确召回并部署到英雄当前附近；部署点现在固定，后续不会自动跟随英雄。';}
function clearCompanionPath(from,to){const d=dist(from,to),steps=Math.max(1,Math.ceil(d/5));for(let i=1;i<=steps;i++){const x=from.x+(to.x-from.x)*i/steps,y=from.y+(to.y-from.y)*i/steps;if(WALLS.some(w=>circleWall(x,y,13,w)))return false}return true;}
function stepCompanions(dt){companions.forEach(p=>{p.cooldown-=dt;const targets=enemies.filter(e=>e.alive&&Math.hypot(e.x-p.ax,e.y-p.ay)<=p.leash+p.range&&clearCompanionPath(p,e)&&clearCompanionPath({x:p.ax,y:p.ay},e)).sort((a,b)=>dist(a,p)-dist(b,p));const target=targets[0];if(companionMode==='roam'){let tx=p.ax,ty=p.ay;if(target){const reach=p.role==='shooter'?p.range*.68:100;const d=Math.max(1,dist(target,p));if(d>reach){tx=target.x;ty=target.y;}else{tx=p.x;ty=p.y;}}const ad=Math.hypot(tx-p.ax,ty-p.ay);if(ad>p.leash){tx=p.ax+(tx-p.ax)/ad*p.leash;ty=p.ay+(ty-p.ay)/ad*p.leash;}if(!clearCompanionPath(p,{x:tx,y:ty})){tx=p.ax;ty=p.ay;}const d=Math.hypot(tx-p.x,ty-p.y),v=Math.min(d,(p.role==='shooter'?42:32)*dt);if(d>.5){moveSolid(p,(tx-p.x)/d*v,(ty-p.y)/d*v,13);}}
if(p.role==='shooter'&&target&&dist(target,p)<=p.range&&p.cooldown<=0){p.cooldown=.95;shot(p,target,'#398c78',.28,p.id);hit(target,time<p.buffedUntil?3.2:2.7,'summon');}
if(p.role==='shield'&&p.cooldown<=0){p.cooldown=1.8;let nearby=false;if(dist(hero,p)<=p.range){hero.hp=Math.min(100*config.health,hero.hp+2);nearby=true;}companions.forEach(ally=>{if(ally!==p&&dist(ally,p)<=p.range){ally.buffedUntil=time+1.5;nearby=true;}});if(nearby)effects.push({x:p.x,y:p.y,life:.4,max:.4,kind:'shield',radius:p.range});}});}
function setPlaying(value){playing=Boolean(value)&&!waveEnded&&runState.phase==='combat';if(frame){cancelAnimationFrame(frame);frame=0}last=0;$('gp-play').textContent=playing?'暂停示意 Ⅱ':'播放示意 ▶';$('gp-play').setAttribute('aria-pressed',String(playing));if(!economyActive||(runState.phase==='combat'&&!waveEnded))$('gp-status').textContent=(playing?'播放中':'已暂停')+' · 临时强攻 '+Math.max(0,Math.ceil(buffs.power-time))+'秒 / 轻快 '+Math.max(0,Math.ceil(buffs.haste-time))+'秒 · 轻击汽水 '+Math.max(0,Math.ceil(shopItems.tonic?10-time:0))+'秒 · 波次剩余 '+Math.max(0,Math.ceil(WAVE_SECONDS-time))+'秒 · 弹射 '+config.traits.ricochetBounces+'次 / 搜索 '+config.traits.ricochetRange+'px';if(playing)frame=requestAnimationFrame(tick);}
function hit(e,amount,kind){if(!e.alive)return;e.hp-=amount*effectiveDamage();e.flash=.14;e.hitAt=time;if(e.hp<=0){e.alive=false;e.respawn=time+1.3+(e.id%3)*.2;kills++;creditKill(e);effects.push({x:e.x,y:e.y,life:.35,max:.35,kind:'pop'});}}

function enemyMaxHP(){return 8*config.risks.enemyHealth}
function basicReach(){return BASIC.range*config.basicRange}
function creditKill(e){const amount=config.risks.coinMultiplier;if(!economyActive){coins+=amount;return}if(!playing||waveEnded||runState.phase!=='combat')return;const event={runToken:runState.runToken,wave:runState.wave,enemyLifeId:String(runState.wave)+':'+e.id+':'+e.life,coins:amount};if(window.CandyRun&&typeof window.CandyRun.onKill==='function'&&window.CandyRun.onKill(event)===true)coins+=amount;}
function finishWave(){if(waveEnded||runState.phase!=='combat')return false;waveEnded=true;setPlaying(false);$('gp-play').disabled=true;$('gp-finish').disabled=true;$('gp-status').textContent='本波已结束 · 去波末补给，选择增益与兑换道具；快进没有额外金币。';if(window.CandyRun&&typeof window.CandyRun.finishWave==='function')window.CandyRun.finishWave({runToken:runState.runToken,wave:runState.wave});return true;}
function effectiveBasicSpeed(){return config.basicSpeed*(shopItems.tonic&&time<10?1.2:1)}
function effectiveDamage(){return config.damage+(buffs.power>time?.25:0)}
function effectiveSpeed(){return (config.speed+.08*shopItems.shoes)*config.risks.playerSpeed+(buffs.haste>time?.2:0)}
function circleWall(x,y,r,w){return x>w.x-r&&x<w.x+w.w+r&&y>w.y-r&&y<w.y+w.h+r}
function moveSolid(e,dx,dy,r=19,reflect=false){const steps=Math.max(1,Math.ceil(Math.max(Math.abs(dx),Math.abs(dy))/5));let bounced=false;for(let i=0;i<steps;i++){let nx=cap(e.x+dx/steps,48+r,752-r),ny=cap(e.y+dy/steps,48+r,422-r),bx=nx!==e.x+dx/steps,by=ny!==e.y+dy/steps;for(const w of WALLS){if(circleWall(nx,e.y,r,w)){nx=e.x;bx=true}if(circleWall(nx,ny,r,w)){ny=e.y;by=true}}e.x=nx;e.y=ny;if(reflect&&(bx||by)){if(time-e.wallAt>.08){if(bx)e.vx=-e.vx*.75;if(by)e.vy=-e.vy*.75;e.bounces++;e.wallAt=time;bounced=true;if(e.bounces>=3){e.vx=0;e.vy=0;e.knockUntil=time;}}break;}}return bounced;}
function navigationTarget(e,target){if(e.waypoints&&e.waypoints.length){if(dist(e,e.waypoints[0])<6)e.waypoints.shift();if(e.waypoints.length)return e.waypoints[0];}for(const w of WALLS){const right=e.x<w.x-18&&target.x>w.x+w.w+18,left=e.x>w.x+w.w+18&&target.x<w.x-18;if((right||left)&&e.y>w.y-28&&e.y<w.y+w.h+28){const top=w.y-38,bottom=w.y+w.h+38,gap=Math.abs(e.y-top)<Math.abs(e.y-bottom)?top:bottom;e.waypoints=[{x:e.x,y:gap},{x:right?w.x+w.w+32:w.x-32,y:gap}];return e.waypoints[0]}}return target;}
function inImpactFan(e,origin,angle){const d=dist(e,origin);const delta=Math.atan2(Math.sin(Math.atan2(e.y-origin.y,e.x-origin.x)-angle),Math.cos(Math.atan2(e.y-origin.y,e.x-origin.x)-angle));return d<=config.traits.impactRange&&Math.abs(delta)<=IMPACT.halfAngle;}
function startKnock(e,angle){e.vx=Math.cos(angle)*config.traits.impactForce;e.vy=Math.sin(angle)*config.traits.impactForce;e.knockUntil=time+.8;e.bounces=0;e.blastUsed=false;e.wallAt=-1;}
function knockCollision(e){if(e.blastUsed||!e.alive)return;for(const other of enemies){if(other===e||!other.alive||dist(e,other)>37)continue;const key=[e.id,other.id].sort().join(':');if((pairCooldowns.get(key)||-1)>time)continue;pairCooldowns.set(key,time+1.2);e.blastUsed=true;effects.push({x:other.x,y:other.y,life:.35,max:.35,kind:'blast',radius:55});const center={x:other.x,y:other.y};enemies.forEach(v=>{if(v.alive&&dist(v,center)<=55)hit(v,1.8,'collision')});break;}}
function updatePickups(){pickups.forEach(p=>{if(time>=p.ready&&dist(hero,p)<26){buffs[p.kind]=time+8;p.ready=time+21;effects.push({x:p.x,y:p.y,life:.6,max:.6,kind:'shield'});}});}
function clearShot(from,to){const d=dist(from,to),steps=Math.max(1,Math.ceil(d/5));for(let i=1;i<=steps;i++){const x=from.x+(to.x-from.x)*i/steps,y=from.y+(to.y-from.y)*i/steps;if(WALLS.some(w=>circleWall(x,y,2,w)))return false}return true;}
function findRicochetTarget(origin,visited){return enemies.filter(e=>e.alive&&!visited.has(e.id)&&dist(e,origin)<=config.traits.ricochetRange&&clearShot(origin,e)).sort((a,b)=>dist(a,origin)-dist(b,origin)||a.id-b.id)[0]}
function closest(from,exclude){return enemies.filter(e=>e.alive&&e.id!==exclude).sort((a,b)=>dist(a,from)-dist(b,from))[0]}
function shot(a,b,color,life=.2,source=null){if(!b)return;shots.push({x:a.x,y:a.y,tx:b.x,ty:b.y,color,life,max:life,source});if(shots.length>16)shots.shift();}
function step(dt){time+=dt;clock+=dt*effectiveSpeed();hero.x=400+80*Math.cos(clock*.45);hero.y=235+48*Math.sin(clock*.45);hero.flash=Math.max(0,hero.flash-dt);updatePickups();for(const [key,until] of pairCooldowns)if(until<time)pairCooldowns.delete(key);enemies.forEach(e=>{e.flash=Math.max(0,e.flash-dt);if(!e.alive){if(time>=e.respawn){const p=starts[(e.id+Math.floor(time/4))%starts.length];e.x=p[0];e.y=p[1];e.hp=enemyMaxHP();e.life++;e.alive=true;e.hitAt=time;e.vx=0;e.vy=0;e.knockUntil=0;e.blastUsed=false;e.waypoints=[];}return;}const d=Math.max(1,dist(e,hero));let speed=(25+(e.id%3)*3)*config.risks.enemySpeed;if(has('honey')&&d<config.traits.honeyRadius)speed*=config.traits.honeySlow;if(e.knockUntil>time){moveSolid(e,e.vx*dt,e.vy*dt,19,true);const decay=Math.exp(-2.3*dt);e.vx*=decay;e.vy*=decay;knockCollision(e);}else if(charge.phase==='dash'&&e.id===charge.enemy){moveSolid(e,charge.dx*155*config.risks.enemySpeed*dt,charge.dy*155*config.risks.enemySpeed*dt);}else if(!(charge.phase==='warn'&&e.id===charge.enemy)){const target=navigationTarget(e,hero),dd=Math.max(1,dist(e,target));moveSolid(e,(target.x-e.x)/dd*speed*dt,(target.y-e.y)/dd*speed*dt);}if(d<30&&time-e.hitAt>.7){hero.hp-=7;hero.flash=.2;e.hitAt=time;const m=Math.max(1,dist(e,hero));moveSolid(e,((e.x-hero.x)/m)*25,((e.y-hero.y)/m)*25);}
if(has('orbit')){for(let j=0;j<config.traits.orbitCount;j++){const a=time*2.1+j*Math.PI*2/config.traits.orbitCount,orb={x:hero.x+config.traits.orbitRadius*Math.cos(a),y:hero.y+config.traits.orbitRadius*Math.sin(a)};if(dist(e,orb)<25&&time-e.hitAt>.3){hit(e,2.1,'orbit');break;}}}});
if(hero.hp<=0){hero.hp=100*config.health;effects.push({x:hero.x,y:hero.y,life:.65,max:.65,kind:'shield'});}
basicCooldown-=dt;if(basicCooldown<=0){const target=closest(hero);if(target&&dist(target,hero)<=basicReach()){basicCooldown=BASIC.cooldown/effectiveBasicSpeed();const a=Math.atan2(target.y-hero.y,target.x-hero.x);effects.push({x:hero.x,y:hero.y,life:.18,max:.18,kind:'basic',angle:a});hit(target,BASIC.damage*(1+.08*shopItems.gloves)*config.risks.basicDamage,'basic');}}
for(const k of ['crit','honey','summon','bounce'])timers[k]-=dt;
if(has('crit')&&timers.crit<=0){timers.crit=.85;const first=enemies.filter(e=>e.alive&&dist(hero,e)<=260&&clearShot(hero,e)).sort((a,b)=>dist(a,hero)-dist(b,hero))[0];if(first){let previous=first;const visited=new Set([first.id]);shot(hero,first,'#e17a38');hit(first,3,'crit');for(let hop=0;hop<config.traits.ricochetBounces;hop++){effects.push({x:previous.x,y:previous.y,life:.25,max:.25,kind:'search',radius:config.traits.ricochetRange});const next=findRicochetTarget(previous,visited);if(!next)break;visited.add(next.id);shot(previous,next,'#e7a25d',.3);hit(next,3*Math.pow(.5,hop+1),'crit');previous=next;}}}
if(has('honey')&&timers.honey<=0){timers.honey=.6;enemies.forEach(e=>{if(e.alive&&dist(e,hero)<config.traits.honeyRadius)hit(e,1.25,'honey')});effects.push({x:hero.x,y:hero.y,life:.35,max:.35,kind:'honey'});}
if(has('summon'))stepCompanions(dt);
if(has('bounce')&&timers.bounce<=0){const target=closest(hero);if(target&&dist(target,hero)<=config.traits.impactRange){timers.bounce=IMPACT.cooldown;const a=Math.atan2(target.y-hero.y,target.x-hero.x);effects.push({x:hero.x,y:hero.y,life:.24,max:.24,kind:'bounce',angle:a});enemies.forEach(e=>{if(e.alive&&inImpactFan(e,hero,a)){hit(e,IMPACT.damage,'bounce');if(e.alive)startKnock(e,a);}});}}
if(time>=charge.at&&charge.phase==='idle'){const e=enemies.find(e=>e.alive);if(e){charge={phase:'warn',enemy:e.id,target:{x:hero.x,y:hero.y},from:{x:e.x,y:e.y},at:time+1};}}
if(time>=charge.at&&charge.phase==='warn'){const e=enemies[charge.enemy];const dx=charge.target.x-charge.from.x,dy=charge.target.y-charge.from.y,d=Math.max(1,Math.hypot(dx,dy));charge.dx=dx/d;charge.dy=dy/d;charge.phase=e.alive?'dash':'idle';charge.at=time+(e.alive?.7:3);}
else if(time>=charge.at&&charge.phase==='dash'){charge.phase='idle';charge.at=time+4;}
shots.forEach(s=>s.life-=dt);shots=shots.filter(s=>s.life>0);effects.forEach(e=>e.life-=dt);effects=effects.filter(e=>e.life>0).slice(-20);if(economyActive&&time>=WAVE_SECONDS)finishWave();}
function robot(e){const hp=cap(e.hp/enemyMaxHP(),0,1);return '<g transform="translate('+f(e.x)+' '+f(e.y)+')" opacity="'+(e.flash>0?'.65':'1')+'"><ellipse cy="18" rx="20" ry="7" fill="#5f71602b"/><g transform="translate(17 -16)"><path d="M0 0v15" stroke="#bd914c" stroke-width="4"/><circle cx="-5" r="5" fill="none" stroke="#bd914c" stroke-width="3"/><circle cx="5" r="5" fill="none" stroke="#bd914c" stroke-width="3"/></g><circle r="19" fill="'+(e.flash>0?'#fff2bb':'#544061')+'" stroke="#352b43" stroke-width="2"/><circle cx="-6" cy="-3" r="5" fill="#d1aa60"/><path d="m4-7 8 8m0-8-8 8" stroke="#d1aa60" stroke-width="3"/><path d="M-8 10h16" stroke="#b89865" stroke-width="2"/><rect x="-17" y="26" width="34" height="3" rx="2" fill="#b2b5a0"/><rect x="-17" y="26" width="'+f(34*hp)+'" height="3" rx="2" fill="#725778"/></g>';}
function render(){const x=hero.x,y=hero.y;layers.walls.innerHTML=WALLS.map(w=>'<g><rect x="'+w.x+'" y="'+w.y+'" width="'+w.w+'" height="'+w.h+'" rx="10" fill="#a6bdb2" stroke="#608b7b" stroke-width="3"/><path d="M'+(w.x+7)+' '+(w.y+14)+'v'+(w.h-28)+'" stroke="#e7f2e7" stroke-width="4"/></g>').join('');layers.pickups.innerHTML=pickups.filter(p=>time>=p.ready).map(p=>'<g transform="translate('+p.x+' '+p.y+')"><path d="M-13 0-7-12H7L13 0 7 12H-7Z" fill="'+(p.kind==='power'?'#ffb1a0':'#b6dff1')+'" stroke="#6a6b69" stroke-width="2"/><text y="4" text-anchor="middle" font-size="13">'+(p.kind==='power'?'+':'↗')+'</text><text y="27" text-anchor="middle" font-size="10" fill="#44564b">'+(p.kind==='power'?'强攻8秒':'轻快8秒')+'</text></g>').join('');let zones='',fx='',pets='';if(has('honey'))zones+='<circle cx="'+f(x)+'" cy="'+f(y)+'" r="'+config.traits.honeyRadius+'" fill="#ffe52c" fill-opacity=".24" stroke="#b6a01b" stroke-width="3" stroke-dasharray="7 5"/>';if(has('orbit')){zones+='<circle cx="'+f(x)+'" cy="'+f(y)+'" r="'+config.traits.orbitRadius+'" fill="none" stroke="#a17bcb" stroke-width="2" opacity=".65"/>';for(let j=0;j<config.traits.orbitCount;j++){const a=time*2.1+j*Math.PI*2/config.traits.orbitCount,ox=x+config.traits.orbitRadius*Math.cos(a),oy=y+config.traits.orbitRadius*Math.sin(a);fx+='<g transform="translate('+f(ox)+' '+f(oy)+') rotate('+f(a*180/Math.PI)+')"><path d="M-19-7v14l10-7ZM19-7v14L9 0Z" fill="#bea3e0"/><circle r="11" fill="#9b75c6" stroke="#fff1" stroke-width="3"/><path d="M-6-7 6 7" stroke="#f7ddff" stroke-width="3"/></g>';}}
if(has('summon')){companions.forEach(p=>{zones+='<circle cx="'+f(p.ax)+'" cy="'+f(p.ay)+'" r="'+p.leash+'" fill="none" stroke="#77a792" opacity=".24" stroke-dasharray="3 7"/><path d="M'+f(p.ax-5)+' '+f(p.ay)+'h10m-5-5v10" stroke="#56856f" opacity=".6"/>';pets+='<g transform="translate('+f(p.x)+' '+f(p.y)+')"><circle r="'+p.range+'" fill="none" stroke="#469078" opacity=".22" stroke-width="1"/>'+(p.role==='shooter'?'<rect x="-13" y="-13" width="26" height="26" rx="8" fill="#8bc9ac" stroke="#36896a" stroke-width="2"/><circle cx="-4" cy="-2" r="2"/><circle cx="5" cy="-2" r="2"/><path d="M10 3h13" stroke="#dbb06b" stroke-width="6"/>':'<circle r="13" fill="#fff2ce" stroke="#6ea695" stroke-width="2"/><path d="M-7-3 0-8 7-3 5 6 0 10-5 6Z" fill="#96d9cf"/>')+'<text y="28" text-anchor="middle" font-size="10" fill="#376a56">'+(p.role==='shooter'?'射手伙伴':'护盾伙伴')+'</text></g>';});}

layers.zones.innerHTML=zones;layers.enemies.innerHTML=enemies.filter(e=>e.alive).map(robot).join('');layers.shots.innerHTML=shots.map(s=>'<path d="M'+f(s.x)+' '+f(s.y)+'L'+f(s.tx)+' '+f(s.ty)+'" stroke="'+s.color+'" stroke-width="'+f(3+config.damage)+'" opacity="'+f(s.life/s.max)+'" fill="none" stroke-linecap="round"/>').join('');
layers.tele.innerHTML=charge.phase==='warn'?'<path d="M'+f(charge.from.x)+' '+f(charge.from.y)+'L'+f(charge.target.x)+' '+f(charge.target.y)+'" stroke="#97452f" stroke-width="24" opacity=".16"/><path d="M'+f(charge.from.x)+' '+f(charge.from.y)+'L'+f(charge.target.x)+' '+f(charge.target.y)+'" stroke="#97452f" stroke-width="3" stroke-dasharray="8 5"/><circle cx="'+f(charge.target.x)+'" cy="'+f(charge.target.y)+'" r="16" stroke="#97452f" stroke-width="2" fill="none"/>':'';
const maxhp=100*config.health,hp=cap(hero.hp/maxhp,0,1);layers.hero.innerHTML=pets+'<g transform="translate('+f(x)+' '+f(y)+')" filter="url(#gp-shadow)"><circle r="24" fill="'+(hero.flash>0?'#fff':COLORS[config.character])+'"/><ellipse cx="-13" cy="-21" rx="9" ry="15" fill="'+COLORS[config.character]+'"/><ellipse cx="13" cy="-21" rx="9" ry="15" fill="'+COLORS[config.character]+'"/><ellipse cy="2" rx="17" ry="14" fill="#fff2d5"/><circle cx="-6" r="3" fill="#343044"/><circle cx="6" r="3" fill="#343044"/><path d="M-5 8q5 5 10 0" fill="none" stroke="#343044" stroke-width="2"/><rect x="-30" y="34" width="60" height="6" rx="3" fill="#a4b39c"/><rect x="-30" y="34" width="'+f(60*hp)+'" height="6" rx="3" fill="#468f78"/><text y="54" text-anchor="middle" font-size="10" fill="#395346">生命上限 '+Math.round(maxhp)+'</text></g>';
effects.forEach(e=>{const p=1-e.life/e.max;if(e.kind==='bounce'||e.kind==='basic'){const r=e.kind==='basic'?basicReach():config.traits.impactRange,half=e.kind==='basic'?.65:IMPACT.halfAngle,a=e.angle||0,x1=e.x+r*Math.cos(a-half),y1=e.y+r*Math.sin(a-half),x2=e.x+r*Math.cos(a+half),y2=e.y+r*Math.sin(a+half);fx+='<path d="M'+f(e.x)+' '+f(e.y)+'L'+f(x1)+' '+f(y1)+'A'+r+' '+r+' 0 0 1 '+f(x2)+' '+f(y2)+'Z" fill="'+(e.kind==='basic'?'#ffffff':'#ef8fb0')+'" fill-opacity="'+f(.45*(1-p))+'" stroke="'+(e.kind==='basic'?'#fffaf0':'#c85e8d')+'" stroke-width="2"/>';}else fx+='<circle cx="'+f(e.x)+'" cy="'+f(e.y)+'" r="'+f(e.kind==='honey'?config.traits.honeyRadius-13+p*14:e.radius?e.radius*p:15+p*30)+'" fill="none" stroke="'+(e.kind==='honey'?'#d2b800':e.kind==='search'?'#db9c55':e.kind==='blast'?'#d27762':e.kind==='shield'?'#65b2a2':'#c7a568')+'" opacity="'+f(1-p)+'" stroke-width="3"/>';});layers.fx.innerHTML=fx;if(time-uiTime>.3||time===0){uiTime=time;$('gp-hud').textContent='击破 '+kills+' / 本波赚 '+coins+' / 可用 '+runState.balance+' / 敌HP '+enemyMaxHP().toFixed(0)+' · '+effectiveDamage().toFixed(2)+'× 伤害 / '+effectiveSpeed().toFixed(2)+'× 移动';if(!economyActive||(runState.phase==='combat'&&!waveEnded))$('gp-status').textContent=(playing?'播放中':'已暂停')+' · 临时强攻 '+Math.max(0,Math.ceil(buffs.power-time))+'秒 / 轻快 '+Math.max(0,Math.ceil(buffs.haste-time))+'秒 · 轻击汽水 '+Math.max(0,Math.ceil(shopItems.tonic?10-time:0))+'秒 · 波次剩余 '+Math.max(0,Math.ceil(WAVE_SECONDS-time))+'秒 · 弹射 '+config.traits.ricochetBounces+'次 / 搜索 '+config.traits.ricochetRange+'px';}}
function tick(ts){if(!playing)return;const dt=last?Math.min((ts-last)/1000,.04):0;last=ts;step(dt);render();if(playing)frame=requestAnimationFrame(tick);else frame=0;}
window.updateGameplayConcept=function(next,options={}){const key=Object.hasOwn(NAMES,next.character)?next.character:'orbit';const list=Array.from(new Set((next.mechanisms||[key]).filter(k=>Object.hasOwn(NAMES,k)))).slice(0,5);if(key!==config.character)companionMode='roam';const trait=next.traits||{},risk=next.risks||{};config={character:key,mechanisms:list.length?list:[key],damage:cap(Number(next.damage)||1,.1,5),health:cap((Number(next.health)||1)*(Number(risk.playerHealth)||1)*(shopItems.shield?1.25:1),.3,5),speed:cap(Number(next.speed)||1,.7,2.5),basicSpeed:cap(Number(next.basicSpeed)||1,.5,3),basicRange:cap(Number(next.basicRange)||1,.5,2),traits:{orbitCount:Math.round(cap(Number(trait.orbitCount)||2,2,4)),orbitRadius:cap(Number(trait.orbitRadius)||76,60,110),ricochetBounces:Math.round(cap(Number(trait.ricochetBounces)||1,1,4)),ricochetRange:cap(Number(trait.ricochetRange)||120,60,300),honeyRadius:cap(Number(trait.honeyRadius)||103,80,145),honeySlow:cap(Number(trait.honeySlow)||.45,.25,.8),summonCount:Math.round(cap(Number(trait.summonCount)||(key==='summon'?2:1),1,4)),impactRange:cap(Number(trait.impactRange)||68,45,80),impactForce:cap(Number(trait.impactForce)||220,100,300)},risks:{enemyHealth:cap(Number(risk.enemyHealth)||1,1,3),coinMultiplier:Math.round(cap(Number(risk.coinMultiplier)||1,1,4)),basicDamage:cap(Number(risk.basicDamage)||1,.5,3),enemySpeed:cap(Number(risk.enemySpeed)||1,.5,2),playerSpeed:cap(Number(risk.playerSpeed)||1,.5,2)}};if(!economyActive||options.forceReset)resetScene();else render();document.querySelectorAll('[data-gp-character]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.gpCharacter===key)));$('gp-explain').textContent='当前可见机制：'+config.mechanisms.map(k=>NAMES[k]).join(' + ')+'。伤害 '+config.damage.toFixed(2)+'× / 最大生命 '+config.health.toFixed(2)+'× / 移动 '+config.speed.toFixed(2)+'×。基础近战攻速 '+effectiveBasicSpeed().toFixed(2)+'× / 范围 '+config.basicRange.toFixed(2)+'×；弹射搜索 '+config.traits.ricochetRange+'px，最多 '+config.traits.ricochetBounces+' 次，单次投射不重复目标。商店：绒垫护符 '+(shopItems.shield?'已启用':'无')+' / 弹性鞋垫 '+shopItems.shoes+'级 / 软拳护套 '+shopItems.gloves+'级。'+(config.mechanisms.includes('bounce')&&config.mechanisms.includes('honey')?'冲击可能把敌人推离蜜圈，融合不一定只有好处。':'敌人按规则受击；倒下后从边缘重新入场。')};
window.finishGameplayWave=finishWave;
window.setGameplayRunState=function(next){const token=String(next.runToken),wave=Math.max(1,Number(next.wave)||1),fresh=!economyActive||token!==runState.runToken||wave!==runState.wave;const phase=['combat','between','complete'].includes(next.phase)?next.phase:'between';if(fresh||phase!=='combat')setPlaying(false);economyActive=true;runState={runToken:token,wave,phase,balance:Math.max(0,Number(next.balance)||0)};const items=next.items||{};shopItems={shield:!!items.shield,tonic:!!items.tonic,shoes:Math.round(cap(Number(items.shoes)||0,0,2)),gloves:Math.round(cap(Number(items.gloves)||0,0,2))};window.updateGameplayConcept(next.config||config,{forceReset:fresh});if(phase!=='combat')waveEnded=true;$('gp-play').disabled=phase!=='combat'||waveEnded;$('gp-finish').disabled=phase!=='combat'||waveEnded;if(fresh&&phase==='combat'){$('gp-status').textContent='第 '+wave+' 波已就绪 · 点击播放；演示生命已重置，波内道具另计。';}else if(phase!=='combat'){$('gp-status').textContent=phase==='complete'?'六波演示完成 · 查看结算与已购道具。':'波末补给 · 战斗已暂停，完成选择后进入下一波。';}};
$('gp-roam').onclick=()=>setCompanionMode('roam');$('gp-station').onclick=()=>setCompanionMode('station');$('gp-redeploy').onclick=redeployCompanions;
$('gp-play').onclick=()=>setPlaying(!playing);$('gp-finish').onclick=finishWave;
document.querySelectorAll('[data-gp-character]').forEach(b=>b.onclick=()=>{const key=b.dataset.gpCharacter;if(typeof window.selectConceptCharacter==='function')window.selectConceptCharacter(key);else window.updateGameplayConcept({character:key,mechanisms:[key],damage:1,health:1,speed:1});});
document.addEventListener('visibilitychange',()=>{if(document.hidden)setPlaying(false)});
const reduce=window.matchMedia('(prefers-reduced-motion: reduce)');reduce.addEventListener('change',e=>{if(e.matches)setPlaying(false)});
window.updateGameplayConcept(config);setPlaying(false);
})();
```

### 脚本 2

```javascript
(function(root){
'use strict';
const names={orbit:'星绒',crit:'脆跳',honey:'蜜团',summon:'棉队',bounce:'砰砰'};
const labels={orbit:'糖果星环',crit:'有限弹射',honey:'蜂蜜领域',summon:'独立伙伴',bounce:'抱枕冲击'};
const catalog=[];
function add(id,slot,name,effect,cap=1,parent=null,group=''){catalog.push({id,slot,name,effect,cap,parent,group});}
for(const key of Object.keys(names))add(key+'.root','trait',labels[key],{orbit:'增加 2 颗随主角转动的糖果；有独立命中冷却。',crit:'增加射击；命中后最多反射 1 次，仅搜寻 120 示意单位内未命中过的敌人。',honey:'增加半径 103 的蜂蜜圈；圈内敌人速度为 45%，并受持续伤害。',summon:'增加独立地图伙伴：棉队初始 2 位，跨角色新学 1 位；可游走或驻守。',bounce:'增加 68 距离的小扇形击退；撞怪范围伤害、撞墙反弹，有次数与冷却上限。'}[key],1,null,key);
add('orbit.more','trait','多一颗星糖','每级糖果 +1，最多 4 颗；共享单目标命中冷却，不代表伤害线性翻倍。',2,'orbit.root','orbit');
add('orbit.wide','trait','外轨拓展','每级轨道半径 +12；初始 76，最多 100。离得太近仍有轨道空隙。',2,'orbit.root','orbit');
add('crit.chain','trait','接力弹射','每级多反射 1 个目标；最多 3 次。每跳仍受搜寻距离限制，不重复命中同一目标。',2,'crit.root','crit');
add('crit.reach','trait','远距寻路','每级反射搜寻距离 +45；120 → 165 → 210。只扩每跳搜寻距离，不加反射次数。',2,'crit.root','crit');
add('crit.relay','trait','终段接力','前置接力弹射 2 级；再增加 1 次反射，总上限 4 次，仍受每跳距离与不重复目标限制。',1,'crit.chain','crit');
catalog[catalog.length-1].parentLevel=2;
add('honey.wide','trait','蜜圈扩展','每级蜂蜜半径 +14；103 → 117 → 131。',2,'honey.root','honey');
add('honey.sticky','trait','浓蜜配方','每级圈内敌人速度系数 −0.10；45% → 35% → 25%，不无限叠加。',2,'honey.root','honey');
add('summon.team','trait','伙伴扩编','每级新增 1 位伙伴，上限 4 位；每位都有独立位置、索敌和攻击冷却。',2,'summon.root','summon');
add('bounce.reach','trait','短冲延伸','每级冲击距离 +6；68 → 74 → 80。扇形角度保持约 51°，不会变成大扫屏。',2,'bounce.root','bounce');
add('bounce.force','trait','弹性填充','每级击退初速度 +35；220 → 255 → 290。仍限制反弹次数与碰撞连锁。',2,'bounce.root','bounce');
add('base.health','base','厚实绒面','最大生命 +10% 基准值；不改变任何技能伤害。',5);
add('base.speed','base','轻快连击','统一基础近战攻击速度 +10% 基准值；星环、弹射、领域、伙伴与冲击冷却不变。',5);
add('base.range','base','伸展挥击','统一基础近战范围 +10% 基准值；不扩大任何角色特性范围。',5);
add('risk.bounty','risk','悬赏加码','每级敌人最大生命 +50% 基准值；每次击杀金币 +100% 基准值。更难清怪；额外金币可在波末商店兑换道具。',2);
add('risk.glass','risk','脆壳利刃','仅基础近战伤害 ×1.60；最大生命 ×0.65。角色特性不增伤；更容易失误。',1);
add('risk.fleet','risk','加速齿轮','每级玩家移动速度 +20% 基准值；敌人移动速度也 +20%。更易转场，但追兵更快。',2);
const byId=Object.fromEntries(catalog.map(c=>[c.id,c]));
function initial(character='orbit',seed=1030){return {character,seed,levels:{[character+'.root']:1},picks:[]};}
function eligible(c,s){return (s.levels[c.id]||0)<c.cap&&(!c.parent||(s.levels[c.parent]||0)>=(c.parentLevel||1));}
function hash(str){let x=2166136261;for(const c of str){x^=c.charCodeAt(0);x=Math.imul(x,16777619);}return x>>>0;}
function offers(s){if(s.picks.length>=5)return [];return ['trait','base','risk'].map(slot=>{const pool=catalog.filter(c=>c.slot===slot&&eligible(c,s));if(!pool.length)throw Error('empty '+slot);return pool[hash(s.seed+'|'+s.character+'|'+s.picks.join(',')+'|'+slot)%pool.length];});}
function choose(s,id){if(!offers(s).some(c=>c.id===id))throw Error('Card is not currently offered');return {...s,levels:{...s.levels,[id]:(s.levels[id]||0)+1},picks:[...s.picks,id]};}
function config(s){const l=id=>s.levels[id]||0;return {character:s.character,mechanisms:Object.keys(names).filter(k=>l(k+'.root')),damage:1,health:1+.10*l('base.health'),speed:1,basicSpeed:1+.10*l('base.speed'),basicRange:1+.10*l('base.range'),traits:{orbitCount:2+l('orbit.more'),orbitRadius:76+12*l('orbit.wide'),ricochetBounces:1+l('crit.chain')+l('crit.relay'),ricochetRange:120+45*l('crit.reach'),honeyRadius:103+14*l('honey.wide'),honeySlow:.45-.10*l('honey.sticky'),summonCount:Math.min(4,(s.character==='summon'?2:1)+l('summon.team')),impactRange:68+6*l('bounce.reach'),impactForce:220+35*l('bounce.force')},risks:{enemyHealth:1+.5*l('risk.bounty'),coinMultiplier:1+l('risk.bounty'),basicDamage:l('risk.glass')?1.6:1,playerHealth:l('risk.glass')?.65:1,enemySpeed:1+.2*l('risk.fleet'),playerSpeed:1+.2*l('risk.fleet')},ownedLevels:{...s.levels}};}
root.CandyCatalogModel={names,labels,catalog,byId,initial,eligible,offers,choose,config};
})(globalThis);
(function(root){'use strict';
const items=[
{id:'shield',name:'绒垫护符',price:2,kind:'下波消耗品',duration:'下一波整波',cap:1,effect:'下一波最大生命 ×1.25；进入下一波时消耗，不叠加。',downside:'不提高攻击或金币收益。'},
{id:'tonic',name:'轻击汽水',price:3,kind:'下波限时消耗品',duration:'下一波开始后的前 10 秒战斗时间',cap:1,effect:'下一波前 10 秒，基础近战攻速 ×1.20；暂停时不计时，不提高特性频率。',downside:'第 10 秒自动失效；不会保留到再下一波。'},
{id:'shoes',name:'弹性鞋垫',price:5,kind:'本局装备',duration:'本局剩余波次',cap:2,effect:'每件移动速度 +8% 基准值；最多 2 件，从下一波生效。',downside:'不增加生命、普攻或特性伤害；新局清空。'},
{id:'gloves',name:'软拳护套',price:5,kind:'本局装备',duration:'本局剩余波次',cap:2,effect:'每件基础近战伤害 +8% 基准值；最多 2 件，从下一波生效。',downside:'不强化星环、弹射、蜂蜜、伙伴或冲击伤害；新局清空。'}
];const byId=Object.fromEntries(items.map(i=>[i.id,i]));let serial=0;
function create({model,character='orbit',seed=1030,onChange=()=>{}}){let s,seen;function notify(reason){onChange(snapshot(),reason);}function reset(c=s?.cards.character||character,k=s?.cards.seed||seed){s={runToken:'candy-run-'+(++serial),wave:1,phase:'combat',cards:model.initial(c,k),coins:0,earned:0,spent:0,kills:0,cardPicked:false,queued:{shield:false,tonic:false},gear:{shoes:0,gloves:0},active:{shield:false,tonic:false,shoes:0,gloves:0},stock:{shield:1,tonic:1,shoes:1,gloves:1}};seen=new Set();notify('reset');return snapshot();}
function snapshot(){return JSON.parse(JSON.stringify(s));}
function valid(e){return e&&e.runToken===s.runToken&&e.wave===s.wave;}
function onKill(e){if(s.phase!=='combat'||!valid(e)||!['string','number'].includes(typeof e.enemyLifeId)||String(e.enemyLifeId).length===0)return false;const key=s.runToken+'|'+s.wave+'|'+e.enemyLifeId;if(seen.has(key))return false;seen.add(key);const value=model.config(s.cards).risks.coinMultiplier;s.coins+=value;s.earned+=value;s.kills++;notify('kill');return true;}
function finishWave(e){if(s.phase!=='combat'||!valid(e))return false;s.phase=s.wave===6?'complete':'between';s.cardPicked=false;notify('finish');return true;}
function offers(){return s.phase==='between'&&!s.cardPicked?model.offers(s.cards):[];}
function choose(id){if(!offers().some(c=>c.id===id))return false;s.cards=model.choose(s.cards,id);s.cardPicked=true;notify('card');return true;}
function reason(id){const i=byId[id];if(!i)return '未知道具';if(s.phase!=='between')return s.phase==='complete'?'本局已结算':'战斗中不可购买';if(!s.stock[id])return '本补给阶段售罄';if((id==='shield'||id==='tonic')&&s.queued[id])return '已备好下一波';if((id==='shoes'||id==='gloves')&&s.gear[id]>=i.cap)return '已达本局叠加上限';if(s.coins<i.price)return '金币不足，还差 '+(i.price-s.coins)+' 枚';return '';}
function buy(id){if(reason(id))return false;const i=byId[id];s.coins-=i.price;s.spent+=i.price;s.stock[id]--;if(id==='shield'||id==='tonic')s.queued[id]=true;else s.gear[id]++;notify('buy');return true;}
function nextWave(){if(s.phase!=='between'||!s.cardPicked)return false;s.wave++;s.phase='combat';s.active={shield:s.queued.shield,tonic:s.queued.tonic,...s.gear};s.queued={shield:false,tonic:false};s.stock={shield:1,tonic:1,shoes:1,gloves:1};s.cardPicked=false;seen=new Set();notify('next');return true;}
const api={reset,snapshot,onKill,finishWave,offers,choose,buy,nextWave,reason};reset(character,seed);return api;}
root.CandyShopModel={items,byId,create};
})(globalThis);
(function(){'use strict';
const M=window.CandyCatalogModel,S=window.CandyShopModel,$=id=>document.getElementById(id);let run,detail='orbit.root',tab=0;
function elem(tag,text,cls){const e=document.createElement(tag);if(text!==undefined)e.textContent=text;if(cls)e.className=cls;return e;}
const tabs=['ct-tab-build','ct-tab-tree','ct-tab-shop'],panels=['ct-panel-build','card-catalog','item-catalog'];
function showTab(index,focus=false){tab=index;tabs.forEach((id,i)=>{$(id).setAttribute('aria-selected',String(i===index));$(id).tabIndex=i===index?0:-1;$(panels[i]).hidden=i!==index;if(i===index&&focus)$(id).focus();});}
tabs.forEach((id,i)=>{$(id).onclick=()=>showTab(i);$(id).onkeydown=e=>{if(['ArrowLeft','ArrowRight','Home','End'].includes(e.key)){e.preventDefault();showTab(e.key==='Home'?0:e.key==='End'?2:(i+(e.key==='ArrowRight'?1:2))%3,true);}};});
function hashTab(){const index=panels.indexOf(location.hash.slice(1));if(index>=0){showTab(index);$(panels[index]).scrollIntoView({block:'start'});}}window.addEventListener('hashchange',hashTab);
function status(c,s){const l=s.cards.levels[c.id]||0;return l>=c.cap?'已满级':l?'已拥有 · 可升级':M.eligible(c,s.cards)?'可获取':'锁定 · 缺前置';}
function inspect(id){detail=id;const s=run.snapshot(),c=M.byId[id],l=s.cards.levels[id]||0;$('ct-detail').replaceChildren(elem('div',c.id+' · '+status(c,s),'ct-badge'),elem('h3',c.name+'　'+l+' / '+c.cap+' 级'),elem('p',c.effect),elem('p','前置：'+(c.parent?M.byId[c.parent].name+' '+(c.parentLevel||1)+' 级':'无')+'。'+(c.slot==='trait'?'同父分支可同时学习，无互斥。':'不占用机制位。')));document.querySelectorAll('.ct-node[data-card-id]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.cardId===id)));}
function node(c,s){const b=elem('button',undefined,'ct-node'),l=s.cards.levels[c.id]||0;b.type='button';b.dataset.cardId=c.id;b.dataset.status=l?'owned':M.eligible(c,s.cards)?'eligible':'locked';b.setAttribute('aria-pressed',String(c.id===detail));b.append(elem('b',c.name),elem('small',status(c,s)+' · '+l+'/'+c.cap+' 级'));b.onclick=()=>inspect(c.id);return b;}
function renderTrees(s){$('ct-trees').replaceChildren();for(const key of Object.keys(M.names)){const box=elem('article',undefined,'ct-tree');box.append(elem('h3',M.names[key]+' 的起始树'),node(M.byId[key+'.root'],s));const children=elem('div',undefined,'ct-children');M.catalog.filter(c=>c.parent===key+'.root').forEach(c=>{children.append(node(c,s));const descendants=M.catalog.filter(d=>d.parent===c.id);if(descendants.length){const branch=elem('div',undefined,'ct-children');descendants.forEach(d=>branch.append(node(d,s)));children.append(branch);}});box.append(children);$('ct-trees').append(box);}$('ct-extra').replaceChildren();for(const slot of ['base','risk']){const box=elem('article',undefined,'ct-tree');box.append(elem('h3',slot==='base'?'基础词条 · 无前置':'风险规则 · 无前置'));const list=elem('div',undefined,'ct-children');M.catalog.filter(c=>c.slot===slot).forEach(c=>list.append(node(c,s)));box.append(list);$('ct-extra').append(box);}inspect(detail);}
function renderShop(s){$('shop-grid').replaceChildren();for(const item of S.items){const box=elem('article',undefined,'ct-tree shop-item');box.dataset.itemId=item.id;box.append(elem('span',item.kind,'ct-badge'),elem('h3',item.name),elem('strong',item.price+' 枚金币','shop-price'),elem('p',item.effect),elem('p',item.downside,'ct-note'),elem('p','持续：'+item.duration,'ct-note'));const owned=item.id==='shield'||item.id==='tonic'?(s.queued[item.id]?'已备好下一波':'未备货'):'已装备 '+s.gear[item.id]+' / '+item.cap;box.append(elem('p',owned+' · 每个补给阶段限购 1 件','ct-note'));const reason=run.reason(item.id),button=elem('button',reason||'兑换 · '+item.price+' 金币','btn light');button.type='button';button.disabled=Boolean(reason);button.dataset.buy=item.id;button.onclick=()=>{if(run.buy(item.id))$('shop-feedback').textContent='已兑换 '+item.name+'；从下一波开始生效。';};box.append(button);$('shop-grid').append(box);}}
function balances(s){$('shop-balance').textContent=s.coins+' 枚';$('shop-ledger').textContent='累计获得 '+s.earned+' − 已花费 '+s.spent+' = 余额 '+s.coins+' · '+s.kills+' 次有效击杀';$('ct-wallet').textContent=s.coins+' 枚';$('ct-phase').textContent=s.phase==='combat'?'第 '+s.wave+' / 6 波 · 战斗中':s.phase==='between'?'第 '+s.wave+' / 5 次补给 · 已停战':'本局结算 · 六波演示结束';}
function render(reason){const s=run.snapshot(),config=M.config(s.cards);balances(s);if(reason==='kill'){renderShop(s);window.setGameplayRunState?.({phase:s.phase,runToken:s.runToken,wave:s.wave,config,items:s.active,balance:s.coins});return;}$('ct-round').textContent=s.phase==='between'?(s.cardPicked?'本次免费三选一已领取':'本次免费三选一：请选择 1 张'):(s.phase==='complete'?'5 次构筑完成；查看道具账本与卡片树':'打完本波再选牌与兑换道具');$('ct-title').textContent=M.names[s.cards.character]+' / 起始 '+M.labels[s.cards.character];$('ct-mechanisms').textContent='当前特性：'+config.mechanisms.map(k=>M.labels[k]).join(' + ');$('ct-hp').textContent=(config.health*config.risks.playerHealth).toFixed(2)+'×';$('ct-speed').textContent=config.basicSpeed.toFixed(2)+'×';$('ct-range').textContent=config.basicRange.toFixed(2)+'×';$('ct-coins').textContent=config.risks.coinMultiplier+' 枚';$('ct-perks').replaceChildren();run.offers().forEach((c,i)=>{const b=elem('button',undefined,'perk'),l=s.cards.levels[c.id]||0;b.type='button';b.dataset.offerId=c.id;b.dataset.cardId=c.id;b.dataset.slot=c.slot;b.append(elem('span',['① 随机特性 · 获取 / 升级','② 基础词条 · 不强化角色特性','③ 风险规则 · 收益与代价'][i]),elem('b',c.name+'　'+(l+1)+'/'+c.cap+' 级'),elem('span',c.effect),elem('small',c.id+' · 免费，本次只选 1 张'));b.onclick=()=>{detail=c.id;run.choose(c.id);};$('ct-perks').append(b);});$('ct-result').textContent=s.phase==='combat'?'先在上方演示中点击开始。击杀获得真实本局金币；战斗暂停不发钱，提前结束也没有额外奖励。':s.phase==='between'?(s.cardPicked?'免费卡已领取。道具可选购，也可保留金币；准备好后进入下一波。':'先领取 1 张免费卡；购买道具另花金币，不占用三选一机会。'):'演示结束。金币、装备只属于这局；重开会清空。';$('ct-picked').replaceChildren();s.cards.picks.forEach((id,i)=>$('ct-picked').append(elem('span',(i+1)+'. '+M.byId[id].name,'tag')));document.querySelectorAll('[data-build]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.build===s.cards.character)));$('ct-next-wave').disabled=s.phase!=='between'||!s.cardPicked;$('ct-next-wave').textContent=s.phase==='complete'?'本局已结算':s.phase==='combat'?'等待本波结束':s.cardPicked?'进入第 '+(s.wave+1)+' 波 →':'先选择 1 张免费卡';$('ct-open-shop').textContent=s.phase==='between'?'去兑换道具 →':'查看道具图鉴';$('ct-seed').value=s.cards.seed;renderTrees(s);renderShop(s);window.setGameplayRunState?.({phase:s.phase,runToken:s.runToken,wave:s.wave,config,items:s.active,balance:s.coins});}
function reset(character,seed){detail=(character||run.snapshot().cards.character)+'.root';$('shop-feedback').textContent='';run.reset(character,seed);showTab(0);}
run=S.create({model:M,onChange:(_s,reason)=>{if(run)render(reason);}});
window.CandyRun={onKill:e=>run.onKill(e),finishWave:e=>run.finishWave(e),reset:()=>reset(),snapshot:()=>run.snapshot(),nextWave:()=>run.nextWave()};
window.selectConceptCharacter=key=>{if(M.names[key])reset(key);};document.querySelectorAll('[data-build]').forEach(b=>b.onclick=()=>reset(b.dataset.build));$('ct-reset').onclick=()=>reset();$('ct-apply').onclick=()=>reset(undefined,Math.max(1,Math.min(999999,Math.trunc(Number($('ct-seed').value)||1030))));$('ct-new-seed').onclick=()=>reset(undefined,run.snapshot().cards.seed%999999+1);$('ct-next-wave').onclick=()=>run.nextWave();$('ct-open-shop').onclick=()=>showTab(2);render('init');hashTab();
})();
```

### 脚本 3

```javascript
'use strict';
const $=id=>document.getElementById(id);
const stages=[['战斗 / 60–90 秒 · 提案','走位创造空间','角色依构筑自动攻击：环绕流保持合适距离，投射流寻找射线，领域流牵引怪群。冲击流才主动寻找击退与撞墙机会。'],['收集 / 波内持续','拾取也是走位选择','经验碎片让你冒一点险离开安全区；波末统一选择增益。拾取辅助是舒适度选择，不该成为必选税。'],['构筑 / 波末暂停','三选一，改变下一波','每次分别从特性树、基础词条、风险契约中抽一张：第一格可跨角色获得特性，也可升级已有分支；第二格只加强生命或基础近战；第三格用明确代价换取收益。固定种子可复现，真实权重与数值待测。'],['补给 / 波末商店','金币换来另一种选择','击杀金币可以积攒，也可以在波末兑换消耗品、限时增益或本局装备；免费三选一独立进行。下一波前确认补给，不在战斗中暂停购物。'],['结算 / 胜败都有出口','记住这套，再来一局','胜利展示流派与关键选择；失败说明主要伤害来源，允许一键重试。局内增益清空，局外先只考虑外观与角色解锁。']];
function stage(i){document.querySelectorAll('[data-stage]').forEach(b=>b.setAttribute('aria-pressed',String(+b.dataset.stage===i)));$('stageLabel').textContent=stages[i][0];$('stageTitle').textContent=stages[i][1];$('stageText').textContent=stages[i][2]}
document.querySelectorAll('[data-stage]').forEach(b=>b.onclick=()=>stage(+b.dataset.stage));stage(0);
const enemies=["<b>发条豆兵 · 普通 / 追击</b><p><b>攻击：</b>小步聚拢后短促碰撞，靠数量挤压走位路线。</p><p><b>预警：</b>背部钥匙加快半圈，球壳压低、短腿停顿后再冲出。</p><p><b>应对：</b>绕外缘分割队列，避免被多个豆兵夹住；击退可以打散它们，但不是唯一解法。</p><p><b>设计边界：</b>出生点不贴脸；碰撞群保留缺口，不能所有豆兵同时突进。</p>", "<b>铆钉哨兵 · 普通 / 远程</b><p><b>攻击：</b>发射少量慢速圆头机械弹，压迫安全站位。</p><p><b>预警：</b>发射管先回缩蓄压，指示灯闪烁，随后按锁定方向发射。</p><p><b>应对：</b>横移躲开弹道，利用发射后的回压间隔绕侧面接近。</p><p><b>设计边界：</b>限制同屏哨兵与弹量；发射后不持续跟踪，弹道不能被糖果特效遮住。</p>", "<b>弹簧冲车 · 普通 / 冲锋</b><p><b>攻击：</b>弹簧释放后沿直线冲撞，穿过原本安全的空地。</p><p><b>预警：</b>车头压低、轮子刹停、弹簧压缩；地面出现短直线路径。</p><p><b>应对：</b>看到蓄力就横向换线，冲过后从侧后方追击。</p><p><b>设计边界：</b>启动后不急转追踪；多车错开冲锋，碰撞结束有明显恢复期。</p>", "<b>校时技师 · 普通 / 支援</b><p><b>攻击：</b>向附近机器发送短时校准信号，提供一次有限护盾。</p><p><b>预警：</b>天线抬起并按节拍闪烁，收拢的机械刻度圈预示支援范围。</p><p><b>应对：</b>优先切入技师，或引导敌群离开信号圈再打；不必硬吃强化群。</p><p><b>设计边界：</b>同类护盾不叠加，无持续无限治疗；首轮信号有足够打断或换位时间。</p>", "<b>铁壳重锤 · 精英 / 重击</b><p><b>攻击：</b>交替单臂砸地与短距离重步推进，检验近身进退节奏。</p><p><b>预警：</b>锤臂齿轮先咬合抬起，落点圆环收缩；重步前身体后仰。</p><p><b>应对：</b>绕到未攻击的侧面，等锤臂卡住的恢复窗口输出，近战不贪刀。</p><p><b>设计边界：</b>高击退抗性但不无敌；硬直窗口清楚，避免同时叠加密集远程压制。</p>", "<b>总发条长 · Boss / 节拍压场</b><p><b>攻击：</b>两个阶段提案：交替锤臂砸地与扇形机械弹；第二阶段用带缺口的转动扫线迫使移动。</p><p><b>预警：</b>将动作的手臂先亮灯，中央钟盘倒数；扫线方向与安全缺口提前可见。</p><p><b>应对：</b>读懂钟盘节拍，躲到空侧；连续动作结束后主发条露出，形成统一输出窗口。</p><p><b>设计边界：</b>不无限受击退；每套攻击必须保留出口，阶段变化不给无预警伤害。</p>"];function inspect(i){$('inspect').innerHTML=enemies[i];document.querySelectorAll('[data-enemy]').forEach(b=>b.setAttribute('aria-pressed',String(+b.dataset.enemy===i)))}document.querySelectorAll('[data-enemy]').forEach(b=>b.onclick=()=>inspect(+b.dataset.enemy));inspect(0);
```
