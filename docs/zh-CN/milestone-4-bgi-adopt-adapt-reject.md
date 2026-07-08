# Milestone 4：BetterGI 参考模式对照表

## 定位

`better-genshin-impact`（GPL-3.0）是 Windows Graphics Capture（WGC）的现实工程样本。本表只记录可以从它的实现中学到什么、必须改造什么、以及明确不能照搬什么。Microsoft Learn 是 API 行为的第一规范来源。

## 结论速览

| 模式 | 决策 | 备注 |
| ---- | ---- | ---- |
| HWND → GraphicsCaptureItem | Adapt | 同一条公开 COM 互操作路径，换用 Vortice/CsWinRT 实现 |
| D3D11 device / WinRT 互操作 | Adapt | 创建自有的轻量 D3D11 device，不依赖 SharpDX，不共享渲染器 device |
| FramePool 创建 | Adopt | `CreateFreeThreaded`，单帧使用每次重建 |
| `frame.Surface` → native Texture2D | Adapt | 用 `IDirect3DDxgiInterfaceAccess.GetInterface` + 正确 IID |
| Staging texture CPU readback | Adapt | 公开 API，适配 Vortice；必须正确处理 `RowPitch` |
| 窗口边框 vs 客户区坐标 | Adapt | `DwmGetWindowAttribute` + `GetClientRect` + `ClientToScreen` |
| CaptureItem.Closed + 资源释放 | Adapt | 注意 `GraphicsCaptureItem` 不是 `IDisposable`；session/pool 必须 Dispose |
| HDR 捕获后转 SDR | **Reject** | 必须保留原始 BGRA8/FP16 数值；任何 Tone Mapping 都应是显式后处理 |
| 异常后静默降级到 BGRA8 | **Reject** | 记录 requested/actual/warning，失败或要求用户确认 |
| 连续 62 FPS 捕获 + 读写锁 | **Reject** | 首轮只实现单帧测量 |
| OpenCV Mat / SharpDX 依赖 | **Reject** | 不引入额外库；只输出 packed BGRA byte buffer |

## 逐项说明

### 1. HWND → GraphicsCaptureItem（Adapt）

BetterGI 使用 `IGraphicsCaptureItemInterop.CreateForWindow` 手动创建 `GraphicsCaptureItem`，而不是使用系统 Picker。这是公开 Windows API，本项目可直接采用同一方式：从 `ChartWindow` 的 `WindowHandle` 创建捕获目标。

差异：
- 不使用 `WinrtModule.GetActivationFactory` 的封装，改用 CsWinRT 的 `ActivationFactory` + `As` 查询接口。
- 不直接调用 `GraphicsCaptureItem.FromAbi` 获取托管对象；通过 CsWinRT 的标准投影方式获取实例。

### 2. D3D11 device / WinRT 互操作（Adapt）

BetterGI 使用 SharpDX 创建 `Device` 并调用 `CreateDirect3D11DeviceFromDXGIDevice` 获得 `IDirect3DDevice`。本项目已经使用 Vortice 3.8.3，因此等价路径是：

```text
Vortice.ID3D11Device
→ 查询 IDXGIDevice
→ CreateDirect3D11DeviceFromDXGIDevice
→ Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice
```

关键差异：
- 不共享渲染项目的 `D3D11DeviceResources`。
- 捕获项目创建并自有一个轻量 D3D11 device/context，避免 immediate-context 同步、device-lost 耦合和反向依赖。

### 3. FramePool 创建（Adopt）

BetterGI 使用 `Direct3D11CaptureFramePool.Create`（DispatcherQueue 版本）。本项目使用 `CreateFreeThreaded`，因为：

- 后端没有 DispatcherQueue。
- 单帧捕获无需 UI 线程上下文。
- `FrameArrived` 在工作线程触发，用 `TaskCompletionSource` 传回结果。

尺寸来源：
- `FramePool` 必须用 `GraphicsCaptureItem.Size`，不能用色卡物理尺寸。
- 有效区域由 `frame.ContentSize` 决定。
- 色卡 `IntendedPhysicalSize` 仅用于后续坐标映射与验证。

### 4. `frame.Surface` → native Texture2D（Adapt）

BetterGI 通过 `IDirect3DDxgiInterfaceAccess` 获取 `ID3D11Texture2D` 指针。这是 CsWinRT 投影对象的标准 COM 互操作路径。本项目必须沿用：

```text
IDirect3DSurface
→ 查询 IDirect3DDxgiInterfaceAccess
→ GetInterface(ID3D11Texture2D IID)
→ Vortice ID3D11Texture2D
```

禁止使用普通 C# `as` 或手工 vtable。

### 5. Staging texture CPU readback（Adapt）

BetterGI 创建与 `ContentSize` 相同大小的 staging texture，并复制到 `Mat`。本项目用 `Vortice` 创建 `ID3D11Texture2D` staging texture，并：

- 调用 `Map` 读取。
- 处理 `MappedSubresource.RowPitch` 可能不等于 `Width × 4` 的情况。
- 只复制 `frame.ContentSize` 区域到 packed BGRA buffer。
- 输出 `PackedRowStride = ContentSize.Width × 4`。

### 6. 窗口边框 vs 客户区坐标（Adapt）

BetterGI 用 `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` 获取窗口扩展边框，用 `GetClientRect` + `ClientToScreen` 获取客户区屏幕位置，从而计算客户区相对捕获帧的偏移。

本项目必须：
- 不假设 `DWMWA_EXTENDED_FRAME_BOUNDS` 等于 WGC 帧原点。
- 建立 `WindowGeometrySnapshot`，同时记录 `WindowRect`、`ExtendedFrameBounds`、`ClientRectInScreen`。
- 比较候选原点与 `CaptureItemSize`/`ContentSize`/`SurfaceSize`，只有在唯一匹配时才标记 `Verified`。
- 多个候选产生相同最终 offset 时视为等价；产生不同 offset 时才 `Unverified`。
- 捕获前后各采集一次几何，窗口移动/改大小/DPI 则本次作废。

### 7. CaptureItem.Closed + 资源释放（Adapt）

BetterGI 的 `Stop()` 顺序：

1. `_captureSession?.Dispose()`
2. `_captureFramePool?.Dispose()`
3. `_captureItem = null`
4. `_stagingTexture?.Dispose()`
5. `_d3dDevice?.Dispose()`

本项目采用更严格的单帧状态机：

- `TaskCompletionSource.RunContinuationsAsynchronously`
- `Interlocked` 保证只有一种完成路径（FrameArrived / Timeout / Cancel / Closed）
- `Linked CancellationTokenSource` 合并用户取消与超时
- `finally` 中：退订 `FrameArrived` 和 `Closed`、关闭 session、释放 pool、释放 staging texture、释放 frame、释放 `GraphicsCaptureItem` 引用
- `GraphicsCaptureItem` 不是 `IDisposable`，不能调用 `Dispose()`。

### 8. HDR 捕获后转 SDR（Reject）

BetterGI 在 HDR 模式下捕获 `R16G16B16A16Float`，通过 compute shader 转成 SDR 纹理，再供 OCR 使用。这正好破坏校准工具最核心的原始 HDR 数值。

本项目：
- 保留原始 BGRA8/FP16 buffer（FP16 后续里程碑）。
- 任何 Tone Mapping 都是显式、可审计的后处理步骤。
- 不引入 compute shader 做 HDR → SDR 转换。

### 9. 异常后静默降级到 BGRA8（Reject）

BetterGI 尝试 FP16，失败就捕获异常并改成 BGRA8，同时把 `_isHdrEnabled` 设为 false。本项目必须：

- 明确记录 `RequestedPixelFormat` 和 `ActualPixelFormat`。
- 如果请求格式无法创建，返回失败或要求用户确认改用其他格式。
- 不静默回退。

### 10. 连续 62 FPS 捕获 + 读写锁（Reject）

BetterGI 是实时识别工具，需要长期运行、限制帧率、保存最新帧。本项目本轮只需单帧测量：

- 用户触发一次捕获。
- 等待第一帧有效数据。
- 复制后立刻停止并释放资源。
- 不引入 `ReaderWriterLockSlim`、latest-frame 缓存或视频录制链路。

### 11. OpenCV Mat / SharpDX 依赖（Reject）

BetterGI 依赖 SharpDX 和 OpenCvSharp。本项目：

- 已使用 Vortice，不再引入 SharpDX。
- 只输出 packed BGRA byte buffer，不需要 OpenCV `Mat`。
- 不引入 OpenCvSharp、OCR 或图像处理库。

## 数据保留

BetterGI PR #3185 的观察是：在 HDR 桌面中，WGC BGRA8 捕获 `#FFFFFF` 时实际得到约 `#F7F7F7`，极端情况下约 `#F0F0F0`。这是本项目完成后的真实回归样本，但不是实现依据或验收标准。验收时只记录实际捕获值，不预设应为 `#FF`/`#F7`/`#F0`。

## 参考链接

- Microsoft Learn: Screen capture - Windows apps
- Microsoft Learn: Direct3D11CaptureFramePool.CreateFreeThreaded
- GitHub: babalae/better-genshin-impact (GPL-3.0)
