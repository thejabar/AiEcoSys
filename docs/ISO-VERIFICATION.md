# AiEcoSys Phase 5: ISO Verification Report

**Report version**: 1.0.0
**Audit date**: 2026-11-25
**Product**: AiEcoSys
**Report type**: Phase 5 release verification — installation media

## 1. Scope

This report verifies the Phase 5 release ISO:

- `D:\AiEcoSys\Release\ISO\AiEcoSys.iso`

The deliverable must be a **genuine, mountable ISO 9660 installation image** and
must contain the release payload with valid checksums.

## 2. Method and tooling

The ISO was authored with a real ISO 9660 writer rather than by renaming an
archive:

- Writer library: `pycdlib` (Python) — Joliet extensions, interchange level 3.
- Staging directory: `D:\UBOS\.temp\opencode\iso-stage`
- Writer invocation: `PyCdlib.new()` + `add_*` per staged file + `write()`.

The image is not a renamed ZIP; it uses ISO 9660/Joliet structures.

## 3. Artifact facts

| Property | Value |
|----------|-------|
| ISO path | `D:\AiEcoSys\Release\ISO\AiEcoSys.iso` |
| Size | 192,790,528 bytes |
| SHA-256 | `48c4c6bab5177f4b5888c7d3a8b37626d2e5c10217a0f5f0b86c5b2a32c9fa1b` |
| Format | ISO 9660 + Joliet |
| Interchange level | 3 |
| Content type | Data/installation media (not bootable OS media) |

## 4. Content verification

A staging tree was assembled and every staged file was read back out of the ISO
with `PyCdlib.get_file_from_iso` and compared by SHA-256 against the staged
source. All comparisons passed:

| ISO path | Staged source | Match |
|----------|--------------|-------|
| `/AiEcoSys-Setup.exe` | `D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe` | PASS |
| `/LICENSE` | `D:\AiEcoSys\Release\Portable\LICENSE` | PASS |
| `/README.md` | `D:\AiEcoSys\Release\Portable\README.md` | PASS |
| `/RELEASE-NOTES.md` | `D:\AiEcoSys\Release\Portable\RELEASE-NOTES.md` | PASS |
| `/SHA256SUMS.txt` | staged checksum file | PASS |
| `/THIRD-PARTY-NOTIES.md` | staged checksum file | PASS |
| `/Documentation/...` | staged docs | PASS |
| `/Portable/AiEcoSys.exe` | `D:\AiEcoSys\Release\Portable\AiEcoSys.exe` | PASS |
| `/Portable/catalogue/api.json` | `D:\AiEcoSys\Release\Portable\catalogue\api.json` | PASS |
| `/Portable/config/app-settings.example.json` | staged example | PASS |
| `/Scripts/...` | staged scripts | PASS |

Content inventory verified inside the image (38 files):

- `/AiEcoSys-Setup.exe`
- `/Portable/AiEcoSys.exe`
- `/Portable/catalogue/api.json`
- `/Portable/LICENSE`
- `/Portable/README.md`
- `/Portable/RELEASE-NOTES.md`
- `/Portable/THIRD-PARTY-NOTIES.md`
- `/Portable/config/app-settings.example.json`
- `/Documentation/...` (guides and PDFs)
- `/Scripts/...` (PowerShell maintenance scripts)
- `/SHA256SUMS.txt`

## 5. Checksum verification

The staged `SHA256SUMS.txt` was validated: all entries resolved and all
SHA-256 digests matched the staged files. No missing or mismatched entries were
found.

## 6. Results

| Check | Result |
|-------|--------|
| ISO writer produced valid ISO 9660 | PASS |
| All staged files present in image | PASS |
| All staged files byte-identical on read-back | PASS |
| SHA256SUMS entries all valid | PASS |
| Image size sane and non-empty | PASS |

**Conclusion: PASS**

## 7. Limitations and not-tested items

- The ISO is installation media, not a bootable operating-system image.
- OS-level mount/loopback mount on Windows was not performed; verification used
  library-level ISO parsing and read-back.
- The image was not mounted on a mountable device.

## 8. Related artifacts

- Installer: `D:\AiEcoSys\Release\Installer\AiEcoSys-Setup.exe`
- Portable: `D:\AiEcoSys\Release\Portable\AiEcoSys.exe`
- Release checksums: `D:\AiEcoSys\Release\SHA256SUMS.txt`
- Installer verification: `D:\AiEcoSys\docs\INSTALLER-VERIFICATION.md`
