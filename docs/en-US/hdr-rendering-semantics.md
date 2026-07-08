# HDR Rendering Semantics and Known Limitations

## Raw scRGB Input

The HDR manual color input is **raw linear scRGB**:

- `1.0` = 80 nits (the scRGB reference white).
- `2.5` = 200 nits when the paper-white is 200 nits.
- `12.5` = 1000 nits.

This is the most controllable representation for a calibration tool because the value maps directly to absolute luminance. Tone mapping is only meaningful when the source color is not already expressed in absolute linear units. Therefore, for HDR raw scRGB input, the intended tone mapper is **Direct scRGB** (passthrough).

If you need paper-white scaling for an SDR-encoded color, use the SDR manual input and the **Reference white scaled** tone mapper instead. The SDR input is first linearized from sRGB, then scaled by `paperWhite / 80 nits`.

## Tone Mappers

- **Direct scRGB**: Passes the linear color through unchanged. Use this for HDR raw scRGB input or when you want the color value to reach the swapchain exactly as entered.
- **Reference white scaled**: Linearizes sRGB-encoded input and scales it by `paperWhite / 80 nits` and `2 ^ exposureEv`. This tone mapper is intended for SDR-encoded input; applying it to raw scRGB input will apply an additional absolute scale, which is usually not what you want.

## Peak Brightness

The **Peak brightness (nits)** value is currently recorded in the render session but **does not limit the output pixels**. The two existing tone mappers (Direct scRGB and Reference white scaled) do not clip or roll off at the peak brightness. A future tone mapper that uses a parameterized curve may consume this value, but until then it is for documentation only.

If you set peak brightness to 1000 nits but emit values that exceed 1000 nits in absolute terms, the swapchain will contain those values and the display will clip or tone map them according to its own behavior.

## HDR10 Output

HDR10 output is **experimental**. The current implementation:

- Packs pixels into `R10G10B10A2_UNORM`.
- Converts BT.709 primaries to BT.2020.
- Applies PQ / ST.2084 EOTF encoding.
- Does **not** set `IDXGISwapChain4.SetHDRMetaData` / `DXGI_HDR_METADATA_HDR10`. Static HDR metadata (mastering primaries, white point, MaxCLL, MaxFALL, etc.) is left to the DWM default.

Because the metadata is not explicitly controlled, HDR10 should not be treated as a calibrated reference output. Use it only for visual/functional experiments.

## HDR Capability Probing

HDR capability is probed through DXGI 1.6 `IDXGIOutput6::GetDesc1`:

- `HdrActive` is derived from the output's current `ColorSpace` (`RGB_FULL_G10_NONE_P709` or `RGB_FULL_G2084_NONE_P2020`).
- `HdrSupported` is a conservative heuristic based on the reported `MaxLuminance`.
- When the window cannot be matched to a DXGI output, the state is reported as **HDR capability unknown**, not "HDR unsupported".

This distinction matters for hybrid-GPU, remote-desktop, and virtual-display scenarios where DXGI may not report a meaningful output.

## Device-Lost Handling

The current renderer does not fully implement D3D device recovery:

- `DXGI_ERROR_DEVICE_REMOVED`
- `DXGI_ERROR_DEVICE_RESET`
- Full device teardown and SwapChain recreation

These are known limitations. If a device-lost event occurs, the chart window will likely fail to render until the application is restarted. A design for device recovery will be established before the WGC capture backend shares the same D3D device resources.
