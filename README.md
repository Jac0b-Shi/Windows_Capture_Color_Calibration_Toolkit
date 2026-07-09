# WGC Color Calibrator

[简体中文](README.zh-CN.md)

WGC Color Calibrator is an experimental Windows developer tool for measuring how Windows Graphics Capture reports digital color patches under SDR and HDR desktop conditions.

## What it does

1. Generates known color charts (grayscale ramp, HDR scRGB ramp, single-color, near-white) and renders them in a D3D11 swapchain window.
2. Captures the chart window via WGC in either BGRA8 or FP16 RGBA format.
3. Samples each color patch from the captured pixels and records expected vs. captured values.
4. Exports measurement sessions as JSON, CSV, raw frames, and debug overlays.
5. Compares built-in HDR-to-SDR tone-mapping operators (Clamp, LinearScale, Reinhard, ExposureGamma) against FP16 capture data, producing per-operator SDR preview PNGs and per-patch CSV.

## Requirements

- Windows 10 version 1809 or later is the current packaging baseline (`TargetPlatformMinVersion=10.0.17763.0`).
- WGC requires `Windows.Graphics.Capture` support. The minimum Windows version for the HDR/FP16 measurement path has not been fully validated.
- .NET SDK 10.0.x.
- Windows App SDK package dependencies restored from NuGet.
- Visual Studio with Windows app development components is recommended for running the WinUI project.
- A current Windows 11 HDR system is recommended for development and experiments.

## Build

```powershell
dotnet restore WgcColorCalibrator.sln
dotnet build WgcColorCalibrator.sln --no-restore -p:Platform=x64
dotnet test WgcColorCalibrator.sln --no-build -p:Platform=x64
```

## Project status

| Milestone | Description | Status |
|-----------|-------------|--------|
| 1 | Repository foundation, core domain model, WinUI shell | Done |
| 2 | Chart rendering, layout engine, D3D11 swapchain | Done |
| 3 | HDR-capable D3D11 chart window (scRGB FP16 swapchain) | Done |
| 4 | WGC BGRA8 single-frame capture and measurement loop | Done |
| 5 | WGC FP16 raw capture baseline | Done |
| 6 | HDR-to-SDR operator comparison (built-in operators, export folder) | In progress |

The capture backend, FP16 readback, and measurement sampling pipeline are considered stable and are not modified during Milestone 6 work.

## License

Licensed under LGPL-3.0-only. See `LICENSE`.
