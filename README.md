# AiEcoSys

AiEcoSys is a Windows desktop application for exploring AI providers and models, comparing published token pricing, selecting models for a task, and managing an offline catalogue. It is built with .NET 10, C#, and WPF (MVVM).

**Publisher:** The Jabar  
**Version:** 1.0.0  
**Platform:** Windows 10 / Windows 11 (x64)

## Main features

- **Dashboard** - provider/model counts, catalogue status, and recent activity.
- **Model Explorer** - search by name, modality, and context window.
- **Provider Manager** - provider metadata, API endpoints, and connection tests.
- **Pricing** - input/output prices in USD per 1M tokens, with missing-value and zero-cost filters.
- **Model Selection** - filter by task, provider, modality, context, and price.
- **Catalogue** - save and restore local JSON snapshots; optional online refresh.
- **Integrations** - opt-in AcademyOS, UBOS, and MavenMeeM endpoints.
- **Security** - Windows Credential Manager storage and HTTPS validation.
- **Settings** - data and snapshot locations, offline mode, and behaviour options.
- **About** - version, runtime, licence, and project links.

AiEcoSys does not invent model metadata or prices. Values come from the bundled Models.dev catalogue; absent values are shown as `—`.

## Download

Official release assets are published on GitHub Releases:

- Releases: <https://github.com/thejabar/AiEcoSys/releases>
- v1.0.0 (initial public release): <https://github.com/thejabar/AiEcoSys/releases/tag/v1.0.0>

Expected v1.0.0 assets include the Windows installer, portable executable, ISO, checksums, and guides. Verify SHA-256 checksums before running downloaded files.

## Requirements

- Windows 10 or Windows 11, x64.
- The portable and installed builds are self-contained and do not require a separately installed .NET runtime.
- A writable non-C: data location. The default installed data directory is `D:\AiEcoSys\Data`.

## Install

1. Download `AiEcoSys-Setup.exe` and `SHA256SUMS.txt` from the GitHub release.
2. Verify the SHA-256 checksum (see [Verify downloads](#verify-downloads)).
3. Run `AiEcoSys-Setup.exe`.

Default locations:

- Program files: `D:\AiEcoSys\Application`
- User data: `D:\AiEcoSys\Data`

The installer creates Start Menu entries and an optional desktop shortcut. It does not delete user data during an ordinary uninstall unless you explicitly confirm data removal.

## Run the portable version

1. Download the portable package/executable from the GitHub release.
2. Verify its SHA-256 checksum.
3. Extract/copy to a writable non-C: location and run `AiEcoSys.exe`.

The portable build keeps settings and snapshots in a `Data` folder beside the executable and loads the bundled `catalogue\api.json`. It does not require a prior installation.

## Offline catalogue

AiEcoSys ships with a bundled Models.dev `api.json` catalogue for offline use. The application can load the bundled/local snapshot without network access. Online refresh is optional and can be disabled by offline mode.

## Provider configuration and secure credential storage

- Provider API endpoints and model metadata are loaded from the catalogue.
- Provider credentials can be stored through the Security view.
- Credentials are stored in **Windows Credential Manager** for the current user and are never written to settings, snapshots, or logs.
- Remote integration endpoints must use HTTPS; HTTP is accepted only for localhost.

## Optional integrations

AcademyOS, UBOS, and MavenMeeM integrations are opt-in and disabled by default. Enable them only if you use those systems, configure HTTPS endpoints, and save integration settings explicitly.

## Verify downloads

From PowerShell in the folder containing the downloaded files:

```powershell
Get-FileHash .\AiEcoSys-Setup.exe -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

Compare the computed hash to the published checksum. Do not run a file whose hash does not match.

## Uninstall and recovery

- Uninstall through Windows Apps or the Start Menu uninstall shortcut. User data is preserved by default.
- Recovery scripts and guides are included in the repository/release documentation.
- See [docs/RECOVERY-GUIDE.md](docs/RECOVERY-GUIDE.md) and [docs/UNINSTALLATION-GUIDE.md](docs/UNINSTALLATION-GUIDE.md).

## Build from source

Development/build (example):

```powershell
dotnet build .\AiEcoSys.slnx -c Release
dotnet test .\AiEcoSys.slnx -c Release
dotnet publish .\src\AiEcoSys.Desktop\AiEcoSys.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\Release\Portable
```

The bundled catalogue is `assets\catalogue\api.json`, derived from [Models.dev](https://models.dev). See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Documentation

- [Installation guide](docs/INSTALLATION-GUIDE.md)
- [User guide](docs/USER-GUIDE.md)
- [Configuration guide](docs/CONFIGURATION-GUIDE.md)
- [Recovery guide](docs/RECOVERY-GUIDE.md)
- [Security guide](docs/SECURITY-GUIDE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Changelog](CHANGELOG.md)

## Contributing and security

- Contributions: [CONTRIBUTING.md](CONTRIBUTING.md)
- Code of conduct: [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)
- Security reporting: [SECURITY.md](SECURITY.md)

## Third-party notices and licence

- Third-party notices: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)
- Licence: [MIT](LICENSE)

## Scope and future plans

**Available in v1.0.0 (verified):** desktop catalogue views, local snapshot management, pricing/selection views, credential storage via Windows Credential Manager, optional integration settings, portable/installer/ISO release artifacts.

**Planned/future (not claimed as v1.0.0 functionality):** additional provider integrations, enhanced sync workflows, expanded automation, and signed installers. Future changes will be versioned separately and will not silently alter v1.0.0 release artifacts.
