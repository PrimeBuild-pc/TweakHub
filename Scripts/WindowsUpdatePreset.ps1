param(
    [Parameter(Mandatory)]
    [ValidateSet('Default', 'Disabled', 'Security', 'Restore', 'Status')]
    [string]$Preset,
    [Parameter(Mandatory)]
    [string]$BackupPath
)

$ErrorActionPreference = 'Stop'

# Behavior adapted for TweakHub from Chris Titus Tech's WinUtil update presets (MIT).
# The script is bundled with TweakHub and never downloads or executes remote code.
$registryValues = @(
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'; Names = @('NoAutoUpdate', 'AUOptions', 'NoAutoRebootWithLoggedOnUsers', 'AUPowerManagement') },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Names = @('ExcludeWUDriversInQualityUpdate', 'DeferFeatureUpdates', 'DeferFeatureUpdatesPeriodInDays', 'DeferQualityUpdates', 'DeferQualityUpdatesPeriodInDays') },
    @{ Path = 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings'; Names = @('BranchReadinessLevel', 'DeferFeatureUpdatesPeriodInDays', 'DeferQualityUpdatesPeriodInDays') },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Device Metadata'; Names = @('PreventDeviceMetadataFromNetwork') },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DriverSearching'; Names = @('DontPromptForWindowsUpdate', 'DontSearchWindowsUpdate', 'DriverUpdateWizardWuSearchEnabled') },
    @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config'; Names = @('DODownloadMode') },
    @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer'; Names = @('SettingsPageVisibility') }
)
$serviceNames = @('BITS', 'wuauserv', 'UsoSvc')
$taskPaths = @(
    '\Microsoft\Windows\InstallService\*',
    '\Microsoft\Windows\UpdateOrchestrator\*',
    '\Microsoft\Windows\UpdateAssistant\*',
    '\Microsoft\Windows\WaaSMedic\*',
    '\Microsoft\Windows\WindowsUpdate\*',
    '\Microsoft\WindowsUpdate\*'
)

function Get-UpdateTasks {
    $seen = @{}
    foreach ($path in $taskPaths) {
        foreach ($task in @(Get-ScheduledTask -TaskPath $path -ErrorAction SilentlyContinue)) {
            $key = $task.TaskPath + $task.TaskName
            if (-not $seen.ContainsKey($key)) {
                $seen[$key] = $true
                $task
            }
        }
    }
}

function Get-RegistryValue([string]$Path, [string]$Name) {
    try { return (Get-ItemPropertyValue -Path $Path -Name $Name -ErrorAction Stop) } catch { return $null }
}

function Test-RegistryValueAbsent([string]$Path, [string]$Name) {
    try {
        $key = Get-Item -Path $Path -ErrorAction Stop
        return -not ($key.GetValueNames() -contains $Name)
    } catch { return $true }
}

function Save-Backup {
    if (Test-Path -LiteralPath $BackupPath) { return }

    $services = foreach ($name in $serviceNames) {
        $service = Get-CimInstance Win32_Service -Filter "Name='$name'" -ErrorAction SilentlyContinue
        if ($service) { [pscustomobject]@{ Name = $name; StartMode = $service.StartMode; State = $service.State } }
    }
    $tasks = foreach ($task in @(Get-UpdateTasks)) {
        [pscustomobject]@{ TaskPath = $task.TaskPath; TaskName = $task.TaskName; Enabled = [bool]$task.Settings.Enabled }
    }

    $directory = Split-Path -Parent $BackupPath
    if ($directory) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    $temp = $BackupPath + '.tmp'
    [pscustomobject]@{ Services = @($services); Tasks = @($tasks) } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $temp -Encoding UTF8
    Move-Item -LiteralPath $temp -Destination $BackupPath -Force
}

function Restore-Backup {
    if (-not (Test-Path -LiteralPath $BackupPath)) { return }
    $backup = Get-Content -LiteralPath $BackupPath -Raw | ConvertFrom-Json

    foreach ($item in @($backup.Services)) {
        $startup = if ($item.StartMode -eq 'Auto') { 'Automatic' } else { $item.StartMode }
        Set-Service -Name $item.Name -StartupType $startup
        if ($item.State -eq 'Running') { Start-Service -Name $item.Name -ErrorAction SilentlyContinue }
        else { Stop-Service -Name $item.Name -Force -ErrorAction SilentlyContinue }
    }
    foreach ($item in @($backup.Tasks)) {
        $task = Get-ScheduledTask -TaskPath $item.TaskPath -TaskName $item.TaskName -ErrorAction SilentlyContinue
        if ($task) {
            if ($item.Enabled) { $task | Enable-ScheduledTask | Out-Null }
            else { $task | Disable-ScheduledTask | Out-Null }
        }
    }
    Remove-Item -LiteralPath $BackupPath -Force
}

function Clear-ManagedPolicies {
    foreach ($group in $registryValues) {
        foreach ($name in $group.Names) {
            if ($name -eq 'SettingsPageVisibility') {
                if ((Get-RegistryValue $group.Path $name) -ne 'hide:windowsupdate') { continue }
            }
            Remove-ItemProperty -Path $group.Path -Name $name -ErrorAction SilentlyContinue
        }
    }
}

function Set-Policy([string]$Path, [string]$Name, [int]$Value) {
    New-Item -Path $Path -Force | Out-Null
    New-ItemProperty -Path $Path -Name $Name -PropertyType DWord -Value $Value -Force | Out-Null
}

function Set-UpdateAvailability {
    Set-Service -Name BITS -StartupType Manual
    Set-Service -Name wuauserv -StartupType Manual
    Set-Service -Name UsoSvc -StartupType Automatic
    Start-Service -Name UsoSvc -ErrorAction SilentlyContinue
    Get-UpdateTasks | Enable-ScheduledTask -ErrorAction SilentlyContinue | Out-Null
}

function Get-DetectedPreset {
    $services = @{}
    foreach ($name in $serviceNames) {
        $service = Get-CimInstance Win32_Service -Filter "Name='$name'" -ErrorAction SilentlyContinue
        $services[$name] = $service.StartMode
    }
    $tasks = @(Get-UpdateTasks)
    $allTasksDisabled = $tasks.Count -eq 0 -or @($tasks | Where-Object { $_.Settings.Enabled }).Count -eq 0
    $availabilityIsDefault = $services.BITS -eq 'Manual' -and $services.wuauserv -eq 'Manual' -and $services.UsoSvc -eq 'Auto'
    $noDisabledTasks = @($tasks | Where-Object { -not $_.Settings.Enabled }).Count -eq 0

    $au = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
    $wu = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'
    $driver = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DriverSearching'
    $metadata = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Device Metadata'
    $delivery = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config'

    if ((Get-RegistryValue $au 'NoAutoUpdate') -eq 1 -and
        (Get-RegistryValue $au 'AUOptions') -eq 1 -and
        (Get-RegistryValue $delivery 'DODownloadMode') -eq 0 -and
        @($serviceNames | Where-Object { $services[$_] -ne 'Disabled' }).Count -eq 0 -and $allTasksDisabled) {
        return 'Disabled'
    }

    if ($availabilityIsDefault -and $noDisabledTasks -and
        (Get-RegistryValue $wu 'ExcludeWUDriversInQualityUpdate') -eq 1 -and
        (Get-RegistryValue $wu 'DeferFeatureUpdates') -eq 1 -and
        (Get-RegistryValue $wu 'DeferFeatureUpdatesPeriodInDays') -eq 365 -and
        (Test-RegistryValueAbsent $wu 'DeferQualityUpdates') -and
        (Test-RegistryValueAbsent $wu 'DeferQualityUpdatesPeriodInDays') -and
        (Get-RegistryValue $metadata 'PreventDeviceMetadataFromNetwork') -eq 1 -and
        (Get-RegistryValue $driver 'DontPromptForWindowsUpdate') -eq 1 -and
        (Get-RegistryValue $driver 'DontSearchWindowsUpdate') -eq 1 -and
        (Get-RegistryValue $driver 'DriverUpdateWizardWuSearchEnabled') -eq 0 -and
        (Get-RegistryValue $au 'AUOptions') -eq 4 -and
        (Get-RegistryValue $au 'NoAutoRebootWithLoggedOnUsers') -eq 1) {
        return 'Security'
    }

    $managedValuesAbsent = $true
    foreach ($group in $registryValues) {
        foreach ($name in $group.Names) {
            if ($name -eq 'SettingsPageVisibility') {
                if ((Get-RegistryValue $group.Path $name) -eq 'hide:windowsupdate') { $managedValuesAbsent = $false }
            } elseif (-not (Test-RegistryValueAbsent $group.Path $name)) {
                $managedValuesAbsent = $false
            }
        }
    }
    if ($managedValuesAbsent -and $availabilityIsDefault -and $noDisabledTasks) { return 'Default' }
    return 'Custom'
}

if ($Preset -eq 'Status') {
    Write-Output ('PRESET:' + (Get-DetectedPreset))
    exit 0
}
if ($Preset -eq 'Restore') {
    Restore-Backup
    Write-Output 'PRESET:RESTORED'
    exit 0
}

Save-Backup
Clear-ManagedPolicies

switch ($Preset) {
    'Default' {
        Set-UpdateAvailability
    }
    'Disabled' {
        $au = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
        $delivery = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config'
        Set-Policy $au 'NoAutoUpdate' 1
        Set-Policy $au 'AUOptions' 1
        Set-Policy $delivery 'DODownloadMode' 0
        foreach ($name in $serviceNames) {
            Stop-Service -Name $name -Force -ErrorAction SilentlyContinue
            Set-Service -Name $name -StartupType Disabled
        }
        Remove-Item -Path (Join-Path $env:SystemRoot 'SoftwareDistribution\*') -Recurse -Force -ErrorAction SilentlyContinue
        Get-UpdateTasks | Disable-ScheduledTask -ErrorAction SilentlyContinue | Out-Null
    }
    'Security' {
        Set-UpdateAvailability
        $wu = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'
        $au = Join-Path $wu 'AU'
        $metadata = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Device Metadata'
        $driver = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DriverSearching'
        Set-Policy $metadata 'PreventDeviceMetadataFromNetwork' 1
        Set-Policy $driver 'DontPromptForWindowsUpdate' 1
        Set-Policy $driver 'DontSearchWindowsUpdate' 1
        Set-Policy $driver 'DriverUpdateWizardWuSearchEnabled' 0
        Set-Policy $wu 'ExcludeWUDriversInQualityUpdate' 1
        Set-Policy $wu 'DeferFeatureUpdates' 1
        Set-Policy $wu 'DeferFeatureUpdatesPeriodInDays' 365
        # Quality/security updates intentionally remain on the normal schedule.
        Set-Policy $au 'AUOptions' 4
        Set-Policy $au 'NoAutoRebootWithLoggedOnUsers' 1
        Set-Policy $au 'AUPowerManagement' 0
    }
}

Write-Output ('PRESET:' + $Preset)
