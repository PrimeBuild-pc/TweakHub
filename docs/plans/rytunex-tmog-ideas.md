# Ideas from RyTuneX and TMOG (2026-10-20)

Sources studied:
- RyTuneX (AGPL-3.0, WinUI 3): source read, no code copied. Only behaviour ideas are reused.
- TMOG (closed source, C++/Win32): installer unpacked with innounp, static import/string analysis of `Task Manager.exe`
  (REA skill workflow without a native engine). Findings: PDH English counters, CallNtPowerInformation per-core MHz,
  SRUM read through ESENT on a VSS copy, Wait Chain Traversal, IP Helper connection tables, `StartupApproved` toggles,
  ACPI thermal zones captioned as firmware sensors, and a "never show a zero it did not measure" rule.

## Already in TweakHub (no work)
Startup toggles via `StartupApproved`, connections with signatures, DISM/SFC/CHKDSK, WinGet catalogue, power-plan editor,
temp cleanup.

## Implemented
1. **Tweaks reverted by Windows** (RyTuneX re-applies after a Windows build change). TweakHub knows better than a build
   number: a tweak with a TweakHub backup that is no longer in effect was changed by someone else. The Registry tweaks page
   shows a banner listing them with one **Reapply** button. A backup alone is not proof (the Backup button and cancelled
   applies create one), so a successful apply is recorded in `Data\applied-tweaks.json` and removed on restore. Partially applied tweaks are not counted (they already show their
   own state).
2. **Driver backup** (RyTuneX System Info "export drivers"). Built-in script: `pnputil /export-driver *` into
   `Data\Drivers\<PC>-<date>` in portable builds (the backup travels on the USB stick), `Documents\TweakHub\Drivers`
   in installed builds (not the roaming AppData profile: a backup is 1–3 GB).
3. **GPU shader cache cleanup** (RyTuneX Deep Clean, safe subset only). DirectX, AMD, NVIDIA and Intel shader caches of the
   current user; files in use are skipped. No Prefetch, Windows Update cache or Winsock reset.

## Skipped on purpose
Stress test engine, SRUM history, wait-chain analysis (out of TweakHub scope), "intelligent" scoring.
