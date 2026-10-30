# HMI-Game

空白 Godot 3D 工程，替代原来的 Unity / 团结引擎占位工程。

## 环境

- 创建和无界面验证版本：**Godot 4.6.3 stable**（`4.6.3.stable.official.7d41c59c4`），标准版。
- 渲染器：**Compatibility**（`gl_compatibility`）。
- 无脚本、无模型、无玩法，也没有第三方资源或插件；打开当前空工程不需要 .NET SDK。未来纯 C# ECS 核心路线见 [AGENTS.md](AGENTS.md)。
- 仓库外已完成隔离 Godot .NET 工具链的 C# / Linux 运行及 Android ARM64 调试 APK 构建验证，详见 [工具链记录](docs/toolchain.md)。本工程尚未转换为 C# 项目；系统 `godot` 仍是标准版。
- Godot 编辑器不依赖 Unity Hub，也无需激活 Unity / 团结引擎许可证。

## 打开与运行

1. 使用 Godot 4.6.3 标准版，在项目管理器中导入仓库根目录的 `project.godot`。
2. 打开项目；主场景为 `scenes/main.tscn`，仅含一个空的 `Node3D` 根节点。
3. 按 **F6** 运行当前场景，或按 **F5** 运行工程。

场景未添加相机、灯光或物体，因此运行窗口为空白是预期行为，并非已有可玩的游戏。

也可在安装并配置好 Godot 命令行后，从项目根目录运行：

```sh
godot --editor --path .
godot --path .
```

## 验证

已在云端使用上述实际 Godot 版本通过以下检查（退出码均为 0）：

```sh
godot --headless --editor --path . --import
godot --headless --path . --quit-after 5
```

这些检查覆盖工程导入和空主场景初始化，不代表已验证图形窗口、GPU 渲染、导出包或其他操作系统。

`.godot/` 是可重建的本地缓存，不纳入版本控制；导出产物放在 `exports/` 或 `builds/` 中，也不提交。不要提交导出凭据。

## 开发规范

后续设计、ECS 实现和变更追溯遵循 [AGENTS.md](AGENTS.md)；开发记录见 [docs/development-log.md](docs/development-log.md)。

## 游戏设计（尚未实现）

完整文字设计入口：[docs/game-design.md](docs/game-design.md)，其中链接机制、技能/道具/敌人目录及原型参数附录。
HTML 讨论稿用于展示和讨论；开发以文字记录中区分的已确认要求、提案及待决问题为依据，不把原型等同游戏成品。
技能树、道具机制与角色的解耦扩展及回归门槛见 [AGENTS.md 第 3.4 节](AGENTS.md#34-技能树道具机制角色扩展与回归门槛)。

局内与 UI 的四张最新 AI 概念图：[美术参考索引](docs/art-reference/README.md)。UI 布局提案已通过，非实机截图；包含最新远镜头、无网格地面与伙伴不被攻击的说明，机制数值仍待确认。

## 开发分支 M1：独立 ECS 核心（场景尚未接入）

新增 `src/Core` 纯 C# 8 / netstandard2.1 核心、`content/gameplay.json` 与可运行控制台回归；本次提交仍保留空 Godot 场景，不把未验证的宿主/UI 混入核心里程碑。

需要 .NET SDK 10.0.403：

```sh
dotnet build src/Core/Hmi.Core.csproj -c Release
dotnet run --project tests/CoreTests/Hmi.CoreTests.csproj -c Release
```

[实际 M1 证据与限制](docs/evidence/FEAT-0001/M1-core.md) · [架构 ADR](docs/architecture/ADR-0001-portable-ecs.md) · **[次日逐项决策 Review](docs/overnight-decisions-2026-10-30.md)**。
