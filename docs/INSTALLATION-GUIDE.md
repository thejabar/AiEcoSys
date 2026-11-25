# AiEcoSys Installation Guide

**Version 1.0.0** · Windows 10/11 x64

## 1. What you receive

The release contains:

| Item | Purpose |
|------|---------|
| `AiEcoSys-Setup.exe` | Windows installer |
| `Portable\AiEcoSys.exe` | Self-contained portable application |
| `Portable\catalogue\api.json` | Offline Models.dev catalogue |
| `Documentation\` | Guides and reports |
| `scripts\` | Install, verify, backup, restore, and recovery scripts |
| `SHA256SUMS.txt` | SHA-256 checksums |

The application is self-contained. You do not need to install .NET, Node.js,
or any other runtime.

## 2. System requirements

- Windows 10 or Windows 11, x64.
- A writable non-C: location for the application and data. The supported
  default is the `D:` drive.
- At least 250 MB free for the installed application and catalogue.
- No network connection is required for installation or offline use.

The installer and application are per-user. Administrator elevation is not
required when the chosen directories are writable by the current user.

## 3. Verify the download

From PowerShell, in the folder containing the release files:

```powershell
Get-FileHash .\AiEcoSys-Setup.exe -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

The computed hash must match the entry for `AiEcoSys-Setup.exe`. Do not run an
installer whose hash does not match.

## 4. Install with the wizard

1. Run `AiEcoSys-Setup.exe`.
2. Accept the MIT licence.
3. Choose the program directory. The default is:

   `D:\AiEcoSys\Application`

4. Choose whether to create a desktop shortcut. A Start Menu shortcut is
   always created.
5. Complete the wizard and leave **Launch AiEcoSys** selected if you want to
   start the application immediately.

The installer creates these data directories:

- `D:\AiEcoSys\Data\settings`
- `D:\AiEcoSys\Data\snapshots`

It writes `aiecoSys.paths.json` beside `AiEcoSys.exe` so the application knows
where its data lives. The data directory is separate from the program files.

### Custom install location

You may select another absolute, non-C: directory. If you do, the installer
derives the data directory as `<install directory>\Data`. For example:

- Program: `E:\Apps\AiEcoSys`
- Data: `E:\Apps\AiEcoSys\Data`

You can override the data directory during a scripted install with
`/DATA=<absolute non-C: path>`.

## 5. Silent install

```powershell
.\AiEcoSys-Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- `
  /DIR="D:\AiEcoSys\Application" /DATA="D:\AiEcoSys\Data"
```

Add `/TASKS=desktopicon` to create the desktop shortcut.

The supplied wrapper performs validation and logging:

```powershell
.\scripts\Install-AiEcoSys.ps1 `
  -InstallerPath .\AiEcoSys-Setup.exe `
  -InstallDir D:\AiEcoSys\Application `
  -DataDir D:\AiEcoSys\Data
```

## 6. First run

Launch **AiEcoSys** from the Start Menu. The Dashboard should report:

- 328 providers
- 10,283 models
- Data source: `Local Models.dev api.json (offline snapshot)`

If the counts are zero, the bundled catalogue was not installed correctly.
Run:

```powershell
.\scripts\Verify-AiEcoSys.ps1 -InstallDir D:\AiEcoSys\Application
```

## 7. Portable installation

The portable build needs no installer:

1. Copy the `Portable` folder to a writable non-C: directory.
2. Run `AiEcoSys.exe`.
3. The application creates `Data\settings` and `Data\snapshots` beside the
   executable and loads `catalogue\api.json`.

Do not copy only the executable; the `catalogue` folder is required for
offline data.

## 8. Upgrade

Run the newer installer over the existing installation. The default program
directory is reused and user data is preserved. See
[UPGRADE-GUIDE.md](UPGRADE-GUIDE.md).

## 9. Uninstall

Use **Settings → Apps → AiEcoSys → Uninstall**, or the Start Menu uninstall
shortcut. User data is preserved unless you explicitly confirm deletion. See
[UNINSTALLATION-GUIDE.md](UNINSTALLATION-GUIDE.md).

## 10. Offline installation

The installer and portable build contain the catalogue and all runtime files.
No download is required. Online catalogue refresh is optional and can be
disabled in Settings.

## 11. Troubleshooting

- **SmartScreen warning**: the installer is not code-signed. Verify the
  SHA-256 before choosing **More info → Run anyway**.
- **Cannot write to the chosen directory**: choose a writable non-C: folder.
- **Blank or empty catalogue**: verify `catalogue\api.json` exists beside the
  executable.
- **Data path error**: confirm the path is absolute and not on C:.

More help: [TROUBLESHOOTING.md](TROUBLESHOOTING.md).
