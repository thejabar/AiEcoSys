# AiEcoSys 1.0.0 — Release Notes

**Release date**: 2026-11-25
**Platform**: Windows 10/11 x64
**Deployment**: self-contained; no external .NET runtime required

## Highlights

- Ten functional views: Dashboard, Model Explorer, Provider Manager, Pricing,
  Model Selection, Catalogue, Integrations, Security, Settings, and About.
- Offline Models.dev catalogue with 328 providers and 10,283 models.
- Real token pricing in USD per 1M tokens where the source provides it.
- Windows Credential Manager integration for provider credentials.
- Portable and installed layouts with separate, non-C: data directories.
- Catalogue snapshot save and recovery.
- Optional opt-in AcademyOS, UBOS, and MavenMeeM integration endpoints.

## Distribution

- `AiEcoSys-Setup.exe` — Windows installer.
- `Portable\AiEcoSys.exe` — self-contained portable application.
- `AiEcoSys.iso` — installation media containing the installer, portable
  build, documentation, scripts, and checksums.

## Known limitations

- Online catalogue refresh requires network access and is disabled in offline
  mode.
- Provider connection tests use an HTTPS `HEAD` request; a provider may reject
  `HEAD` even when its API is available.
- The installer is not code-signed. Windows SmartScreen may show a warning for
  an unsigned publisher.
- The ISO is installation media only; it is not a bootable operating-system
  image.

## Verification

See `docs/PHASE-5-FINAL-RELEASE-REPORT.md`, `docs/INSTALLER-VERIFICATION.md`,
`docs/ISO-VERIFICATION.md`, and `docs/FINAL-SECURITY-REPORT.md`.
