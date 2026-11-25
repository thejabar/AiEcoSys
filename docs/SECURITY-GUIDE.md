# AiEcoSys Security Guide

**Version 1.0.0** — Windows 10/11 x64 (current verified release)

## 1. Data protection

- No telemetry is implemented or sent by v1.0.0.
- Provider credentials are stored in **Windows Credential Manager** under `AiEcoSys:<provider>` and never written to settings, snapshots, or logs.
- Settings/snapshots contain no credentials.
- Diagnostics redact values that look like keys, tokens, secrets, or passwords.

## 2. Transport security

- Integration endpoints must use HTTPS.
- HTTP is accepted only for localhost endpoints.
- Provider connection tests use HTTPS `HEAD` requests.

## 3. Local data safety (v1.0.0 actual behavior)

- In v1.0.0, data and snapshot paths must be absolute and **not on the C: drive** (current build constraint).
- The application refuses C: data locations in v1.0.0.
- Future releases plan standard Windows user-data locations (`%LOCALAPPDATA%` / user-selected app-data dirs) with appropriate separation from program files. This is planned, not v1.0.0 behavior.

## 4. Platform-native credential stores (roadmap)

- **v1.0.0 verified:** Windows Credential Manager.
- **Planned/future:** macOS Keychain, Linux Secret Service/libsecret, Android Keystore, iOS Keychain, and other platform-native stores as implemented and tested. Do not claim these as v1.0.0.

## 5. Build and release security

- v1.0.0 installer is not code-signed; SmartScreen may warn. Verify SHA-256 before running.
- Bundled catalogue is redistributed data (Models.dev, MIT) and contains no credentials.
- No secrets in source, configuration, or release assets.

## 6. Reporting a vulnerability

Report security issues privately via GitHub private vulnerability reporting/security advisory for this repository when available. Do not open public issues for vulnerabilities. See [SECURITY.md](../SECURITY.md).
