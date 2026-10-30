# 隔离 Godot C# 工具链验证（CHG-0002）

验证日期：2026-10-30。此记录依据仓库外 `godot-toolchain-verification/` 的 README、manifest、
验证脚本与结果记录整理；这是**隔离探针**，不是 HMI-Game 游戏代码或 Unity 迁移验证。
本仓库仍是空白标准版 Godot 工程，没有 `.cs` / `.csproj`；系统 `godot` 命令未被替换。

## 验证基线

| 项目 | 实际版本 / 配置 |
| --- | --- |
| 宿主平台 | Linux x86_64 |
| Godot .NET | `4.6.3.stable.mono.official.7d41c59c4` |
| 导出模板 | 官方同版本 .NET 模板，仅提取 Linux x86_64 / Android 所需部分 |
| .NET | SDK `10.0.403`、runtime `10.0.14`、Android workload `36.1.2` |
| 探针核心 | `netstandard2.1` / C# 8.0，纯 C# 整数加法 |
| Godot 宿主 | `Godot.NET.Sdk/4.6.3`，`net10.0`，引用探针核心 |
| Java | Temurin OpenJDK `17.0.21+9` |
| Android 工具 | command-line tools `23.0`；platform-tools `37.0.1` |
| Android SDK | platform 36 revision 2；build-tools `36.1.0` |
| 原生构建 | NDK `29.0.14206865`；CMake `3.10.2.4988404` |
| Android 输出 | Gradle debug APK，ARM64-only，minSDK 24 / targetSDK 36 |

实际 4.6.3 模板 `android/build/config.gradle` 要求 SDK 36 / build-tools 36.1.0 / NDK 29。
这与当时版本化导出文档中的 SDK 35 / build-tools 35.0.1 / NDK 28 不一致，以实际模板为本次基线。
先前按文档安装的旧版本包仍保留；未删除或擅自调整官方模板。

## 已验证与证据索引

下列证据位于仓库外验证目录的 `logs/`，不随本仓库上传原始日志、工具或产物。

| 检查 | 结果 | 本地证据文件 |
| --- | --- | --- |
| C# 核心及宿主编译 | 通过，零编译警告/错误 | `build.log`、`desktop-final.log` |
| headless 编辑器导入 | 退出码 0，存在下述退出诊断 | `editor-import.log`、`desktop-final.log` |
| C# 宿主实际执行 | `BOUNDARY_PROBE=42`，退出码 0 | `host-run.log`、`desktop-final.log` |
| Linux 调试包导出与执行 | `BOUNDARY_PROBE=42`，退出码 0 | `linux-export.log`、`linux-run.log` |
| Android managed publish + Gradle 导出 | 退出码 0 | `android-export.log` |
| APK 签名验证 | v2 验证通过，1 个签名者；仅调试签名 | `apk-signature.log` |
| APK ZIP 对齐 | `zipalign -c -P 16 4` 通过 | `apk-alignment.log` |
| APK 元数据与内容 | ARM64，包含 Host.dll、Core.dll、Godot / Mono 原生库 | `apk-badging.log`、`apk-payload.log` |

APK 大小：96,514,078 字节；SHA-256：
`a73bab2b7e63da8d619e1348ddd3b990dcd1d9869ddc8ab43cccf35693651dca`。
APK 对齐检查不是设备启动或 Android 16 KiB 页面完整兼容性验证。

## 重现入口与命令

以下路径均相对于**仓库外验证目录**，不是本仓库自带脚本。
换机器须先按上述版本重新准备工具链、模板、最小探针和导出预设，不能只克隆本仓库就运行这些命令。
`env.sh` 设置隔离工具/缓存路径；`GODOT_NET` 指向 .NET 编辑器，`TOOLCHAIN_ROOT` 指向验证目录。

```sh
# 工作目录：仓库外 godot-toolchain-verification/
source ./env.sh
./verify-desktop.sh
# verify-desktop.sh 的主要步骤：
cd "$TOOLCHAIN_ROOT/smoke"
dotnet build Host/Host.csproj -v minimal
"$GODOT_NET" --headless --editor --path Host --import
"$GODOT_NET" --headless --path Host
"$GODOT_NET" --headless --path Host --export-debug Linux "$TOOLCHAIN_ROOT/smoke/out/probe.x86_64"
"$TOOLCHAIN_ROOT/smoke/out/probe.x86_64" --headless
# Android 导出：
"$GODOT_NET" --headless --path "$TOOLCHAIN_ROOT/smoke/Host" --export-debug Android "$TOOLCHAIN_ROOT/smoke/out/probe.apk"
```

不要把 `net10.0` 宿主与可移植核心混为一谈；本次结果只证明最小 `netstandard2.1` 探针可被此 Godot 宿主调用。
它不证明任何 Unity / 团结版本、完整 ECS 框架、游戏依赖或 API 集合兼容。

## 失败修复、警告与未覆盖范围

- 最初缺少 solution 导致导出失败；在隔离探针中创建 `.sln` 后 Linux 导出通过。
- Android 初始验证缺少 ETC2/ASTC 导入配置；仅在隔离探针启用后重新验证。
- .NET 10 使用 Gradle 导出；本版本非 Gradle APK 模板仅支持 `net9.0`。
- 官方模板 AGP 8.6.1 的已测试上限为 compileSdk 35，但模板要求 36；构建出现相关警告。
- 还出现 SDK XML v4 / 解析器 v3 警告、无项目图标回退警告；未压制或隐藏。
- Android 预设加载时出现关闭阶段 `EditorSettings not instantiated` 诊断；进程退出码为 0，
  两次 C# 实际运行输出正确，但不能称为“所有引擎日志干净”。
- 工具下载/构建曾遇到云端网络和本地缓存问题，使用获授权的进程级适配及隔离目录解决；
  未关闭 TLS 校验、未改变全局证书/设置。具体网络配置、凭据和签名材料不进入仓库。
- Android SDK 正式许可已获用户明确同意；未接受预览许可。
- **未验证**：真实设备、模拟器、GPU / UI、性能、发布签名、商店提交、Unity / 团结、iOS、Web。
- Godot C# Android 支持仍为实验性；正式项目选型和发布前应另行确认支持范围并做设备测试。

## 资产与来源校验

官方 Godot 编辑器与模板发布源：
[Godot 4.6.3 官方构建](https://github.com/godotengine/godot-builds/releases/tag/4.6.3-stable)。
本地保留下载清单及厂商摘要；Godot/JDK 校验 SHA-256，.NET SDK 校验 SHA-512，
Google CLI 归档按厂商元数据校验 SHA-1。

- .NET 编辑器归档 SHA-256：`702a8a6785060203fd46373adf864586ecf0c377ac685d7107dde66b1eba0a9e`
- .NET 模板归档 SHA-256：`6436f474ee085fb0a94d3d296b9821736bdd663b6e91b2228fdf77066f1d1400`

不提交约 93 MiB 的 APK、探针构建缓存、下载包、SDK、导出模板或调试密钥。
后续如需提供可跨机器重现的探针源码/脚本，单独评审并脱敏交付；当前记录不冒充完整可复现测试套件。
