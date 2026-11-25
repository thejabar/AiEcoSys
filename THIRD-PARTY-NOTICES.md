# Third-Party Notices

AiEcoSys includes or depends on the following third-party components.

## Models.dev catalogue

`assets/catalogue/api.json` is derived from the Models.dev project.

- Project: https://models.dev
- Source: https://github.com/sst/models.dev
- Licence: MIT
- Copyright: Copyright (c) 2025 models.dev

The catalogue is redistributed as data. AiEcoSys does not modify model prices
or capabilities; absent values are displayed as missing.

## .NET 10 runtime

The self-contained Windows build includes the .NET runtime and WPF components.

- Project: https://dotnet.microsoft.com
- Licence: MIT
- Copyright: .NET Foundation and Contributors

## Test-only packages

The test projects use xUnit, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio,
and coverlet.collector. These packages are not included in the distributed
application.

| Package | Licence |
|---------|---------|
| xunit | Apache-2.0 |
| xunit.runner.visualstudio | Apache-2.0 |
| Microsoft.NET.Test.Sdk | MIT |
| coverlet.collector | MIT |

## Inno Setup

The Windows installer is built with Inno Setup.

- Project: https://jrsoftware.org/isinfo.php
- Licence: Inno Setup License
- Copyright: Copyright (C) 1997-2026 Jordan Russell and Martijn Laan

Inno Setup is a build-time tool and is not redistributed as part of the
application.
