# AiEcoSys

AiEcoSys is a Windows desktop application for exploring AI providers and
models, comparing published token pricing, selecting models for a task, and
managing an offline catalogue. It is built with .NET 10 and WPF.

![AiEcoSys icon](assets/AiEcoSys.png)

## Features

- **Dashboard** — provider/model counts, catalogue status, and recent activity.
- **Model Explorer** — search by name, modality, and context window.
- **Provider Manager** — provider metadata, API endpoints, and connection tests.
- **Pricing** — input/output prices in USD per 1M tokens, with missing-value
  and zero-cost filters.
- **Model Selection** — filter by task, provider, modality, context, and price.
- **Catalogue** — save and restore local JSON snapshots; optional online refresh.
- **Integrations** — opt-in AcademyOS, UBOS, and MavenMeeM endpoints.
- **Security** — Windows Credential Manager storage and HTTPS validation.
- **Settings** — data and snapshot locations, offline mode, and behaviour options.
- **About** — version, runtime, licence, and project links.

AiEcoSys does not invent model metadata or prices. Values come from the bundled
Models.dev catalogue; absent values are shown as `—`.

## Requirements

- Windows 10 or Windows 11, x64.
- The portable and installed builds are **self-contained** and do not require a
  separately installed .NET runtime.
- A writable non-C: data location. The default installed data directory is
  `D:\AiEcoSys\Data`.

## Install

Run `AiEcoSys-Setup.exe` and choose an installation directory. The default is:

- Program files: `D:\AiEcoSys\Application`
- User data: `D:\AiEcoSys\Data`

The installer creates Start Menu entries and an optional desktop shortcut. It
does not delete user data during an ordinary uninstall; the uninstaller asks
before removing it.

## Portable use

Extract the portable folder to a writable non-C: location and run
`AiEcoSys.exe`. The portable build keeps settings and snapshots in a `Data`
folder beside the executable and loads the bundled
`catalogue\api.json`. It does not require a prior installation.

## Data and privacy

- No telemetry is sent. The current release does not implement telemetry
  collection and has no telemetry setting.
- Credentials are stored with the Windows Credential Manager for the current
  user. They are never written to settings, snapshots, or logs.
- Remote integration endpoints must use HTTPS. HTTP is accepted only for
  localhost.
- The application does not modify AcademyOS, UBOS, or MavenMeeM installations.

## Build from source

```powershell
dotnet build .\AiEcoSys.slnx -c Release
dotnet test .\AiEcoSys.slnx -c Release
dotnet publish .\src\AiEcoSys.Desktop\AiEcoSys.Desktop.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\Release\Portable
```

The bundled catalogue is `assets\catalogue\api.json`, derived from
[Models.dev](https://models.dev). See `THIRD-PARTY-NOTICES.md`.

## Documentation

- [Installation guide](docs/INSTALLATION-GUIDE.md)
- [User guide](docs/USER-GUIDE.md)
- [Configuration guide](docs/CONFIGURATION-GUIDE.md)
- [Recovery guide](docs/RECOVERY-GUIDE.md)
- [Security guide](docs/SECURITY-GUIDE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)

## Licence

MIT. See [LICENSE](LICENSE).

Copyright (c) 2026 thejabar.
