# Milestone 6: HDR-to-SDR Operator Comparison

## Goal

Compare independently implemented HDR-to-SDR tone-mapping operators against the raw FP16 WGC capture data, and use the results as a behavioral reference for evaluating other HDR-to-SDR pipelines (including BetterGI) without copying any third-party shader code.

## Background

Milestone 5 proved that WGC `R16G16B16A16Float` capture preserves HDR scRGB values above 1.0, while WGC BGRA8 saturates them to 255. This means any downstream HDR-to-SDR workflow must start from FP16 raw data, not from the 8-bit BGRA8 capture that Windows Desktop Duplication / WGC provides by default.

## Scope

- Input: an existing FP16 raw captured frame (`R16G16B16A16Float`, raw RGBA16F bytes).
- Output: one SDR preview PNG and one per-patch CSV for each operator.
- Operators implemented in this project:
  1. `ClampToSdr` — `out = saturate(x)`
  2. `LinearScale` — `out = saturate(x / inputWhiteScRgb)` where `inputWhiteScRgb` is the dimensionless scRGB value that should map to SDR white. This avoids mixing scRGB values with display nits.
  3. `Reinhard` — `out = x / (1 + x)`
  4. `ExposureGamma` — `out = pow(1 - exp(-x * exposure), 1 / 2.2)`
- The BGRA8 capture curve measured in Milestone 5 is kept as an empirical reference dataset (`ObservedBgra8Curve`).
  It is **not** a generic operator; it must always be exported together with the display, HDR state, swapchain format, tone-mapper, and Windows build metadata that produced it.

## Constraints

- BetterGI shader code is GPL-3.0 and must not be copied into this project.
- BetterGI may be studied only as a behavioral reference; any operator here is implemented independently.
- No Genshin Impact window capture, no continuous capture, and no automatic compensation.
- All tone-mapping operators live in the Core/Rendering layer, not in the capture backend or the UI.

## Deliverables

1. `IHdrToSdrOperator` abstraction and operator implementations in `WgcColorCalibrator.Core.Rendering`.
2. A service that applies each operator to a `CapturedFrame` (FP16) and produces:
   - SDR BGRA8 PNG preview
   - Per-patch CSV with expected, captured, SDR-mapped, and delta values
3. Unit tests using synthetic HDR ramp data to verify each operator's mathematical behavior.
4. UI command on the Measurement page to export operator comparison results for the current FP16 capture.

## Success Criteria

- Each operator produces a mathematically correct SDR mapping for a synthetic linear scRGB ramp.
- The CSV clearly shows where each operator clips or preserves HDR detail relative to the raw FP16 data.
- The implementation does not reference or contain BetterGI shader source code.
