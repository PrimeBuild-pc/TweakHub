using System.IO;
using TweakHub.Localization;

namespace TweakHub.Services
{
    /// <summary>
    /// The built-in scripts: what each runs, whether it needs administrator rights, how many minutes it may take and
    /// whether playbooks may include it. The Scripts page and playbooks read this one table, so they cannot disagree.
    /// Playbooks take only unattended scripts; interactive, troubleshooting and reset scripts stay out.
    /// </summary>
    internal static class BuiltInScripts
    {
        public static readonly IReadOnlyDictionary<string, (bool Administrator, int Minutes, bool Playbook)> All =
            new Dictionary<string, (bool, int, bool)>
            {
                ["winget"] = (false, 15, false),
                ["gaming-runtimes"] = (false, 45, true),
                ["dotnet-developer-setup"] = (false, 120, true),
                ["ctt-winutil"] = (true, 30, false),
                ["change-local-password"] = (true, 10, false),
                ["dism-sfc-chkdsk"] = (true, 90, false),
                ["component-cleanup"] = (true, 60, true),
                ["temp-cleanup"] = (true, 30, true),
                ["shader-cache-cleanup"] = (false, 15, true),
                ["driver-backup"] = (true, 30, true),
                ["driver-restore"] = (true, 30, false),
                ["network-reset"] = (true, 15, false),
                ["windows-update-reset"] = (true, 20, false),
                ["microsoft-store-repair"] = (false, 20, false),
                ["gaming-services-repair"] = (true, 30, false),
                ["print-spooler-repair"] = (true, 10, false),
                ["onedrive-repair"] = (false, 15, false),
                ["wsl-setup"] = (true, 45, true),
                ["empty-standby-list"] = (true, 10, false),
                ["remove-windows-ai"] = (true, 30, true),
                ["adobe-hosts-block"] = (true, 10, true),
                ["adobe-hosts-unblock"] = (true, 10, false),
            };

        public static IEnumerable<string> PlaybookIds => All.Where(script => script.Value.Playbook).Select(script => script.Key);

        private static string LoadWinGetScript()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "WinGetInstall.ps1");
            return System.IO.File.Exists(path)
                ? System.IO.File.ReadAllText(path)
                : L.Get("Scripts:WingetScriptMissingPreview");
        }

        // The caller's folders, resolved before elevation. Portable: the backup travels with TweakHub on the USB drive;
        // installed: Documents, because gigabytes of drivers do not belong in the roaming AppData profile.
        internal static string DriverBackupPath => AppDataPath.IsPortable
            ? Path.Combine(AppDataPath.BasePath, "Drivers")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TweakHub", "Drivers");

        /// <summary>Adds every driver package of a backup folder to Windows and installs it on matching devices.</summary>
        internal static string DriverRestoreScript(string source) => "$source = " + PowerShellService.Literal(source) + @"
$ErrorActionPreference = 'Stop'
$packages = @(Get-ChildItem -LiteralPath $source -Filter *.inf -Recurse -File)
if ($packages.Count -eq 0) { throw ""No driver package (.inf) found in $source"" }
""Installing $($packages.Count) driver packages from $source...""
& pnputil.exe /add-driver (Join-Path $source '*.inf') /subdirs /install
$code = $LASTEXITCODE
# 3010: a restart finishes the installation. 259: packages were added to the driver store but no device present needed them.
if ($code -notin 0, 259, 3010) { throw ""pnputil failed with exit code $code"" }
if ($code -eq 3010) { 'VERIFIED:Drivers installed. Restart Windows to finish.' } else { ""VERIFIED:$($packages.Count) driver packages added to Windows."" }
";

        /// <summary>The PowerShell a built-in script runs.</summary>
        public static string Body(string id) => id switch
        {
            "winget" => LoadWinGetScript(),
            "ctt-winutil" => "irm christitus.com/win | iex",
            "gaming-runtimes" => @"
$packages = @(
    'Microsoft.VCRedist.2005.x86', 'Microsoft.VCRedist.2005.x64',
    'Microsoft.VCRedist.2008.x86', 'Microsoft.VCRedist.2008.x64',
    'Microsoft.VCRedist.2010.x86', 'Microsoft.VCRedist.2010.x64',
    'Microsoft.VCRedist.2012.x86', 'Microsoft.VCRedist.2012.x64',
    'Microsoft.VCRedist.2013.x86', 'Microsoft.VCRedist.2013.x64',
    'Microsoft.VCRedist.2015+.x86', 'Microsoft.VCRedist.2015+.x64',
    'Microsoft.DirectX', 'CreativeTechnology.OpenAL', 'Microsoft.XNARedist',
    'Nvidia.PhysX', 'Nvidia.PhysXLegacy'
)
foreach ($package in $packages) {
    Write-Output ""Installing $package...""
    & winget.exe install --id $package --exact --silent --accept-source-agreements --accept-package-agreements --disable-interactivity
    if ($LASTEXITCODE -ne 0) { throw ""$package failed with exit code $LASTEXITCODE"" }
}
",
            "dotnet-developer-setup" => @"
$packages = 'Git.Git', 'Microsoft.DotNet.SDK.10'
foreach ($package in $packages) {
    Write-Output ""Installing $package...""
    & winget.exe install --id $package --exact --silent --accept-source-agreements --accept-package-agreements --disable-interactivity
    if ($LASTEXITCODE -ne 0) { throw ""$package failed with exit code $LASTEXITCODE"" }
}
Write-Output 'Installing Visual Studio Community with the .NET desktop workload...'
& winget.exe install --id Microsoft.VisualStudio.2022.Community --exact --silent --accept-source-agreements --accept-package-agreements --disable-interactivity --override '--wait --quiet --add Microsoft.VisualStudio.Workload.ManagedDesktop --includeRecommended'
if ($LASTEXITCODE -ne 0) { throw ""Visual Studio failed with exit code $LASTEXITCODE"" }
",
            "change-local-password" => @"
try {
    $targetUser = if ($env:TWEAKHUB_TARGET_USER) { $env:TWEAKHUB_TARGET_USER } else { $env:USERNAME }
    $account = Get-LocalUser -Name $targetUser -ErrorAction Stop
    if ($account.PrincipalSource -ne 'Local') { throw 'The current account is not a local Windows account.' }
    & net.exe user $targetUser '*'
    if ($LASTEXITCODE -ne 0) { throw ""net user failed with exit code $LASTEXITCODE"" }
    Write-Host '`nPassword changed successfully.' -ForegroundColor Green
} catch {
    Write-Host ""`n$($_.Exception.Message)"" -ForegroundColor Red
    Read-Host 'Press Enter to close'
    exit 1
}
Read-Host 'Press Enter to close'
",
            "dism-sfc-chkdsk" => @"
$commands = @(
    @{ Executable = 'DISM.exe'; Arguments = @('/Online', '/Cleanup-Image', '/CheckHealth') },
    @{ Executable = 'DISM.exe'; Arguments = @('/Online', '/Cleanup-Image', '/ScanHealth') },
    @{ Executable = 'DISM.exe'; Arguments = @('/Online', '/Cleanup-Image', '/RestoreHealth') },
    @{ Executable = 'sfc.exe'; Arguments = @('/scannow') },
    @{ Executable = 'chkdsk.exe'; Arguments = @($env:SystemDrive, '/scan') }
)
foreach ($command in $commands) {
    $executable = $command.Executable
    $arguments = $command.Arguments
    Write-Output ""`n> $executable $arguments""
    & $executable @arguments
    if ($LASTEXITCODE -ne 0) { throw ""$executable failed with exit code $LASTEXITCODE"" }
}
",
            "component-cleanup" => @"
& DISM.exe /Online /Cleanup-Image /StartComponentCleanup
if ($LASTEXITCODE -ne 0) { throw ""DISM failed with exit code $LASTEXITCODE"" }
",
            // The caller's temp folder: a standard user elevates as another account, whose GetTempPath is not theirs.
            "temp-cleanup" => "$userTemp = " + PowerShellService.Literal(Path.GetTempPath()) + @"
$ErrorActionPreference = 'Stop'
# Only files untouched for a day, so running installers and apps keep their current temp files.
$cutoff = (Get-Date).AddDays(-1)
$script:freed = 0
function Clear-Folder([string]$path) {
    foreach ($item in Get-ChildItem -LiteralPath $path -Force -ErrorAction SilentlyContinue) {
        # Never follow junctions or symbolic links out of the temp folder.
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        if ($item.PSIsContainer) {
            Clear-Folder $item.FullName
            try { [IO.Directory]::Delete($item.FullName) } catch { }
        } elseif ($item.LastWriteTime -lt $cutoff) {
            try { Remove-Item -LiteralPath $item.FullName -Force -ErrorAction Stop; $script:freed += $item.Length } catch { }
        }
    }
}
foreach ($folder in @($userTemp, (Join-Path $env:SystemRoot 'Temp')) | Select-Object -Unique) {
    if (Test-Path -LiteralPath $folder) { Clear-Folder $folder }
}
Delete-DeliveryOptimizationCache -Force -ErrorAction SilentlyContinue
'Freed {0:N1} MB of temporary files. Files in use or newer than one day were kept.' -f ($script:freed / 1MB)
",
            // Caches the GPU driver rebuilds on its own; files a running game holds open are skipped.
            "shader-cache-cleanup" => @"
$ErrorActionPreference = 'Stop'
$local = [Environment]::GetFolderPath('LocalApplicationData')
$low = Join-Path $env:USERPROFILE 'AppData\LocalLow'
$roots = @(
    'D3DSCache', 'AMD\DxCache', 'AMD\DxcCache', 'AMD\DX9Cache', 'AMD\VkCache', 'AMD\GLCache', 'AMD\OglCache',
    'NVIDIA\DXCache', 'NVIDIA\GLCache', 'Intel\ShaderCache' | ForEach-Object { Join-Path $local $_ }
) + @('AMD\DxCache', 'AMD\DxcCache', 'AMD\VkCache', 'NVIDIA\PerDriverVersion\DXCache', 'NVIDIA\PerDriverVersion\GLCache',
    'Intel\ShaderCache' | ForEach-Object { Join-Path $low $_ })
$script:freed = 0
$script:kept = 0
function Clear-Folder([string]$path) {
    foreach ($item in Get-ChildItem -LiteralPath $path -Force -ErrorAction SilentlyContinue) {
        # Never follow junctions or symbolic links out of the cache folder.
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        if ($item.PSIsContainer) { Clear-Folder $item.FullName; continue }
        try { Remove-Item -LiteralPath $item.FullName -Force -ErrorAction Stop; $script:freed += $item.Length } catch { $script:kept++ }
    }
}
foreach ($root in $roots) {
    if (Test-Path -LiteralPath $root) { Clear-Folder $root }
}
'Freed {0:N1} MB of GPU shader caches. {1} files in use were kept.' -f ($script:freed / 1MB), $script:kept
",
            "driver-backup" => "$destination = " + PowerShellService.Literal(DriverBackupPath) + @"
$ErrorActionPreference = 'Stop'
$target = Join-Path $destination ('{0}-{1}' -f $env:COMPUTERNAME, (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $target -Force | Out-Null
& pnputil.exe /export-driver * $target
if ($LASTEXITCODE -ne 0) { throw ""pnputil failed with exit code $LASTEXITCODE"" }
$count = @(Get-ChildItem -LiteralPath $target -Filter *.inf -Recurse -File).Count
if ($count -eq 0) { throw 'pnputil exported no third-party driver package.' }
""VERIFIED:$count driver packages exported to $target""
",
            // The page asks for the folder; the preview shows the default backup location.
            "driver-restore" => DriverRestoreScript(DriverBackupPath),
            "network-reset" => @"
& ipconfig.exe /flushdns
if ($LASTEXITCODE -ne 0) { throw ""ipconfig failed with exit code $LASTEXITCODE"" }
& netsh.exe winsock reset
if ($LASTEXITCODE -ne 0) { throw ""Winsock reset failed with exit code $LASTEXITCODE"" }
& netsh.exe int ip reset
if ($LASTEXITCODE -ne 0) { throw ""TCP/IP reset failed with exit code $LASTEXITCODE"" }
",
            "windows-update-reset" => @"
$services = 'bits', 'wuauserv', 'cryptsvc'
$stamp = Get-Date -Format yyyyMMddHHmmss
try {
    Stop-Service $services -Force -ErrorAction Stop
    $caches = (Join-Path $env:SystemRoot 'SoftwareDistribution'), (Join-Path $env:SystemRoot 'System32\catroot2')
    foreach ($cache in $caches) {
        if (Test-Path $cache) {
            $newName = '{0}.tweakhub-{1}' -f [IO.Path]::GetFileName($cache), $stamp
            Rename-Item $cache $newName -ErrorAction Stop
        }
    }
} finally {
    foreach ($service in $services) { Start-Service $service -ErrorAction Continue }
}
Write-Output 'Windows Update caches recreated.'
",
            "microsoft-store-repair" => @"
# Source: https://support.microsoft.com/account-billing/microsoft-store-doesn-t-open-126a875d-8b72-def1-0af6-d325276a058b
function Test-Store {
    $package = Get-AppxPackage Microsoft.WindowsStore -ErrorAction SilentlyContinue
    if (-not $package -or $package.Status -ne 'Ok' -or -not (Test-Path (Join-Path $package.InstallLocation 'AppxManifest.xml'))) { return $false }
    Start-Process 'ms-windows-store:'
    Start-Sleep -Seconds 5
    return [bool](Get-Process WinStore.App -ErrorAction SilentlyContinue)
}
if (Test-Store) { Write-Output 'VERIFIED:Microsoft Store is registered and opens.'; return }
& wsreset.exe
if (Test-Store) { Write-Output 'VERIFIED:Microsoft Store cache was reset and the app opens.'; return }
$package = Get-AppxPackage Microsoft.WindowsStore -ErrorAction SilentlyContinue
if ($package) { $package | Reset-AppxPackage }
if (Test-Store) { Write-Output 'VERIFIED:Microsoft Store was reset and opens.'; return }
if ($package) { Add-AppxPackage -DisableDevelopmentMode -Register (Join-Path $package.InstallLocation 'AppxManifest.xml') }
if (-not (Test-Store)) { throw 'Microsoft Store was reset and re-registered, but it still does not open. Run the DISM + SFC repair next.' }
Write-Output 'VERIFIED:Microsoft Store was re-registered and opens.'
",
            "gaming-services-repair" => @"
# Source: https://support.xbox.com/help/games-apps/troubleshooting/gaming-services-repair-tool
function Test-GamingServices {
    $package = Get-AppxPackage Microsoft.GamingServices -ErrorAction SilentlyContinue
    $services = @(Get-Service GamingServices,GamingServicesNet -ErrorAction SilentlyContinue)
    return $package -and $package.Status -eq 'Ok' -and $services.Count -eq 2
}
if (Test-GamingServices) { Write-Output 'VERIFIED:Xbox Gaming Services package and services are registered.'; return }
$tool = Join-Path $env:TEMP 'TweakHub-GamingRepairTool.exe'
try {
    Invoke-WebRequest 'https://aka.ms/GamingRepairTool' -OutFile $tool -UseBasicParsing
    $signature = Get-AuthenticodeSignature $tool
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft') {
        throw 'The Gaming Services Repair Tool does not have the expected valid Microsoft signature.'
    }
    $process = Start-Process $tool -Wait -PassThru
    if ($process.ExitCode -notin 0, 3010) { throw ""Gaming Services Repair Tool failed with exit code $($process.ExitCode)."" }
    if (Test-GamingServices) { Write-Output 'VERIFIED:Xbox Gaming Services package and services were repaired.'; return }
    'Microsoft.GamingServices','Microsoft.GamingApp' | ForEach-Object {
        Get-AppxPackage $_ -ErrorAction SilentlyContinue | Reset-AppxPackage
    }
    $process = Start-Process $tool -Wait -PassThru
    if ($process.ExitCode -notin 0, 3010) { throw ""Gaming Services Repair Tool retry failed with exit code $($process.ExitCode)."" }
    if (-not (Test-GamingServices)) { throw 'Gaming Services is still unavailable. Restart Windows, then run this fix again.' }
    Write-Output 'VERIFIED:Xbox Gaming Services packages were reset and both services are registered.'
} finally {
    Remove-Item $tool -Force -ErrorAction SilentlyContinue
}
",
            "print-spooler-repair" => @"
# Source: https://support.microsoft.com/windows/hardware/printer/fix-printer-connection-and-printing-problems-in-windows
Get-Service Spooler -ErrorAction Stop | Out-Null
Stop-Service Spooler -Force
$queue = Join-Path $env:WINDIR 'System32\spool\PRINTERS'
Get-ChildItem $queue -Force -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction Stop
Set-Service Spooler -StartupType Automatic
Start-Service Spooler
if ((Get-Service Spooler).Status -ne 'Running' -or (Get-ChildItem $queue -Force -ErrorAction SilentlyContinue)) {
    throw 'Print Spooler did not return to a running state with an empty queue.'
}
Write-Output 'VERIFIED:Print Spooler is running and the print queue is empty.'
",
            "onedrive-repair" => @"
# Source: https://support.microsoft.com/onedrive/reset-onedrive
$paths = @(
    (Join-Path $env:LOCALAPPDATA 'Microsoft\OneDrive\OneDrive.exe'),
    (Join-Path $env:ProgramFiles 'Microsoft OneDrive\OneDrive.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Microsoft OneDrive\OneDrive.exe')
)
$client = $paths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $client) { throw 'OneDrive is not installed.' }
Stop-Process -Name OneDrive -Force -ErrorAction SilentlyContinue
$reset = Start-Process $client -ArgumentList '/reset' -Wait -PassThru
if ($reset.ExitCode -ne 0) { throw ""OneDrive reset failed with exit code $($reset.ExitCode)."" }
Start-Process $client
Start-Sleep -Seconds 8
if (-not (Get-Process OneDrive -ErrorAction SilentlyContinue)) { throw 'OneDrive was reset but did not restart.' }
Write-Output 'VERIFIED:OneDrive was reset and restarted. Sync connections will be rebuilt.'
",
            "wsl-setup" => @"
# Source: https://learn.microsoft.com/windows/wsl/install
$features = 'Microsoft-Windows-Subsystem-Linux', 'VirtualMachinePlatform'
$restartRequired = $false
foreach ($feature in $features) {
    $state = (Get-WindowsOptionalFeature -Online -FeatureName $feature).State
    if ($state -ne 'Enabled') {
        $result = Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart
        $restartRequired = $restartRequired -or $result.RestartNeeded
    }
}
& wsl.exe --update
if ($LASTEXITCODE -ne 0 -and -not $restartRequired) { throw ""WSL update failed with exit code $LASTEXITCODE."" }
& wsl.exe --set-default-version 2
if ($LASTEXITCODE -ne 0 -and -not $restartRequired) { throw ""Setting WSL 2 as default failed with exit code $LASTEXITCODE."" }
$distros = @(& wsl.exe --list --quiet) -replace ""`0"", ''
if (-not ($distros -match '^Ubuntu')) {
    & wsl.exe --install --distribution Ubuntu --no-launch
    if ($LASTEXITCODE -ne 0 -and -not $restartRequired) { throw ""Ubuntu installation failed with exit code $LASTEXITCODE."" }
}
if ($restartRequired) { Write-Output 'VERIFIED:WSL features were enabled. Restart Windows to finish setup, then launch Ubuntu once.'; return }
& wsl.exe --status | Out-Null
if ($LASTEXITCODE -ne 0 -or -not ((@(& wsl.exe --list --quiet) -replace ""`0"", '') -match '^Ubuntu')) {
    throw 'WSL commands completed, but WSL 2 or Ubuntu could not be verified.'
}
Write-Output 'VERIFIED:WSL 2 is available and Ubuntu is registered.'
",
            "empty-standby-list" => @"
$ErrorActionPreference = 'Stop'
$command = Get-Command RAMMap64.exe, RAMMap.exe -ErrorAction SilentlyContinue | Select-Object -First 1
$executable = $command.Source
if (-not $executable) {
    $packageRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages'
    $executable = Get-ChildItem $packageRoot -Filter 'RAMMap*.exe' -Recurse -ErrorAction SilentlyContinue |
        Where-Object Name -Match '^RAMMap(64)?\.exe$' |
        Select-Object -ExpandProperty FullName -First 1
}
if (-not $executable) { throw 'RAMMap was not found. Install Microsoft RAMMap from External Tools first.' }
$process = Start-Process -FilePath $executable -ArgumentList '-Et' -Wait -PassThru
if ($process.ExitCode -ne 0) { throw ""RAMMap failed with exit code $($process.ExitCode)"" }
Write-Output 'Standby list emptied once. Do not schedule this operation.'
",
            "remove-windows-ai" => @"
$ErrorActionPreference = 'Stop'
$policyPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer'
# New-Item -Force on an existing key deletes its values and subkeys: create the key only when it is missing.
if (-not (Test-Path $policyPath)) { New-Item $policyPath -Force | Out-Null }
New-ItemProperty $policyPath -Name SettingsPageVisibility -PropertyType String -Value 'hide:aicomponents' -Force | Out-Null
$notepadPath = 'HKLM:\SOFTWARE\Policies\WindowsNotepad'
if (-not (Test-Path $notepadPath)) { New-Item $notepadPath -Force | Out-Null }
New-ItemProperty $notepadPath -Name DisableAIFeatures -PropertyType DWord -Value 1 -Force | Out-Null
Get-AppxPackage -AllUsers '*Copilot*' | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -AllUsers }
Get-AppxPackage -AllUsers Microsoft.MicrosoftOfficeHub | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -AllUsers }
Get-AppxPackage MicrosoftWindows.Client.CoreAI | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName }
if (Get-Service WSAIFabricSvc -ErrorAction SilentlyContinue) { Set-Service WSAIFabricSvc -StartupType Disabled }
if ((Get-WindowsOptionalFeature -Online -FeatureName Recall -ErrorAction SilentlyContinue).State -eq 'Enabled') {
    Disable-WindowsOptionalFeature -FeatureName Recall -Online -NoRestart | Out-Null
}
if (Get-Command winget.exe -ErrorAction SilentlyContinue) {
    $winget = Start-Process winget.exe -ArgumentList @('uninstall', '-e', '--name', 'Copilot', '--silent', '--force', '--accept-source-agreements') -Wait -PassThru -WindowStyle Hidden
    if ($winget.ExitCode -ne 0) { Write-Warning 'Copilot was not installed through WinGet or could not be removed.' }
}
Write-Output 'Available Windows AI components were disabled or removed. A restart is recommended.'
",
            "adobe-hosts-block" => @"
$ErrorActionPreference = 'Stop'
$hostsPath = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'
$start = '# TweakHub Adobe block START'
$end = '# TweakHub Adobe block END'
$pattern = '(?ms)^# TweakHub Adobe block START\r?\n.*?^# TweakHub Adobe block END\r?\n?'
$content = [IO.File]::ReadAllText($hostsPath)
$content = [regex]::Replace($content, $pattern, '')
$list = Invoke-RestMethod -Uri 'https://github.com/Ruddernation-Designs/Adobe-URL-Block-List/raw/refs/heads/master/hosts'
if ([string]::IsNullOrWhiteSpace([string]$list)) { throw 'Downloaded hosts list is empty.' }
$block = ""$start`r`n$([string]$list)`r`n$end`r`n""
[IO.File]::WriteAllText($hostsPath, $content.TrimEnd() + ""`r`n"" + $block, [Text.UTF8Encoding]::new($false))
& ipconfig.exe /flushdns | Out-Null
Write-Output 'Adobe block list added between TweakHub markers.'
",
            "adobe-hosts-unblock" => @"
$ErrorActionPreference = 'Stop'
$hostsPath = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'
$pattern = '(?ms)^# TweakHub Adobe block START\r?\n.*?^# TweakHub Adobe block END\r?\n?'
$content = [IO.File]::ReadAllText($hostsPath)
$updated = [regex]::Replace($content, $pattern, '')
[IO.File]::WriteAllText($hostsPath, $updated, [Text.UTF8Encoding]::new($false))
& ipconfig.exe /flushdns | Out-Null
Write-Output 'TweakHub Adobe block removed.'
",
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown built-in script.")
        };
    }
}
