# Changelog

All notable changes to AiEcoSys are documented in this file.

## [1.0.0] - 2026-11-25

### Added
- Initial public release of AiEcoSys.
- WPF desktop application (.NET 10, Windows 10/11 x64) with ten functional views: Dashboard, Model Explorer, Provider Manager, Pricing, Model Selection, Catalogue, Integrations, Security, Settings, and About.
- Offline Models.dev catalogue bundled with the application (328 providers, 10,283 models at release time).
- Token pricing display in USD per 1M tokens where source data is available.
- Local catalogue snapshot save/restore and optional online refresh.
- Windows Credential Manager integration for provider credentials.
- Portable and installer distributions with separate, non-C: data directories.
- PowerShell maintenance scripts for install, verify, backup, restore, repair, update, diagnostics, and cache cleanup.
- Release documentation and PDF guides (Installation, User, Recovery).

### Security
- No telemetry collection or transmission.
- Credentials stored in Windows Credential Manager; never written to settings, snapshots, or logs.
- HTTPS required for remote integration endpoints (localhost HTTP exception).

### Known limitations
- Installer is not code-signed; Windows SmartScreen may warn.
- ISO is installation media, not a bootable OS image.
- Online catalogue refresh and provider `HEAD` connectivity checks require network access.
