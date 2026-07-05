# WGC HDR Color Calibrator  
## 项目初始化需求与工程执行规格

- 文档状态：Draft v0.2
- 主要用途：供 Codex 初始化项目、建立可编译基线和实现首个垂直切片
- 工作语言：中文优先
- 代码、API、命令行、Schema、资源键：英文
- UI 与公开文档：至少简体中文和英文
- 暂定项目名：`WgcHdrColorCalibrator`
- 暂定仓库名：`wgc-hdr-color-calibrator`

---

# 1. 文档目的

本文档不是概念性产品介绍，而是项目初始化阶段的执行规格。

Codex 应依据本文档完成：

1. 建立 C#/.NET 与 WinUI 3 项目骨架。
2. 建立可扩展的色卡、渲染、捕获、采样、分析和导出模块边界。
3. 建立简体中文与英文双语本地化基础。
4. 实现一个最小但真实可运行的 WGC 捕获垂直切片。
5. 建立测试、CI、日志、配置、文档与错误处理基础。
6. 记录所有未经验证的 WGC/HDR 行为，不得将推测写成既定事实。

初始化阶段不要求完成完整的颜色校准算法、逆向补偿或 3D LUT。

---

# 2. 项目背景

Windows Graphics Capture（WGC）广泛用于：

- 窗口和显示器捕获；
- OCR；
- 计算机视觉；
- 自动化；
- 游戏辅助；
- 截图和录制；
- AI Vision；
- 流媒体处理。

在 Windows HDR / Advanced Color 环境中，WGC 捕获到的颜色可能与以下值存在差异：

1. 应用程序声明或渲染的理论颜色；
2. 用户通过屏幕取色器观察到的显示颜色；
3. 其他捕获路径获得的颜色；
4. 下游 OpenCV、OCR 或图像处理模块实际接收到的颜色。

目前缺少一个面向开发者的、可复现、可量化、可导出结果的独立工具，用于测量这些差异。

本项目源于 BetterGI 在 Windows HDR 环境中的文字颜色识别问题，但项目必须保持独立，不得以 BetterGI 的内部结构、特定游戏或 OCR 逻辑作为核心架构前提。

---

# 3. 产品定位

本项目是：

> 面向开发者的 Windows 捕获色彩测量、比较与标定工具。

本项目首先关注：

> Windows Graphics Capture 在 SDR/HDR 环境下实际输出的像素值和颜色行为。

项目未来可以扩展为：

> Windows Capture Color Calibration Toolkit

但初始化阶段只实现 WGC 后端。

---

# 4. 核心问题模型

项目需要能够记录和比较以下三个层级：

```text
A. Expected / Declared Color
   色卡生成器声明的理论颜色

B. Display-observed Color
   由用户通过外部取色器或未来的屏幕测量流程录入的显示颜色

C. WGC-captured Color
   WGC 捕获后实际读取的像素值
```

主要比较关系：

```text
A -> B
应用渲染、游戏 tone mapping、Windows HDR 合成或显示链路产生的变化

B -> C
WGC 捕获、像素格式转换、色彩空间处理或下游读取产生的变化

A -> C
捕获和视觉算法最终看到的总体差异
```

MVP 必须完成 `A -> C`。

`B` 在 MVP 中作为可选手工输入字段保留，后续再实现自动化辅助。

---

# 5. 项目目标

## 5.1 主要目标

1. 生成可配置的数字色卡。
2. 使用 WGC 捕获目标窗口或显示器。
3. 读取指定色块中央区域的像素值。
4. 记录 Expected 与 Captured 的对应关系。
5. 输出基础颜色统计和误差。
6. 导出可机器解析的 Profile。
7. 为后续捕获后端、分析器、色卡和导出器扩展提供稳定接口。
8. 保证 UI 和公开用户文档至少支持英文与简体中文。

## 5.2 次要目标

1. 为 OCR 和 UI 颜色识别提供捕获空间阈值参考。
2. 测量近白色、灰阶、高亮和低饱和颜色在 WGC 路径中的变化。
3. 为未来经验映射、插值模型和 LUT 研究积累数据。
4. 支持用户共享不同硬件、驱动和 Windows 构建下的 Profile。

## 5.3 非目标

初始化阶段不得实现或承诺：

- 无损恢复 HDR 原始画面；
- 从 8-bit 捕获结果唯一反推出原始 HDR 亮度；
- 专业显示器校色；
- ICC Profile 生成；
- 修改 Windows HDR 配置；
- 替代色度计、分光光度计或摄影色卡；
- 针对某个游戏硬编码 tone mapping；
- BetterGI 专用逻辑；
- 跨平台；
- 实时录像；
- 完整插件市场；
- 自动上传用户数据；
- 使用未经验证的固定补偿曲线。

---

# 6. 成功标准

项目初始化完成后，必须满足以下结果：

1. 新开发者可在受支持的 Windows 开发环境中克隆并构建项目。
2. 应用可启动并显示 WinUI 3 主窗口。
3. 用户可选择内置色卡或输入一个自定义 RGB/HEX 颜色。
4. 应用可显示数字色块窗口或色卡区域。
5. 用户可通过系统 WGC 选择器选择捕获源。
6. 应用可捕获至少一帧并读取色块中央区域的像素。
7. UI 可同时显示 Expected 和 Captured 值。
8. 用户可导出 JSON 和 CSV。
9. 切换中文或英文后，核心 UI 不出现硬编码未本地化文本。
10. 单元测试和构建检查可在 CI 中运行。
11. 所有 HDR 和像素格式相关假设都在文档中标明验证状态。

---

# 7. 技术栈

## 7.1 固定选择

- 语言：C#
- 运行时：当前受支持的 .NET LTS，初始化时优先评估 `.NET 8`
- UI：WinUI 3
- 框架：Windows App SDK
- 捕获：`Windows.Graphics.Capture`
- 图形接口：Direct3D 11 / DXGI 互操作
- 测试：xUnit
- 日志：`Microsoft.Extensions.Logging`
- 依赖注入：`Microsoft.Extensions.DependencyInjection`
- 配置：`Microsoft.Extensions.Configuration`
- 序列化：`System.Text.Json`
- 主数据格式：JSON
- 表格导出：CSV
- 版本控制：Git
- CI：GitHub Actions

## 7.2 不应在初始化阶段引入

除非有明确、记录充分的必要性，不应引入：

- Electron；
- Tauri；
- Rust FFI；
- OpenCV；
- Win2D 作为核心依赖；
- 大型 MVVM 框架；
- 数据库；
- 插件动态加载框架；
- AutoMapper；
- Reactive Extensions；
- 原生 C++ 项目。

允许使用轻量 MVVM 支持库，但必须先判断 WinUI 3 原生绑定和少量基础类是否已足够。

## 7.3 原生互操作原则

WGC、D3D11、DXGI 和 WinRT 互操作应集中在独立项目中。

业务层不得直接操作：

- COM 指针；
- D3D11 Texture；
- WinRT Surface；
- CaptureFramePool；
- HWND 初始化细节。

所有原生资源必须：

- 有明确所有权；
- 可确定性释放；
- 支持取消；
- 在日志中记录失败上下文；
- 避免泄漏到 UI 层。

---

# 8. Windows 版本与能力策略

## 8.1 目标原则

项目不以开发者当前使用的 Insider Preview 构建作为最低版本。

目标是：

> 支持正式稳定版 Windows 中，能够满足本项目所需 WGC HDR 捕获路径的最早版本。

最低版本不得在未验证前硬编码为结论。

## 8.2 初始化阶段要求

Codex 必须建立 Capability Probe，并记录：

- Windows version/build；
- `GraphicsCaptureSession.IsSupported()`；
- 是否启用 HDR / Advanced Color；
- 当前显示器信息；
- 捕获源类型；
- 请求的像素格式；
- 实际成功创建的像素格式；
- D3D feature level；
- 适配器名称；
- 是否发生格式降级。

## 8.3 像素格式

MVP 必须设计为支持至少两条逻辑路径：

```text
BGRA8:
B8G8R8A8UIntNormalized

FP16:
R16G16B16A16Float
```

要求：

1. 不得假设所有系统都支持所有格式。
2. 格式创建失败时应给出结构化错误，不得静默降级。
3. 用户主动允许时才可以降级。
4. 导出 Profile 必须记录请求格式与实际格式。
5. FP16 的数值语义、色彩空间和 HDR 行为必须通过实验确认。
6. 在实验确认前，不得把 FP16 数值直接标记为“真实 HDR RGB”。

`R10G10B10A2` 可列为未来研究项，不作为初始化要求。

---

# 9. 总体架构

```text
Chart Definition
    ↓
Chart Layout
    ↓
Chart Renderer
    ↓
Capture Backend
    ↓
Frame Decoder
    ↓
Patch Sampler
    ↓
Measurement Session
    ↓
Analyzer
    ↓
Profile / Report Exporter
```

架构必须支持替换其中任一模块，但初始化阶段不要求实现运行时动态插件加载。

采用：

> 稳定接口 + 内置实现 + 未来可扩展

而不是：

> 过早设计完整插件系统

---

# 10. Solution 与项目结构

建议结构：

```text
WgcHdrColorCalibrator.sln

src/
  WgcHdrColorCalibrator.App/
  WgcHdrColorCalibrator.Core/
  WgcHdrColorCalibrator.Capture.Abstractions/
  WgcHdrColorCalibrator.Capture.Wgc/
  WgcHdrColorCalibrator.Rendering/
  WgcHdrColorCalibrator.Analysis/
  WgcHdrColorCalibrator.Export/
  WgcHdrColorCalibrator.Infrastructure/

tests/
  WgcHdrColorCalibrator.Core.Tests/
  WgcHdrColorCalibrator.Analysis.Tests/
  WgcHdrColorCalibrator.Export.Tests/
  WgcHdrColorCalibrator.Capture.Wgc.Tests/

docs/
  zh-CN/
    requirements.md
    architecture.md
    development.md
    measurement-model.md
  en-US/
    requirements.md
    architecture.md
    development.md
    measurement-model.md

samples/
  charts/
    near-white.sample.json
    grayscale.sample.json
    custom.sample.csv

schemas/
  measurement-profile.schema.json
  chart-definition.schema.json
```

可根据 WinUI 3 打包模板限制微调，但不得将所有代码放入单个 App 项目。

---

# 11. 核心领域模型

## 11.1 颜色值

至少定义：

```csharp
public readonly record struct Rgb8(byte R, byte G, byte B);
public readonly record struct Rgba8(byte R, byte G, byte B, byte A);
public readonly record struct RgbaFloat(float R, float G, float B, float A);
public readonly record struct Hsv(double H, double S, double V);
public readonly record struct Xyz(double X, double Y, double Z);
public readonly record struct Lab(double L, double A, double B);
```

要求：

- 明确数值范围；
- 明确通道顺序；
- 避免用 `System.Drawing.Color` 作为核心领域类型；
- 转换函数必须可单元测试；
- 不得隐式混用 gamma-encoded 和 linear 数值；
- 未知色彩空间必须显式标记为 `Unknown`。

## 11.2 色彩空间元数据

建议：

```csharp
public enum ColorEncoding
{
    Unknown,
    SrgbEncoded,
    LinearScRgb,
    DisplayObserved,
    CaptureNative
}
```

不得仅根据像素格式自动断言色彩空间。

## 11.3 Patch

```csharp
public sealed record ColorPatchDefinition(
    string Id,
    string Label,
    Rgb8 ExpectedColor,
    string? Category,
    double Weight,
    IReadOnlyDictionary<string, string>? Metadata);
```

要求：

- `Id` 唯一且稳定；
- `Label` 是数据标签，不是必须本地化的 UI 文本；
- `Category` 使用英文机器标识；
- `Weight` 默认 1.0；
- Metadata 的键使用英文。

---

# 12. 色卡模块

## 12.1 抽象

```csharp
public interface IChartProvider
{
    string Id { get; }
    string NameResourceKey { get; }
    string DescriptionResourceKey { get; }

    ChartDefinition Create(ChartGenerationOptions options);
}
```

```csharp
public sealed record ChartDefinition(
    string Id,
    string Name,
    IReadOnlyList<ColorPatchDefinition> Patches,
    ChartLayoutDefinition Layout,
    IReadOnlyDictionary<string, string>? Metadata);
```

## 12.2 内置色卡

MVP 至少实现：

### Manual / Single Color

- 用户输入 HEX 或 RGB；
- 生成一个大色块；
- 用于快速测试某个值。

### Near White

覆盖近白色和低饱和高亮区域，例如：

- 灰阶近白；
- 少量暖白；
- 少量冷白；
- 轻微单通道偏差。

具体采样表应由配置生成，不应硬编码在 UI。

### Grayscale

- 从黑到白的可配置灰阶；
- 支持步长或采样点数量；
- 至少包含端点。

### RGB Cube

- 初始化阶段仅实现小规模预设，如 `5³` 或 `9³`；
- 高密度 `17³`、`33³` 为后续功能；
- 必须避免一次渲染过多 patch 导致 UI 或捕获不稳定。

### Custom JSON

用户可导入符合 Schema 的色卡。

### Custom CSV

第一版 CSV 至少支持：

```csv
id,label,r,g,b,category,weight
```

## 12.3 用户自定义要求

用户必须能够：

- 从 GUI 添加颜色；
- 删除颜色；
- 调整顺序；
- 设置标签；
- 设置类别；
- 保存为自定义色卡；
- 重新加载；
- 导出 JSON 或 CSV。

初始化阶段可先实现基础增删和文件导入导出，不要求复杂表格编辑器。

---

# 13. 色卡布局和渲染

## 13.1 布局

每个 Patch 必须记录：

- 逻辑矩形；
- 实际像素矩形；
- 中央安全采样矩形；
- 边缘排除区域。

建议模型：

```csharp
public sealed record PatchPlacement(
    string PatchId,
    PixelRect Bounds,
    PixelRect SafeSampleBounds);
```

## 13.2 Patch 尺寸

不得默认使用 1×1 像素 patch。

默认建议：

- Patch：至少 8×8 或 16×16 逻辑像素；
- 中央采样：排除边缘后取 4×4 或更大；
- 根据 DPI 和实际物理像素记录缩放结果。

必须支持配置：

- patch size；
- gap；
- border；
- safe sample inset；
- column count；
- window background。

## 13.3 渲染要求

色卡显示窗口必须：

- 禁止透明；
- 禁止动画；
- 禁止阴影覆盖测试区域；
- 禁止亚像素文字叠加在 patch 内；
- 保证 patch 使用明确的纯色填充；
- 记录 DPI；
- 记录窗口位置和实际像素尺寸；
- 允许全屏或独立窗口模式；
- 允许将 UI 控件区与测试色卡区分离。

## 13.4 渲染风险

Codex 必须记录并验证：

- WinUI Brush 到最终交换链/合成颜色是否发生转换；
- XAML 渲染是否适合作为严格色卡源；
- 是否需要独立 Direct3D/Composition 渲染路径；
- DPI 缩放是否影响 patch 边界；
- 色卡窗口被捕获时是否包含边框、阴影或系统装饰。

MVP 可先使用 WinUI/XAML 或 Canvas 生成色块，但必须把 Renderer 抽象独立出来，以便后续替换为 Direct3D 渲染器。

---

# 14. 捕获模块

## 14.1 抽象

```csharp
public interface ICaptureBackend : IAsyncDisposable
{
    string Id { get; }

    Task<CaptureCapabilities> ProbeAsync(
        CancellationToken cancellationToken);

    Task<CaptureSource> SelectSourceAsync(
        WindowId ownerWindow,
        CancellationToken cancellationToken);

    Task StartAsync(
        CaptureStartOptions options,
        CancellationToken cancellationToken);

    Task<CapturedFrame> CaptureSingleFrameAsync(
        CancellationToken cancellationToken);

    Task StopAsync(
        CancellationToken cancellationToken);
}
```

接口可按实际 WinUI/WinRT 限制微调，但职责不得混入 UI。

## 14.2 WGC 实现要求

WGC 后端必须：

- 支持系统 Capture Picker；
- 正确关联 WinUI 窗口 HWND；
- 支持窗口和显示器捕获；
- 支持单帧采集；
- 为未来连续采集预留能力；
- 处理捕获源尺寸变化；
- 正确重建 FramePool；
- 处理捕获源关闭；
- 避免在 UI 线程执行高频帧处理；
- 确保 Frame 和 Surface 的生命周期不越界；
- 支持取消和关闭；
- 释放所有事件订阅和 GPU 资源。

## 14.3 帧模型

```csharp
public sealed record CapturedFrame(
    int Width,
    int Height,
    CapturePixelFormat PixelFormat,
    ColorEncoding Encoding,
    DateTimeOffset CapturedAt,
    TimeSpan? SystemRelativeTime,
    ReadOnlyMemory<byte> PixelBytes,
    int RowPitch,
    IReadOnlyDictionary<string, string> Metadata);
```

FP16 可使用独立的强类型缓冲模型，不得强行塞入 BGRA8 字节解释。

## 14.4 单帧优先

初始化阶段优先保证：

- 能可靠捕获一帧；
- 能稳定复制到 CPU 可读缓冲；
- 能正确处理 row pitch；
- 能读取中央 ROI；
- 能重复执行多次且无明显资源泄漏。

连续预览不是首要验收标准。

---

# 15. 采样模块

## 15.1 抽象

```csharp
public interface IPatchSampler
{
    PatchSample Sample(
        CapturedFrame frame,
        PatchPlacement placement,
        SamplingOptions options);
}
```

## 15.2 MVP 采样方式

至少支持：

- Center Pixel；
- Center Mean；
- Center Median。

默认建议：

> 中央安全区域逐通道 Median。

原因：

- 避免边缘混色；
- 降低单像素异常影响；
- 对纯色色块足够稳定。

## 15.3 采样结果

```csharp
public sealed record PatchSample(
    string PatchId,
    SampleMethod Method,
    int SampleCount,
    Rgb8? Rgb8Value,
    RgbaFloat? FloatValue,
    ChannelStatistics Statistics,
    IReadOnlyList<string> Warnings);
```

统计至少包括：

- min；
- max；
- mean；
- median；
- standard deviation；
- unique value count。

---

# 16. Measurement Session

一次测量会话应记录：

- 色卡定义；
- 色卡布局；
- 捕获后端；
- 捕获源；
- 系统能力；
- 显示器和 GPU 信息；
- HDR 状态；
- 请求/实际像素格式；
- 每个 patch 的 Expected 值；
- 可选 Display-observed 值；
- Captured 样本；
- 警告与错误；
- 应用版本；
- Schema 版本；
- 时间戳。

建议：

```csharp
public sealed record MeasurementSession(...);
```

Measurement Session 是分析和导出的唯一输入，不允许 Analyzer 直接访问 UI 或捕获对象。

---

# 17. 分析模块

## 17.1 抽象

```csharp
public interface IMeasurementAnalyzer
{
    string Id { get; }
    string NameResourceKey { get; }

    AnalyzerResult Analyze(
        MeasurementSession session,
        AnalyzerOptions options);
}
```

## 17.2 MVP 分析器

### Basic Difference Analyzer

输出：

- `ΔR`；
- `ΔG`；
- `ΔB`；
- 通道绝对误差；
- 最大通道误差；
- 平均绝对误差；
- Expected/Captured HEX；
- Expected/Captured HSV。

### Grayscale Response Analyzer

对灰阶色卡输出：

- Expected value；
- Captured value；
- 单调性；
- clipping / plateau 提示；
- 暗部和高光压缩提示。

不得在 MVP 中声称这是完整 tone curve。

### Near-white Analyzer

输出：

- Captured V 最小值和分布；
- Captured S 最大值和分布；
- 近白区是否发生通道偏移；
- 可能的 clipping；
- 可选的阈值建议。

阈值建议必须标记为：

> Experimental / 实验性

不得作为通用正确值。

## 17.3 Lab 与 Delta E

可在初始化骨架中预留接口，但只有在以下条件满足时才启用：

- 输入颜色编码明确；
- 转换假设明确；
- 白点明确；
- 测试覆盖充分。

不得对 `Unknown` 编码的 FP16 捕获值直接计算并展示具有权威含义的 Delta E。

---

# 18. 逆向补偿与 LUT

## 18.1 初始化阶段原则

初始化阶段不实现完整逆变换。

只允许建立以下接口和实验目录：

```text
Forward mapping:
Expected -> Captured

Estimated inverse:
Captured -> Estimated Expected
```

任何 Estimated inverse 都必须：

- 标记为估算；
- 绑定具体 Profile；
- 不宣称跨设备通用；
- 报告不可逆区间；
- 检测 clipping 和多对一映射；
- 给出置信度或适用范围。

## 18.2 未来扩展

未来可能支持：

- 1D 灰阶曲线；
- 分通道曲线；
- 3D scattered interpolation；
- 3D LUT；
- `.cube` 导出；
- 捕获空间目标阈值投影。

其中优先级高于整图恢复的是：

> 将目标颜色条件从显示/理论空间映射到 WGC 捕获空间。

---

# 19. Profile 与 Schema

## 19.1 原则

- JSON key 永远使用英文；
- 使用明确 Schema version；
- 不以 UI 本地化文本作为机器标识；
- 保留未知字段扩展能力；
- 数值单位必须明确；
- 日期使用 ISO 8601；
- 文件编码使用 UTF-8；
- 浮点数使用不受当前区域设置影响的序列化。

## 19.2 顶层结构建议

```json
{
  "schemaVersion": "0.1.0",
  "application": {},
  "system": {},
  "gpu": {},
  "display": {},
  "hdr": {},
  "capture": {},
  "chart": {},
  "layout": {},
  "measurements": [],
  "analysis": [],
  "warnings": [],
  "createdAt": "2026-07-05T00:00:00Z"
}
```

## 19.3 Measurement 结构建议

```json
{
  "patchId": "near-white-255",
  "expected": {
    "encoding": "SrgbEncoded",
    "rgb8": [255, 255, 255]
  },
  "displayObserved": null,
  "captured": {
    "pixelFormat": "B8G8R8A8UIntNormalized",
    "encoding": "Unknown",
    "rgb8": [242, 242, 242]
  },
  "sampling": {
    "method": "CenterMedian",
    "sampleCount": 16
  },
  "warnings": []
}
```

---

# 20. 导出模块

## 20.1 MVP

必须支持：

### JSON Profile

- 完整会话；
- 可重新导入；
- 通过 JSON Schema 验证。

### CSV

每行一个 Patch，至少包含：

```text
patchId
label
expectedR
expectedG
expectedB
capturedR
capturedG
capturedB
deltaR
deltaG
deltaB
capturePixelFormat
sampleMethod
```

## 20.2 后续

- Markdown 报告；
- HTML 报告；
- PNG 图表；
- `.cube` LUT；
- Profile 对比；
- 匿名化共享包。

---

# 21. GUI 需求

## 21.1 导航结构

建议页面：

1. Home / 首页
2. Chart / 色卡
3. Capture / 捕获
4. Measurements / 测量
5. Analysis / 分析
6. Export / 导出
7. Settings / 设置
8. Diagnostics / 诊断
9. About / 关于

初始化阶段可以合并页面，但 ViewModel 和服务边界应保留。

## 21.2 首个垂直切片 UI

主窗口至少提供：

- 语言切换；
- HDR/WGC 能力摘要；
- 色卡类型选择；
- 单颜色 HEX/RGB 输入；
- 生成色卡按钮；
- 选择捕获源按钮；
- 捕获单帧按钮；
- Expected 颜色展示；
- Captured 颜色展示；
- RGB/HEX/HSV 数值；
- 保存 JSON；
- 保存 CSV；
- 日志/错误提示入口。

## 21.3 交互原则

- 长操作异步执行；
- 支持取消；
- 捕获期间禁用冲突操作；
- 错误信息对用户可理解；
- 诊断信息可复制；
- 不使用阻塞式 MessageBox 作为主要流程；
- 不静默吞掉异常；
- 危险或实验功能明确标记。

## 21.4 可访问性

至少做到：

- 控件有可访问名称；
- 键盘可操作；
- 不仅靠颜色表达状态；
- 高对比度模式下基本可用；
- UI 文本允许扩展，避免固定宽度截断。

---

# 22. 本地化与文档策略

## 22.1 语言政策

- 中文是维护者的主要设计和开发沟通语言。
- 英文是代码和公开技术标识的规范语言。
- 简体中文和英文文档并行维护。
- 两种语言要求语义同步，不要求机械逐句对应。
- 维护者具备英文校对能力，因此英文版不得被视为低质量自动翻译副本。

## 22.2 UI 本地化

首发必须支持：

- `zh-CN`
- `en-US`

使用 WinUI 3 / Windows App SDK 的 `.resw` 与资源加载机制。

不得在 XAML、ViewModel、服务和异常展示代码中硬编码面向用户的字符串。

资源目录建议：

```text
Strings/
  en-US/
    Resources.resw
  zh-CN/
    Resources.resw
```

资源键示例：

```text
Navigation.Home
Navigation.Chart
Capture.SelectSource
Capture.CaptureFrame
Error.CaptureNotSupported
Analyzer.BasicDifference.Name
Chart.NearWhite.Name
```

## 22.3 资源键原则

- 英文；
- 稳定；
- 语义化；
- 不包含完整英文句子；
- 插件和模块返回资源键，不返回固定显示名称；
- 缺失翻译时记录警告并回退英文。

## 22.4 文档结构

```text
README.md
README.zh-CN.md

docs/
  en-US/
  zh-CN/
```

README：

- `README.md` 为英文公开入口；
- 顶部提供中文入口；
- `README.zh-CN.md` 为中文入口；
- 顶部提供英文入口。

初始化阶段必须同时创建：

- 项目概述；
- 构建要求；
- 当前状态；
- 已知限制；
- 贡献指南；
- 双语文档同步规则。

## 22.5 机器数据

以下内容始终使用英文：

- JSON key；
- Schema；
- CLI 参数；
- 文件格式枚举；
- Plugin/Module ID；
- 日志 Event ID；
- Namespace；
- Class；
- Method；
- Branch；
- Commit type。

---

# 23. 配置与设置

设置至少包括：

- UI language；
- default chart；
- patch size；
- safe sample inset；
- sample method；
- preferred capture pixel format；
- fallback policy；
- export directory；
- diagnostic logging level。

设置持久化必须：

- 有版本；
- 可恢复默认；
- 解析失败时不导致应用无法启动；
- 不存储敏感信息。

---

# 24. 日志和诊断

## 24.1 日志要求

使用结构化日志。

关键事件：

- App startup；
- Capability probe；
- Capture picker opened/closed；
- Capture source selected；
- D3D device created；
- Frame pool created；
- Pixel format requested/actual；
- Frame received；
- Surface copied；
- Sampling completed；
- Export completed；
- Resource disposal；
- Unexpected exception。

## 24.2 隐私

日志不得默认包含：

- 截图内容；
- 完整屏幕图像；
- 用户文件内容；
- 与测量无关的窗口标题；
- 未经说明的硬件唯一标识。

## 24.3 Diagnostics 页面

显示并支持复制：

- App version；
- Windows build；
- Windows App SDK version；
- .NET version；
- GPU；
- display；
- HDR status；
- WGC support；
- pixel format；
- last error；
- enabled experimental flags。

---

# 25. 错误模型

定义结构化错误：

```csharp
public sealed record AppError(
    string Code,
    string MessageResourceKey,
    ErrorSeverity Severity,
    string? TechnicalDetails,
    Exception? Exception);
```

错误代码使用稳定英文标识，例如：

```text
CAPTURE_NOT_SUPPORTED
CAPTURE_PICKER_CANCELLED
CAPTURE_SOURCE_CLOSED
FRAMEPOOL_CREATE_FAILED
PIXEL_FORMAT_UNSUPPORTED
FRAME_COPY_FAILED
ROW_PITCH_INVALID
PATCH_OUT_OF_BOUNDS
PROFILE_SCHEMA_INVALID
EXPORT_FAILED
```

用户取消不应作为错误日志。

---

# 26. 测试策略

## 26.1 单元测试

必须覆盖：

- RGB/HSV 转换；
- HEX 解析；
- 色卡生成；
- Patch 唯一性；
- Layout 边界；
- Safe sample bounds；
- Mean/Median 采样；
- row pitch 处理；
- CSV 导入导出；
- JSON round-trip；
- Schema version；
- 本地化资源键完整性；
- Analyzer 数学；
- clipping 检测。

## 26.2 捕获测试

WGC 集成测试受系统环境限制，应分为：

### 自动测试

不依赖真实 HDR 显示器：

- 生命周期；
- 参数验证；
- mock frame；
- CPU buffer decoding；
- cancellation；
- disposal；
- error mapping。

### 手工测试

需要记录步骤：

- SDR display + BGRA8；
- HDR display + BGRA8；
- HDR display + FP16；
- window capture；
- monitor capture；
- DPI 100/125/150/200%；
- 多显示器；
- 捕获源尺寸变化；
- 捕获源关闭；
- Windows HDR 开关变化；
- 不同 SDR content brightness。

## 26.3 Golden Data

为颜色转换、采样和分析准备固定输入输出。

不得以开发机实际捕获结果作为跨机器严格 Golden 值。

---

# 27. CI/CD

## 27.1 Pull Request CI

至少执行：

- restore；
- build；
- test；
- formatting check；
- JSON Schema validation；
- bilingual resource key consistency；
- documentation link check（可后续加入）。

## 27.2 Release

初始化阶段可只建立占位工作流。

未来 Release 应支持：

- x64；
- arm64；
- 打包安装包；
- SHA-256；
- release notes；
- JSON Schema；
- sample charts；
- license notices。

不要求 x86。

## 27.3 Docs-only 过滤

允许对仅修改文档的提交跳过昂贵的 Windows 构建，但必须保留必要的 Markdown/链接检查。

---

# 28. 代码规范

- 启用 nullable reference types；
- 启用 analyzers；
- 异步方法接受 `CancellationToken`；
- 不使用 `.Result` 或 `.Wait()` 阻塞异步调用；
- 避免全局可变单例；
- 服务通过接口和依赖注入；
- 原生资源必须明确释放；
- 公共 API 有 XML 文档；
- 领域模型优先不可变；
- 时间统一使用 `DateTimeOffset`；
- 内部数值序列化使用 invariant culture；
- 用户显示遵循当前区域设置；
- 不在 ViewModel 中直接执行 D3D/WinRT 调用；
- 不捕获并忽略通用 `Exception`；
- 不使用 magic number 表示像素格式、通道或色彩空间。

---

# 29. 安全和隐私

- 捕获必须由用户明确选择源；
- 不实现后台隐藏捕获；
- 不自动保存完整帧；
- 保存截图必须是显式功能；
- Profile 默认只保存数值与元数据；
- 导出前提示可能包含的窗口标题、显示器和硬件信息；
- 未来共享 Profile 时必须提供匿名化选项；
- 不上传遥测，除非未来提供显式 opt-in。

---

# 30. Codex 执行约束

Codex 初始化项目时必须遵守：

1. 先检查工作区状态，不覆盖已有文件。
2. 先写出简短实施计划，再创建项目。
3. 每完成一个垂直步骤立即构建和测试。
4. 遇到 WGC/HDR API 不确定性时，优先查阅微软官方文档和官方示例。
5. 不得凭记忆硬编码最低 Windows 版本。
6. 不得将 `R16G16B16A16Float` 自动解释为任意特定 HDR 标准。
7. 不得为了“看起来完整”创建无实现的大量空接口。
8. 不得引入与 MVP 无关的大型依赖。
9. 不得使用伪捕获结果冒充 WGC 成功。
10. 如果本地环境无法运行 WinUI/WGC，仍应完成可编译结构和纯逻辑测试，并明确记录未验证项。
11. 不得擅自实现遥测、自动上传或截图持久化。
12. 不得把中文从开发流程中移除；中文需求文档与英文公开文档均是一等内容。
13. 所有提交应小而清晰。
14. 任何“临时方案”必须记录 TODO、原因和退出条件。

---

# 31. 初始化里程碑

## Milestone 0：Repository Foundation

交付：

- `.gitignore`
- `.editorconfig`
- `Directory.Build.props`
- `Directory.Packages.props`
- solution 与项目结构
- MIT License 或待确认 License 占位
- `README.md`
- `README.zh-CN.md`
- `CONTRIBUTING.md`
- `CONTRIBUTING.zh-CN.md`
- `SECURITY.md`
- `CODE_OF_CONDUCT.md`
- GitHub issue/PR templates
- 基础 CI
- docs 目录
- schemas 目录

验收：

- solution restore/build；
- test project 可运行；
- README 双语互链。

## Milestone 1：Core Domain

交付：

- 颜色类型；
- HEX/RGB/HSV 基础转换；
- Chart/Patch/Layout 模型；
- Measurement Session 模型；
- JSON serializer；
- CSV serializer；
- 单元测试。

验收：

- 不依赖 WinUI；
- 所有测试通过；
- JSON round-trip 通过。

## Milestone 2：Localization and App Shell

交付：

- WinUI 3 App；
- NavigationView；
- zh-CN/en-US `.resw`；
- 语言设置；
- Settings 与 Diagnostics 页面骨架；
- DI、logging、configuration。

验收：

- 应用启动；
- 中英文可切换或按系统语言加载；
- 核心页面无硬编码用户文本。

## Milestone 3：Chart Rendering Vertical Slice

交付：

- Manual Single Color；
- Near White 基础色卡；
- Chart renderer；
- Patch layout；
- Safe sample bounds 可视化调试模式。

验收：

- 输入 `#FFFFFF` 可显示色块；
- 实际 patch 像素区域可追踪；
- layout 单元测试通过。

## Milestone 4：WGC Single-frame Vertical Slice

交付：

- WGC picker；
- D3D11 device；
- FramePool；
- 单帧获取；
- BGRA8 CPU readback；
- 生命周期和错误处理；
- Diagnostics 记录。

验收：

- 可捕获色卡窗口；
- 可读取中央 ROI；
- 可得到 Captured RGB；
- 重复捕获至少 20 次无崩溃；
- 停止后资源释放。

## Milestone 5：Measurement and Export

交付：

- Expected/Captured 对照；
- Center Median；
- Basic Difference Analyzer；
- JSON Profile；
- CSV；
- 保存对话框；
- export tests。

验收：

- 完成一次完整测量；
- 导出文件可重新解析；
- Profile 包含系统、捕获格式和 warning。

## Milestone 6：FP16 Technical Spike

交付：

- FP16 FramePool 尝试；
- CPU readback；
- 原始 float sample；
- 与 BGRA8 并列显示；
- 支持能力和失败原因记录；
- 技术报告。

验收：

- 不要求所有机器成功；
- 成功时能读取稳定 float 值；
- 失败时有明确错误；
- 文档明确数值语义仍需验证。

---

# 32. 第一阶段不实现的功能

为了控制初始化范围，以下功能只建立 backlog，不实现：

- 自动 Display Picker 取色；
- 原神专用 tone mapping 测试；
- BetterGI 配置导出；
- 17³ 以上高密度 RGB Cube；
- 3D LUT；
- 逆向整图补偿；
- ICC；
- OBS backend；
- Desktop Duplication backend；
- 插件 DLL 动态发现；
- 在线 Profile 数据库；
- 云同步；
- 实时图表；
- 帧率测试；
- 视频编码；
- 截图编辑；
- 摄影色卡识别。

---

# 33. 首个可演示场景

用户流程：

1. 启动应用。
2. 应用显示 WGC/HDR 能力摘要。
3. 用户选择语言。
4. 用户进入 Chart 页面。
5. 用户输入 `#FFFFFF`。
6. 应用打开或显示纯白色 patch。
7. 用户点击“选择捕获源”。
8. 系统 WGC Picker 打开。
9. 用户选择色卡窗口。
10. 用户点击“捕获单帧”。
11. 应用在中央安全区域采样。
12. 应用显示：

```text
Expected: #FFFFFF
Captured: #F2F2F2
Delta:    -13, -13, -13
Format:   B8G8R8A8UIntNormalized
Method:   CenterMedian
```

13. 用户导出 JSON 和 CSV。
14. Profile 中记录系统、HDR、GPU、显示器、捕获格式和警告。

这是初始化阶段最重要的端到端验收场景。

---

# 34. 开放技术问题

Codex 不得自行掩盖这些问题。应在 `docs/zh-CN/open-questions.md` 和英文对应文档中持续记录。

1. WGC 在 HDR 桌面中使用 BGRA8 时实际采用什么转换？
2. FP16 WGC Surface 的编码和颜色空间如何可靠判断？
3. XAML 纯色填充是否能作为足够稳定的理论输入？
4. Windows HDR SDR content brightness 是否影响窗口色卡及捕获结果？
5. Display Picker/外部取色器读到的值属于哪个空间？
6. 游戏 UI 是否在独立渲染层中进行 tone mapping？
7. 同一 Expected RGB 在不同背景下是否获得不同 Captured 值？
8. 捕获窗口和捕获显示器是否产生不同结果？
9. 多显示器、不同 ICC/Advanced Color 配置是否改变映射？
10. 是否存在 clipping 或明显多对一映射？
11. FP16 是否真的比 BGRA8 保留更多可用于反推的信息？
12. 最早正式支持本项目所需 HDR WGC 路径的 Windows 稳定版本是什么？
13. WinUI 3 打包和非打包模式哪种更适合首发？
14. 是否需要 Direct3D renderer 才能控制色卡输出的编码？

---

# 35. Definition of Done：项目初始化

只有满足以下条件，才能认为 Codex 完成了“项目初始化”：

- [ ] 目录结构符合本文档或有书面理由说明调整。
- [ ] Solution 可恢复依赖。
- [ ] 所有项目可构建。
- [ ] 单元测试通过。
- [ ] WinUI 3 App 可启动。
- [ ] zh-CN 和 en-US 本地化生效。
- [ ] Manual Single Color 可用。
- [ ] 至少一个内置色卡可用。
- [ ] WGC Picker 可打开。
- [ ] BGRA8 单帧捕获可运行。
- [ ] 中央区域采样可运行。
- [ ] Expected/Captured 可显示。
- [ ] JSON 和 CSV 可导出。
- [ ] Diagnostics 页面可复制环境信息。
- [ ] 原生资源无明显泄漏。
- [ ] README 中明确项目仍处于实验阶段。
- [ ] 未完成和未验证事项均有记录。
- [ ] 不存在伪造数据、静默 fallback 或未说明的硬编码补偿。

---

# 36. 建议的首批提交

```text
chore: initialize solution and repository standards
docs: add bilingual project requirements
feat(core): add color and chart domain models
test(core): cover chart generation and color parsing
feat(app): add WinUI shell and localization resources
feat(chart): add manual and near-white chart providers
feat(wgc): add capability probe and capture source picker
feat(wgc): implement BGRA8 single-frame capture
feat(sample): add center median patch sampling
feat(export): add JSON profile and CSV export
docs: record WGC HDR open questions and validation status
```

---

# 37. Codex 首轮任务提示词

可将以下内容与本文档一起提供给 Codex：

> 阅读本需求文档，并将其视为项目初始化的约束来源。先检查当前工作区和 Git 状态，然后给出不超过 12 项的实施计划。先完成 Milestone 0–2，并确保每一步都可以构建和测试。不要在第一轮实现 FP16、LUT、逆向补偿或动态插件加载。遇到 WinUI 3、WGC、D3D11 或 Windows 最低版本问题时，只使用微软官方文档或官方示例作为依据，并将无法验证的内容记录到 open-questions 文档。代码、API、Schema、资源键使用英文；开发文档优先中文，同时创建语义同步的英文版本。不要提交伪实现，不要静默降级，不要将推测写成事实。

---

# 38. 当前决策摘要

已经确定：

- 使用 C#；
- 使用 WinUI 3；
- 使用 Windows App SDK；
- WGC 是首个捕获后端；
- 色卡必须模块化；
- 用户可自定义色彩表；
- 分析器和导出器模块化；
- 至少支持中英文；
- 中文是主要设计与开发语言；
- 英文是代码和公开机器接口的规范语言；
- 文档中英文并行高质量维护；
- 初期重点是测量，不是恢复；
- 先测正向映射，再研究有限逆向估算；
- BGRA8 与 FP16 应分别测量；
- 不依赖 BetterGI；
- BetterGI HDR 调试只是次要应用场景。

尚未确定：

- 正式项目名；
- License；
- 最低 Windows 稳定版本；
- Windows App SDK 精确版本；
- .NET 8 或更新 LTS；
- packaged 或 unpackaged；
- 色卡 renderer 最终使用 XAML 还是 Direct3D；
- FP16 值的可靠语义；
- 是否以及何时支持 LUT；
- 是否建设公共 Profile 数据库。

这些未决事项不得阻塞 Milestone 0–5，但必须保留决策记录。
