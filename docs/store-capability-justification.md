# TweakHub: Microsoft Store capability justification (policy 10.6.3)

Product ID `9P5R2R5TRKL3` · package `LorenzoMassafra.TweakHub` · analysed package: `1.8.1.0` (x64, source commit `e9da5b4`, manifest `packaging/msix/AppxManifest.xml`).
Support: `primebuild.official@gmail.com` · `https://primebuild.website` · source: `https://github.com/PrimeBuild-pc/TweakHub` (MIT).

Everything below was read from the code and manifest of this repository. Where something was **not** tested or is not true, it says so (see "Not verified" and "Gaps found").

---

## 1. Paste-ready text for the Partner Center "business justification" field

> TweakHub is a Windows 11 tuning and diagnostics utility. Its only purpose is to change real Windows settings that the user selects: registry values (policies, services, device and interrupt settings, power and latency options), Windows services, power plans, hosts file, driver-store packages, and to restore them afterwards. A copy of those changes that stays inside a private package registry or file view would be invisible to Windows and would make every feature a silent no-op.
>
> **allowElevation.** The app manifest is `asInvoker` and the package runs at medium integrity. Nothing runs elevated at start-up. Each operation that needs administrator rights starts one child process (`powershell.exe`, `reg.exe`, `powercfg.exe`, or an elevated restart of TweakHub itself) with the standard UAC prompt, only after the user clicked that operation and confirmed it. Elevated scripts are written to disk, hashed, and run only if the bytes still match the hash the user approved; output returns on a private named pipe. There is no silent elevation, no service, no scheduled task of ours and no auto-start.
>
> **unvirtualizedResources.** Virtualization is disabled for the whole package (`RegistryWriteVirtualization` and `FileSystemWriteVirtualization` = disabled) for two reasons. (1) The registry writes must reach the real HKLM/HKCU hives, including keys chosen by the user (custom tweaks, imported playbooks, device-specific keys under `Enum\...`), so a fixed `ExcludedKeys` list cannot describe them. (2) The UI process, the elevated children and system tools (`pnputil`, `powercfg`, `sc.exe`, `winget`) must read and write the same backup, undo and log files, otherwise a change could not be restored.
>
> Only operations the user starts are performed; each one is previewed, backed up where technically possible, and reversible from the app. TweakHub has no telemetry and no server. Downloaded tools are started by the user, from the vendor's own domain, and are checked by SHA-256 or Authenticode signature before they run (details and the two exceptions in the attached document). What remains after uninstall, and how to remove it, is documented in `docs/PRIVACY.md` ("Uninstalling TweakHub").
>
> Full technical answers (exact paths, keys, writes, downloads, uninstall behaviour): https://github.com/PrimeBuild-pc/TweakHub/blob/main/docs/store-capability-justification.md

(≈2,000 characters. Shorten the second paragraph first if the field has a smaller limit.)

---

## 2. Point-by-point answers

### 2.1 Exact paths and keys excluded from virtualization
None are listed individually. The manifest does not use `ExcludedKeys` or `ExcludedDirectories`. Virtualization is switched off for the **entire package**. Effective scope = whatever the user asks TweakHub to change:

| Area | Targets written by TweakHub (observed in code) |
|---|---|
| Registry, machine | `HKLM\SOFTWARE\Policies\Microsoft\...` (≈50 references: Windows Update, Defender-related policies, telemetry, Windows AI, etc.), `HKLM\SOFTWARE\Policies\WindowsNotepad`, `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\...`, `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\...`, `HKLM\SOFTWARE\Microsoft\WindowsUpdate`, `HKLM\SYSTEM\CurrentControlSet\Control\...` (priority, power, GPU/graphics, memory), `HKLM\SYSTEM\CurrentControlSet\Services\...` (service start type, driver parameters), `HKLM\SYSTEM\CurrentControlSet\Enum\<device>\Device Parameters\Interrupt Management\...` (MSI mode and affinity) |
| Registry, user | `HKCU\Control Panel\Desktop` and `\Mouse`, `HKCU\Software\Microsoft\Windows\CurrentVersion\...` (Explorer, privacy, themes), `HKCU\Software\Microsoft\GameBar`, `HKCU\System\GameConfigStore`, `HKCU\Software\Microsoft\DirectX`, `HKCU\Software\Policies\Microsoft\...`, plus user-defined custom tweaks |
| File system | `%LOCALAPPDATA%\TweakHub\Store\{Data,Apps,Machine}` (settings, `registry-backup.json`, `operations.jsonl`, undo records, tools, pending-restart state), `%PROGRAMDATA%\TweakHub-DriverBackups`, `%PROGRAMDATA%\TweakHub-Tron-*` (admin-only), `%SystemRoot%\Temp` and the user temp folder (hash-checked script files, temp cleanup), `%SystemRoot%\System32\drivers\etc\hosts` (only the Adobe block/unblock script), GPU shader caches under `%LOCALAPPDATA%` (cache cleanup), driver-store packages via `pnputil` |

### 2.2 Whole package or specific keys/directories?
Whole package: `<desktop6:RegistryWriteVirtualization>disabled</…>` and `<desktop6:FileSystemWriteVirtualization>disabled</…>` in `<Properties>`, together with the `unvirtualizedResources` capability. No `ExcludedKeys`/`ExcludedDirectories`.

### 2.3 Writes by the packaged app vs. the elevated child

| Process | What it writes |
|---|---|
| Packaged app (medium IL, user token) | Its own data (`Store\Data`: settings, favourites, profiles, `registry-backup.json`, `operations.jsonl`, undo records, script history, consent files), downloaded tools under `Store\Apps`, `HKCU` tweaks that a standard user can write (`RegistryService` writes directly when no elevation is needed), temp script files, exports the user selects |
| Elevated child (one per user-confirmed operation, UAC every time) | `reg.exe add/delete` for HKLM and `HKCU\Software\Policies` (`RegistryService.RunElevatedReg`); `powercfg.exe` (`PowerService.RunMutationAsync`); hash-checked PowerShell (`PowerShellService.ExecuteScriptAsync(requireAdministrator)`): services (`sc.exe config`, `Set-Service`), driver store (`pnputil`), power-plan values, hosts file, cleanup scripts, device enable/disable/removal; interactive scripts (`AutomatedScriptsPage`); Tron (`TronService`); third-party scripts (`ThirdPartyScriptService`); installer of a TweakHub update (outside the Store build, see 2.8); elevated restart of TweakHub itself (`App.RestartElevated`, offered for bulk operations/playbooks) |

### 2.4 Writes to HKLM, Windows, Program Files, services, scheduled tasks, browser/app data, third-party resources
All are user-initiated and each is previewed (and, where the change is a registry value, backed up before it is written).

| Target | Purpose | Code |
|---|---|---|
| HKLM policy/system/service keys (table 2.1) | Tweaks and policy resets the user ticks; undo restores the saved value | `TweakService`, `RegistryService`, `PolicyService`, `OptionalServicesService` |
| Policy reset covers **third-party** policy keys (for example `HKLM\SOFTWARE\Policies\Google\Chrome`) | The Policies page lists forced policies found on the PC and lets the user reset the ones TweakHub did not create; it backs them up first. Nothing is touched until the user picks a category. | `PolicyService.ResetAsync` |
| Services | Set start type (`sc.exe config`, `Set-Service`) for optional services, Windows Update preset, Startup page; optional stop | `OptionalServicesService`, `Scripts/WindowsUpdatePreset.ps1`, `SystemTriageService` |
| Scheduled tasks | **Created**: none permanently (one short-lived limited-rights task, below). **Changed/removed**: tasks the user disables/removes/restores on the Startup page (restore re-registers the saved XML). | `SystemTriageService`; `ToolDownloadService.UninstallWithoutElevationAsync` registers a limited task named `TweakHub-<guid>` for a `winget uninstall` and unregisters it in `finally` |
| Program Files / Windows | No file is written in Program Files. Under `%SystemRoot%`: temp files, `Temp` cleanup, DISM/SFC component cleanup and repair (`component-cleanup`, `dism-sfc-chkdsk`), hosts file (Adobe block list only), driver store via `pnputil` | `BuiltInScripts`, `DriverStoreService` |
| Third-party app data | Only GPU shader caches (NVIDIA/AMD/Intel DX/GL caches) and temp folders, by the cleanup scripts. No browser profile, cookie, password or history file is read or written. (Browser vendors appear only as policy key names.) | `BuiltInScripts` `shader-cache-cleanup`, `temp-cleanup` |
| Installed apps | `winget install/uninstall` of a package the user picked; Copilot/AI components removal script | `ToolDownloadService`, `BuiltInScripts` `remove-windows-ai` |
| Network settings | DNS provider change on the Security page, with the previous configuration saved | `PrivacyService` |

### 2.5 Reproducible failure with standard MSIX virtualization
**Not reproduced by a recorded test in this repository.** The argument below follows from Microsoft's documented behaviour plus the code, and should be demonstrated on the test VM before it is quoted as an observation.

With default virtualization:
1. Registry writes by the packaged process to HKCU are copy-on-write into a private per-package hive and HKLM writes are not applied to the machine hive. Windows, Explorer, services and other apps never see them, so a tweak reports success but changes nothing.
2. Files written under `%LOCALAPPDATA%` by the package go to its private store and disappear at uninstall, while the elevated children (`powershell.exe`, `reg.exe`, `pnputil.exe`, `powercfg.exe`) are system tools that act on the real file system and hive. The backup/undo records (`registry-backup.json`, `Undo\<scope>\*`) and the pending-restart records are shared between the UI process, the elevated child and, after an elevated restart, a second TweakHub process. If the two sides use different views, an applied change has no matching undo record and "restore" cannot find what to restore.
3. The elevated hash-checked script is written to a temp folder by the UI and read by the child; both must see the same file.

Suggested demonstration (test VM `NeuroTune-W11`, not run for this document): install the same package with virtualization enabled, apply one HKLM tweak and one HKCU tweak, then check with `reg query` from an unrelated process and compare `Store\Data\registry-backup.json` as seen by the app and by Explorer.

### 2.6 MSIX Flexible Virtualization
Evaluated on paper only (Microsoft's "flexible virtualization" `ExcludedKeys`/`ExcludedDirectories`, which per Microsoft's documentation also needs `unvirtualizedResources`). No prototype was built or tested.

- **Filesystem:** narrowing is feasible. The set of paths is small and known: `%LOCALAPPDATA%\TweakHub`, `%PROGRAMDATA%\TweakHub-*`, `%SystemRoot%\Temp`, user temp, the GPU shader cache folders and the hosts file. This is **proposed separately and not applied**; it would need the MSIX schema for `virtualization:FileSystemWriteVirtualization` in the manifest and a clean-VM test of every elevated flow.
- **Registry:** a fixed key list does not hold, because the target set is open-ended and user-controlled: custom registry tweaks and imported playbooks accept any HKLM/HKCU path (`CustomRegistryTweak`, `PlaybookService`); MSI/affinity writes go under `Enum\<instance id of whichever device the user picked>`; policy reset walks all of `HKLM\SOFTWARE\Policies`; the Startup page restores arbitrary Run keys, services and tasks. An exclusion list would silently disable these features for any key not in the list, which is the failure described in 2.5.

### 2.7 `unvirtualizedResources` only for documented, user-chosen operations
Yes, by design and by the code paths found: every write in 2.3/2.4 starts from a user action (button, confirmation dialog or an imported playbook that the user confirms and, for administrator steps, re-confirms after an elevated restart with a fingerprint check). There is no background writer, no start-up registration, no service, no auto-run task and no update self-installation in the Store build. `ElevationHardeningTests` and `StorePackageTests` cover the elevation and package rules. The test suite does not prove that no other path exists; this is a code review, not a proof.

### 2.8 Downloaded executables and tools

| What | Source domain | Saved to | Validation | Updates | User action |
|---|---|---|---|---|---|
| TweakHub update (EXE build) | `api.github.com`, `github.com` release assets | temp/download folder, held open until run | SHA-256 from the release's `.sha256` asset; asset URLs validated | **Disabled in the Store build**: `UpdateService` returns before any network work and, on request, opens the Microsoft Store page | confirmation dialog |
| Tron (OSINT/cleanup tool) | `bmrf.org/repos/tron/` | `Store\Data\Tools\Tron`; run copy in `%PROGRAMDATA%\TweakHub-Tron-<guid>` (admin-only ACL) | SHA-256 and size from the publisher's `sha256sums.txt` over HTTPS (no redirects); hash re-checked after copy; settings checked (no auto-run/email/upload/reboot) | Latest listed version is offered; re-download manual | separate steps: download, extract, run (UAC) |
| Microsoft Safety Scanner | `go.microsoft.com/fwlink/?LinkId=212732` | temp folder | Authenticode `Valid` and subject `O=Microsoft...` required | manual | user starts it |
| Marius Heier latency scripts | `tools.mariusheier.com` | temp, deleted before run | Script downloaded once; SHA-256 shown; consent stored **per hash**; elevated window refuses a changed script | re-consent if the script changes | consent dialog + UAC |
| winget packages | Windows Package Manager (`winget`) | managed by winget | winget's own manifest hashes | winget | user picks the package |
| External tool catalogue entries (vendor sites) | vendor domains (e.g. `asus.com`, `msi.com`, `gigabyte.com`, `techpowerup.com`, `hwinfo.com`) | **not downloaded by TweakHub**: the link opens in the user's browser | the browser and the vendor | n/a | user clicks |
| Gaming Repair Tool (`microsoft-store-repair`, `gaming-services-repair`) | `aka.ms/GamingRepairTool` (Microsoft) | temp | `Get-AuthenticodeSignature` checked in the script | manual | user starts it |
| `ctt-winutil` (Chris Titus WinUtil) | `christitus.com/win` | not saved: `irm … \| iex` | **No hash and no signature**; the remote script can change | manual | confirmation dialog warns "downloads and executes a remote script that can change over time", plus UAC |

No tool is downloaded or run at install time or in the background.

### 2.9 What remains after uninstall, where it is stated, how to restore and clean up
Uninstalling an MSIX removes the package files and its data inside the package container. It does **not** revert anything TweakHub applied to Windows. Remaining items and removal:

| Remains | Location | How to remove |
|---|---|---|
| Applied tweaks, policies, service start types, power plan values, hosts entries | Windows | Restore from the app **before** uninstalling (every tweak has Undo/Restore; policies page can reset). After uninstall, change them by hand or reinstall and restore. |
| Backups, undo records, logs, settings, downloaded tools | `%LOCALAPPDATA%\TweakHub` (`Store\Data`, `Store\Apps`, `Store\Machine`, `crash.log`) | Delete the folder after restoring |
| Driver backups | `%PROGRAMDATA%\TweakHub-DriverBackups` (admin-only) | Restore drivers first if needed (`driver-restore` script), then delete the folder as administrator |
| Tron run copy | `%PROGRAMDATA%\TweakHub-Tron-*` | Delete as administrator; older copies are removed on the next Tron run |
| Packages installed by winget through TweakHub | Windows | `winget uninstall` or Settings → Apps |

Where declared: `packaging/msix/README.md` and `docs/PRIVACY.md` ("Data stored locally", and the new "Uninstalling TweakHub" section added with this document). **It is not yet shown inside the app or in the Store listing**; see Gaps.

---

## 3. Gaps found

| # | Gap | Status |
|---|---|---|
| G1 | Uninstall behaviour (changes persist, where data lives, how to remove) was in `packaging/msix/README.md` and one line of `PRIVACY.md`, not as a clear user-facing section | **Fixed in docs**: new "Uninstalling TweakHub" section in `docs/PRIVACY.md`. Still to do by Lorenzo: repeat two sentences in the Store listing description; an in-app notice would need six localised strings (not small, not done). |
| G2 | Support contact in `docs/PRIVACY.md` and `packaging/msix/README.md` was GitHub Issues only (not accepted by Microsoft as support contact) | **Fixed**: email and website added; GitHub kept as secondary channel for bug reports. The manifest has no support field. The Store listing's support contact lives in Partner Center and was not touched or checked. The in-app About page links to the GitHub repository and Discord, not labelled as support; no change made. |
| G3 | `ctt-winutil` runs `irm christitus.com/win \| iex` with no hash or signature. It is behind a confirmation dialog and UAC, but it is remote code that can change, which a reviewer may flag independently of the capabilities | **Not changed** (removing a feature from the Store build is a product decision). Recommended: hide this one card when `StoreDistribution.IsStoreBuild`. One-line change plus a test, would need a new package version. |
| G4 | Tron's expected hash comes from the same publisher host as the file (integrity against transport/corruption, not against a compromised publisher) | Documented; no change. |
| G5 | Elevated-virtualization failure (2.5) is argued, not demonstrated by a stored test | Open: run the VM demonstration before relying on it in an appeal. |
| G6 | The MSIX README says "Public support: GitHub Issues" and "Output: artifacts/msix/1.7.0.0" (outdated example version) | Support line fixed; version example left as is. |

No code or manifest was changed, so the package is unchanged: `artifacts/msix/1.8.1.0/TweakHub_1.8.1.0_x64-Store.msix` (SHA-256 in `SHA256SUMS.txt` next to it).

## 4. Not verified
- Microsoft's current wording of 10.6.3 and the exact limit of the justification field (the paste-ready text is ~2,000 characters).
- Behaviour of the elevated child's package context and virtualization on a real install (2.5).
- Flexible Virtualization schema, minimum OS build and capability requirement beyond what Microsoft's documentation (linked in `packaging/msix/README.md`) states.
- The Store listing (description, support contact, privacy URL) as currently entered in Partner Center.
- The registry list in 2.1 comes from a static scan of string literals; keys built at run time (device ids, user input) are described by category.
