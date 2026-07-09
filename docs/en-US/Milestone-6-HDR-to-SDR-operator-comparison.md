# Milestone 6: HDR-to-SDR Operator Comparison

## Goal

Compare configurable HDR-to-SDR mapping operators against raw FP16 WGC capture data. The milestone focuses on the mathematical mapping from linear scRGB to SDR-linear [0, 1], and produces both per-patch CSV exports and SDR preview PNGs. The project ships only independently implemented operators and a restricted expression evaluator; any third-party operator code is supplied locally by the user.

## Background

Milestone 5 proved that WGC `R16G16B16A16Float` capture preserves HDR scRGB values above 1.0, while WGC BGRA8 saturates them to 255. This means any downstream HDR-to-SDR workflow must start from FP16 raw data, not from the 8-bit BGRA8 capture that Windows Desktop Duplication / WGC provides by default.

## Scope

- Input: an existing FP16 raw captured frame (`R16G16B16A16Float`, raw RGBA16F bytes).
- Output: one SDR preview PNG and one per-patch CSV for each operator.
- Built-in operators implemented in this project:
  1. `ClampToSdr` — `out = saturate(x)`
  2. `LinearScale` — `out = saturate(x / inputWhiteScRgb)` where `inputWhiteScRgb` is the dimensionless scRGB value that should map to SDR white. This avoids mixing scRGB values with display nits.
  3. `Reinhard` — `out = x / (1 + x)`
  4. `ExposureGamma` — `out = pow(1 - exp(-x * exposure), 1 / gamma)`
- `ObservedBgra8Curve`: an empirical reference dataset recorded from the Milestone 5 BGRA8 capture. It is **not** a generic operator; it must always be exported together with the display, HDR state, swapchain format, tone-mapper, and Windows build metadata that produced it.
- `CustomExpressionOperator`: a local, user-defined operator based on a restricted mathematical expression language. The project only provides the evaluator; user expressions are not shipped with the application and are not part of the default operator list.
- Optional `ExternalProcessOperator`: invokes an external tool through stdin/stdout or temporary files. The main application only exchanges data; it does not load the tool into its address space.

## Constraints

- The project does not ship, copy, or derive from third-party shader or capture code.
- Built-in operators and the expression evaluator are implemented independently.
- No Genshin Impact window capture, no continuous capture, and no automatic compensation.
- All tone-mapping operators live in the `WgcColorCalibrator.Core.Rendering` layer, not in the capture backend or the UI.
- User-provided expressions and external tools run only on the user's machine. The user is responsible for ensuring that any local operator is licensed appropriately.

## Deliverables

1. `IHdrToSdrOperator` abstraction and operator implementations in `WgcColorCalibrator.Core.Rendering.HdrToSdr`.
2. A restricted expression evaluator for `CustomExpressionOperator` supporting scalar variables, parameters, and a small set of mathematical functions.
3. A service that applies each operator to a `CapturedFrame` (FP16) and produces:
   - SDR BGRA8 PNG preview
   - Per-patch CSV with expected, captured, SDR-mapped, and delta values
4. Unit tests using synthetic HDR ramp data to verify each operator's mathematical behavior.
5. UI command on the Measurement page to export operator comparison results for the current FP16 capture.

## Success Criteria

- Each built-in operator produces a mathematically correct SDR mapping for a synthetic linear scRGB ramp.
- The expression evaluator rejects unsafe constructs and evaluates the supported subset correctly.
- The CSV clearly shows where each operator clips or preserves HDR detail relative to the raw FP16 data.
- The implementation does not reference or contain third-party shader source code.
