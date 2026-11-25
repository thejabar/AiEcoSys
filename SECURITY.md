# Security Policy

## Supported version

| Version | Supported |
|---------|-----------|
| 1.0.0   | Yes |

## Reporting a vulnerability

Do not report security vulnerabilities in public issues.

Use GitHub's private vulnerability reporting / security advisory mechanism for
this repository when available. If private reporting is unavailable, contact the
maintainer through a private channel and include:

- Affected version and component
- Reproduction steps or proof of concept
- Impact assessment
- Any suggested mitigation

## Security expectations

- No plaintext credentials in source, config, logs, or release assets.
- Provider credentials are stored in Windows Credential Manager.
- Remote integration endpoints must use HTTPS (localhost HTTP exception).
- No telemetry is collected or transmitted by the current release.

## Release integrity

Verify downloaded release assets against the SHA-256 checksums published in the
GitHub release and `Release/SHA256SUMS.txt`.
