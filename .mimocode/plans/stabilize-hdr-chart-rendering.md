# Stabilization Plan: HDR Chart Rendering Semantics and Lifecycle

## 1. Scope and overall approach

This round hardens the HDR D3D11 chart window so it no longer has known runtime crashes or silently-wrong semantics. The architecture from the previous milestone is kept; we only fix the gaps.

**User decisions driving this plan**
- HDR10 must be fully implemented (PQ/BT.2020/R10G10B10A2), not disabled.
- HDR manual color input must use a dedicated set of linear-scRGB float NumberBoxes, separate from the SDR 0–255 input.
- Tone Mapping mode from the UI must actually flow into rendering.
- P1 items to include in this round: all-adapter/output enumeration, color-space verification, dynamic display/DPI/HDR changes, FilePicker HWND, ChartPage event unsubscription, debug-overlay brightness control.

**Boundaries that stay unchanged**
- All Vortice/D3D11/COM code stays inside `WgcColorCalibrator.Rendering.Direct3D11`; App and Core do not reference Vortice.
- XAML preview remains SDR-only.
- WGC capture and CPU readback are still out of scope.
- Only `DirectScRgb` and `ReferenceWhiteScaled` tone mappers exist; no Genshin approximation.
- HDR10 keeps the "Experimental" UI label and a warning in `ChartRenderSession`.
- All user-visible strings go to both `en-US` and `zh-CN` `.resw` files.
- Commits must be GPG-signed.

**Recommended commit split**

This round will be committed in two GPG-signed commits, as you requested:

1. `fix(rendering): correct HDR color semantics and tone mapping`
   - HDR10 encoding, separate HDR float input, tone-mapping mode wiring, output-mode resolution fix.
2. `fix(rendering): stabilize D3D11 chart lifecycle and display enumeration`
   - SwapChain panel layout, host disposal, all-adapter output enumeration, color-space verification, dynamic changes (including periodic hotplug polling), FilePicker HWND, page event unsubscription, overlay brightness.

---

## 2. P0 fixes

### P0.1 — Implement HDR10 PQ/BT.2020/R10G10B10A2 encoding

**Problem:** `TextureChartRenderer` only supports `B8G8R8A8_UNorm` and `R16G16B16A16_Float`. Choosing `Hdr10` throws `NotSupportedException` at render time.

**Files to change**
- `src/WgcColorCalibrator.Core/Colors/ColorSpaceConverter.cs`
- `src/WgcColorCalibrator.Rendering.Direct3D11/TextureChartRenderer.cs`
- `src/WgcColorCalibrator.Rendering.Direct3D11/D3D11ChartRenderer.cs` (format mapping already exists)

**Concrete changes**
1. In `ColorSpaceConverter` add:
   - `Vector3 LinearScRgbToRec2020(Vector3)` using the standard BT.2020 3×3 matrix (0.708, 0.292, 0.170, 0.797, 0.131, 0.046).
   - `float PqEncode(float normalized)` and `float PqDecode(float)` implementing ST.2084 with `m1 = 2610/4096/4`, `m2 = 2523/4096/128`, `c1`, `c2`, `c3` constants.
   - `float NitsToPqCodeValue(float nits)` dividing by 10,000 before encoding.
2. In `TextureChartRenderer`:
   - `PixelByteSize` returns `4` for `R10G10B10A2_UNorm`.
   - `WritePixel` packs `(uint)(r * 1023) | ((uint)(g * 1023) << 10) | ((uint)(b * 1023) << 20) | ((uint)(a * 3) << 30)` with `a` forced to 1 (2 bits).
   - `MapColor` for `R10G10B10A2_UNorm` tone-maps linear scRGB, converts to BT.2020, PQ-encodes normalized by 10,000 nits, and returns a `Vector4` in `[0,1]` per channel.

**Verification**
- Unit tests: PQ round-trip, known nits → expected PQ code values, BT.2020 matrix against reference vectors.
- Manual: open ChartWindow in HDR10 mode; it must not throw and the status must show `R10G10B10A2_UNorm` + `RGB_FULL_G2084_NONE_P2020`.

---

### P0.2 — Split SDR and HDR manual color input

**Problem:** The same R/G/B 0–255 boxes are reused for HDR, interpreted as linear scRGB, so `#FFFFFF` becomes 20,000 nits instead of 200 nits.

**Files to change**
- `src/WgcColorCalibrator.App/Pages/ChartPage.xaml`
- `src/WgcColorCalibrator.App/Pages/ChartPage.xaml.cs`
- `src/WgcColorCalibrator.App/Strings/en-US/Resources.resw`
- `src/WgcColorCalibrator.App/Strings/zh-CN/Resources.resw`

**Concrete changes**
1. In `ChartPage.xaml`:
   - Wrap the existing 0–255 boxes in `SdrManualPanel`.
   - Add `HdrManualPanel` with three `NumberBox` controls (min 0, default 1.0, no max or very large max) and labels for linear scRGB R/G/B.
2. In `ChartPage.xaml.cs`:
   - Toggle `SdrManualPanel`/`HdrManualPanel` visibility based on `OutputModeComboBox` (and `ChartTypeComboBox` for manual single-color).
   - `ReadHdrColorFromManualInputs()` reads the three float boxes into `HdrColor`.
   - Keep `ManualColor` as the SDR `#RRGGBB` value and only set `ManualHdrColor` when HDR mode is selected.
3. Add resource strings for the new labels in both `.resw` files.

**Verification**
- Unit test: `ManualSingleColorChartProvider` with `ManualHdrColor` produces a `LinearScRgb` patch with the expected float values.
- Manual: switching Output mode between SDR and HDR immediately swaps the visible input panel; HDR default values like `1.0,1.0,1.0` are 80 nits, not 20,000 nits.

---

### P0.3 — Wire ToneMapping mode from the UI

**Problem:** `ToneMappingModeComboBox` defaults to `ReferenceWhiteScaled` but `ChartPage` never reads it; rendering always uses `DirectScRgb`.

**Files to change**
- `src/WgcColorCalibrator.App/Pages/ChartPage.xaml.cs`
- `src/WgcColorCalibrator.App/Services/ChartWorkspaceService.cs`
- `src/WgcColorCalibrator.Core/Charts/ChartGenerationOptions.cs`
- `src/WgcColorCalibrator.Core/Charts/ChartRenderingParameters.cs`
- `src/WgcColorCalibrator.Core/Charts/ManualSingleColorChartProvider.cs`

**Concrete changes**
1. In `ChartPage.xaml.cs` add `ReadToneMappingMode()` that reads `ToneMappingModeComboBox.SelectedItem.Tag` as `ToneMappingMode`.
2. Pass the result into `ChartGenerationOptions.ToneMappingMode`.
3. In `ManualSingleColorChartProvider` pass `options.ToneMappingMode` into `ChartRenderingParameters`.
4. In `ChartWorkspaceService` add `public ToneMappingMode CurrentToneMappingMode { get; set; }` and update it in `GenerateChart` and `SetImportedChart`.

**Verification**
- Unit test: after `GenerateChart`, `service.CurrentToneMappingMode` matches the selected combo box value.
- Manual: select `ReferenceWhiteScaled`, paper white 200, generate a white patch; output should be visibly brighter than `DirectScRgb` on the same monitor.

---

### P0.4 — Unify Requested/Actual output mode resolution

**Problem:** `ChartWorkspaceService.ResolveOutputMode` resolves once and passes the resolved mode into `ChartRenderOptions.OutputMode`. The renderer then resolves again and records `options.OutputMode` as `RequestedOutputMode`, losing the original user request and any fallback warnings.

**Files to change**
- `src/WgcColorCalibrator.Core/Rendering/OutputModeResolver.cs`
- `src/WgcColorCalibrator.Core/Rendering/ChartRenderOptions.cs`
- `src/WgcColorCalibrator.Core/Rendering/ChartRenderSession.cs`
- `src/WgcColorCalibrator.App/Services/ChartWorkspaceService.cs`
- `src/WgcColorCalibrator.Rendering.Direct3D11/D3D11ChartRenderer.cs`
- `src/WgcColorCalibrator.App/Rendering/Xaml/XamlChartPreviewRenderer.cs`

**Concrete changes**
1. Add a new `OutputModeResolution` record in `OutputModeResolver.cs`:
   ```csharp
   public sealed record OutputModeResolution(
       RenderOutputMode RequestedMode,
       RenderOutputMode ActualMode,
       IReadOnlyList<string> Warnings,
       DisplayOutputMetadata DisplayOutput);
   ```
2. Add `OutputModeResolver.ResolveDetailed(...)` that returns this record. Keep the existing `Resolve(...)` for backward compatibility, or replace all callers.
3. Update `ChartRenderOptions` to carry both `RequestedOutputMode` and `ActualOutputMode` (or replace `OutputMode` with `OutputModeResolution`).
4. Update `D3D11ChartRenderer` to use the pre-resolved `ActualOutputMode` for rendering and record both `RequestedOutputMode` and `ActualOutputMode` in `ChartRenderSession`.
5. Update `ChartWorkspaceService` to call `OutputModeResolver.ResolveDetailed` once and pass the result into `ChartRenderOptions`. Remove its duplicate `ResolveOutputMode` private method.
6. Update `XamlChartPreviewRenderer` to construct the new `ChartRenderOptions`.

**Verification**
- Unit test: request HDR with display HDR inactive and behavior `SwitchToSdr`; result must record `RequestedMode = HdrScRgb`, `ActualMode = SdrSrgb`, and a warning about fallback.
- Manual: status bar shows both the user request and the actual mode.

---

### P0.5 — Fix ChartWindow / SwapChainPanelHost lifecycle leak

**Problem:** `D3D11ChartRenderer` caches `SwapChainPanelHost` instances in a dictionary keyed by the panel. When the window closes, the host is never evicted, so `ChartWindow`, `SwapChainPanel`, `IDXGISwapChain`, and GPU back-buffers accumulate.

**Files to change**
- `src/WgcColorCalibrator.Rendering.Direct3D11/D3D11ChartRenderer.cs`
- `src/WgcColorCalibrator.Rendering.Direct3D11/SwapChainPanelHost.cs`
- `src/WgcColorCalibrator.App/Windows/ChartWindow.xaml.cs`
- `src/WgcColorCalibrator.App/Services/ChartWorkspaceService.cs`
- `src/WgcColorCalibrator.Core/Rendering/IChartRenderer.cs` (add `DetachHost` or make it return `IDisposable`)

**Concrete changes**
1. Add `void DetachHost(object panel)` to `IChartRenderer` and implement it in `D3D11ChartRenderer`:
   - If a host exists for the panel, set `ISwapChainPanelNative.SetSwapChain(null)` to detach it from the panel, then dispose the host, then remove it from the dictionary.
2. In `SwapChainPanelHost`, add `DetachFromPanel()` that calls `SetSwapChain(null)` and disposes its own resources (texture renderer, swap chain, render target view).
3. In `ChartWindow.xaml.cs`, store a reference to the `SwapChainPanel`. In the `Closed` event, call `_renderer.DetachHost(ChartSwapChainPanel)` before the window is destroyed.
4. In `ChartWorkspaceService.CloseChartWindow`, ensure the `Closed` handler runs and the renderer detaches.

**Verification**
- Manual: open and close ChartWindow repeatedly; inspect GPU memory or add a debug log; the number of live hosts should not grow.

---

### P0.6 — Wait for panel layout before creating the swap chain

**Problem:** `ChartWindow` renders immediately after `Activate`, before `ActualWidth`/`ActualHeight` are known. If the panel has zero size, the renderer creates a 1×1 swap chain.

**Files to change**
- `src/WgcColorCalibrator.App/Windows/ChartWindow.xaml.cs`
- `src/WgcColorCalibrator.App/Services/ChartWorkspaceService.cs`
- `src/WgcColorCalibrator.Rendering.Direct3D11/D3D11ChartRenderer.cs` (optional resize-on-demand)

**Concrete changes**
1. In `ChartWindow.xaml.cs`:
   - Add `bool _isLoaded` and `bool _hasSize`.
   - Handle `ChartSwapChainPanel.Loaded` and `SizeChanged`.
   - Only call `_workspaceService.OnChartWindowReady()` once `_isLoaded && _hasSize && XamlRoot != null`.
2. In `ChartWorkspaceService`, split `OpenChartWindow` into:
   - Window creation/activation (no render yet).
   - `OnChartWindowReady()` that is called from the panel after layout is known, then probes display and renders.
3. In `D3D11ChartRenderer`, if the host already exists with a 1×1 swap chain and a larger size arrives, recreate the swap chain at the new size.

**Verification**
- Manual: add logging of intended physical size; first render must be > 1×1 physical pixels on normal displays.
- Unit test: not easy without UI automation; rely on manual check.

---

## 3. P1 fixes

### P1.1 — Enumerate all DXGI adapters/outputs for HMONITOR matching

**Problem:** `DisplayOutputProbe` only enumerates the adapter that owns the current D3D11 device. On a multi-GPU system, the window may be on a monitor connected to a different adapter, returning `Unknown`.

**Files to change**
- `src/WgcColorCalibrator.Rendering.Direct3D11/DisplayOutputProbe.cs`
- `src/WgcColorCalibrator.Rendering.Direct3D11/D3D11DeviceResources.cs` (expose `DXGIFactory`)

**Concrete changes**
1. Replace single-adapter enumeration with:
   ```csharp
   foreach (var adapter in _factory.EnumAdapters1())
   {
       for (uint i = 0; adapter.EnumOutputs(i, out var output) == Result.Ok; i++)
       {
           if (output.Description.Monitor == hMonitor)
               return BuildMetadata(output);
       }
   }
   ```
2. Ensure `DisplayOutputProbe` is created with the factory from `D3D11DeviceResources` rather than creating its own device.

**Verification**
- Manual on a hybrid-GPU machine: move ChartWindow to internal display and external display; both should return non-Unknown metadata.

---

### P1.2 — Verify and record DXGI color-space support

**Problem:** `SetColorSpace1` is called without checking `CheckColorSpaceSupport` or reading back the result with `GetColorSpace1`. `ChartRenderSession.HdrOutputActive` is just a mode comparison, not a runtime confirmation.

**Files to change**
- `src/WgcColorCalibrator.Rendering.Direct3D11/SwapChainPanelHost.cs`
- `src/WgcColorCalibrator.Rendering.Direct3D11/D3D11ChartRenderer.cs`
- `src/WgcColorCalibrator.Core/Rendering/ChartRenderSession.cs` (optional: record color-space support flags and result)

**Concrete changes**
1. In `SwapChainPanelHost` add `public bool TrySetColorSpace(ColorSpaceType requested, out ColorSpaceType actual)`:
   - Query `IDXGISwapChain3`.
   - Call `CheckColorSpaceSupport` for the requested color space; if not supported, return false.
   - Call `SetColorSpace1`; check the result.
   - Call `GetColorSpace1` to confirm the actual color space.
2. In `D3D11ChartRenderer`, if `TrySetColorSpace` returns false or actual differs, add a warning to the session and set `HdrOutputActive = false`.

**Verification**
- Unit test: mock is hard; rely on manual inspection of `ChartRenderSession` warnings and `HdrOutputActive` flag on SDR vs HDR displays.

---

### P1.3 — Handle dynamic display, DPI, and HDR state changes

**Problem:** Probing only happens when the window opens or debug overlay toggles. Moving the window, changing DPI, or toggling Windows HDR after opening does not update the render session.

**Files to change**
- `src/WgcColorCalibrator.App/Windows/ChartWindow.xaml.cs`
- `src/WgcColorCalibrator.App/Services/ChartWorkspaceService.cs`
- `src/WgcColorCalibrator.Rendering.Direct3D11/D3D11ChartRenderer.cs`
- `src/WgcColorCalibrator.Core/Rendering/IDisplayOutputProbe.cs` (optional)

**Concrete changes**
1. In `ChartWindow.xaml.cs`:
   - Track the current `DisplayInformation` and `HMONITOR`.
   - Handle `DisplayInformation.DisplayContentsInvalidated` (DPI/display change) and `DisplayInformation.ColorChanged` (HDR toggle on Win10 1903+).
   - On `Window.Current.CoreWindow.SizeChanged` and `LocationChanged`, detect HMONITOR change.
2. In `ChartWorkspaceService`, add `ReProbeAndRender()` that re-probes display metadata, re-resolves output mode, and re-renders if changed.
3. In `D3D11ChartRenderer`, if `ActualOutputMode` changes due to re-probing, recreate the swap chain at the new format/color space.
4. Add a periodic hotplug guard: use a `DispatcherQueueTimer` on the UI thread every 2 seconds to compare the current HMONITOR with the last-known HMONITOR; if it changed (or `DisplayInformation` reports invalidation), trigger `ReProbeAndRender()`. This covers monitor hotplug without relying on a low-level DXGI notification event.

**Verification**
- Manual: open ChartWindow, toggle Windows HDR, move to a different display, change DPI; the status should update without closing the window.

---

### P1.4 — Initialize FilePicker with the main window HWND

**Problem:** `ChartPage.InitializePicker` is empty and relies on a wrong comment. Packaged WinUI 3 pickers still need the owner HWND.

**Files to change**
- `src/WgcColorCalibrator.App/Pages/ChartPage.xaml.cs`
- `src/WgcColorCalibrator.App/Windows/MainWindow.xaml.cs` (expose HWND if needed)

**Concrete changes**
1. In `MainWindow.xaml.cs`, add a public property `public nint WindowHandle { get; }` initialized with `WinRT.Interop.WindowNative.GetWindowHandle(this)`.
2. In `ChartPage.xaml.cs`, pass `App.Services.GetRequiredService<MainWindow>().WindowHandle` to `InitializeWithWindow` for both `FileOpenPicker` and `FileSavePicker`.
3. Remove the incorrect comment.

**Verification**
- Manual: click Import/Export on the Chart page; the picker should open and not crash or hang silently.

---

### P1.5 — Unsubscribe ChartPage from Workspace events on navigation

**Problem:** `ChartPage` constructor subscribes to `_workspaceService.StateChanged`. Since `WorkspaceService` is a singleton and pages are recreated on navigation, old pages leak and all of them re-render on every state change.

**Files to change**
- `src/WgcColorCalibrator.App/Pages/ChartPage.xaml.cs`

**Concrete changes**
1. Store the event handler delegate in a field.
2. In `OnNavigatedFrom` or `Page.Unloaded`, unsubscribe the handler from `_workspaceService.StateChanged`.
3. Optionally set a flag to ignore late callbacks.

**Verification**
- Manual: navigate away from Chart page and back; each state change should only update the current page once.

---

### P1.6 — Tone-map debug overlay brightness

**Problem:** Debug overlay uses `Vector4(255,255,255,255)` and `Vector4(255,0,0,255)`, which are extreme in FP16 scRGB.

**Files to change**
- `src/WgcColorCalibrator.Core/Rendering/ChartRenderOptions.cs`
- `src/WgcColorCalibrator.Rendering.Direct3D11/TextureChartRenderer.cs`
- `src/WgcColorCalibrator.App/Services/ChartWorkspaceService.cs` (set default)

**Concrete changes**
1. Add `float DebugOverlayBrightnessNits` to `ChartRenderOptions` (default 200 nits, i.e., scRGB 2.5 for ReferenceWhiteScaled; use 1.0 for DirectScRgb).
2. In `TextureChartRenderer` draw the overlay using the tone mapper with the same `ToneMappingParameters` as patches, so the overlay is rendered at the configured brightness independent of mode.

**Verification**
- Manual: enable debug overlay in HDR mode; the overlay should be visible but not searingly bright.

---

## 4. UI/UX changes summary

- Chart page gains a second manual-color panel (`HdrManualPanel`) with three linear-scRGB float NumberBoxes, shown only when output mode is HDR and chart type is manual single-color.
- The existing `#RRGGBB` / 0–255 panel is renamed `SdrManualPanel` and shown only for SDR output.
- `ToneMappingModeComboBox` is read during generation; the selected mode is reflected in `ChartWorkspaceService.CurrentToneMappingMode` and the renderer.
- HDR10 remains in the Output mode dropdown but is labeled as Experimental; selecting it adds a warning to the session.
- Status text shows both requested and actual output mode, plus display metadata and any warnings.

---

## 5. Unit tests to add/update

1. `ColorSpaceConverterTests`:
   - `LinearScRgbToRec2020` reference values.
   - `PqEncode` / `PqDecode` round-trip for 80, 200, 1000 nits.
   - `NitsToPqCodeValue` produces expected packed 10-bit values.
2. New test project `tests/WgcColorCalibrator.Rendering.Direct3D11.Tests`:
   - `TextureChartRendererTests` for `R10G10B10A2_UNorm` packing and FP16 packing of known colors.
   - `SwapChainPanelHostTests` for color-space support flags (where possible without a real window).
   - Only tests that do not require a real GPU/swapchain should be added; heavy runtime paths stay manual.
3. `OutputModeResolverTests`:
   - `ResolveDetailed` returns correct `RequestedMode`, `ActualMode`, and warnings for each policy.
4. `ManualSingleColorChartProviderTests`:
   - HDR manual color input produces `LinearScRgb` patch with correct `HdrColor`.
5. `ChartWorkspaceServiceTests` (if not already present):
   - `GenerateChart` stores `CurrentToneMappingMode`.
   - `ResolveOutputMode` fallback records the original request.
6. `ResourceKeyConsistencyTests`:
   - New HDR float input resource keys exist in both languages.

---

## 6. Risks and mitigation

| Risk | Mitigation |
|------|------------|
| HDR10 PQ/BT.2020 math is subtle and error-prone | Use reference test vectors from ITU-R BT.2100; verify round-trip and known nits points. Keep the "Experimental" label. |
| Splitting UI input changes may break existing SDR generation | Keep SDR path unchanged; only add new HDR path. Add unit tests for `ManualSingleColorChartProvider`. |
| Host disposal timing may cause COM access violations | Detach swap chain from panel before disposing; ensure operations are on the UI thread. |
| Dynamic-change detection can fire rapidly | Throttle re-render with a small debounce (e.g., 200 ms) and only re-probe if HMONITOR or scale changed. Use a 2-second UI-thread timer for hotplug polling. |
| Color-space verification may fail on some drivers | Treat unsupported color space as a warning and fallback to SDR, not a crash. |
| Large commit | Split into two commits as recommended; if forced to one, group logically and keep tests passing. |

---

## 7. Verification commands

Run after every commit boundary and at the end:

```powershell
git status --short
git diff --check
dotnet restore WgcColorCalibrator.sln --locked-mode
dotnet build WgcColorCalibrator.sln --no-restore -p:Platform=x64
dotnet test WgcColorCalibrator.sln --no-build
```

Manual checks:
1. SDR mode: generate a grayscale chart and verify it opens at the correct size.
2. HDR scRGB mode: verify the float input panel appears, default `1,1,1` is 80 nits, and ReferenceWhiteScaled at 200 nits paper white is brighter than DirectScRgb.
3. HDR10 mode: select it, open chart window, verify no exception and status shows `R10G10B10A2_UNorm` + `RGB_FULL_G2084_NONE_P2020`.
4. Close and reopen ChartWindow 3 times; verify no GPU resource growth (debug log or memory check).
5. Move the window to another display (if available); verify metadata updates.
6. Toggle Windows HDR while the window is open; verify the status updates.
7. Import/Export buttons open the file picker.
8. Navigate away from Chart page and back; verify only one page responds to state changes.

---

## 8. Resolved decisions

1. **Commit split:** Two commits as listed above.
2. **HDR10 test coverage:** Add a new `tests/WgcColorCalibrator.Rendering.Direct3D11.Tests` project for GPU-related logic that can be tested without a real window.
3. **Dynamic-change detection scope:** Include periodic HMONITOR polling for monitor hotplug in addition to WinUI display events.
