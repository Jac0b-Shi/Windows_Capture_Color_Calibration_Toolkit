# WGC Color Calibrator

[English](README.md)

WGC Color Calibrator 是一个实验性的 Windows 开发者工具，用于测量 Windows Graphics Capture 在 SDR/HDR 桌面环境中实际返回的数字色块像素值。

## 功能

1. 生成已知色卡（灰度渐变、HDR scRGB 渐变、单色、近白色），在 D3D11 swapchain 窗口中渲染。
2. 通过 WGC 以 BGRA8 或 FP16 RGBA 格式捕获色卡窗口。
3. 从捕获像素中采样每个色块，记录期望值与实际捕获值。
4. 将测量会话导出为 JSON、CSV、原始帧和调试覆盖层。
5. 对比内置 HDR→SDR 色调映射算子（Clamp、LinearScale、Reinhard、ExposureGamma）与 FP16 捕获数据，生成每个算子的 SDR 预览 PNG 和逐色块 CSV。

## 构建要求

- Windows 10 版本 1803 或更高版本（WGC 需要 `Windows.Graphics.Capture`）。
- .NET SDK 10.0.x。
- 可从 NuGet 恢复 Windows App SDK 相关依赖。
- 推荐使用安装了 Windows 应用开发组件的 Visual Studio 运行 WinUI 项目。

## 构建

```powershell
dotnet restore WgcColorCalibrator.sln
dotnet build WgcColorCalibrator.sln --no-restore -p:Platform=x64
dotnet test WgcColorCalibrator.sln --no-build -p:Platform=x64
```

## 项目状态

| 里程碑 | 描述 | 状态 |
|--------|------|------|
| 1 | 仓库基础、核心领域模型、WinUI 外壳 | 完成 |
| 2 | 色卡渲染、布局引擎、D3D11 swapchain | 完成 |
| 3 | HDR 可用 D3D11 色卡窗口（scRGB FP16 swapchain） | 完成 |
| 4 | WGC BGRA8 单帧捕获和测量循环 | 完成 |
| 5 | WGC FP16 原始捕获基线 | 完成 |
| 6 | HDR→SDR 算子对比（内置算子、导出文件夹） | 进行中 |

WGC 捕获后端、FP16 读回和测量采样管线已稳定，Milestone 6 期间不修改这些部分。

## 许可证

本项目使用 LGPL-3.0-only。详见 `LICENSE`。
