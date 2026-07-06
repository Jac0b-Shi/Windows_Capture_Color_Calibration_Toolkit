## Summary

## Verification

- [ ] `dotnet restore WgcColorCalibrator.sln`
- [ ] `dotnet build WgcColorCalibrator.sln --configuration Release --no-restore -p:Platform=x64`
- [ ] `dotnet test WgcColorCalibrator.sln --configuration Release --no-build`

## WGC/HDR impact

- [ ] No WGC/HDR behavior claims changed.
- [ ] Unverified behavior is recorded in `docs/zh-CN/open-questions.md` and `docs/en-US/open-questions.md`.

