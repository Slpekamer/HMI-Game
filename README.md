# HMI-Game

空白 Godot 3D 工程，替代原来的 Unity / 团结引擎占位工程。

## 环境

- 创建和无界面验证版本：**Godot 4.6.3 stable**（`4.6.3.stable.official.7d41c59c4`），标准版。
- 渲染器：**Compatibility**（`gl_compatibility`）。
- 无脚本、无模型、无玩法，也没有第三方资源或插件；暂不选择脚本语言，不需要 .NET SDK。
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
