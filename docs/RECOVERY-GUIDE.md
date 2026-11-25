# AiEcoSys Recovery Guide

**Version 1.0.0** — Windows 10/11 x64 (current verified release)

> **v1.0.0 note:** The current v1.0.0 build defaults to `D:\AiEcoSys` paths and requires a writable non-C: data location. This is a v1.0.0 implementation constraint. Future releases plan standard Windows locations. Commands below show v1.0.0 defaults; substitute your actual configured paths.

## 1. What can be recovered

| Data | Location | Recovery path |
|------|----------|---------------|
| Application settings | `<data>\settings\app-settings.json` | Restore from backup |
| Catalogue snapshots | `<data>\snapshots\latest.json` | Restore from backup or use bundled catalogue |
| Cached catalogue | `<data>\cache` | Delete and reload bundled catalogue |
| Provider credentials | Windows Credential Manager (`AiEcoSys:<provider>`) | Recreate in Security view; not in backups |

## 2. Backup

```powershell
.\scripts\Backup-AiEcoSys.ps1 -DataDir D:\AiEcoSys\Data -BackupDir D:\AiEcoSys\Backups
```

## 3. Restore settings and snapshots

```powershell
.\scripts\Restore-AiEcoSys.ps1 -BackupFile D:\AiEcoSys\Backups\AiEcoSys-backup-<timestamp>.zip -DataDir D:\AiEcoSys\Data -Force
```

## 4. Reinstall without losing user data

```powershell
.\scripts\Install-AiEcoSys.ps1 -InstallerPath D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe -InstallDir D:\AiEcoSys\Application -DataDir D:\AiEcoSys\Data
```

## 5. Missing/corrupt catalogue

```powershell
.\scripts\Remove-AiEcoSysCache.ps1 -DataDir D:\AiEcoSys\Data -Force
.\scripts\Verify-AiEcoSys.ps1 -InstallDir D:\AiEcoSys\Application
```

## 6. Recover missing credentials

Re-enter in **Security** view; stored in Windows Credential Manager.

## 7. Diagnostics

```powershell
.\scripts\Get-AiEcoSysDiagnostics.ps1 -InstallDir D:\AiEcoSys\Application
```

Diagnostics redact key/token/secret/password-like values.

## 8. Scope

Recovery covers AiEcoSys-owned data only. It does not modify AcademyOS, UBOS, or MavenMeeM.
