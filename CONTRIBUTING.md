# Contributing to TweakHub

Thanks for helping improve TweakHub. This page is the short version of what maintainers expect: how to build and test, how the translations and catalogs are wired, the rules for tweaks, and how to report hardware problems. By contributing you agree that your work is released under the [MIT license](LICENSE).

## Before you start

- **Windows 11 (build 22000 or newer) and the .NET 10 SDK** are the supported development environment (see [Build from source](README.md#build-from-source)).
- Search the [existing issues](https://github.com/PrimeBuild-pc/TweakHub/issues) before opening one, and keep a pull request focused on a single change.
- Generated binaries belong in [Releases](https://github.com/PrimeBuild-pc/TweakHub/releases), not in Git; `bin/`, `obj/` and `artifacts/` are ignored by `.gitignore`.

## Build and test

```powershell
dotnet restore TweakHub.sln
dotnet build TweakHub.sln -c Release --no-restore
dotnet test TweakHub.Tests/TweakHub.Tests.csproj -c Release --no-build --no-restore
```

These are the exact commands CI runs on every push and pull request to `main` (`.github/workflows/ci.yml`), so a change that builds and tests clean locally is what gets merged. The suite is NUnit, in `TweakHub.Tests/`. Because the projects target `net10.0-windows10.0.22000.0`, `dotnet test` on a host older than Windows 11 build 22000 discovers the tests but runs none and reports `No test is available`.

The hardware collector has its own dependency-free checks:

```powershell
powershell.exe -NoProfile -File Tests/HardwareCollector.Tests.ps1
```

## Translations (six languages)

The interface and installer ship in English, Russian, Simplified Chinese, Spanish, Italian and Japanese (`Localization/Localization.cs` keeps the supported list).

- Strings live in `Resources/<Domain>.resx` (English, the neutral resource) and `Resources/<Domain>.<culture>.resx` for `ru`, `zh-CN`, `es`, `it` and `ja`. The domains are `UI`, `Tweaks`, `Tools`, `Scripts`, `Security`, `Devices` and `Maintenance`.
- Code reads them with `L.Get("Domain:Key")` (or `L.Format` for placeholders); XAML uses `{loc:Loc Domain:Key}`.
- A new string must be added to the English file **and to all five translations**. `TweakHub.Tests/LocalizationTests.cs` fails the build when a translation has missing or extra keys, blank values, mismatched `{0}` placeholders or `|` file-filter separators, invisible characters, or English text that is not a name or technical term on the `SameAsEnglish` allow list.
- Translated text should read like the rest of the app: sentence case, no trailing period on button labels, and product/technical names (for example `WinGet`, `NVMe`) stay in English.

## Adding an External Tools card

The catalog is a curated list in `Services/ShortcutService.cs`, inside `LoadExternalTools()`. Add one entry:

```csharp
new ExternalTool { Name = "Example Tool", Description = L.Get("Tools:DescriptionExampleTool"), Category = "System Utilities", WingetId = "Vendor.ExampleTool" },
```

- `Name` must be unique, and `Category` must be one of the categories accepted by `TweakHub.Tests/ShortcutServiceTests.cs` (for example `System Utilities`, `CPU & Memory`, `Benchmarks & Stability`, `OSINT & Security`).
- Each card carries **either** a curated `WingetId` **or** an HTTPS `DownloadUrl` — exactly one of the two. Pages must be the tool's official HTTPS site; no `.ps1` links, no `irm | iex` scripts.
- `Description` goes through `L.Get("Tools:...")`, so add the key to `Resources/Tools.resx` and to the five translated `Tools.*.resx` files.
- `ShortcutServiceTests` also pins the catalog size (100–200 tools) and the exact URL or WinGet id of a few cards, so run the test suite after editing.

## Tweaks need preview, backup and rollback

Built-in tweaks live in `Services/TweakService.cs` and every one of them must follow the rule the README makes to users:

1. **Preview** — the user sees the exact values that will change before anything is applied.
2. **Backup** — the original state is saved automatically (`RegistryService` writes it into `Data/registry-backup.json`, power and Windows Update changes keep their own backups).
3. **Verification and rollback** — the change is verified and can be undone, individually or through **Restore All Changes**, on the same screen.

A tweak that cannot be restored does not belong in the built-in catalogue; put it under Automated Scripts with a clear warning instead. Also:

- Set `RiskLevel` (1–5) so the risk badge and the risk filter can describe it.
- `TweakHub.Tests/TweakCatalogueTests.cs` pins the approved list of tweak ids — add the new `Id` there in the same change.
- Follow the existing naming: `snake_case` ids, localized `Name`/`Description` keys in all six languages.

## Reporting a hardware problem

Device detection, motherboard wiring and driver reports are most useful with a hardware report:

1. Run [`Scripts/Collect-TweakHub-Hardware.cmd`](Scripts/Collect-TweakHub-Hardware.cmd) — it changes nothing and uploads nothing, and creates a `TweakHub-Hardware-*.json` file on the Desktop.
2. Read [docs/hardware-reports.md](docs/hardware-reports.md) for what the report contains, its privacy limits and the copy-paste Discord message.
3. Review the JSON yourself and share **only that file**, preferably privately.

For a regular bug, include the TweakHub version, the affected tab, what you expected, what happened, and a screenshot taken after pressing **Scan**.

## Pull requests

- Branch from `main` and keep the diff limited to the change you describe.
- Build and test with the commands above before pushing; CI runs the same three commands on your pull request.
- Match the style of the surrounding code and documentation: C# with `Nullable` enabled and implicit usings, four-space XAML indentation with aligned attributes, and Markdown that links to real files in the repository.
- In the pull request body, list the commands you ran and their results, and reference the issue number.
