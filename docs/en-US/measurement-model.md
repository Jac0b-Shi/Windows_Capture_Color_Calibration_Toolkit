# Measurement Model

The initialization-stage comparison is:

```text
Expected / Declared Color -> WGC-captured Color
```

`Display-observed Color` is reserved as an optional field. Automated screen measurement is not implemented in the first phase.

## Encoding Principles

- `Rgb8` represents 8-bit RGB channels in R, G, B order.
- `Rgba8` represents 8-bit RGBA channels in R, G, B, A order.
- `RgbaFloat` is only a floating-point channel container. It does not imply an HDR standard or color space.
- Unverified or unknown color spaces use `Unknown`.

## Profile

JSON keys are English. Dates use ISO 8601. Floating-point serialization uses invariant culture.

