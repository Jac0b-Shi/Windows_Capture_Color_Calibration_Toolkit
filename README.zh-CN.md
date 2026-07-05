# WGC Color Calibrator

[English](README.md)

WGC Color Calibrator 是一个实验性的 Windows 开发者工具，用于测量 Windows Graphics Capture 在 SDR/HDR 桌面环境中实际返回的数字色块像素值。

当前范围：

- 仓库基础。
- 颜色、色卡、布局和测量会话核心领域模型。
- 带双语资源的 WinUI 3 应用外壳。

第一阶段不实现：

- FP16 捕获。
- LUT 生成。
- 逆向补偿。
- 动态插件加载。
- 连续捕获。
- WGC 单帧捕获。

## 构建要求

- Windows 开发机。
- .NET SDK 10.0.x。
- 可从 NuGet 恢复 Windows App SDK 相关依赖。
- 推荐使用安装了 Windows 应用开发组件的 Visual Studio 运行 WinUI 项目。

项目文件当前使用 `TargetPlatformMinVersion=10.0.17763.0` 作为 Windows App SDK 打包基线。这不是本项目所需 WGC HDR 测量路径的最低系统版本结论。该问题记录在 `docs/zh-CN/open-questions.md`。

## 构建

```powershell
dotnet restore WgcColorCalibrator.sln
dotnet build WgcColorCalibrator.sln --no-restore
dotnet test WgcColorCalibrator.sln --no-build
```

## 当前状态

本仓库处于初始化阶段。在完成验证并写入文档前，不得把任何 Windows 版本、GPU、像素格式或 HDR 模式下的 WGC 色彩语义当作已确认事实。

## 许可证

本项目使用 LGPL-3.0-only。详见 `LICENSE`。

