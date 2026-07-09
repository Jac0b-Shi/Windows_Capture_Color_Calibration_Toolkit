# 里程碑 6：HDR 到 SDR 映射算子对比

## 目标

在已有 FP16 原始 WGC 捕获数据上，独立实现若干 HDR-to-SDR tone-mapping 算子，生成 SDR 预览和 CSV 数据，作为评估其他 HDR-to-SDR 流程（包括 BetterGI）的行为基准。不复制任何第三方 shader 源码。

## 背景

里程碑 5 已经证明：WGC `R16G16B16A16Float` 捕获能保留 scRGB 大于 1.0 的数值，而 WGC BGRA8 会把它们全部饱和到 255。因此任何下游 HDR-to-SDR 工作流都必须从 FP16 原始数据开始，而不是默认的 8-bit BGRA8 捕获。

## 范围

- 输入：已存在的 FP16 原始帧（`R16G16B16A16Float`，原始 RGBA16F 字节）。
- 输出：每个算子各一张 SDR 预览 PNG 和一份逐 patch CSV。
- 本项目独立实现的算子：
  1. `ClampToSdr`：`out = saturate(x)`
  2. `LinearScale`：`out = saturate(x / inputWhiteScRgb)`，其中 `inputWhiteScRgb` 是希望映射为 SDR 白色的无量纲 scRGB 值。这样可避免把 scRGB 数值与显示器 nits 单位混用。
  3. `Reinhard`：`out = x / (1 + x)`
  4. `ExposureGamma`：`out = pow(1 - exp(-x * exposure), 1 / 2.2)`
- 里程碑 5 测得的 BGRA8 曲线作为经验参考数据集 `ObservedBgra8Curve`。
  它**不是**通用算子，导出时必须附带产生它的显示器、HDR 状态、swapchain 格式、tone mapper 和 Windows 版本元数据。

## 约束

- BetterGI 的 shader 代码是 GPL-3.0，禁止复制到本项目。
- BetterGI 只能作为行为参考；本项目所有算子必须独立实现。
- 不接原神窗口，不做连续捕获，不做自动补偿。
- 所有 tone-mapping 算子位于 Core/Rendering 层，不侵入 capture backend 或 UI。

## 交付物

1. 在 `WgcColorCalibrator.Core.Rendering` 中定义 `IHdrToSdrOperator` 抽象并实现上述算子。
2. 提供一项服务，对 `CapturedFrame`（FP16）应用每个算子，输出：
   - SDR BGRA8 PNG 预览
   - 逐 patch CSV（包含 expected、captured、SDR 映射值和 delta）
3. 使用合成 HDR ramp 数据对每种算子的数学行为做单元测试。
4. 在测量页增加命令，用于导出当前 FP16 捕获的算子对比结果。

## 验收标准

- 每个算子对合成 linear scRGB ramp 都能产生数学正确的 SDR 映射。
- CSV 能清楚体现不同算子相对于原始 FP16 数据的裁切/保留细节差异。
- 实现不引用、也不包含 BetterGI 的 shader 源码。
