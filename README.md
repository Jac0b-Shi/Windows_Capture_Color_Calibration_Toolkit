# WGC Color Calibrator

[简体中文](README.zh-CN.md)

WGC Color Calibrator is an experimental Windows developer tool for measuring how Windows Graphics Capture reports digital color patches under SDR and HDR desktop conditions.

Current scope:

- Repository foundation.
- Core color, chart, layout, and measurement domain model.
- WinUI 3 application shell with bilingual resources.

Not implemented in this first phase:

- FP16 capture.
- LUT generation.
- inverse compensation.
- dynamic plugin loading.
- continuous capture.
- WGC single-frame capture.

## Requirements

- Windows development machine.
- .NET SDK 10.0.x.
- Windows App SDK package dependencies restored from NuGet.
- Visual Studio with Windows app development components is recommended for running the WinUI project.

The project file currently uses `TargetPlatformMinVersion=10.0.17763.0` as the Windows App SDK packaging baseline. This is not a verified minimum version for the required WGC HDR measurement path. That open question is tracked in `docs/en-US/open-questions.md`.

## Build

```powershell
dotnet restore WgcColorCalibrator.sln
dotnet build WgcColorCalibrator.sln --no-restore
dotnet test WgcColorCalibrator.sln --no-build
```

## Status

This repository is in the initialization stage. It must not be used as evidence that a given Windows version, GPU, pixel format, or HDR mode has specific WGC color semantics until that behavior is verified and documented.

## License

Licensed under LGPL-3.0-only. See `LICENSE`.

