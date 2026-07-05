# Contributing

[简体中文](CONTRIBUTING.zh-CN.md)

This project prioritizes small, verified changes.

## Rules

- Keep code identifiers, namespaces, API names, JSON keys, resource keys, and CLI parameters in English.
- Keep developer design documents in Chinese first, with a synchronized high-quality English version.
- Do not introduce fake capture results, silent pixel-format fallback, or undocumented hard-coded color compensation.
- Use Microsoft official documentation or official samples as the basis for WinUI 3, Windows Graphics Capture, D3D11, DXGI, HDR pixel formats, and minimum Windows version claims.
- Record unverified WGC/HDR behavior in `docs/en-US/open-questions.md`.
- Run `dotnet restore`, `dotnet build`, and `dotnet test` before submitting changes.

## Scope Discipline

The initialization stage stops at repository foundation, core domain, and application shell. FP16, LUT, inverse compensation, dynamic plugin loading, and continuous capture are later milestones.

