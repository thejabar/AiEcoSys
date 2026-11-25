# AiEcoSys Developer Guide

## Scope

This guide is for contributors and maintainers working on AiEcoSys source. It is **developer documentation**, not an end-user installation requirement.

## Current maintainer working path (developer-specific example)

The current maintainer development environment uses:

- `D:\AiEcoSys` — local project/repository working directory

This path is a **developer-specific example** and convenience for the current maintainer. It must not be imposed on end users. End-user installation paths must follow product conventions and the actual behavior of the release being documented.

## v1.0.0 implementation reality (important)

The verified v1.0.0 release currently enforces a non-C: data location and defaults to `D:\AiEcoSys` paths in code/installer defaults. This is a known v1.0.0 implementation constraint. Future releases should move to standard Windows conventions:

- Program files: `%ProgramFiles%\The Jabar\AiEcoSys` (or user-selected)
- User data: `%LOCALAPPDATA%\The Jabar\AiEcoSys` (or user-selected)

Do **not** modify verified v1.0.0 release artifacts. Any implementation change belongs in a future versioned release.

## Build/test

```powershell
dotnet build .\AiEcoSys.slnx -c Release
dotnet test .\AiEcoSys.slnx -c Release
```

## Release safety

- Preserve verified v1.0.0 hashes/artifacts.
- Never force-push or rewrite published history.
- Do not commit secrets, user data, snapshots, or build outputs.
- Verify SHA-256 before publishing release assets.
