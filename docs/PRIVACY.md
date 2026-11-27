# Privacy policy

_Last updated: 2026-11-27_

TweakHub is a free, open-source (MIT) Windows 11 tuning tool. This policy describes what the program does with your data. The source code is public, so every statement below can be checked.

## Summary

- TweakHub has **no analytics, no telemetry, no crash reporting and no accounts**.
- The project does not run any server and receives **no data** from your PC.
- Everything TweakHub reads or writes stays on your computer, in the program folder (`Data`, portable mode) or `%LOCALAPPDATA%\TweakHub`.
- The only network requests are the ones listed below, and each is started by an action of yours or by the update check of the installer and portable builds.

## Data stored locally

Settings, tweak backups (original registry values), restore logs, script history, profiles and favourites. Nothing is uploaded. You can delete it by removing the `Data` folder (or `%LOCALAPPDATA%\TweakHub`) after restoring your tweaks.

Hardware, driver, device, startup and security information is read from Windows to show it to you in the app. It is not sent anywhere. Reports and CSV/JSON exports are written only where you choose, and you decide whether to share them.

## Uninstalling TweakHub

Uninstalling the app (Microsoft Store, installer or deleting the portable folder) removes the program, **not the changes it made to Windows**. Registry values, policies, service start types, power-plan values, hosts-file entries, removed or disabled devices and apps installed through winget stay as they are. The Microsoft Store build shows this notice in the app under Settings, "Before you uninstall".

- **Before uninstalling**, restore what you changed from inside TweakHub (each tweak has Undo/Restore; the Policies page can reset policies; driver packages can be added back from their backup).
- **Left on the PC after uninstall:** `%LOCALAPPDATA%\TweakHub` (settings, backups, undo records, operation log, downloaded tools, crash log), `%PROGRAMDATA%\TweakHub-DriverBackups` (driver backups, administrators only) and, if you ran Tron, `%PROGRAMDATA%\TweakHub-Tron-*` (administrators only). Delete them yourself once you no longer need to restore anything; the two `%PROGRAMDATA%` folders need an administrator.
- Tools you installed through `winget` are removed with `winget uninstall` or Settings, Apps.

## Network requests

| When | Destination | What is sent |
|---|---|---|
| Update check (installer and portable builds only, on start and on demand; the Microsoft Store build makes no update request and is updated by the Store) | `api.github.com` (this repository's releases) | A standard HTTPS request, so GitHub sees your IP address and the program's user agent. An update is downloaded only after you confirm and is verified with SHA-256. |
| Breached password check (Security page) | `api.pwnedpasswords.com` (Have I Been Pwned) | Only the **first 5 characters of the SHA-1 hash** of the password (k-anonymity). The password never leaves your PC. |
| Account deletion links (Security page, on request) | `raw.githubusercontent.com` (JustDeleteMe directory) | A plain download of a public list. |
| Microsoft Safety Scanner, Tron, third-party scripts or tools (only when you start them) | Microsoft, GitHub or the tool's official site | A plain download. Files are checked (Microsoft signature or SHA-256) before use. |
| WinUtil and the Marius Heier scripts (only when you start one) | `christitus.com`, which redirects to GitHub, or `tools.mariusheier.com` | A plain download of the main script to a temporary file. TweakHub shows its SHA-256, asks for your consent to that exact version and runs only that copy, with UAC. **The hash covers only the main script.** WinUtil can download further programs and files while it runs (for example what you pick in its window); those requests come from WinUtil, under its own behaviour, and TweakHub does not check them. |
| Hash lookups (VirusTotal, Triage) | Opens your **browser** with the hash in the URL | Only the file hash. TweakHub never uploads files. |
| Links to documentation and tools | Your browser | Nothing is sent by TweakHub; the site you open applies its own policy. |
| WinGet installs (only when you start one) | Windows Package Manager | Handled by `winget` and Microsoft, not by TweakHub. |

If you change DNS to a filtering provider (Quad9, Cloudflare, AdGuard) or a Pi-hole from the Security page, your DNS queries go to that provider under its own policy. TweakHub saves the previous configuration so you can restore it.

## Third parties

GitHub, Have I Been Pwned, Microsoft and the sites you choose to open process requests under their own privacy policies. TweakHub shares nothing with them beyond what is in the table above.

## Hardware reports

[`Collect-TweakHub-Hardware.cmd`](../Scripts/Collect-TweakHub-Hardware.cmd) writes a local JSON file after your consent and uploads nothing. If you choose to share it, you do it yourself. See [hardware-reports.md](hardware-reports.md).

## Children

TweakHub does not collect personal data from anyone, including children.

## Changes

Changes to this policy are made in this repository and are visible in its git history. The date at the top changes with each revision.

## Contact

Support: <primebuild.official@gmail.com> · <https://primebuild.website>. Bug reports and feature requests can also be opened at <https://github.com/PrimeBuild-pc/TweakHub/issues>. For anything sensitive use the private report described in [SECURITY.md](../SECURITY.md).
