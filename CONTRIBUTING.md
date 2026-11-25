# Contributing to AiEcoSys

Thank you for your interest in contributing to AiEcoSys.

## Development requirements

- Windows 10/11 x64
- .NET 10 SDK
- Git

## Build and test

```powershell
dotnet build .\AiEcoSys.slnx -c Release
dotnet test .\AiEcoSys.slnx -c Release
```

## Guidelines

- Keep changes focused and consistent with the existing structure and style.
- Do not commit secrets, credentials, user data, snapshots, build outputs, or release binaries.
- Do not modify verified v1.0.0 release artifacts; future fixes should be versioned releases.
- Add or update tests for behavioral changes.
- Review security implications, especially around credential handling, path validation, and external endpoints.
- Update documentation when user-facing behavior changes.

## Pull requests

- Describe the problem, the change, and how it was tested.
- Include exact test commands and results.
- Note any security or compatibility impact.

## Reporting security issues

Do not open public issues for vulnerabilities. See [SECURITY.md](SECURITY.md).
