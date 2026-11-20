# TweakHub: Microsoft Store MSIX build

This is a first certification candidate with the current administrative features. Building, schema validation and local signatures do not establish Store approval or successful deployment. No upload or submission is part of this script.

## Identity and support

Microsoft assigns the technical identity values in Partner Center.

- Package name: `LorenzoMassafra.TweakHub`
- Publisher: `CN=ACD03D0F-7A52-4C93-ADF8-A07702180E72`
- Publisher display name: **Prime Build**
- Package family: `LorenzoMassafra.TweakHub_2j208vxf88tt6`
- Product ID: `9P5R2R5TRKL3`
- Public support: [GitHub Issues](https://github.com/PrimeBuild-pc/TweakHub/issues).

Technical identity strings are assigned by Microsoft and are preserved. Do not replace them with the public brand name.

## Rebuild

From the repository root, run Windows PowerShell 5.1 on Windows 11 with .NET SDK **10.0.403** and Windows SDK **10.0.26100.0** (x64 MakeAppx, SignTool and mt.exe). `identity.json` pins toolchain and runtime versions. `packages.lock.json` pins NuGet dependency versions and content hashes.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\packaging\msix\Build-Msix.ps1 -SignForTest
```

Omit `-SignForTest` to produce only the unsigned Store artifact. Use `-OutputDirectory <path>` for a separate rebuild. `-UpdateLockFile` is only for an intentional dependency/toolchain update; normal builds enforce locked restore. The package version is the stable project version plus `.0` (the Store reserves the last component).

This is Release, win-x64, self-contained .NET 10.0.14, untrimmed, with separate native/runtime files, no ReadyToRun or native self-extraction. No machine-wide .NET or Windows App SDK installation is required. Windows 11 build 22000 is the minimum. `MaxVersionTested=10.0.26100.0` is manifest metadata, not evidence of an OS test in this session.

Output: `artifacts/msix/1.7.0.0/`, containing `*-Store.msix`, optional `*-Test.msix`, public `*-Test.cer`, `SHA256SUMS.txt` and a build receipt. These are actual MakeAppx-generated MSIX files; no installer is renamed and no sparse/external-location package is used. A plain `.msix` is a supported submission artifact; `.msixupload` is not generated.

Two independent builds were compared: all uncompressed entries, including executable bytes, runtime/native DLLs, manifest, icons and block map, match. MakeAppx writes current ZIP timestamps, so the container hash changes between runs even after input timestamp normalization. The build is reproducible in payload, **not bit for bit at container level**. Test signatures additionally change with each generated certificate/signing time. Do not expect the delivered SHA-256 after a fresh build.

## Capabilities and Microsoft approval

`runFullTrust`, `allowElevation`, `unvirtualizedResources` are explicitly declared. The app runs as a packaged classic desktop app. `mediumIL` describes the full-trust app type; it does not cancel the executable's UAC manifest or runtime elevation requests.

- `runFullTrust`: required for the existing desktop application and child-process operations.
- `allowElevation`: preserves the app's current launch/runtime elevation. Microsoft requires advance justification and approval under strict criteria. Follow the contact route in the official capability documentation; no approval or contact has been made by this build.
- `unvirtualizedResources`: both RegistryWriteVirtualization and FileSystemWriteVirtualization are disabled. HKCU tweaks must affect the actual operating system rather than a private package hive; state and undo files must be visible to the existing elevated/non-elevated child processes and survive uninstall. This is another restricted capability requiring review; Microsoft explicitly warns approval is uncommon. Do not remove it merely to hide real behavior.

System changes and external data persist after package removal. Uninstalling MSIX does not revert tweaks, power plans, downloaded tools, scheduled tasks or global backups. Restore the changes through the app first, save evidence/backups, then uninstall and reset the test VM.

## Channel-specific behavior

- The embedded EXE manifest remains `asInvoker`; selected operations still use UAC/`runas`, and an elevated restart retains the same compiled Store channel.
- Store paths are `%LOCALAPPDATA%\TweakHub\Store\Data` (settings, profiles and undo files), `...\Apps` (downloaded portable tools), and `...\Machine` (restart verification). `portable.flag` is omitted from the package and cannot enable portable mode in this build.
- PowerShell context (`TWEAKHUB_ROOT`, `TWEAKHUB_APPS` and working directory) uses the writable Store root. Bundled scripts are read from the package by absolute executable-relative paths. Temporary scripts, administrator-only verified execution folders and existing integrity checks are preserved.
- **Import from another copy** can import a portable or installed data folder. It validates profiles, saves the current profile as a recovery copy, imports undo records scoped to this PC and account, and copies missing portable tools. Older undo records require confirmation of their origin. The import preserves the source files and populated destination backups.
- Global driver backups remain in `%PROGRAMDATA%\TweakHub-DriverBackups`; the crash log remains in `%LOCALAPPDATA%\TweakHub\crash.log`. User-selected exports/driver backups still use their chosen locations.
- The EXE updater returns before network/download/install work. An explicit update check opens the product's Microsoft Store page.
- Optional AMD ADLX integration needs `amdadlx64.dll` from the AMD graphics driver; it is not a bundled prerequisite. Test the existing fallback on systems without that driver. Downloaded tools and remote scripts remain available with their existing confirmation/integrity behavior; these features are part of the certification experiment.

The EXE/portable build defaults and existing installer workflows are preserved; Store behavior is selected only with `StorePackage=true` / `MSIX_STORE`.

## Signing and validation

Microsoft signs Store-distributed MSIX packages. Use the unsigned `*-Store.msix` artifact for Store submission.

The optional test copy uses a one-year, self-signed RSA/SHA-256 code-signing certificate with the exact Publisher subject. The build creates it temporarily in **CurrentUser\My**, never in host trust stores. Its non-exportable private key and certificate are deleted in `finally`; no PFX/private key is delivered or stored in the repo. Only the public CER is exported. The test signature has no timestamp and is usable only while the certificate is valid. Trust the CER **only inside the disposable VM**.

MakeAppx runs with schema/semantic validation enabled. `mt.exe` checks the embedded EXE launch level, and the build compares file/package/runtime versions. `Test-Package.ps1` verifies identity, display name, architecture, capability declarations, unvirtualized writes, executable/launch asset presence, icon dimensions, self-contained dependencies and every SHA-256 block in AppxBlockMap. For test packages it verifies the CMS signature and checks the Windows APPX SIP result; an untrusted-root result is expected on the host. This is not a full Windows App Certification Kit run or installation test.

```powershell
.\packaging\msix\Test-Package.ps1 -PackagePath .\artifacts\msix\1.7.0.0\TweakHub_1.7.0.0_x64-Store.msix
.\packaging\msix\Test-Package.ps1 -PackagePath .\artifacts\msix\1.7.0.0\TweakHub_1.7.0.0_x64-Test.msix -CertificatePath .\artifacts\msix\1.7.0.0\TweakHub_1.7.0.0_x64-Test.cer
dotnet test .\TweakHub.Tests\TweakHub.Tests.csproj -c Release
dotnet test .\TweakHub.Tests\TweakHub.Tests.csproj -c Release -p:StorePackage=true --filter 'FullyQualifiedName~StorePackageTests'
```

Before Store submission, run the Windows App Certification Kit and verify package launch, data migration, updates, uninstall, and elevated operations on a clean test system.

## Microsoft references

- [Generate desktop MSIX components](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion)
- [MakeAppx and its limited validation](https://learn.microsoft.com/en-us/windows/msix/package/create-app-package-with-makeappx-tool)
- [Restricted capabilities, including elevation](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations)
- [Virtualization and persistent system-visible state](https://learn.microsoft.com/en-us/windows/msix/desktop/flexible-virtualization)
- [Store signing and local test signing](https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview)
