# Privacy policy

_Last updated: 2026-11-16_

TweakHub is a free, open-source (MIT) Windows 11 tuning tool. This policy describes what the program does with your data. The source code is public, so every statement below can be checked.

## Summary

- TweakHub has **no analytics, no telemetry, no crash reporting and no accounts**.
- The project does not run any server and receives **no data** from your PC.
- Everything TweakHub reads or writes stays on your computer, in the program folder (`Data`, portable mode) or `%LOCALAPPDATA%\TweakHub`.
- The only network requests are the ones listed below, and each is started by an action of yours or by the update check.

## Data stored locally

Settings, tweak backups (original registry values), restore logs, script history, profiles and favourites. Nothing is uploaded. You can delete it by removing the `Data` folder (or `%LOCALAPPDATA%\TweakHub`) after restoring your tweaks.

Hardware, driver, device, startup and security information is read from Windows to show it to you in the app. It is not sent anywhere. Reports and CSV/JSON exports are written only where you choose, and you decide whether to share them.

## Network requests

| When | Destination | What is sent |
|---|---|---|
| Update check (on start and on demand) | `api.github.com` (this repository's releases) | A standard HTTPS request, so GitHub sees your IP address and the program's user agent. An update is downloaded only after you confirm and is verified with SHA-256. |
| Breached password check (Security page) | `api.pwnedpasswords.com` (Have I Been Pwned) | Only the **first 5 characters of the SHA-1 hash** of the password (k-anonymity). The password never leaves your PC. |
| Account deletion links (Security page, on request) | `raw.githubusercontent.com` (JustDeleteMe directory) | A plain download of a public list. |
| Microsoft Safety Scanner, Tron, third-party scripts or tools (only when you start them) | Microsoft, GitHub or the tool's official site | A plain download. Files are checked (Microsoft signature or SHA-256) before use. |
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

Open an issue at <https://github.com/PrimeBuild-pc/TweakHub/issues>. For anything sensitive use the private report described in [SECURITY.md](../SECURITY.md).
