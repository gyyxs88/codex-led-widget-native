# Codex LED Widget Native

Codex LED Widget Native 是一个原生 Windows 桌面悬浮小组件，用于显示本机 Codex 额度。

本项目是 WPF / .NET 9 原生重写版，目标是保留液态玻璃小组件的呈现效果，同时显著降低 Electron 版本的运行体积。

> 本项目受 [xicunwus2025-sys/codex-led-widget](https://github.com/xicunwus2025-sys/codex-led-widget) 启发，并保留对原项目的署名说明。

## 功能

- 显示 Codex 5 小时窗口额度和 7 天窗口额度
- 左侧双半圆液体仪表：左侧 5h，右侧 1w
- 显示重置时间，例如 `6月8日 19:03`
- 支持中文 / English 切换
- 支持窗口置顶、隐藏、刷新、退出
- 支持等比缩放，避免文字位图拉伸发糊
- 原生 WPF 窗口，框架依赖版约 214KB，单文件自包含版约 71MB

## 运行要求

源码构建需要：

- Windows 10/11
- .NET SDK 9
- 已安装并登录 Codex CLI

单文件发布版已经包含 .NET 运行时，但仍需要使用者本机已安装并登录 Codex CLI，否则无法读取额度。

## 开发

```powershell
dotnet restore .\CodexLedWidgetNative.sln
dotnet test .\CodexLedWidgetNative.sln
dotnet build .\CodexLedWidgetNative.sln
```

调试运行：

```powershell
dotnet run --project .\CodexLedWidget.Wpf\CodexLedWidget.Wpf.csproj
```

## 发布

框架依赖版，体积最小，但目标机器需要安装 .NET 9 Desktop Runtime：

```powershell
dotnet publish .\CodexLedWidget.Wpf\CodexLedWidget.Wpf.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=false `
  -o .\publish\framework-dependent
```

单文件自包含版，可以直接发送一个 exe：

```powershell
dotnet publish .\CodexLedWidget.Wpf\CodexLedWidget.Wpf.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o .\publish\single-file
```

## 隐私说明

本工具通过本机 Codex CLI 的 app-server 接口读取额度信息，不保存、不上传、不显示认证 Token。

## 许可证

MIT License。详见 [LICENSE](LICENSE)。
