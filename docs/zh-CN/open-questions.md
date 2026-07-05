# 开放技术问题

本文件记录尚未验证的 Windows、WGC、HDR、WinUI、D3D11 和 DXGI 行为。未经验证前，不得把这些内容写成事实。

| ID | 问题 | 状态 | 依据或下一步 |
| --- | --- | --- | --- |
| WGC-HDR-001 | WGC 在 HDR 桌面中使用 BGRA8 时实际采用什么转换？ | 未验证 | 后续以微软官方文档、官方示例和本项目实验记录为依据。 |
| WGC-HDR-002 | FP16 WGC Surface 的编码和颜色空间如何可靠判断？ | 未验证 | 第一轮不实现 FP16。 |
| WGC-HDR-003 | XAML 纯色填充是否能作为足够稳定的理论输入？ | 未验证 | Milestone 3 渲染垂直切片时验证。 |
| WGC-HDR-004 | Windows HDR SDR content brightness 是否影响窗口色卡及捕获结果？ | 未验证 | 需要手工实验。 |
| WGC-HDR-005 | Display Picker 或外部取色器读到的值属于哪个空间？ | 未验证 | 后续记录工具和系统设置。 |
| WGC-HDR-006 | 捕获窗口和捕获显示器是否产生不同结果？ | 未验证 | 后续 WGC 单帧实验。 |
| WGC-HDR-007 | 多显示器、ICC、Advanced Color 配置是否改变映射？ | 未验证 | 后续诊断页采集元数据。 |
| WGC-HDR-008 | FP16 是否比 BGRA8 保留更多可用于反推的信息？ | 未验证 | Milestone 6 技术 spike。 |
| WGC-HDR-009 | 最早正式支持本项目所需 HDR WGC 路径的 Windows 稳定版本是什么？ | 未验证 | 不以当前 Insider/开发机版本作为最低版本结论。 |
| WINUI-001 | WinUI 3 打包和非打包模式哪种更适合首发？ | 未验证 | 第一轮采用可构建 Shell，发布形态后续决策。 |
| WINUI-002 | 是否需要 Direct3D renderer 才能控制色卡输出编码？ | 未验证 | XAML renderer 验证后决策。 |

