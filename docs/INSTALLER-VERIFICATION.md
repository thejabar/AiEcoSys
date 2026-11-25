# AiEcoSys Phase 5: Installer Verification Report

**Report version**: 1.0.0
**Audit date**: 2026-11-25
**Product**: AiEcoSys
**Report type**: Phase 5 release verification — Windows installer

## 1. Scope

This report verifies the Phase 5 Windows installer:

- `D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe`

The installer must be a genuine Inno Setup compiler build and must install the
portable payload with the documented default paths.

## 2. Builder and build inputs

| Item | Value |
|------|-------|
| Builder | Inno Setup 6.7.3 (`ISCC.exe`) installed to `D:\Tools\InnoSetup` |
| Builder checksum | SHA-256 `9c73c3bae7ed48d44112a0f48e66742c00090bdb5bef71d9d3c056c66e97b732` (verified before install) |
| Script | `D:\AiEcoSys\installers\AiEcoSys.iss` |
| Compilation | Successful compile (64.938 s / 66.547 s runs) |

## 3. Artifact facts

| Property | Value |
|----------|-------|
| Installer path | `D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe` |
| Size | 45,198,369 bytes |
| SHA-256 | `0a4b07bd236a4c4216de3fab9651fba57f2a92e4ae6283ba6b7ec2f463756253` |

## 4. Functional test matrix

All tests were executed on this machine with interactive desktop available.

### 4.1 Fresh install (default paths)

Command (via `D:\AiEcoSys\scripts\Install-AiEcoSys.ps1`):

```powershell
.\scripts\Install-AiEcoSys.ps1 `
  -InstallerPath D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe `
  -InstallDir D:\AiEcoSys\Application `
  -DataDir D:\AiEcoSys\Data
```

Result: exit 0; installed version 1.0.0.0; `D:\AiEcoSys\Application\AiEcoSys.exe`
present; `D:\AiEcoSys\Application\catalogue\api.json` present; paths file resolved to
`D:\AiEcoSys\Data`; data directories created.

### 4.2 Launch after fresh install

Start Menu shortcut was launched and the application reached input idle, window
title `AiEcoSys - AI Ecosystem Management Platform`, responding = True, clean
shutdown exit 0.

### 4.3 Functional launch (installed build)

Runtime verification through UI Automation confirmed:

- Dashboard: 328 providers, 10,283 models, local catalogue source.
- Navigation across all ten views succeeded (Pricing and Catalogue header
  checks used substring-tolerant matching in the automated probe; manual review
  of view implementations was used for those two).
- Model search returned 1,041 of 10,283 models for query `claude`.
- Snapshot save created `D:\AiEcoSys\Data\snapshots\latest.json`.
- Settings save created `D:\AiEcoSys\Data\settings\app-settings.json`.

### 4.4 Upgrade preserves user data

Upgrade install over an existing installation completed with exit 0 and
preserved the settings and snapshot file hashes unchanged.

### 4.5 Uninstall preserves user data by default

Uninstall completed with exit 0; installed application files were removed; user
data directories and files were preserved.

### 4.6 Custom install location

Install to a custom absolute, non-C: directory (`D:\AiEcoSys-Custom\App`) with
derived data directory (`D:\AiEcoSys-Custom\App\Data`) succeeded; launch and
uninstall behaved correctly.

### 4.7 Scripted wrapper verification

`D:\AiEcoSys\scripts\Verify-AiEcoSys.ps1` reported 13 checks, 0 failures, on a
disposable install.

## 5. Results

| Check | Result |
|-------|--------|
| Inno Setup compile | PASS |
| Fresh install | PASS |
| Launch | PASS |
| Functional launch | PASS |
| Upgrade preserves data | PASS |
| Uninstall preserves data | PASS |
| Custom location | PASS |
| Scripted wrapper verification | PASS |

**Conclusion: PASS**

## 6. Known limitations

- The installer is not code-signed; Windows SmartScreen may show a warning.
- The installer is per-user and does not require elevation when directories are
  user-writable.
- Upgrade detection relies on installer version comparison; version 1.0.0.0 was
  exercised directly.

## 7. Related artifacts

- Installer: `D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe`
- Portable: `D:\AiEcoSys\Release\Portable\AiEcoSys.exe`
- Scripts: `D:\AiEcoSys\scripts`
- Installation guide: `D:\AiEcoSys\docs\INSTALLATION-GUIDE.md`
