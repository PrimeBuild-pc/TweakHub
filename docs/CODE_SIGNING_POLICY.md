# Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

_This policy applies once TweakHub is accepted into the SignPath Foundation program. Until then releases are unsigned and the SHA-256 checksums are the way to verify them._

## What is signed

Only the Windows binaries built from this repository: `TweakHub.exe`, the application's own assemblies and the installer `TweakHub-Setup-<version>.exe`. Third-party binaries are not re-signed with this certificate and keep their own signatures. See [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).

## How it is built

Signing requests come only from the GitHub Actions workflow in `.github/workflows/release.yml`, from a tagged commit of the public repository `PrimeBuild-pc/TweakHub`, on GitHub-hosted runners. Binaries are never signed from a developer machine, and the project holds no private key.

## Roles

| Role | Who | Responsibility |
|---|---|---|
| Author | [PrimeBuild-pc](https://github.com/PrimeBuild-pc) | Writes and merges the code that is built. |
| Reviewer | [PrimeBuild-pc](https://github.com/PrimeBuild-pc) | Reviews changes, including external pull requests, before they are merged. |
| Approver | [PrimeBuild-pc](https://github.com/PrimeBuild-pc) | Approves each signing request in SignPath. |

The project has a single maintainer, who holds all three roles and uses multi-factor authentication on GitHub and SignPath. Contributions from others are accepted only through pull requests that the maintainer reviews. If more maintainers join, the roles will be split and this page updated.

## Rules

- TweakHub is open source (MIT) and contains no malware or potentially unwanted software.
- It changes only what the user previews and confirms, and keeps backups to restore it (see the [README](../README.md)).
- Every release asset has a SHA-256 file.
- If a signed build is found to violate this policy, the certificate use is stopped and the incident is disclosed.

## Privacy

TweakHub does not collect or transmit personal data. See the [privacy policy](PRIVACY.md).
