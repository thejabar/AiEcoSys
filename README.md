# AiEcoSys

AiEcoSys is an evolving, open-source AI ecosystem for exploring AI providers and models, comparing published pricing/capabilities, managing credentials securely, and organising model selection through a central catalogue with online and offline support.

**Publisher:** The Jabar  
**Current release:** v1.0.0 (Windows 10/11 x64, verified)  
**Repository:** <https://github.com/thejabar/AiEcoSys>  
**Releases:** <https://github.com/thejabar/AiEcoSys/releases>

## Product status

### Available now (v1.0.0, verified)
- Windows 10/11 x64 desktop application (.NET 10, C#, WPF, MVVM).
- Self-contained installer, portable executable, and ISO installation media.
- Bundled offline Models.dev catalogue; optional online refresh.
- Model/provider/pricing exploration and selection views.
- Windows Credential Manager for provider credentials.
- Opt-in integrations (AcademyOS, UBOS, MavenMeeM) with HTTPS validation.

### Planned / under development (not available in v1.0.0)
- Linux and macOS desktop editions.
- Android and iOS/iPadOS applications.
- PowerShell 7+ module, Bash/Zsh CLI, and standalone CLI.
- Web application / PWA.
- REST API.
- Docker and server environments.
- Platform-native credential stores outside Windows (Keychain, Secret Service, Android Keystore, etc.).

Do not treat planned platforms/features as available until they are released and verified.

## Windows v1.0.0 installation (current verified release)

> **Important (v1.0.0 actual behavior):** The current verified v1.0.0 build enforces a non-C: data location and defaults to `D:\AiEcoSys` paths. This is a known v1.0.0 implementation limitation. Future releases plan standard Windows conventions (`%ProgramFiles%\The Jabar\AiEcoSys` for program files and appropriate `%LOCALAPPDATA%`/user-selected app-data locations), with user-selectable install/data directories. Do not assume D: is a universal end-user requirement; it is the current default/constraint of v1.0.0.

### Installer
1. Download `AiEcoSys-Setup.exe` and `SHA256SUMS.txt` from the [v1.0.0 release](https://github.com/thejabar/AiEcoSys/releases/tag/v1.0.0).
2. Verify SHA-256 before running.
3. Run the installer.

**v1.0.0 default locations (current build):**
- Program files: `D:\AiEcoSys\Application` (custom non-C: path selectable)
- User data: `D:\AiEcoSys\Data` (custom non-C: path selectable; must be writable)

The installer creates Start Menu entries and an optional desktop shortcut. It does not delete user data during ordinary uninstall unless explicitly confirmed.

### Portable
1. Download `AiEcoSys.exe` (portable) from the v1.0.0 release and verify SHA-256.
2. Copy to a writable non-C: location.
3. Run `AiEcoSys.exe`. A `Data` folder is created beside the executable.

The portable edition should be used from a writable user-selected location; in v1.0.0 it currently requires a non-C: location.

### Verify downloads
```powershell
Get-FileHash .\AiEcoSys-Setup.exe -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

## Offline catalogue
AiEcoSys ships with a bundled Models.dev `api.json` catalogue for offline use. Online refresh is optional and can be disabled by offline mode.

## Security and credentials
- **v1.0.0 (Windows):** Provider credentials are stored in Windows Credential Manager for the current user; never written to settings, snapshots, or logs.
- Remote integration endpoints must use HTTPS (localhost HTTP exception).
- No telemetry is collected or transmitted by the current v1.0.0 release.
- **Future/planned:** Keychain (macOS/iOS), Secret Service/libsecret (Linux), Android Keystore, and other platform-native stores will be used only when implemented and tested.

## Configuration and integrations
- Provider/model metadata comes from the bundled catalogue.
- Optional integrations (AcademyOS, UBOS, MavenMeeM) are opt-in, disabled by default.
- See [Configuration guide](docs/CONFIGURATION-GUIDE.md) and [User guide](docs/USER-GUIDE.md).

## Uninstall and recovery
- Uninstall via Windows Apps or Start Menu shortcut. User data preserved by default.
- See [Recovery guide](docs/RECOVERY-GUIDE.md) and [Uninstallation guide](docs/UNINSTALLATION-GUIDE.md).

## Build from source (developers)
```powershell
dotnet build .\AiEcoSys.slnx -c Release
dotnet test .\AiEcoSys.slnx -c Release
dotnet publish .\src\AiEcoSys.Desktop\AiEcoSys.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\Release\Portable
```

Developer note: `D:\AiEcoSys` is the current maintainer's local working path/example; it is not a required end-user installation path.

## Documentation
- [Installation guide](docs/INSTALLATION-GUIDE.md)
- [User guide](docs/USER-GUIDE.md)
- [Configuration guide](docs/CONFIGURATION-GUIDE.md)
- [Recovery guide](docs/RECOVERY-GUIDE.md)
- [Security guide](docs/SECURITY-GUIDE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Changelog](CHANGELOG.md)

## Contributing, security, licence
- [CONTRIBUTING.md](CONTRIBUTING.md)
- [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)
- [SECURITY.md](SECURITY.md)
- [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)
- [MIT License](LICENSE)

## Roadmap (planned, not yet released)
| Area | Status |
|---|---|
| Windows v1.0.0 desktop | **Available (verified)** |
| Linux/macOS desktop | Planned |
| Android/iOS/iPadOS | Planned |
| PowerShell 7+ module | Planned |
| Bash/Zsh/standalone CLI | Planned |
| Web app / PWA | Planned |
| REST API | Planned |
| Docker/server deployment | Planned |

Future releases will be versioned separately; v1.0.0 verified release artifacts will not be silently changed.
