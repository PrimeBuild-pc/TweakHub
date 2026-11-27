# Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

_Production signing begins after SignPath Foundation issues the release certificate. Published releases from before that rollout are unsigned and include SHA-256 checksums._

## What is signed

Only the Windows binaries built from this repository: `TweakHub.exe`, the application's own assemblies and the installer `TweakHub-Setup-<version>.exe`. Third-party binaries are not re-signed with this certificate and keep their own signatures. See [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).

## How it is built

Signing requests originate from the [Release workflow](../.github/workflows/release.yml) on GitHub-hosted runners for the public repository `PrimeBuild-pc/TweakHub`. Production signing uses a tagged commit on `main` and requires approval in SignPath. The maintainer can run the same build and signing steps manually with the self-signed test certificate. Test packages are available as GitHub Actions artifacts. SignPath holds the private keys.

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
