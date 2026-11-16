# Security policy

## Supported versions

Only the latest release on the [Releases](https://github.com/PrimeBuild-pc/TweakHub/releases) page receives security fixes.

## Reporting a vulnerability

Please do **not** open a public issue for a vulnerability. Use GitHub's private reporting instead:

<https://github.com/PrimeBuild-pc/TweakHub/security/advisories/new>

Include the version, steps to reproduce and the impact you see. This is a volunteer project, so there is no guaranteed response time, but reports are read and taken seriously. Please allow time for a fix before disclosing publicly.

## Scope

In scope: privilege escalation through the elevated-script flow, tampering with downloads or updates, unsafe handling of registry backups, and anything that makes TweakHub change a system differently from what it previews.

Out of scope: changes the app makes after you confirm them, behaviour of third-party tools it links to (report those upstream), and issues that need an already-compromised administrator account.

## Verifying a release

Every release asset has a matching `.sha256` file, and builds are produced by CI from tagged commits (`.github/workflows/release.yml`). See also the [privacy policy](docs/PRIVACY.md) and the [code signing policy](docs/CODE_SIGNING_POLICY.md).
