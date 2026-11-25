# AiEcoSys Uninstallation Guide

**Version 1.0.0** · Windows 10/11 x64

## 1. Uninstall through Windows

Open **Settings -> Apps -> Installed apps -> AiEcoSys -> Uninstall**, or use the
Start Menu uninstall shortcut.

## 2. What is removed

- Program installation files, including the uninstaller entry.

## 3. What is preserved

- User data (settings, snapshots) is preserved by default. The uninstaller asks
  before deleting user data.

## 4. Delete user data explicitly

```powershell
.\scripts\Uninstall-AiEcoSys.ps1 `
  -InstallDir D:\AiEcoSys\Application `
  -DataDir D:\AiEcoSys\Data `
  -DeleteUserData
```

This is destructive and requires confirmation unless `-Force` is supplied.

## 5. Post-uninstall state

The application should not start after a clean uninstall. Reinstall with the
installer if the application is needed again.
