# AiEcoSys Recovery Guide

**Version 1.0.0** · Windows 10/11 x64

This guide describes how to recover an AiEcoSys installation after data loss,
corruption, failed updates, or missing catalogue files.

## 1. What can be recovered

AiEcoSys stores three kinds of user data:

| Data | Location | Recovery path |
|------|----------|---------------|
| Application settings | `<data>\settings\app-settings.json` | Restore from a backup |
| Catalogue snapshots | `<data>\snapshots\latest.json` | Restore from a backup or refresh from the bundled catalogue |
| Cached catalogue | `<data>\cache` | Delete and reload the bundled catalogue |
| Provider credentials | Windows Credential Manager (`AiEcoSys:<provider>`) | Recreate via the Security view; stored outside AiEcoSys files |

Credentials are never stored in AiEcoSys files and are not included in backups.
If a credential is lost, re-enter it through the Security view.

## 2. Back up before recovery

Create a backup with the supplied script:

```powershell
.\scripts\Backup-AiEcoSys.ps1 `
  -DataDir D:\AiEcoSys\Data `
  -BackupDir D:\AiEcoSys\Backups
```

The backup is a ZIP containing `settings` and `snapshots` plus a
`backup-manifest.json`. The SHA-256 of the archive is written next to it.

## 3. Restore settings and snapshots

```powershell
.\scripts\Restore-AiEcoSys.ps1 `
  -BackupFile D:\AiEcoSys\Backups\AiEcoSys-backup-<timestamp>.zip `
  -DataDir D:\AiEcoSys\Data `
  -Force
```

The restore script refuses unsafe archive paths and writes a safety archive of
the current data before overwriting.

After a restore, start AiEcoSys and confirm the Dashboard reports the expected
provider and model counts. The snapshot is validated on load; a corrupt snapshot
falls back to the bundled catalogue.

## 4. Reinstall without losing user data

If the application files are missing or corrupted:

```powershell
.\scripts\Install-AiEcoSys.ps1 `
  -InstallerPath D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe `
  -InstallDir D:\AiEcoSys\Application `
  -DataDir D:\AiEcoSys\Data
```

This reinstalls program files only. Settings and snapshots under the data
directory are not touched.

## 5. Recover a missing or corrupt bundled catalogue

1. Delete the cache:

```powershell
.\scripts\Remove-AiEcoSysCache.ps1 -DataDir D:\AiEcoSys\Data -Force
```

2. Verify the installation:

```powershell
.\scripts\Verify-AiEcoSys.ps1 -InstallDir D:\AiEcoSys\Application
```

3. Start AiEcoSys. The application loads `catalogue\api.json` beside the
   executable and falls back to the legacy development catalogue when the
   bundled file is absent.

## 6. Recover missing credentials

1. Open **Security**.
2. Re-enter the provider identifier and secret.
3. Select **Store credential**.

The secret is written to Windows Credential Manager under the
`AiEcoSys:<provider>` name.

## 7. Collect diagnostics for support

```powershell
.\scripts\Get-AiEcoSysDiagnostics.ps1 `
  -InstallDir D:\AiEcoSys\Application
```

The script writes a diagnostics file under `D:\AiEcoSys\Logs` and redacts
settings values that look like keys, tokens, secrets, or passwords.

## 8. Recovery checklist

- [ ] Backup created before any destructive step.
- [ ] Snapshot restored and validated.
- [ ] Settings restored and JSON validity confirmed.
- [ ] Application reinstalled and verified.
- [ ] Catalogue reloaded from the bundled file.
- [ ] Credentials recreated.
- [ ] Diagnostics captured for support.

## 9. Scope of recovery

Recovery covers AiEcoSys-owned data only. It does not modify AcademyOS, UBOS, or
MavenMeeM installations, and it does not attempt to repair other applications.
