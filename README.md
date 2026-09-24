# Codex LED Widget Native

一个让你更加焦虑地盯着 Codex 额度流失的 Windows 桌面小组件。

它会悬在桌面上，用一个看起来很无辜的玻璃圆环提醒你：你的 Codex 额度正在安静地蒸发。

这是 WPF / .NET 9 原生重写版，目标是保留液态玻璃小组件的呈现效果，同时摆脱 Electron 打包后“一个额度提醒器比论文还重”的尴尬。

> 本项目受 [xicunwus2025-sys/codex-led-widget](https://github.com/xicunwus2025-sys/codex-led-widget) 启发，并保留对原项目的署名说明。

## 功能

- 按 Codex app-server 返回的额度池动态显示窗口；兼容旧版 5 小时/7 天双窗口与新版独立模型额度池
- 左侧环形仪表显示当前周额度，弧长就是剩余比例，颜色随额度从绿经黄橙渐变到红
- 显示当前可用的额度重置次数（服务端提供时）
- 显示重置时间，例如 `6月8日 19:03`，方便你和时间谈判
- 支持中文 / English 切换，焦虑也可以国际化
- 支持窗口置顶、隐藏、刷新、退出，想看就看，想逃避也可以
- 支持一键收起为 360 风格悬浮球，左键临时显示 `-1%` 的额度焦虑动画，右键菜单展开面板，刷新后恢复真实值
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
