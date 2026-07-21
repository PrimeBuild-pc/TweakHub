<div align="center">

<img src="assets/logo.png" alt="TweakHub wrench logo" width="128"/>

# TweakHub

**A portable Windows 11 toolkit for power users who configure, rebuild and maintain PCs frequently.**

Made with ❤️ for Windows power users.

[![Release](https://img.shields.io/github/v/release/PrimeBuild-pc/TweakHub?style=flat-square&logo=github)](https://github.com/PrimeBuild-pc/TweakHub/releases/latest)
[![CI](https://img.shields.io/github/actions/workflow/status/PrimeBuild-pc/TweakHub/ci.yml?branch=main&style=flat-square&label=CI)](https://github.com/PrimeBuild-pc/TweakHub/actions/workflows/ci.yml)
[![Total downloads](https://img.shields.io/github/downloads/PrimeBuild-pc/TweakHub/total?style=flat-square&label=total%20downloads)](https://github.com/PrimeBuild-pc/TweakHub/releases)
[![Latest release downloads](https://img.shields.io/github/downloads/PrimeBuild-pc/TweakHub/latest/total?style=flat-square&label=latest%20release)](https://github.com/PrimeBuild-pc/TweakHub/releases/latest)
[![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?style=flat-square&logo=windows11)](https://www.microsoft.com/windows/windows-11)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/PrimeBuild-pc/TweakHub?style=flat-square)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/PrimeBuild-pc/TweakHub?style=flat-square&logo=github)](https://github.com/PrimeBuild-pc/TweakHub/stargazers)

[![Download latest release](https://img.shields.io/badge/Download-Latest%20Release-2EA44F?style=for-the-badge&logo=github&logoColor=white)](https://github.com/PrimeBuild-pc/TweakHub/releases/latest)

</div>

> [!WARNING]
> TweakHub changes Windows settings. Review every preview and create a restore point before applying changes.

## Why TweakHub?

TweakHub is built to live on a USB drive. Extract it once, add your preferred tools, scripts, Registry tweaks, favorites and appearance settings, then carry the complete folder between PCs. Your portable configuration stays with the app instead of being scattered across every machine you maintain.

It runs as the current user and requests administrator privileges only when an operation requires them.

## Highlights

| Area | What TweakHub provides |
|---|---|
| **Registry & power** | Curated Windows 11 tweaks, automatic backups, verification and rollback |
| **Portable profiles** | Custom tools, scripts, tweaks, favorites and appearance stored beside the app |
| **Automation** | Built-in maintenance commands plus portable PowerShell and CMD scripts with preview, elevation, timeout and cancellation |
| **Repair** | DISM, SFC and online CHKDSK repair workflow plus policy, update, network and memory diagnostics with portable logs |
| **Toolbox** | Categorized Winget packages, trusted HTTPS links and user-created tool cards |
| **Quick access** | Common Windows administration consoles and control panels in one place |
| **Appearance** | System, light or dark theme with accent and transparency controls |
| **Updates** | Confirmed in-app updates with SHA-256 verification |

TweakHub supports **Windows 11 build 22000 or newer**.

## Screenshots

<p align="center">
  <a href="https://github.com/user-attachments/assets/ab7d6276-abec-42b4-9c1e-94778bbf2bcd">
    <img src="https://github.com/user-attachments/assets/ab7d6276-abec-42b4-9c1e-94778bbf2bcd" alt="TweakHub main view" width="900"/>
  </a>
</p>

<table>
  <tr>
    <td><a href="https://github.com/user-attachments/assets/55e50d9d-8717-4016-a049-19f5f82638f2"><img src="https://github.com/user-attachments/assets/55e50d9d-8717-4016-a049-19f5f82638f2" alt="TweakHub screenshot 2" width="260"/></a></td>
    <td><a href="https://github.com/user-attachments/assets/968caa7e-f20d-423e-8a41-5f987cff24fb"><img src="https://github.com/user-attachments/assets/968caa7e-f20d-423e-8a41-5f987cff24fb" alt="TweakHub screenshot 3" width="260"/></a></td>
    <td><a href="https://github.com/user-attachments/assets/4afa7472-638d-4a8a-9079-ec086c2b661d"><img src="https://github.com/user-attachments/assets/4afa7472-638d-4a8a-9079-ec086c2b661d" alt="TweakHub screenshot 4" width="260"/></a></td>
    <td><a href="https://github.com/user-attachments/assets/48defa49-24b9-405f-a193-0f088b140111"><img src="https://github.com/user-attachments/assets/48defa49-24b9-405f-a193-0f088b140111" alt="TweakHub screenshot 5" width="260"/></a></td>
  </tr>
</table>

<p align="center"><sub>Click any screenshot to open it at full size. On narrow screens, swipe the thumbnail row sideways.</sub></p>

## Download & quick start

### Portable — recommended

1. Download the portable archive from [the latest release](https://github.com/PrimeBuild-pc/TweakHub/releases/latest).
2. Extract the complete archive to a USB drive or local folder.
3. Run `TweakHub.exe`.
4. Keep `TweakHub.exe`, `portable.flag` and the generated `Data` directory together when moving the app.

That folder carries custom tools, scripts, Registry tweaks, favorites, appearance settings, operation logs and rollback data between PCs.

### Installer

The installer provides a conventional per-PC setup. Installed builds store user data under `%AppData%\TweakHub` instead of beside the executable.

You can also install or upgrade TweakHub with WinGet:

```powershell
winget install --id PrimeBuild.TweakHub --exact
```

> [!NOTE]
> Releases may be unsigned and can trigger a Microsoft Defender SmartScreen warning.

## Portable data

Portable builds keep their state in `Data` beside the application files:

| File | Purpose |
|---|---|
| `registry-backup.json` | Original Registry values waiting to be restored |
| `power-backup.json` | Original power settings and active power plan |
| `operations.jsonl` | Registry and power operation audit log |
| `custom-scripts.json` | User-created PowerShell and CMD scripts |
| `custom-tweaks.json` | User-created Registry entries |
| `custom-tools.json` | User-created external-tool cards |
| `favorites.json` | Favorite external tools |
| `favorite-tweaks.json` | Favorite built-in and custom Registry tweaks |
| `appearance.json` | Theme, accent and transparency preferences |
| `pending-restarts.json` | Tweaks waiting for a Windows restart |

Use **About & Settings → Portable configuration** to export or import the complete user profile as a `.tweakhub.json` file. Machine-specific rollback backups are intentionally excluded from profiles. Script and repair output is written to `Data/Logs` in portable builds. Successful built-in script runs are tracked per Windows user in `%LocalAppData%\TweakHub\machine-script-history.json`; this history never travels with the portable profile.

> [!CAUTION]
> Do not delete backup files before restoring outstanding changes. When moving TweakHub, copy the complete portable folder—not only `TweakHub.exe`.

## Memory and privacy tools

SysMain and Prefetch should normally remain enabled. Disable them only after measurements identify them as the source of a real problem; doing so can make applications and games load more slowly.

**Empty Standby List** requires Microsoft Sysinternals RAMMap from External Tools. Windows uses otherwise free RAM as cache and releases it immediately when applications need it. Emptying cached RAM routinely does not improve FPS or general performance and should not be scheduled.

The device-metadata and Windows Update driver policies affect different content: preventing device companion apps does not block every driver, while the driver exclusion policy omits packages Windows Update classifies as drivers. High-risk AI removal and third-party hosts-list scripts always require explicit confirmation.

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

## CI/CD

Every push and pull request to `main` runs the Windows build and tests. A `vX.Y.Z` tag matching the project version publishes the self-contained portable archive, installer and SHA-256 checksums to GitHub Releases. After the initial WinGet package is accepted, later releases automatically submit an updated `PrimeBuild.TweakHub` manifest.

Generated binaries belong in Releases, not in Git.

## Support the project

If TweakHub saves you time, you can support its development here:

[![PayPal](https://img.shields.io/badge/Supporta%20su-PayPal-0070BA?style=for-the-badge&logo=paypal&logoColor=white)](https://paypal.me/PrimeBuildOfficial?country.x=IT&locale.x=it_IT)

## License

[MIT](LICENSE) — provided as-is, without warranty.

<div align="center">

**Made with ❤️ for Windows power users.**

</div>
