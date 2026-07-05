# 测量模型

初始化阶段的核心比较关系是：

```text
Expected / Declared Color -> WGC-captured Color
```

`Display-observed Color` 作为可选字段保留，第一轮不实现自动屏幕测量。

## 编码原则

- `Rgb8` 表示 8-bit RGB 通道，通道顺序为 R、G、B。
- `Rgba8` 表示 8-bit RGBA 通道，通道顺序为 R、G、B、A。
- `RgbaFloat` 只表达浮点通道容器，不暗示 HDR 标准或色彩空间。
- 未验证或未知的色彩空间使用 `Unknown`。

## Profile

JSON key 使用英文。日期使用 ISO 8601。浮点序列化使用 invariant culture。

