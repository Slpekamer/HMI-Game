# 真机反馈体验修复：分段计划与验收边界

基线：已发布 dev `a77c55dc627e22ff812f1ffc2c53edc87aaecb54`，BUG4 Library source v3，810 authored files；恢复ZIP SHA256 `3b4401ba6ba2fbf65546df30f23188de892ad7b27346313865a7d5ed720aa931`，逐文件核验通过。本工作副本独立，不含未接受钱包候选。旧source-manifest/本地历史不能冒充新远端父提交。

## 用户新决定

新需求覆盖此前2D选人偏好：进入首页，再进入带真实Idle的3D选人；怪物有血条，伙伴仍无血条且不受攻击；战斗适配常见比例与安全区；角色真实动作/朝向/脚底连接；修复按住角色按钮丑态、移动阴影闪烁；尤其选卡背景简约化。保留Core/ECS、技能ID、无尽/存档与无闪避，不新增玩法。

## 所有权与顺序

1. **基线复现/实包审计**：implementation owns host/presenter，visual_ui owns GameUi/CandyTheme/UI helpers，art只读GLB动作/足底/阴影，reviewer独立原生门，installer隔离包/工具链。当前APK校验与桌面现象分别记，不将桌面绿反驳用户真机反馈。
2. **首页/3D选人最小增量**：开始→选人→战斗，继续只对validated Active，覆盖取消/返回/失败模态保持原语义；五英雄真实Idle、viewports生命周期、按住/放开/拖出/键盘focus态。先实际图/短片，再提交。
3. **响应式战斗/怪物血条**：不再仅固定1280×720后letterbox自称适配；分离世界viewport与UI布局、安全区。16:9、19.5:9、4:3、窄竖窗受控验证并明确实际测过范围。敌血条只读EntityView Health/MaxHealth，绑定generation、死亡/拾取槽复用清理，伙伴/掉落不得产生血条。不改碰撞或权威血量。
4. **动作/脚底/阴影**：记录真实采样速度与clip时间/状态、慢帧/高攻速、暂停/重启。已发现待证假设：全身Attack覆盖Run腿；固定Run速度与实际4.8m/s不匹配；Advance限制delta0.1而host0.25，低FPS动画可能进一步减速；floor顶面-0.05而角色根0有接地间隙。阴影需同相机/同轨迹单变量A/B，不盲降bias掩盖且不复用已拒acne方案。
5. **统一简约审图与导出**：选卡/商店削减装饰但保留代价/前置/临时期限/安全保存警告；真实操作、截图/影片与全aggregate通过才freeze。本地提交+Library每段保全；远端由唯一publisher按授权处理。

## 必须保留的门

- 角色/伙伴/敌/拾取generation与restart连续循环；paused动作不进、控制不残留；新首页不绕过存档epoch/旧signal拒绝。
- 五角色Idle/Run/Attack实际骨骼变化、最大合法攻速移动、脚底接地及转向，不能仅根平移或synthetic PASS。
- 长文本/触摸按住/焦点、宽高改变中暂停/模态、可用触控安全区域；实际像素和输入，不以包络断言替代。
- Android GLB动画/依赖打包校验独立于桌面；新构建先默认网络，旧BUG4代理/证书例外不自动延伸。未真机运行始终明示。
- 整体商业完成/无限设备支持/手机性能不能由此次局部门推出；旧无尽10–30波真实体验仍未达。

## 2026-11-01 06:25 实证更新（尚未整体通过）

- 首页/五位真实3D Idle选择、长按/拖出取消、敌人部分血条与伙伴无血条、正常退出保存后Continue恢复，已由独立桌面实际输入/影片验证；不是Android播放证据。
- 原整身Attack抢占Run已改为原生AnimationTree上身白名单OneShot。新引入的timer/tree末帧空档经独立60组合红门1220次冲突定位，改以真实OneShot active判定后0冲突；实际近敌移动片可见腿相位。短腿步幅/脚滑仍有残差，未宣布自然跑完成。
- 离树时不得Reset/Advance动画：加IsInsideTree防护，5真实GLB mixer17480检查与HostLifecycle（含真正QueueFree/ExitTree）重新通过。这不是Codex退出fatal的原因：该复现路径没有mixer。
- **阶段1 aggregate阻断**：Codex功能23断言先通过但Godot Mono进程退出偶发绑定/资源清理 fatal（包括 `!rc_owner`、`script_bindings` 非空与 unsafe reference 后崩溃，不是同一个assert）。原BUG4重复干净；新候选ABBA10次5次fatal，删重复Home10次仍1次fatal。新Theme单独并不足复现，不能称根因已定；不加生产强制GC、不把偶然绿覆盖红、不提交为完成。
- 简约菜单在独立r2已通过实际16组页面/3D抑制像素门；响应式独立r3六比例真Expand引擎门通过，原生42图发现余额图标重叠与过高暂停空白，正在修复。r2/r3不混入本阶段源。
- 阴影同轨迹A/B/C：Orthogonal-only B削弱/丢失投影，明确拒绝；floor+petals抬高5cm的C仅接地候选，尚未生产采用。A/C短连续样本未复现用户Android强闪，不能宣称已修。原Run增幅r3因腿形鼓包同样拒绝，不改五资产。
