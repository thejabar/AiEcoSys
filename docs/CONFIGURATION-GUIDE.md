# AiEcoSys Configuration Guide

**Version 1.0.0** · Windows 10/11 x64

This guide documents every configuration surface of the AiEcoSys release.

## 1. Data locations

| Setting | Default | Notes |
|---------|---------|-------|
| Data location | `D:\AiEcoSys\Data` | Absolute, non-C: |
| Snapshot location | `<data>\snapshots` | Must be inside the data directory |

The authoritative resolver order is:

1. `AIECOSYS_DATA_DIR` environment variable (absolute, non-C:).
2. `aiecoSys.paths.json` beside the executable.
3. A `Data` directory beside the executable (portable layout).
4. `D:\AiEcoSys\Data` fallback.

## 2. `aiecoSys.paths.json`

Written by the installer and by the portable build when needed. Example:

```json
{
  "DataDirectory": "D:\\AiEcoSys\\Data"
}
```

The file must contain an absolute, non-C: path. Invalid values fall back to the
default data directory.

## 3. `app-settings.json`

Located at `<data>\settings\app-settings.json`. It contains behaviour settings
and integration opt-ins:

| Key | Type | Default | Meaning |
|-----|------|---------|---------|
| `DataLocation` | string | `D:\AiEcoSys\Data` | Active data root |
| `SnapshotLocation` | string | `<data>\snapshots` | Snapshot root |
| `EnableTelemetry` | bool | `false` | Reserved; the release does not send telemetry |
| `OfflineMode` | bool | `false` | Disables online catalogue refresh |
| `AutoSyncOnLaunch` | bool | `false` | Honours the saved preference; local catalogue always loads first |
| `AcademyOSEnabled` | bool | `false` | Opt-in AcademyOS integration |
| `AcademyOSEndpoint` | string | `null` | AcademyOS endpoint (HTTPS required) |
| `UBOSEnabled` | bool | `false` | Opt-in UBOS integration |
| `UBOSEndpoint` | string | `null` | UBOS endpoint (HTTPS required) |
| `MavenMeeMEnabled` | bool | `false` | Opt-in MavenMeeM integration |
| `MavenMeeMEndpoint` | string | `null` | MavenMeeM endpoint (HTTPS required) |

Saving a new data location moves the snapshot path under the new root when the
snapshot location was using the default.

## 4. Environment variables

| Variable | Effect |
|----------|--------|
| `AIECOSYS_DATA_DIR` | Overrides the data directory. Must be absolute and not on C:. |

## 5. Example configuration file

The portable build and installer both include `config\app-settings.example.json`
and `config\app-settings.example.json` respectively. Copy the example to
`app-settings.json` only if you understand each field; the shipped default
configuration is valid without changes.

## 6. Installer parameters

| Parameter | Meaning |
|-----------|---------|
| `/DIR=<path>` | Program installation directory (absolute, not on C:). |
| `/DATA=<path>` | User data directory (absolute, not on C:). |
| `/TASKS=desktopicon` | Create a desktop shortcut in addition to Start Menu. |

## 7. Scope

Configuration is per-user, per-install. The installed copy writes a
per-user registry key under `HKCU\Software\AiEcoSys` in addition to
`aiecoSys.paths.json`.
