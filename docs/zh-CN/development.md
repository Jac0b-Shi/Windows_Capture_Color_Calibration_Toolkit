# 开发说明

## 环境

- .NET SDK 10.0.x。
- Windows App SDK 由 NuGet 恢复。
- 推荐 Visual Studio Windows 应用开发组件用于运行 WinUI App。

## 验证命令

```powershell
dotnet restore WgcColorCalibrator.sln
dotnet build WgcColorCalibrator.sln --no-restore
dotnet test WgcColorCalibrator.sln --no-build
```

## 网络与代理

当前检查未发现 `HTTP_PROXY`、`HTTPS_PROXY`、`ALL_PROXY` 等环境变量。NuGet 源为 `nuget.org` 和 Visual Studio 离线包源。若模板或 restore 超时，优先检查 NuGet 访问、代理和模板后置 restore。

## 文档同步

中文文档优先承载设计决策；英文文档必须保持语义同步，不作为低质量自动翻译副本。

