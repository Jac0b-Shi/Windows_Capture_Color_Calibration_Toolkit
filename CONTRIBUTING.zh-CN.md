# 贡献指南

[English](CONTRIBUTING.md)

本项目优先接受小而可验证的变更。

## 规则

- 代码标识符、Namespace、API、JSON key、资源键和 CLI 参数使用英文。
- 开发设计文档中文优先，同时维护语义同步的高质量英文版本。
- 不得引入伪捕获结果、静默像素格式降级或未说明的硬编码颜色补偿。
- 涉及 WinUI 3、Windows Graphics Capture、D3D11、DXGI、HDR 像素格式和最低 Windows 版本的结论，必须以微软官方文档或官方示例为依据。
- 未验证的 WGC/HDR 行为写入 `docs/zh-CN/open-questions.md`。
- 提交前执行 `dotnet restore`、`dotnet build` 和 `dotnet test`。

## 范围控制

初始化阶段只完成仓库基础、核心领域模型和应用外壳。FP16、LUT、逆向补偿、动态插件加载和连续捕获属于后续里程碑。

