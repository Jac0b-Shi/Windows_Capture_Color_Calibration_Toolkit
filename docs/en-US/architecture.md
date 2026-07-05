# Architecture

The project uses layered module boundaries:

```text
Chart Definition
  -> Chart Layout
  -> Chart Renderer
  -> Capture Backend
  -> Frame Decoder
  -> Patch Sampler
  -> Measurement Session
  -> Analyzer
  -> Profile / Report Exporter
```

The first phase implements only the Core domain model and the WinUI app shell. WGC, D3D11, DXGI, and pixel-format interop must later be isolated in a dedicated capture project and must not leak into the UI layer.

## Current Projects

- `WgcColorCalibrator.Core`: UI-independent domain types, charts, layouts, measurements, and serialization basics.
- `WgcColorCalibrator.App`: WinUI 3 shell, localization resources, settings skeleton, and diagnostics skeleton.
- `WgcColorCalibrator.Core.Tests`: pure logic unit tests.

## Constraints

- Do not fake capture results.
- Do not silently downgrade pixel formats.
- Do not claim FP16 or HDR color semantics before verification.
- Track unverified behavior in `open-questions.md`.

