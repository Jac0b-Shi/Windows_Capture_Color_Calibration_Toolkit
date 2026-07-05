# 架构

项目采用分层边界：

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

第一轮只实现 Core 领域模型和 WinUI App Shell。WGC、D3D11、DXGI 和像素格式互操作后续必须集中到独立捕获项目中，不得泄漏到 UI 层。

## 当前项目

- `WgcColorCalibrator.Core`：不依赖 WinUI 的领域类型、色卡、布局、测量和序列化基础。
- `WgcColorCalibrator.App`：WinUI 3 应用外壳、本地化资源、设置和诊断页面骨架。
- `WgcColorCalibrator.Core.Tests`：纯逻辑单元测试。

## 约束

- 不伪造捕获结果。
- 不静默降级像素格式。
- 不在未经验证时声明 FP16 或 HDR 数值语义。
- 未验证问题记录到 `open-questions.md`。

