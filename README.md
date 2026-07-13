<div align="center">

<img src="assets/logo.png" alt="TweakHub wrench" width="128"/>

# TweakHub

A portable Windows 11 toolkit for power users who configure, rebuild and maintain PCs frequently.

[![Release](https://img.shields.io/github/v/release/PrimeBuild-pc/TweakHub)](https://github.com/PrimeBuild-pc/TweakHub/releases/latest)
[![CI](https://github.com/PrimeBuild-pc/TweakHub/actions/workflows/ci.yml/badge.svg)](https://github.com/PrimeBuild-pc/TweakHub/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-orange)](LICENSE)

[Download the latest release](https://github.com/PrimeBuild-pc/TweakHub/releases/latest)

</div>

> [!WARNING]
> TweakHub changes Windows settings. Review every preview and create a restore point before applying changes.

## What it does

TweakHub is designed to live on a USB drive. Extract it once, create your tools, scripts, favorites and appearance settings, then carry the complete folder between PCs. Portable user data remains beside the app instead of being left on each machine.

- Applies a small, curated set of Windows 11 Registry and power-plan tweaks.
- Captures original values before every change and restores them after an app restart.
- Validates and logs Registry and power operations.
- Creates, imports, exports and runs PowerShell or CMD scripts with optional elevation, timeout and cancellation.
- Runs verified DISM and SFC repair commands.
- Installs a categorized advanced-tool catalogue and lets power users add custom Winget, HTTPS or confirmed PowerShell cards.
- Exports/imports custom tools, scripts, Registry entries, favorites and appearance as one JSON profile.
- Includes conservative Windows Update controls with persistent rollback and restart indicators.
- Follows Windows appearance by default, with optional theme, accent and transparency overrides.
- Checks for updates at startup and can download, verify and install a release after confirmation.
- Opens common Windows administration utilities.

TweakHub supports Windows 11 build 22000 or newer. It runs as the current user and requests administrator privileges only for operations that require them.

## Install

Download the portable archive from [GitHub Releases](https://github.com/PrimeBuild-pc/TweakHub/releases/latest), extract it to a USB drive or local folder, and run `TweakHub.exe`. Keep `TweakHub.exe`, `portable.flag` and the generated `Data` directory together: copying that complete folder preserves custom tools, scripts, tweaks, favorites, appearance and rollback data.

An installer is also provided for a conventional per-PC installation; installed builds keep user data under `%AppData%\TweakHub`. Releases may be unsigned and can trigger a Microsoft Defender SmartScreen warning. In-app updates are downloaded and SHA-256 verified before installation.

## Build from source

Requirements: Windows 11 and the .NET 10 SDK.

```powershell
git clone https://github.com/PrimeBuild-pc/TweakHub.git
cd TweakHub
dotnet restore TweakHub.sln
dotnet build TweakHub.sln -c Release --no-restore
dotnet test TweakHub.Tests/TweakHub.Tests.csproj -c Release --no-build --no-restore
dotnet publish TweakHub.csproj -c Release -r win-x64 --self-contained true -o publish-standalone
```

## Local data

Portable builds store scripts, favorites, persistent backups and operation logs under `Data` beside the application files. Installed builds use `%AppData%\TweakHub`.

Important files:

- `registry-backup.json`: outstanding Registry rollback data.
- `power-backup.json`: original power settings and active plan.
- `operations.jsonl`: operation audit log.
- `custom-scripts.json`: custom scripts.
- `custom-tweaks.json`: custom Registry entries.
- `custom-tools.json`: user-created external-tool cards.
- `appearance.json`: theme, accent and transparency preferences.

Use **About & Settings → Portable configuration** to move the complete user profile as one `.tweakhub.json` file. Machine-specific rollback backups are intentionally excluded.

Do not delete backup files before restoring outstanding changes.

## Release process

Every push and pull request to `main` runs the Windows build and test workflow. A `vX.Y.Z` tag matching the project version publishes the self-contained portable archive, installer and SHA-256 checksums to GitHub Releases. Generated binaries belong in Releases, not in Git.

## License

[MIT](LICENSE) — provided as-is, without warranty.
