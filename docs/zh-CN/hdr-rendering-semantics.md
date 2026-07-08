# HDR 渲染语义与已知限制

## 原始 scRGB 输入

HDR 手动颜色输入是**原始线性 scRGB**：

- `1.0` = 80 nits（scRGB 参考白）。
- `2.5` = 200 nits（当纸白为 200 nits 时）。
- `12.5` = 1000 nits。

这是校准工具最可控的表示方式，因为数值直接映射到绝对亮度。因此，当颜色已经以绝对线性单位表达时，色调映射没有额外意义。对于 HDR 原始 scRGB 输入，推荐的色调映射器是**直接 scRGB**（直通）。

如果你需要为 SDR 编码的颜色做纸白缩放，请使用 SDR 手动输入并选择**参考白缩放**色调映射器。SDR 输入会先被从 sRGB 线性化，然后按 `paperWhite / 80 nits` 缩放。

## 色调映射器

- **直接 scRGB**：直接透传线性颜色。用于 HDR 原始 scRGB 输入，或你希望颜色原样进入 swapchain 的场景。
- **参考白缩放**：将 sRGB 编码的输入线性化后，按 `paperWhite / 80 nits` 和 `2 ^ exposureEv` 缩放。该色调映射器用于 SDR 编码输入；若将其用于原始 scRGB 输入，会额外施加一次绝对缩放，通常不是预期行为。

## 峰值亮度

**峰值亮度（nits）** 当前只记录在渲染会话中，**不会限制输出像素**。现有的两种色调映射器（直接 scRGB 和参考白缩放）都不会在峰值亮度处裁剪或滚降。未来若有参数化曲线色调映射器，可能会使用这个值，但在此之前的峰值亮度仅用于记录。

如果你将峰值亮度设为 1000 nits，但输出的绝对值超过 1000 nits，swapchain 仍会包含这些值，显示设备会按自身行为进行裁剪或色调映射。

## HDR10 输出

HDR10 输出目前是**实验性**的。当前实现：

- 将像素打包为 `R10G10B10A2_UNORM`。
- 将 BT.709 色域转换到 BT.2020。
- 应用 PQ / ST.2084 EOTF 编码。
- **不会设置** `IDXGISwapChain4.SetHDRMetaData` / `DXGI_HDR_METADATA_HDR10`。静态 HDR 元数据（mastering primaries、白点、MaxCLL、MaxFALL 等）交给 DWM 默认处理。

由于元数据没有被显式控制，HDR10 不应被视为已校准的参考输出，仅用于视觉/功能实验。

## HDR 能力探测

HDR 能力通过 DXGI 1.6 `IDXGIOutput6::GetDesc1` 探测：

- `HdrActive` 由输出当前的 `ColorSpace`（`RGB_FULL_G10_NONE_P709` 或 `RGB_FULL_G2084_NONE_P2020`）推导。
- `HdrSupported` 是基于报告的 `MaxLuminance` 的保守启发式。
- 当窗口无法匹配到任何 DXGI 输出时，状态会报告为 **HDR 能力未知**，而不是“HDR 不支持”。

这个区分对混合显卡、远程桌面、虚拟显示器等 DXGI 可能无法报告有意义输出的场景尤其重要。

## Device-Lost 处理

当前渲染器尚未完整实现 D3D 设备恢复：

- `DXGI_ERROR_DEVICE_REMOVED`
- `DXGI_ERROR_DEVICE_RESET`
- 完整设备拆卸与 SwapChain 重建

这些都是已知限制。如果发生 device-lost 事件，色卡窗口很可能无法继续渲染，直到重启应用。在进入与同一 D3D 设备资源共享的 WGC 捕获后端之前，会先建立设备恢复的设计。
