# Milestone 4: BetterGI Reference Pattern Comparison

## Positioning

`better-genshin-impact` (GPL-3.0) is a real-world engineering sample for Windows Graphics Capture (WGC). This table records what we can learn from it, what must be adapted, and what is explicitly rejected. Microsoft Learn remains the primary specification for API behavior.

## Summary

| Pattern | Decision | Notes |
| ------- | -------- | ----- |
| HWND → GraphicsCaptureItem | Adapt | Same public COM interop path, implemented with Vortice/CsWinRT |
| D3D11 device / WinRT interop | Adapt | Create a lightweight, self-owned D3D11 device; no SharpDX, no shared renderer device |
| FramePool creation | Adopt | Use `CreateFreeThreaded`; recreate per single-frame capture |
| `frame.Surface` → native Texture2D | Adapt | Use `IDirect3DDxgiInterfaceAccess.GetInterface` with the correct IID |
| Staging texture CPU readback | Adapt | Public API, adapted to Vortice; must respect `RowPitch` |
| Window frame vs client-area coordinates | Adapt | `DwmGetWindowAttribute` + `GetClientRect` + `ClientToScreen` |
| CaptureItem.Closed + resource cleanup | Adapt | `GraphicsCaptureItem` is not `IDisposable`; session/pool must be disposed |
| HDR capture then tonemap to SDR | **Reject** | Preserve raw BGRA8/FP16 values; any tone mapping must be explicit and auditable |
| Silent fallback to BGRA8 on exception | **Reject** | Record requested/actual/warning and fail or require explicit user confirmation |
| Continuous 62 FPS capture + reader/writer lock | **Reject** | Single-frame measurement only for this milestone |
| OpenCV Mat / SharpDX dependency | **Reject** | No extra libraries; output is a packed BGRA byte buffer only |

## Detailed Notes

### 1. HWND → GraphicsCaptureItem (Adapt)

BetterGI creates the `GraphicsCaptureItem` via `IGraphicsCaptureItemInterop.CreateForWindow` instead of the system picker. This is a public Windows API that we can adopt with the same approach: create the capture target from the `ChartWindow` `WindowHandle`.

Differences:
- Do not use the `WinrtModule.GetActivationFactory` wrapper; use CsWinRT `ActivationFactory` + `As` to query the interface.
- Do not call `GraphicsCaptureItem.FromAbi` directly; obtain the projected instance through the standard CsWinRT path.

### 2. D3D11 device / WinRT interop (Adapt)

BetterGI uses SharpDX to create a `Device` and calls `CreateDirect3D11DeviceFromDXGIDevice` to obtain an `IDirect3DDevice`. The equivalent path for this project is:

```text
Vortice.ID3D11Device
→ query IDXGIDevice
→ CreateDirect3D11DeviceFromDXGIDevice
→ Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice
```

Key differences:
- Do not share the renderer's `D3D11DeviceResources`.
- The capture project creates and owns a lightweight D3D11 device/context, avoiding immediate-context synchronization, device-lost coupling, and a reverse dependency.

### 3. FramePool creation (Adopt)

BetterGI uses `Direct3D11CaptureFramePool.Create` (DispatcherQueue version). This project uses `CreateFreeThreaded` because:

- The backend has no DispatcherQueue.
- Single-frame capture does not need a UI thread context.
- `FrameArrived` fires on a worker thread and the result is surfaced via `TaskCompletionSource`.

Sizing rules:
- The frame pool must be created with `GraphicsCaptureItem.Size`, not the chart physical size.
- The valid region is given by `frame.ContentSize`.
- The chart's `IntendedPhysicalSize` is used only for later coordinate mapping and validation.

### 4. `frame.Surface` → native Texture2D (Adapt)

BetterGI retrieves the `ID3D11Texture2D` pointer through `IDirect3DDxgiInterfaceAccess`. This is the standard COM interop path for CsWinRT projected objects. This project must use the same path:

```text
IDirect3DSurface
→ query IDirect3DDxgiInterfaceAccess
→ GetInterface(ID3D11Texture2D IID)
→ Vortice ID3D11Texture2D
```

Plain C# casts or manual vtable lookups are not allowed.

### 5. Staging texture CPU readback (Adapt)

BetterGI creates a staging texture matching `ContentSize` and copies it into a `Mat`. This project uses Vortice to create an `ID3D11Texture2D` staging texture and:

- Calls `Map` to read it.
- Handles the case where `MappedSubresource.RowPitch` may differ from `Width × 4`.
- Copies only the `frame.ContentSize` region into a packed BGRA buffer.
- Outputs `PackedRowStride = ContentSize.Width × 4`.

### 6. Window frame vs client-area coordinates (Adapt)

BetterGI uses `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` to obtain the extended window frame, and `GetClientRect` + `ClientToScreen` to obtain the client area in screen space, then derives the client offset relative to the capture frame.

This project must:

- Not assume `DWMWA_EXTENDED_FRAME_BOUNDS` equals the WGC frame origin.
- Build a `WindowGeometrySnapshot` recording `WindowRect`, `ExtendedFrameBounds`, and `ClientRectInScreen`.
- Compare candidate origins against `CaptureItemSize`/`ContentSize`/`SurfaceSize` and mark `Verified` only when a single match is found.
- Treat multiple candidates as equivalent if they produce the same final origin/offset; mark `Unverified` only when they disagree.
- Capture window geometry before and after the capture; invalidate the capture if the window moved, resized, or changed DPI.

### 7. CaptureItem.Closed + resource cleanup (Adapt)

BetterGI's `Stop()` order is:

1. `_captureSession?.Dispose()`
2. `_captureFramePool?.Dispose()`
3. `_captureItem = null`
4. `_stagingTexture?.Dispose()`
5. `_d3dDevice?.Dispose()`

This project uses a stricter single-frame state machine:

- `TaskCompletionSource.RunContinuationsAsynchronously`
- `Interlocked` to ensure only one completion path (`FrameArrived`, `Timeout`, `Cancel`, `Closed`)
- `Linked CancellationTokenSource` merging user cancellation and timeout
- In `finally`: unsubscribe `FrameArrived` and `Closed`, close the session, dispose the pool, dispose the staging texture, dispose the frame, and release the `GraphicsCaptureItem` reference
- `GraphicsCaptureItem` is not `IDisposable`; do not call `Dispose()` on it.

### 8. HDR capture then tonemap to SDR (Reject)

In HDR mode, BetterGI captures `R16G16B16A16Float`, then runs a compute shader to convert it to an SDR texture for OCR. This destroys the raw HDR values that this calibration tool needs.

This project:

- Preserves the raw BGRA8/FP16 buffer (FP16 is deferred to a later milestone).
- Makes any tone mapping an explicit, auditable post-processing step.
- Does not introduce a compute shader for HDR → SDR conversion.

### 9. Silent fallback to BGRA8 on exception (Reject)

BetterGI tries FP16, catches the exception, and falls back to BGRA8 while setting `_isHdrEnabled` to false. This project must:

- Explicitly record `RequestedPixelFormat` and `ActualPixelFormat`.
- Return a failure or require explicit user confirmation when the requested format cannot be created.
- Never silently fall back.

### 10. Continuous 62 FPS capture + reader/writer lock (Reject)

BetterGI is a real-time recognition tool that needs to run continuously, throttle the frame rate, and keep the latest frame. This project only needs single-frame measurement for this milestone:

- User triggers one capture.
- Wait for the first valid frame.
- Copy the data and immediately stop and release resources.
- Do not introduce `ReaderWriterLockSlim`, latest-frame caching, or a video recording pipeline.

### 11. OpenCV Mat / SharpDX dependency (Reject)

BetterGI depends on SharpDX and OpenCvSharp. This project:

- Already uses Vortice and does not introduce SharpDX.
- Outputs only a packed BGRA byte buffer and does not need an OpenCV `Mat`.
- Does not introduce OpenCvSharp, OCR, or image processing libraries.

## Preserved Observation

BetterGI PR #3185 observed that in an HDR desktop, WGC BGRA8 capture of `#FFFFFF` produced approximately `#F7F7F7`, and in extreme cases around `#F0F0F0`. This is a useful real-world regression sample after the backend is complete, but it is not an implementation input or acceptance criterion. Acceptance records the actual captured value without presuming `#FF`, `#F7`, or `#F0`.

## References

- Microsoft Learn: Screen capture - Windows apps
- Microsoft Learn: Direct3D11CaptureFramePool.CreateFreeThreaded
- GitHub: babalae/better-genshin-impact (GPL-3.0)
