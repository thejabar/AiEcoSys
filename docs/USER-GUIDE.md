# AiEcoSys User Guide

**Version 1.0.0** — Windows 10/11 x64 (current verified release)

AiEcoSys is a local desktop catalogue of AI providers and models. It reads Models.dev metadata, shows published token prices when present, and helps you select models for a task. It does not call model APIs or generate text.

> **v1.0.0 limitation:** The current v1.0.0 build requires a writable **non-C:** data location and defaults to `D:\AiEcoSys` paths. Future releases plan standard Windows locations and user-selectable app-data directories. This is a known v1.0.0 implementation constraint.

## 1. Starting the application

Launch **AiEcoSys** from Start Menu, desktop shortcut, or by running `AiEcoSys.exe`. The Dashboard shows provider/model counts, catalogue status, last sync, data source, and recent events.

## 2. Navigation

Dashboard, Model Explorer, Provider Manager, Pricing, Model Selection, Catalogue, Integrations, Security, Settings, About.

## 3. Dashboard

Shows provider/model counts, catalogue status, last sync, data source, and recent sync history.

## 4. Model Explorer

Search by name; filter by modality/context. Table shows model, provider, context, tool calling, and prices. Absent metadata shown as `—`.

## 5. Provider Manager

Shows provider name/id, API endpoint, model count, credential status. "Test connection" sends HTTPS `HEAD` (connectivity check only). Credential status comes from Windows Credential Manager.

## 6. Pricing

Input/output prices in USD per 1M tokens from Models.dev catalogue. Filters: price mode, min input/output. Missing costs shown as `—`.

## 7. Model Selection

Filter by task, provider, input modality, context, min price. Results update immediately.

## 8. Catalogue

- **Save snapshot** writes `latest.json` to configured snapshot directory.
- **Refresh from snapshot** reloads it.
- **Refresh online** downloads `https://models.dev/api.json` when offline mode is disabled.
- Snapshots are JSON; no credentials.

## 9. Integrations

AcademyOS, UBOS, MavenMeeM are opt-in and disabled by default. Remote endpoints must be HTTPS (localhost HTTP allowed). Test uses `HEAD`. AiEcoSys does not modify those systems.

## 10. Security

Credentials stored in Windows Credential Manager as `AiEcoSys:<provider>`. Use Security view to store/delete. Secret field is a password box; secrets never written to settings/snapshots/logs.

## 11. Settings

- **Data Location** — absolute, **non-C:** in v1.0.0 (current build constraint).
- **Snapshot Location** — must be inside data location.
- **Offline mode** — disables online refresh.
- **Automatically refresh catalogue on launch** — when enabled and offline mode off, local catalogue loads first then online refresh follows.

## 12. About

Shows version, build config, runtime, MIT licence, project links.

## 13. Data locations (v1.0.0 actual behavior)

**Portable:** program = folder containing `AiEcoSys.exe`; data = `Data` beside exe; catalogue = `catalogue\api.json` beside exe. Must be on a writable non-C: location in v1.0.0.

**Installed (v1.0.0 defaults):** program = `D:\AiEcoSys\Application`; data = `D:\AiEcoSys\Data`; catalogue = `catalogue\api.json` in program directory. Custom non-C: locations selectable in v1.0.0 installer.

**Future planned:** standard Windows locations (`%ProgramFiles%\The Jabar\AiEcoSys`, `%LOCALAPPDATA%\The Jabar\AiEcoSys` or user-selected app-data dirs) — not available in v1.0.0.

## 14. Offline use

Bundled catalogue works without network. Online refresh/provider tests require network and are optional. No telemetry sent.

## 15. Getting help

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md), [RECOVERY-GUIDE.md](RECOVERY-GUIDE.md), [CONFIGURATION-GUIDE.md](CONFIGURATION-GUIDE.md).
