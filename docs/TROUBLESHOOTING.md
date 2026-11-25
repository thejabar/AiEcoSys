# AiEcoSys Troubleshooting Guide

**Version 1.0.0** · Windows 10/11 x64

## 1. Build and run problems

| Symptom | Cause | Resolution |
|---------|-------|------------|
| `MSB3026` file lock during build | A prior process still holds the output file | Close AiEcoSys processes and rebuild |
| `CS2012` cannot open output for writing | Stale file lock | Kill processes, then `dotnet build` again |

## 2. Launch problems

| Symptom | Cause | Resolution |
|---------|-------|------------|
| Window does not appear | Self-contained runtime mismatch | Use the Release portable build or installer |
| Application exits immediately | Missing bundled catalogue | Verify `catalogue\api.json` beside the executable |

## 3. Catalogue problems

| Symptom | Cause | Resolution |
|---------|-------|------------|
| Zero providers or models | Missing catalogue | Copy `catalogue\api.json`; run the installer or portable copy correctly |
| Wrong model counts | Legacy development catalogue used | Ensure the bundled catalogue is used |

## 4. Data path problems

| Symptom | Cause | Resolution |
|---------|-------|------------|
| Data written to C: | Invalid path configuration | Set an absolute non-C: data directory |
| Snapshot path outside data root | Invalid snapshot setting | Set snapshot path inside the data root |

## 5. Installer problems

| Symptom | Cause | Resolution |
|---------|-------|------------|
| SmartScreen warning | Unsigned installer | Verify SHA-256, then choose **More info -> Run anyway** |
| Installer cannot write to target | Target not writable | Choose a writable directory |
| Launch step fails | Target busy | Close existing instance first |

## 6. Recovery

See [RECOVERY-GUIDE.md](RECOVERY-GUIDE.md) for snapshot, settings, and catalogue
recovery steps.

## 7. Security issues

See [SECURITY-GUIDE.md](SECURITY-GUIDE.md) for reporting and data-protection
details.

## 8. Getting help

- Run diagnostics: `.\scripts\Get-AiEcoSysDiagnostics.ps1`
- Re-verify installation: `.\scripts\Verify-AiEcoSys.ps1`
- Read release notes: `RELEASE-NOTES.md`
