# AiEcoSys Uninstallation Guide

**Version 1.0.0** — Windows 10/11 x64 (current verified release)

> **v1.0.0 note:** Default paths shown are v1.0.0 defaults (`D:\AiEcoSys\...`). Substitute your actual install/data paths.

## 1. Uninstall through Windows

Open **Settings -> Apps -> Installed apps -> AiEcoSys -> Uninstall**, or use the Start Menu uninstall shortcut.

## 2. What is removed

- Program installation files and uninstaller entry.

## 3. What is preserved

- User data (settings, snapshots) is preserved by default unless you explicitly confirm deletion.

## 4. Delete user data explicitly

```powershell
.\scripts\Uninstall-AiEcoSys.ps1 -InstallDir D:\AiEcoSys\Application -DataDir D:\AiEcoSys\Data -DeleteUserData
```

This is destructive and requires confirmation unless `-Force` is supplied.

## 5. Post-uninstall

The application should not start after a clean uninstall. Reinstall with the installer if needed.
