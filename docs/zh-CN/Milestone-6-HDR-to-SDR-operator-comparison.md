# 里程碑 6：HDR 到 SDR 映射算子对比

## 目标

在已有的 FP16 WGC 原始捕获数据上，对比可配置的 HDR-to-SDR 映射算子。本里程碑关注从线性 scRGB 到 SDR-linear [0, 1] 的数学映射，并输出逐 patch CSV 和 SDR 预览 PNG。项目只随附独立实现的内置算子和一个受限表达式求值器；任何第三方算子代码均由用户在本机自行提供。

## 背景

里程碑 5 已经证明：WGC `R16G16B16A16Float` 捕获能保留 scRGB 大于 1.0 的数值，而 WGC BGRA8 会把它们全部饱和到 255。因此任何下游 HDR-to-SDR 工作流都必须从 FP16 原始数据开始，而不是默认的 8-bit BGRA8 捕获。

## 范围

- 输入：已存在的 FP16 原始帧（`R16G16B16A16Float`，原始 RGBA16F 字节）。
- 输出：每个算子各一张 SDR 预览 PNG 和一份逐 patch CSV。
- 本项目随附的内置算子：
  1. `ClampToSdr`：`out = saturate(x)`
  2. `LinearScale`：`out = saturate(x / inputWhiteScRgb)`，其中 `inputWhiteScRgb` 是希望映射为 SDR 白色的无量纲 scRGB 值。这样可避免把 scRGB 数值与显示器 nits 单位混用。
  3. `Reinhard`：`out = x / (1 + x)`
  4. `ExposureGamma`：`out = pow(1 - exp(-x * exposure), 1 / gamma)`
- `ObservedBgra8Curve`：从里程碑 5 BGRA8 捕获中记录的经验参考数据集。它**不是**通用算子，导出时必须附带产生它的显示器、HDR 状态、swapchain 格式、tone mapper 和 Windows 版本元数据。
- `CustomExpressionOperator`：基于受限数学表达式语言的本地用户自定义算子。项目只提供表达式求值器；用户表达式不随应用分发，也不属于默认算子列表。当前提交中这仅是 Core 层能力；UI 对话框和本地持久化在后续步骤中补充。
- 可选的 `ExternalProcessOperator`：通过 stdin/stdout 或临时文件调用外部工具。主程序只进行数据交换，不将工具加载进自身进程空间。

## UI 入口规划

`CustomExpressionOperator` 在本提交中已在 Core 层实现，但 App UI 尚未暴露入口。UI 入口将分两步补充：

1. **第一阶段：导出算子对比。** 在测量页增加“导出算子对比”按钮，仅在 FP16 捕获时启用。该功能对当前 FP16 帧依次运行内置算子（`ClampToSdr`、`LinearScale`、`Reinhard`、`ExposureGamma`），输出一份逐 patch CSV 和每个算子对应的 SDR 预览 PNG。
2. **第二阶段：自定义算子对话框。** 增加“自定义算子...”按钮，打开 `ContentDialog`。第一版仅支持标量表达式（`f(x)`）；RGB 模式（`r`、`g`、`b`、`a`）在标量模式稳定后再考虑。用户自定义算子保存到本机 `%LocalAppData%\WgcColorCalibrator\operators\*.json`，不进入仓库，不作为默认算法，也不随应用分发。

## 约束

- 项目不随附、复制或派生任何第三方 shader 或捕获代码。
- 内置算子和表达式求值器均为独立实现。
- 不接原神窗口，不做连续捕获，不做自动补偿。
- 所有 tone-mapping 算子位于 `WgcColorCalibrator.Core.Rendering` 层，不侵入 capture backend 或 UI。
- 用户提供的表达式和外部工具仅在用户本机运行。用户自行负责确保其本地算子的来源和许可合规。

## 交付物

1. 在 `WgcColorCalibrator.Core.Rendering.HdrToSdr` 中定义 `IHdrToSdrOperator` 抽象并实现各算子。
2. 为 `CustomExpressionOperator` 提供受限表达式求值器，支持标量变量、参数和少量数学函数。
3. 提供一项服务，对 `CapturedFrame`（FP16）应用每个算子，输出 SDR BGRA8 PNG 预览和逐 patch CSV（包含 expected、captured、SDR 映射值和 delta）（下个提交实现）。
4. 使用合成 HDR ramp 数据对每种算子的数学行为做单元测试（已完成）。
5. 在测量页增加命令，用于导出当前 FP16 捕获的算子对比结果（服务实现后补充 UI）。

## 验收标准

- 每个内置算子对合成 linear scRGB ramp 都能产生数学正确的 SDR 映射。
- 表达式求值器能拒绝不安全构造，并正确求值支持的子集。
- CSV 能清楚体现不同算子相对于原始 FP16 数据的裁切/保留细节差异。
- 实现不引用、也不包含第三方 shader 源码。
