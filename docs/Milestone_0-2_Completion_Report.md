# Milestone 0–2 最终交付报告

> 生成日期：2026-07-06
> 项目：WGC Color Calibrator / WGC 色彩校准器

---

## 1. 里程碑范围

| 里程碑 | 范围 | 状态 |
|---|---|---|
| M0 | 仓库初始化、项目结构、CI/CD、文档框架 | 完成 |
| M1 | 核心类型系统、色彩模型、布局引擎、序列化 | 完成 |
| M2 | WinUI 3 外壳、导航、4 页面骨架、本地化 | 完成 |

**M3（Capture、D3D11、FP16、色卡渲染、采样）未开始。**

---

## 2. 验证

```
dotnet restore --source E:\packages\NugetOffline  → 成功
dotnet build --no-restore -p:Platform=x64          → 0 错误 2 警告
dotnet test --no-build                             → 30/30 通过
```

---

## 3. 代码结构

```
src/
├── WgcColorCalibrator.Core/          色卡定义、色彩类型、布局引擎、测量模型、序列化
├── WgcColorCalibrator.App/           WinUI 3 外壳
│   ├── Pages/        Home / Settings / Diagnostics / About
│   ├── Services/     LanguageService / DiagnosticsSnapshotService
│   └── Strings/      en-US / zh-CN Resources.resw
tests/
└── WgcColorCalibrator.Core.Tests/    30 个单元测试
```

---

## 4. 本地化覆盖

| 键 | en-US | zh-CN |
|---|---|---|
| AppDisplayName | WGC Color Calibrator | WGC 色彩校准器 |
| AppDescription | Windows capture color measurement and calibration toolkit. | Windows 捕获色彩测量与校准工具。 |
| PublisherDisplayName | Jac0b Shi | Jac0b Shi |
| MainWindowTitle | WGC Color Calibrator | WGC 色彩校准器 |
| HomeTitle | WGC Color Calibrator | WGC 色彩校准器 |
| Navigation.PaneTitle | WGC Color Calibrator | WGC 色彩校准器 |
| + 其余 27 个键 | ... | ... |

---

## 5. 修复记录

| 问题 | 修复 |
|---|---|
| DEP0700 部署错误（.resw 键名含点号） | App.DisplayName → AppDisplayName，删除所有带点号键名 |
| XAML 崩溃（Window.Title 不能在 XAML 赋值） | 删除 x:Uid="MainWindow"，代码中设 Title |
| 侧边栏标题残留 "WGC HDR" | 改为本地化项目名 |
| app.manifest assemblyIdentity 残留旧名 | WgcHdrColorCalibrator → WgcColorCalibrator |
| README/docs 残留旧名 | 已全部替换为 WgcColorCalibrator |
| launchSettings.json 丢失 | 已重建 |

---

## 6. 名称清理结论

旧名仅在 `docs/WGC_HDR_Color_Calibrator_Requirements_zh-CN.md`（需求原始文档）中保留，其余全部替换为 `WgcColorCalibrator` / `WGC Color Calibrator` / `WGC 色彩校准器`。
