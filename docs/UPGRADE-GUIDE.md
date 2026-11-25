# AiEcoSys Upgrade Guide

**Version 1.0.0** — Windows 10/11 x64 (current verified release)

> **v1.0.0 note:** Default paths in this guide reflect v1.0.0 defaults (`D:\AiEcoSys\...`) and the v1.0.0 non-C: data-location constraint. Substitute your actual configured paths. Future releases plan standard Windows path conventions.

## 1. Upgrade from a previous installer

Run the newer installer over the existing installation:

```powershell
.\scripts\Install-AiEcoSys.ps1 `
  -InstallerPath D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe `
  -InstallDir D:\AiEcoSys\Application `
  -DataDir D:\AiEcoSys\Data
```

The installer reuses the previous program directory when present and preserves
user data.

## 2. What changes between versions

- New views or behaviour changes are delivered through installer updates.
- Catalogue snapshots may need a refresh after a major catalogue change; the
  bundled catalogue is refreshed with each release.

## 3. Post-upgrade verification

```powershell
.\scripts\Verify-AiEcoSys.ps1 -InstallDir D:\AiEcoSys\Application
```

## 4. Rollback

No automatic rollback is provided. Restore a settings/snapshot backup with
`.\scripts\Restore-AiEcoSys.ps1` if the upgrade changed behaviour incorrectly.
Program files can be replaced by reinstalling the previous installer.
