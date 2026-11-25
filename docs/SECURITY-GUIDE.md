# AiEcoSys Security Guide

**Version 1.0.0** · Windows 10/11 x64

## 1. Data protection

- No telemetry is implemented or sent by the current release.
- Credentials are stored in Windows Credential Manager under the
  `AiEcoSys:<provider>` name and never written to settings, snapshots, or logs.
- Settings files and snapshots contain no credentials.
- Diagnostics output redacts values that look like keys, tokens, secrets, or
  passwords.

## 2. Transport security

- Integration endpoints must use HTTPS.
- HTTP is accepted only for localhost endpoints.
- Provider connection tests use HTTPS `HEAD` requests.

## 3. Local data safety

- Data and snapshot paths must be absolute and not on the C: drive.
- The application refuses to operate with C: data locations.
- Portable and installed builds keep user data out of program directories unless
  the installer derives a data directory inside the program folder for custom
  locations.

## 4. Build security

- The self-contained publish was produced with source linking disabled for debug
  symbols and no embedded debug content.
- The installer is not code-signed; Windows SmartScreen may display a warning.
- The bundled catalogue is redistributed data under the MIT licence; it contains
  no credentials.

## 5. Reporting a vulnerability

Report security issues privately through the GitHub security advisory channel for
the target repository. Do not open public issues for vulnerabilities.

## 6. Compliance summary

- No telemetry collection or transmission in the release.
- No secrets in source, configuration, or release artifacts.
- Credential storage outside AiEcoSys-owned files.
- HTTPS-only integration endpoints with localhost exception.
