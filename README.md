# HMI-Game

面向 **团结引擎 1.6.14** 的空白项目仓库骨架。

> 当前是手工准备的最小工程骨架，不是编辑器创建、导入或编译验证过的完整工程。
> 尚无场景、脚本、游戏资产或内部包，也未导入任何 Mac 上的代码。

## 编辑器版本

- 目标发行版本：团结引擎 **1.6.14**。
- `ProjectSettings/ProjectVersion.txt` 固定为 `2022.3.61t15`，依据此前已安装编辑器的版本记录；本次云端环境未能重新核验该二进制。
- 未填写未知的编辑器 revision，未伪造其他 `ProjectSettings` 文件。

## 首次打开

1. 在团结 Hub 中安装/选择团结 **1.6.14**，通过官方流程完成适用的授权。
2. 在 Hub 中添加本仓库根目录 `HMI-Game`，使用该编辑器打开。
3. 等待编辑器导入并生成默认设置、资源元数据及所需的包解析文件。
4. 检查 Console 无错误，创建并保存首个场景，再检查版本控制差异；提交需要保留的 `ProjectSettings`、`Packages/packages-lock.json` 和资源对应的 `.meta` 文件。

`Packages/manifest.json` 当前使用空的 `dependencies`，刻意不声明未经验证的注册表包、模板包或内部包。按后续功能需要，通过目标编辑器的 Package Manager 添加依赖。

## 目录

```text
Assets/                     # 空资源目录，仅 .gitkeep 占位
Packages/manifest.json      # 最小包清单，无外部依赖
ProjectSettings/
  ProjectVersion.txt        # 目标编辑器版本
```

## 当前验证范围

2026-10-30，在本次云端工作区中：

- 已准备最小目录、JSON 包清单、版本文件和 Git 忽略规则。
- 原记录路径 `apps/tuanjie-1.6.14/Editor/Tuanjie` 不存在；PATH 中未找到团结/Unity 编辑器。
- 在检查的常用目录中未发现本地 `.ulf` 许可证文件；这不代表对账号授权状态作出判断。
- **未执行**编辑器创建、项目导入、Console 检查、编译、运行或构建。
- 未重新下载编辑器、接受许可条款或进行许可证激活。

本仓库目前只完成初始化骨架，不代表 HMI 游戏功能已经实现。首次编辑器验证应在具备对应版本及有效授权的环境中进行。

## 版本控制

提交源文件及其 `.meta`；不要提交 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、构建产物、凭据或许可证。`.gitignore` 已覆盖常见生成目录和敏感文件模式，但提交前仍应检查差异。
