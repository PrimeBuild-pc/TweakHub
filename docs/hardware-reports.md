# Hardware reports for TweakHub

Distribute **only [`Collect-TweakHub-Hardware.cmd`](../Scripts/Collect-TweakHub-Hardware.cmd)**. It is a standalone file: its PowerShell payload is embedded, uses Windows PowerShell 5.1 and built-in Windows commands, and downloads nothing. Save the actual file, not an HTML preview or a `.cmd.txt` copy. Discord may require distributing batch files inside a ZIP; extract it before running.

## Instructions for participants

1. Connect the peripherals you want included. TweakHub and ThreadPilot do **not** need to be installed or running.
2. Double-click the CMD, read the notice, type `S`, and press Enter. Administrator privileges are not normally necessary; there is no automatic elevation or persistent execution-policy change.
3. Wait for completion. A `TweakHub-Hardware-<date>-<random>.json` file appears on the Windows Desktop (including a redirected Desktop).
4. Review that JSON in Notepad and share **only that file**, preferably privately with the developer. Nothing is uploaded automatically. Do not share the full TweakHub profile, Registry backups or unrelated application logs.

Windows security or an organization policy may block a downloaded batch/PowerShell script. Do not disable antivirus or bypass organization policies to run it; ask the developer or your administrator instead. If collection fails, send the console error. An unavailable/unwritable Desktop prevents saving; it does not silently save elsewhere.

## What the report contains

- Windows edition/build/architecture; system manufacturer and model.
- Motherboard manufacturer/product/revision; BIOS version/date.
- CPU model/core counts, RAM module models/capacities/speeds/slots, GPU models/drivers, disk models/firmware/capacities.
- All PnP devices, including disconnected/disabled devices, with status/problem codes, class, service, INF, driver provider/version/date.
- Report-local device and parent tokens, model-level hardware IDs for selected hardware buses, PCI/ACPI location paths, bus/address and PCIe current/maximum link generations and widths where Windows exposes them.
- Read-only PCI MSI, message limit, interrupt priority and affinity Registry values. An absent value is not interpreted as disabled or unsupported.
- Collection errors summarized by section and error type/category, without raw exception text that might contain identifiers or paths. A failed CIM section does not prevent collecting other sections; a failed/empty PnP inventory is a fatal error rather than a misleading success.

There is no intentional collection of usernames, computer names, IP/MAC addresses, serial numbers, UUIDs, original PnP instance IDs, personal files, installed application lists or running processes. Device descriptions come from drivers, not user-assigned friendly names; descriptions for software/Bluetooth and other non-allowlisted buses are omitted. Model hardware IDs have their instance/serial segment removed. Original instance IDs remain only in memory while building the report; the token mapping is never saved.

**This is data minimization, not guaranteed anonymity.** Hardware combinations, OEM model/description strings and device history can still identify a machine. Review before sharing and avoid public channels for reports. Tokens are not stable identifiers across scans and cannot be used directly to enable/disable a device.

## How to use reports

The JSON provides evidence for device matching rules, missing motherboard catalog entries, CPU/chipset topology heuristics, PCIe link handling, driver differences, and interrupt settings. Send reports from different motherboard/chipset generations, laptops and USB hub configurations, not just many identical PCs.

For a specific TweakHub bug, also include the **TweakHub version**, the affected tab, expected versus actual behavior, and a screenshot of the result after pressing **Scan**. The collector does not inspect TweakHub's UI, settings or backups. To compare two physical configurations, collect a report before and after moving the device and describe the slot/port used.

Limitations:

- This is a snapshot, not a latency/polling benchmark. No stress test is started, and PCIe power saving can lower the reported speed at idle.
- Some GPU links need vendor APIs (TweakHub uses AMD ADLX); this dependency-free collector does not call them.
- Negotiated USB speed needs native hub IOCTLs used by TweakHub, not available from this PnP inventory. For USB-speed bugs, include a screenshot of **Devices → Connections** too.
- Device topology cannot prove physical slot labels, motherboard lane sharing or BIOS bifurcation. Include the exact board revision and the manufacturer's manual/specification link when reporting wiring discrepancies.
- Missing or access-restricted properties remain null/empty. Admin execution may expose additional properties but is not required or requested automatically.

TweakHub's existing `Data/Logs` files are script/playbook output, and `operations.jsonl` records modifications, not a complete hardware scan. ThreadPilot is not needed for this workflow; installing another application merely to collect these fields adds unnecessary steps and its general logs are not a substitute for this report format.

## Copy-paste Discord message

> Ci aiutate a migliorare il riconoscimento hardware di TweakHub? Estraete `Collect-TweakHub-Hardware.cmd` dallo ZIP, fate doppio clic e confermate con S. Non serve installare/aprire TweakHub o ThreadPilot e normalmente non serve avviare come amministratore. Lo script legge soltanto informazioni hardware e crea `TweakHub-Hardware-....json` sul Desktop: non cambia impostazioni e non invia nulla. Non raccoglie intenzionalmente seriali, nome PC, account o indirizzi di rete; controllate comunque il JSON prima di condividerlo e inviatelo preferibilmente in privato. Se segnalate un problema, aggiungete versione di TweakHub, screenshot dopo Scan e cosa vi aspettavate di vedere.

## Verification

Run the dependency-free checks on Windows:

```powershell
powershell.exe -NoProfile -File Tests/HardwareCollector.Tests.ps1
```

They parse the embedded payload and check token stability, missing parents, hardware ID filtering/serial removal and privacy-preserving section errors. Hardware collection must also be tested on real Windows machines; unit checks cannot establish firmware/driver coverage.
