# Development

## Environment

- .NET SDK 10.0.x.
- Windows App SDK restored from NuGet.
- Visual Studio Windows app development components are recommended for running the WinUI app.

## Verification Commands

```powershell
dotnet restore WgcColorCalibrator.sln
dotnet build WgcColorCalibrator.sln --no-restore
dotnet test WgcColorCalibrator.sln --no-build
```

## Network and Proxy

The current environment check found no `HTTP_PROXY`, `HTTPS_PROXY`, or `ALL_PROXY` environment variables. NuGet sources are `nuget.org` and the Visual Studio offline package source. If templates or restore operations time out, check NuGet access, proxy configuration, and template post-create restore.

## Documentation Sync

Chinese documents carry design decisions first. English documents must remain semantically synchronized and must not be treated as low-quality automatic translations.

