<div align="center">

# TweakHub

A focused Windows 11 tweak and maintenance utility.

[![Release](https://img.shields.io/github/v/release/PrimeBuild-pc/TweakHub?include_prereleases)](https://github.com/PrimeBuild-pc/TweakHub/releases)
[![CI](https://github.com/PrimeBuild-pc/TweakHub/actions/workflows/release-beta.yml/badge.svg)](https://github.com/PrimeBuild-pc/TweakHub/actions/workflows/release-beta.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-orange)](LICENSE)

[Download the latest beta](https://github.com/PrimeBuild-pc/TweakHub/releases)

</div>

> [!WARNING]
> TweakHub is beta software that changes Windows settings. Review every preview and create a restore point before applying changes.

## What it does

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

Download either the installer or portable archive from [GitHub Releases](https://github.com/PrimeBuild-pc/TweakHub/releases). The portable archive contains `portable.flag`, so configuration and rollback data stay in its `Data` folder and travel with the complete application folder. Later releases can be installed from the in-app update prompt after SHA-256 verification. Public beta builds may be unsigned and can trigger a Microsoft Defender SmartScreen warning.

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

Installed builds store scripts, favorites, persistent backups and operation logs under `%AppData%\TweakHub`. Portable builds store them under `Data` beside the application files.

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

A `vX.Y.Z-beta` tag matching the project `Version` runs restore, build, tests, self-contained publish, portable archive creation, installer creation and SHA-256 generation. Generated binaries belong in GitHub Releases, not in Git.

## License

[MIT](LICENSE) — provided as-is, without warranty.
