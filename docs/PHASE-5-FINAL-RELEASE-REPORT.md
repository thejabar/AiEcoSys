# AiEcoSys Phase 5: Final Release Report

**Report version**: 1.0.0
**Release date**: 2026-11-25
**Product**: AiEcoSys
**Phase**: 5 — Release Engineering

## 1. Executive summary

Phase 5 release engineering for AiEcoSys is complete. The Phase 5 deliverable is
a self-contained, portable, installer-based Windows x64 release with documentation,
recovery tooling, installation media, and final verification reports.

This report supersedes earlier Phase reports where their figures are stale.

## 2. Completed work items

| # | Work item | Status |
|---|-----------|--------|
| 1 | Solution audit and baseline build/test | PASS |
| 2 | Path portability correction (`AppPaths`) | PASS |
| 3 | Bundled Models.dev catalogue | PASS |
| 4 | Application icon and metadata | PASS |
| 5 | Self-contained publish | PASS |
| 6 | Portable distribution | PASS |
| 7 | Inno Setup installer | PASS |
| 8 | Installer validation | PASS |
| 9 | PowerShell maintenance scripts | PASS |
| 10 | ISO media | PASS |
| 11 | Documentation and PDFs | PASS |
| 12 | Git preparation (local) | PASS |
| 13 | Final release validation | PASS |

## 3. Final build and test results

### 3.1 Release build

Command: `dotnet build D:\AiEcoSys\AiEcoSys.slnx -c Release --nologo`

Result: **Build succeeded. 0 Warning(s), 0 Error(s).**

### 3.2 Automated tests

Command: `dotnet test D:\AiEcoSys\AiEcoSys.slnx -c Release --nologo`

| Suite | Passed | Failed | Skipped |
|-------|--------|--------|---------|
| AiEcoSys.Core.Tests | 37 | 0 | 0 |
| AiEcoSys.Desktop.Tests | 9 | 0 | 0 |
| **Total** | **46** | **0** | **0** |

### 3.3 Vulnerability scan

`dotnet list package --vulnerable --include-transitive` reported no vulnerable
packages.

## 4. Final release artifacts

| Artifact | Path | Size | SHA-256 |
|----------|------|------|---------|
| Portable executable | `D:\AiEcoSys\Release\Portable\AiEcoSys.exe` | 140,097,058 bytes | `c99f9c0ee8321d2d4cf171675cba8cae3e7e4ae5fc60e62a6e7f5669127dbbd3` |
| Bundled catalogue | `D:\AiEcoSys\Release\Portable\catalogue\api.json` | 6,972,655 bytes | `9c00f5058890ab8eda19b376bc9083945a55abf0010f69a15051abf91584e3c6` |
| Windows installer | `D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe` | 45,198,369 bytes | `0a4b07bd236a4c4216de3fab9651fba57f2a92e4ae6283ba6b7ec2f463756253` |
| ISO image | `D:\AiEcoSys\Release\ISO\AiEcoSys.iso` | 192,790,528 bytes | `48c4c6bab5177f4b5888c7d3a8b37626d2e5c10217a0f5f0b86c5b2a32c9fa1b` |

### 4.1 Portable executable

- Self-contained: true
- Runtime: .NET 10 (Windows x64)
- Single file: true
- Includes native libraries for self-extract: true
- Includes bundled catalogue: true
- Includes licence, README, release notes, third-party notices, example config.

### 4.2 Installer

- Engine: Inno Setup 6.7.3
- Default install dir: `D:\AiEcoSys\Application`
- Default data dir: `D:\AiEcoSys\Data`
- Custom install locations supported via `/DIR` and `/DATA`.
- Start Menu shortcut and optional desktop shortcut supported.
- Upgrade support via standard Inno Setup behaviour.
- Uninstall preserves user data by default; explicit `-DeleteUserData` supported.

### 4.3 ISO

- Format: ISO 9660 + Joliet (interchange level 3)
- Content: installer, portable payload, docs, scripts, checksums
- Verified byte-identical read-back of all staged files.

## 5. Verification totals

| Area | Total | Passed | Failed | Not tested |
|------|-------|--------|--------|------------|
| Automated tests (final) | 46 | 46 | 0 | 0 |
| Installer functional checks | 7 | 7 | 0 | 0 |
| ISO content checks | 38 | 38 | 0 | 0 |
| SHA256SUMS entries validated | 37 | 37 | 0 | 0 |
| Security checks | 10 | 10 | 0 | 0 |
| Build checks | 1 | 1 | 0 | 0 |

## 6. Requirements mapping

| Requirement | Status |
|-------------|--------|
| Self-contained Windows x64 .NET 10 WPF app with icon/version/publisher | PASS |
| Portable distribution | PASS |
| Genuine Inno Setup installer with default paths and options | PASS |
| Installer validation (fresh/upgrade/uninstall/custom) | PASS |
| Nine production PowerShell scripts | PASS |
| Mountable ISO, not renamed ZIP | PASS |
| Documentation and three PDFs | PASS |
| Git preparation, no secrets | PASS |
| Final reports mark PASS/FAIL/NOT TESTED with exact data | PASS |
| Public GitHub publication withheld pending approval | PASS |

## 7. Known limitations

- Installer not code-signed (SmartScreen warning possible).
- ISO is installation media, not bootable OS image.
- Online catalogue refresh and provider `HEAD` checks require network.

## 8. Conclusion

All Phase 5 release engineering work items are complete and verified. The final
release build, installer, portable distribution, ISO, scripts, documentation, and
security checks pass.

**Phase 5 final status: PASS**

## 9. Next steps

- Present this final release audit for user approval.
- On approval, push the prepared local repository to the public GitHub remote.
