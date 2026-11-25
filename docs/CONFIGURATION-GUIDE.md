# AiEcoSys Configuration Guide

**Version 1.0.0** — Windows 10/11 x64 (current verified release)

> **v1.0.0 limitation:** The current v1.0.0 build rejects C: drive paths for data/snapshot locations and defaults to `D:\AiEcoSys\Data`. This is an implementation constraint of v1.0.0, not the intended long-term product convention. Future releases plan standard Windows directories (`%LOCALAPPDATA%\The Jabar\AiEcoSys`, user-selected locations) and appropriate Program Files install defaults. Do not modify v1.0.0 release artifacts.

## 1. Data locations (v1.0.0 actual behavior)

| Setting | v1.0.0 default | v1.0.0 constraint |
|---------|----------------|-------------------|
| Data location | `D:\AiEcoSys\Data` | Absolute path, **non-C:** required by current build |
| Snapshot location | `<data>\snapshots` | Must be inside data directory; **non-C:** required |

Resolver order:
1. `AIECOSYS_DATA_DIR` environment variable (absolute, non-C:).
2. `aiecoSys.paths.json` beside the executable (`DataDirectory`).
3. `Data` directory beside the executable (portable layout).
4. `D:\AiEcoSys\Data` fallback (v1.0.0 legacy fallback).

## 2. `aiecoSys.paths.json`

Written by installer/portable layout when needed. Example:
```json
{
  "DataDirectory": "D:\\AiEcoSys\\Data"
}
```
In v1.0.0, the path must be absolute and non-C:. Invalid values fall back to the resolver defaults.

## 3. `app-settings.json`

Located at `<data>\settings\app-settings.json`.

| Key | Type | Default | Meaning |
|-----|------|---------|---------|
| `DataLocation` | string | `D:\AiEcoSys\Data` | Active data root (v1.0.0: non-C:) |
| `SnapshotLocation` | string | `<data>\snapshots` | Snapshot root (inside data root) |
| `EnableTelemetry` | bool | `false` | Reserved; no telemetry in v1.0.0 |
| `OfflineMode` | bool | `false` | Disables online catalogue refresh |
| `AutoSyncOnLaunch` | bool | `false` | Honors saved preference; local catalogue loads first |
| `AcademyOSEnabled` | bool | `false` | Opt-in AcademyOS integration |
| `AcademyOSEndpoint` | string | `null` | HTTPS required (localhost HTTP exception) |
| `UBOSEnabled` | bool | `false` | Opt-in UBOS integration |
| `UBOSEndpoint` | string | `null` | HTTPS required |
| `MavenMeeMEnabled` | bool | `false` | Opt-in MavenMeeM integration |
| `MavenMeeMEndpoint` | string | `null` | HTTPS required |

## 4. Environment variables

| Variable | v1.0.0 effect |
|----------|---------------|
| `AIECOSYS_DATA_DIR` | Overrides data directory; must be absolute and not on C: in v1.0.0. |

## 5. Installer parameters (v1.0.0)

| Parameter | Meaning |
|-----------|---------|
| `/DIR=<path>` | Program install directory (absolute, non-C: required by v1.0.0) |
| `/DATA=<path>` | User data directory (absolute, non-C: required by v1.0.0) |
| `/TASKS=desktopicon` | Create desktop shortcut |

## 6. Future planned configuration (not v1.0.0)
- Standard Windows defaults: `%ProgramFiles%\The Jabar\AiEcoSys` and `%LOCALAPPDATA%\The Jabar\AiEcoSys` (or user-selected equivalents).
- Platform-native config locations for Linux/macOS/mobile/CLI/server editions.
These are planned; do not claim as v1.0.0 behavior.
