# AiEcoSys Installation Guide

**Version 1.0.0** — Windows 10/11 x64 (current verified release)

## 1. Scope and current v1.0.0 behavior

This guide documents the **current verified v1.0.0 Windows release**. 

> **v1.0.0 limitation (actual build behavior):** The current v1.0.0 application and installer enforce a writable **non-C:** data location and default to `D:\AiEcoSys` paths. This is a known implementation constraint in v1.0.0, not a universal product requirement. Future releases plan standard Windows conventions (e.g., `%ProgramFiles%\The Jabar\AiEcoSys` for program files and user app-data directories under `%LOCALAPPDATA%`/user-selected locations). Do not modify v1.0.0 release artifacts; implementation changes belong in a future versioned release.

## 2. What you receive

| Item | Purpose |
|------|---------|
| `AiEcoSys-Setup.exe` | Windows installer (v1.0.0 verified) |
| `Portable\AiEcoSys.exe` | Self-contained portable application |
| `Portable\catalogue\api.json` | Offline Models.dev catalogue |
| `Documentation\` | Guides and reports |
| `scripts\` | Install, verify, backup, restore, and recovery scripts |
| `SHA256SUMS.txt` | SHA-256 checksums |

The application is self-contained; no separate .NET runtime is required.

## 3. System requirements (v1.0.0)

- Windows 10 or Windows 11, x64.
- A writable **non-C:** location for application/data in the current v1.0.0 build.
- At least 250 MB free for the installed application and catalogue.
- No network connection required for installation or offline use.

## 4. Verify the download

```powershell
Get-FileHash .\AiEcoSys-Setup.exe -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

Do not run an installer whose hash does not match the published checksum.

## 5. Install with the wizard (v1.0.0)

1. Run `AiEcoSys-Setup.exe`.
2. Accept the MIT licence.
3. Choose the program directory. **v1.0.0 default:** `D:\AiEcoSys\Application` (you may select another writable non-C: directory).
4. Optionally create a desktop shortcut.
5. Complete the wizard.

**v1.0.0 default data directory:** `D:\AiEcoSys\Data` (or `<install>\Data` for custom installs in the current installer behavior). The installer writes `aiecoSys.paths.json` beside `AiEcoSys.exe`.

## 6. Silent install (v1.0.0)

```powershell
.\AiEcoSys-Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /DIR="D:\AiEcoSys\Application" /DATA="D:\AiEcoSys\Data"
```

Add `/TASKS=desktopicon` for a desktop shortcut.

Wrapper script (developer/maintenance use; paths shown are examples/defaults for v1.0.0):
```powershell
.\scripts\Install-AiEcoSys.ps1 -InstallerPath .\AiEcoSys-Setup.exe -InstallDir D:\AiEcoSys\Application -DataDir D:\AiEcoSys\Data
```

## 7. Portable installation (v1.0.0)

1. Copy the `Portable` folder to a writable **non-C:** location.
2. Run `AiEcoSys.exe`.
3. The app creates `Data\settings` and `Data\snapshots` beside the executable and loads `catalogue\api.json`.

Do not copy only the executable; the `catalogue` folder is required.

## 8. First run

Launch AiEcoSys. Dashboard should report the bundled catalogue (328 providers, 10,283 models at v1.0.0 release time) and source `Local Models.dev api.json (offline snapshot)`.

## 9. Offline installation

The installer and portable build contain the catalogue and runtime files. No download is required. Online refresh is optional.

## 10. Upgrade / uninstall / recovery

- Upgrade: see [UPGRADE-GUIDE.md](UPGRADE-GUIDE.md).
- Uninstall: see [UNINSTALLATION-GUIDE.md](UNINSTALLATION-GUIDE.md). User data preserved by default unless explicitly removed.
- Recovery: see [RECOVERY-GUIDE.md](RECOVERY-GUIDE.md).

## 11. Future platform installation (planned, not available in v1.0.0)

The following are **planned** and must not be treated as available v1.0.0 packages:
- Linux installation (planned)
- macOS installation (planned)
- Android installation (planned)
- iOS/iPadOS installation (planned)
- PowerShell module installation (planned)
- Bash/Zsh/standalone CLI installation (planned)
- Web app / PWA deployment (planned)
- REST API deployment (planned)
- Docker/server deployment (planned)

## 12. Troubleshooting

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md). For v1.0.0, "data path must not be on C:" is an actual current-build constraint.
