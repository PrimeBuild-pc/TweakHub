# TweakHub: Microsoft Store capability justification (policy 10.6.3)

Product ID `9P5R2R5TRKL3` · package `LorenzoMassafra.TweakHub` · package for resubmission: `1.8.3.0` (x64, manifest `packaging/msix/AppxManifest.xml`, unchanged since 1.8.1). 1.8.2.0 ran WinUtil through the hash-approved flow (gap G3); 1.8.3.0 adds the in-app uninstall notice and the support link (gaps G1, G2). The Microsoft report of 24 Nov 2026 denied `allowElevation` and `unvirtualizedResources` and asked the questions answered in section 2.
Support: `primebuild.official@gmail.com` · support page `https://primebuild.website/apps/tweakhub/support/` · public copy of this document `https://primebuild.website/apps/tweakhub/store-justification/` · source: `https://github.com/PrimeBuild-pc/TweakHub` (MIT).

Everything below was read from the code and manifest of this repository. Where something was **not** tested or is not true, it says so (see "Not verified" and "Gaps found").

---

## 1. Text entered in Partner Center

Each restricted-capability field accepts at most **500 characters**, so the fields carry a summary and the link to the public copy of this document; the answers to every question go in "Notes for certification" (limit 6,000). Links point to primebuild.website, not GitHub, as the report requires.

**runFullTrust** (444 characters)

> TweakHub (WPF/.NET) is a Windows 11 tuning and diagnostics utility. It changes real Windows settings the user selects (registry, services, power plans, drivers, hosts file) through Win32 APIs and system tools a UWP app cannot call, so it runs as a full-trust packaged desktop app. No service, driver, scheduled task or auto-start is installed. Full 10.6.3 answers with VM test logs: https://primebuild.website/apps/tweakhub/store-justification/

**allowElevation** (494 characters)

> The manifest is asInvoker; the app runs at medium IL and nothing is elevated at start-up. Only after the user clicks and confirms an operation that needs admin rights, TweakHub starts one child (reg.exe, powercfg.exe, a hash-checked powershell.exe script or an elevated restart of itself) with the standard UAC prompt. Scripts run only if their SHA-256 matches what the user approved. No silent elevation, service or elevated task. Details: primebuild.website/apps/tweakhub/store-justification/

**unvirtualizedResources** (494 characters)

> Package-wide (desktop6 registry and file-system write virtualization disabled). Tested on Win11 25H2: with default virtualization HKCU tweaks land in the private package hive and the backup goes to LocalCache, so the elevated child cannot read it and undo is lost. Flexible Virtualization fixes files but only listed HKCU keys; custom tweaks and playbooks use any HKCU path. Keys, paths, logs: https://primebuild.website/apps/tweakhub/store-justification/ Support: primebuild.official@gmail.com

**Notes for certification** (4497 characters)

```text
TweakHub 1.8.3 - notes for certification
Support: primebuild.official@gmail.com - support page: https://primebuild.website/apps/tweakhub/support/
Full answers to the 10.6.3 questions, with VM test logs: https://primebuild.website/apps/tweakhub/store-justification/

Changes since the 24 Nov 2026 report: in-app "Before you uninstall" notice (About page); support link to primebuild.website; reproducible VM tests of default and Flexible Virtualization (points 5 and 6 below).

Safe smoke test: start TweakHub without elevation; open Devices, Links and Security to view inventory and status; open a Registry tweak preview without applying it; open Power Plan and About. To see elevation, apply one HKLM registry tweak, accept UAC, then restore it from the app. Use a disposable VM for anything else; do not run firmware, Tron or destructive cleanup scripts for a smoke test. The Store build has no self-update (it opens the Store page).

Answers to the 10.6.3 questions
1-2. Virtualization is disabled package-wide (desktop6 RegistryWriteVirtualization and FileSystemWriteVirtualization = disabled); no ExcludedKeys or ExcludedDirectories. Targets written: HKLM\SOFTWARE\Policies\..., HKLM\SOFTWARE\Microsoft\Windows and Windows NT\CurrentVersion\..., HKLM\SYSTEM\CurrentControlSet\Control\..., \Services\<name> (start type), \Enum\<device>\Device Parameters\Interrupt Management (MSI, affinity); HKCU\Control Panel\Desktop and \Mouse, HKCU\Software\Microsoft\Windows\CurrentVersion\..., HKCU\Software\Microsoft\GameBar, HKCU\System\GameConfigStore, HKCU\Software\Microsoft\DirectX, plus custom tweaks the user defines. Files: %LOCALAPPDATA%\TweakHub (settings, backups, undo, log, tools), %PROGRAMDATA%\TweakHub-DriverBackups and TweakHub-Tron-*, temp folders, the hosts file (Adobe block script only), GPU shader caches (cleanup).
3. The packaged UI (medium IL) writes its own data and HKCU values. One elevated child per confirmed operation writes HKLM, services, power plans, driver store and hosts. Tested: the elevated child has no package identity (GetCurrentPackageFullName returns 15700).
4. Yes, all user-initiated and previewed, registry values backed up first: the HKLM keys above; service start types; scheduled tasks only when the user disables or restores one on the Startup page (plus one temporary limited task for a winget uninstall, deleted after use); nothing in Program Files; under Windows: temp, DISM/SFC, hosts file, driver store via pnputil; third-party data: only GPU shader caches and temp, no browser profiles. Policy reset can remove third-party policy keys the user selects, after a backup.
5. Reproduced on Windows 11 25H2 (26200): the same payload packed with three manifests, a probe run inside the package doing what TweakHub does. With default virtualization the HKCU writes never reach the real hive (private package User.dat), the backup record goes to Packages\<PFN>\LocalCache, the elevated child cannot see it, and the undo record it writes is lost. With virtualization disabled every write is visible and backup/undo is shared.
6. Flexible Virtualization prototype tested: ExcludedDirectory $(KnownFolder:LocalAppData)\TweakHub fixes the shared backup/undo; ExcludedKey works only for listed HKCU keys (GameBar reached Windows, Control Panel\Mouse did not); HKLM keys are rejected by the schema. TweakHub's HKCU targets are open-ended (custom tweaks, imported playbooks, restored Run keys), so a fixed list would silently break them.
7. Yes. Every write starts from a user action and confirmation. No background writer, service, auto-start or self-update in the Store build.
8. Downloads happen only on user action: Tron from bmrf.org (SHA-256 from the publisher's list, re-checked after copy); Microsoft Safety Scanner from go.microsoft.com and the Gaming Repair Tool from aka.ms (Microsoft Authenticode required); Marius Heier scripts from tools.mariusheier.com and WinUtil from christitus.com, redirected to github.com (SHA-256 shown, consent per hash, changed file refused; WinUtil can fetch more at run time, which TweakHub does not check); winget packages (winget hashes). Stored in temp or %LOCALAPPDATA%\TweakHub\Store\Apps. No automatic updates.
9. Uninstall removes the app only; Windows changes stay. Stated in the app (About, "Before you uninstall"), on the support page and in the privacy policy: restore from the app first, then delete %LOCALAPPDATA%\TweakHub and the %PROGRAMDATA%\TweakHub-* folders; remove winget apps with winget uninstall.
```

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
| Elevated child (one per user-confirmed operation, UAC every time) | `reg.exe add/delete` for HKLM and `HKCU\Software\Policies` (`RegistryService.RunElevatedReg`); `powercfg.exe` (`PowerService.RunMutationAsync`); hash-checked PowerShell (`PowerShellService.ExecuteScriptAsync(requireAdministrator)`): services (`sc.exe config`, `Set-Service`), driver store (`pnputil`), power-plan values, hosts file, cleanup scripts, device enable/disable/removal; interactive scripts (`AutomatedScriptsPage`); Tron (`TronService`); third-party scripts, WinUtil included (`ThirdPartyScriptService`); installer of a TweakHub update (outside the Store build, see 2.8); elevated restart of TweakHub itself (`App.RestartElevated`, offered for bulk operations/playbooks) |

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
**Reproduced on 27 Nov 2026** on the test VM `NeuroTune-W11` (Windows 11 Pro 25H2, build 26200.9974, account with UAC). Evidence, scripts and manifests: [`docs/store-evidence/`](store-evidence/).

Method. The 1.8.2.0 Store payload (`dotnet publish -p:StorePackage=true`) was packed three times, changing only the manifest, and signed with a throw-away test certificate:

| Variant | Manifest |
|---|---|
| `Disabled` | the submitted manifest: `desktop6:RegistryWriteVirtualization` and `FileSystemWriteVirtualization` = `disabled`, `unvirtualizedResources` |
| `Default` | standard MSIX virtualization: no `desktop6` switches, no `unvirtualizedResources` |
| `Flex` | Flexible Virtualization: `ExcludedDirectory $(KnownFolder:LocalAppData)\TweakHub`, `ExcludedKey HKEY_CURRENT_USER\Software\Microsoft\GameBar`, `unvirtualizedResources` |

A probe (`probe.ps1`) was started **inside** each package with `Invoke-CommandInDesktopPackage` from a non-elevated shell, so it ran with the package identity at medium integrity, as TweakHub's UI does. It performs TweakHub's own sequence: two HKCU writes (`GameBar`, `Control Panel\Mouse`), the backup record in `%LOCALAPPDATA%\TweakHub\Store\Data`, a script file in `%TEMP%`, then one elevated child through `runas` (UAC accepted), which runs `reg.exe add HKLM\...`, reads the backup, writes the undo record and runs the temp script. `verify.ps1` then checked everything from an ordinary process outside the package. The TweakHub UI itself was not driven; the probe uses the same mechanisms (package identity, `RegistryService`'s direct HKCU write, `runas` child) so the result can be repeated by anyone with the scripts.

Results (screenshots `vm-*.png`):

| Check, seen from outside the package | Disabled | Default | Flex |
|---|---|---|---|
| HKCU `Software\Microsoft\GameBar` value written by the UI | in Windows | **missing** | in Windows (excluded key) |
| HKCU `Control Panel\Mouse` value written by the UI | in Windows | **missing** | **missing** |
| Private package hive `SystemAppData\Helium\User.dat` | not created | created | created |
| HKLM value written by the elevated child | in Windows | in Windows | in Windows |
| Backup record written by the UI | real path | **only in `Packages\<PFN>\LocalCache\Local\TweakHub\...`** | real path |
| Elevated child can read that backup | yes | **no** | yes |
| Undo record written by the child, seen by the UI | yes | **no (lost: the folder exists only in LocalCache)** | yes |
| Script in `%TEMP%` written by the UI and run by the child | yes | yes | yes |
| Package identity of the elevated child | none (`GetCurrentPackageFullName` = 15700) | none | none |

What this means for TweakHub with standard virtualization:
1. Every HKCU tweak applied directly by the UI reports success but never reaches Windows: it lands in the package's private hive.
2. The registry backup and undo records are split between the package's private copy and the real folder. The elevated child cannot read the backup taken before a change, and the undo record it writes is lost, so "restore" cannot work.
3. HKLM changes are **not** affected: the elevated child runs outside the package. Earlier versions of this document said HKLM writes would be lost; the test shows they are not.
4. `%TEMP%` (an existing folder) was not redirected in this test.

Not tested: the elevated restart of TweakHub itself (`App.RestartElevated`) and whether that second process keeps the package identity.

### 2.6 MSIX Flexible Virtualization
**Prototype built and tested** (variant `Flex` above, plus two schema checks):

- **Filesystem: sufficient.** One `ExcludedDirectory`, `$(KnownFolder:LocalAppData)\TweakHub`, made backup and undo records shared between the UI and the elevated child (table above).
- **Registry: only for keys listed in advance.** The excluded `HKCU\Software\Microsoft\GameBar` reached Windows; `HKCU\Control Panel\Mouse`, not listed, stayed in the private hive.
- **HKLM cannot be listed.** A manifest with `ExcludedKey HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows` is rejected by MakeAppx (`C00CE169`, pattern `HKEY_CURRENT_USER\...` or `HKCU\...`; `manifests/FlexHklm.makeappx-error.txt`). HKLM does not need it anyway, since only the elevated child writes there.
- **Without `unvirtualizedResources`.** The same `Flex` manifest without the capability packed, installed (sideloaded, trusted test certificate) and behaved the same (`vm-flex-without-capability.png`). Whether Store ingestion accepts Flexible Virtualization without the capability was not tested.

Why a key list is still insufficient for TweakHub: the HKCU paths written by the UI are open-ended and user-controlled. Custom registry tweaks and imported playbooks accept any HKCU path (`CustomRegistryTweak`, `PlaybookService`), and the Startup page restores arbitrary Run keys. Any key not in the list would be silently written to the private hive, which is failure 1 in 2.5. The built-in catalogue alone could be listed (`HKCU\Control Panel\Desktop`, `\Mouse`, `HKCU\Software\Microsoft\Windows\CurrentVersion\...`, `GameBar`, `HKCU\System\GameConfigStore`, `HKCU\Software\Microsoft\DirectX`), so a Flexible Virtualization manifest is a possible fallback if the package-wide exclusion is refused, at the cost of custom HKCU tweaks in the Store build.

### 2.7 `unvirtualizedResources` only for documented, user-chosen operations
Yes, by design and by the code paths found: every write in 2.3/2.4 starts from a user action (button, confirmation dialog or an imported playbook that the user confirms and, for administrator steps, re-confirms after an elevated restart with a fingerprint check). There is no background writer, no start-up registration, no service, no auto-run task and no update self-installation in the Store build. `ElevationHardeningTests` and `StorePackageTests` cover the elevation and package rules. The test suite does not prove that no other path exists; this is a code review, not a proof.

### 2.8 Downloaded executables and tools

| What | Source domain | Saved to | Validation | Updates | User action |
|---|---|---|---|---|---|
| TweakHub update (EXE build) | `api.github.com`, `github.com` release assets | temp/download folder, held open until run | SHA-256 from the release's `.sha256` asset; asset URLs validated | **Disabled in the Store build**: `UpdateService` returns before any network work and, on request, opens the Microsoft Store page | confirmation dialog |
| Tron (OSINT/cleanup tool) | `bmrf.org/repos/tron/` | `Store\Data\Tools\Tron`; run copy in `%PROGRAMDATA%\TweakHub-Tron-<guid>` (admin-only ACL) | SHA-256 and size from the publisher's `sha256sums.txt` over HTTPS (no redirects); hash re-checked after copy; settings checked (no auto-run/email/upload/reboot) | Latest listed version is offered; re-download manual | separate steps: download, extract, run (UAC) |
| Microsoft Safety Scanner | `go.microsoft.com/fwlink/?LinkId=212732` | temp folder | Authenticode `Valid` and subject `O=Microsoft...` required | manual | user starts it |
| Marius Heier latency scripts | `tools.mariusheier.com` | temp, deleted before run | Script downloaded once; SHA-256 shown; consent stored **per hash**; elevated window refuses a changed script | re-consent if the script changes | consent dialog + UAC |
| `ctt-winutil` (Chris Titus Tech WinUtil) | `christitus.com/win`, which redirects over HTTPS to the GitHub release asset `github.com/ChrisTitusTech/winutil/releases/latest/download/winutil.ps1` | temp, deleted before run | Same flow as the Marius Heier scripts (`ThirdPartyScriptService.ConfirmAndRunAsync`): downloaded once, SHA-256 shown, consent stored **per hash**, elevated window refuses a changed file. **The hash covers only the main script.** WinUtil itself can download more at run time (for example the packages and configurations the user picks in its window, installed through winget or Chocolatey); TweakHub does not check those. No signature check: the script is not Authenticode-signed | re-consent when a new WinUtil release changes the file | consent dialog (shows the hash) + UAC |
| winget packages | Windows Package Manager (`winget`) | managed by winget | winget's own manifest hashes | winget | user picks the package |
| External tool catalogue entries (vendor sites) | vendor domains (e.g. `asus.com`, `msi.com`, `gigabyte.com`, `techpowerup.com`, `hwinfo.com`) | **not downloaded by TweakHub**: the link opens in the user's browser | the browser and the vendor | n/a | user clicks |
| Gaming Repair Tool (`microsoft-store-repair`, `gaming-services-repair`) | `aka.ms/GamingRepairTool` (Microsoft) | temp | `Get-AuthenticodeSignature` checked in the script | manual | user starts it |

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

Where declared: in the app (Store build, About page, "Before you uninstall", six languages, since 1.8.3), on the support page `https://primebuild.website/apps/tweakhub/support/`, in the Store listing description, in `docs/PRIVACY.md` ("Uninstalling TweakHub") and in `packaging/msix/README.md`.

---

## 3. Gaps found

| # | Gap | Status |
|---|---|---|
| G1 | Uninstall behaviour was only in the repository docs | **Fixed in 1.8.3.0**: "Before you uninstall" notice on the About page of the Store build (6 languages), the support page on primebuild.website and the Store listing description. |
| G2 | Support contact was GitHub Issues only (not accepted by Microsoft) | **Fixed**: email and website in `docs/PRIVACY.md`, support page on primebuild.website, "Website and support" link on the About page (1.8.3.0), support contact and website in Partner Center. GitHub stays a secondary channel for bug reports. |
| G3 | `ctt-winutil` runs `irm christitus.com/win \| iex` with no hash or signature. It is behind a confirmation dialog and UAC, but it is remote code that can change, which a reviewer may flag independently of the capabilities | **Fixed in 1.8.2.0, in every build (Store and GitHub)**: the card no longer runs `irm \| iex`. It uses the Marius Heier flow: one download to a temporary file, SHA-256 shown, consent tied to that hash, UAC, and the elevated window refuses the file if it changed after approval. Remaining limit, stated in 2.8 and `docs/PRIVACY.md`: the hash covers only the main script, and WinUtil can download further components at run time. Covered by `ElevationHardeningTests.ThirdPartyScriptRunsOnlyTheApprovedCopy(true)`. |
| G4 | Tron's expected hash comes from the same publisher host as the file (integrity against transport/corruption, not against a compromised publisher) | Documented; no change. |
| G5 | Elevated-virtualization failure was argued, not demonstrated | **Closed 27 Nov 2026**: reproduced on the test VM (2.5) and Flexible Virtualization prototyped (2.6); evidence in `docs/store-evidence/`. |
| G6 | The MSIX README says "Public support: GitHub Issues" and "Output: artifacts/msix/1.7.0.0" (outdated example version) | Support line fixed; version example left as is. |

The manifest is unchanged. The package for resubmission is `TweakHub_1.8.3.0_x64-Store.msix`, built by the "Validate Microsoft Store package" workflow (`.github/workflows/store-package-preview.yml`, pinned .NET SDK 10.0.403) with `packaging/msix/Build-Msix.ps1`.

## 4. Not verified
- Microsoft's current wording of 10.6.3.
- The TweakHub UI was not driven on the VM; the probe in 2.5 reproduces its mechanisms inside the same package. The elevated restart of TweakHub itself was not tested.
- Whether Store ingestion accepts Flexible Virtualization without `unvirtualizedResources` (it works when sideloaded, 2.6). Behaviour on Windows builds older than 26200.
- The Store listing was checked in Partner Center on 27 Nov 2026 (see section 1 and the listing notes in the PR).
- The registry list in 2.1 comes from a static scan of string literals; keys built at run time (device ids, user input) are described by category.
