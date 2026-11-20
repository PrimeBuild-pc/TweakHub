param(
    [Parameter(Mandatory)]
    [ValidateSet('Default', 'Disabled', 'Security', 'Restore', 'Status')]
    [string]$Preset
)

$ErrorActionPreference = 'Stop'

# Behavior adapted for TweakHub from Chris Titus Tech's WinUtil update presets (MIT).
# The script is bundled with TweakHub and never downloads or executes remote code.
$registryValues = @(
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'; Names = @('NoAutoUpdate', 'AUOptions', 'NoAutoRebootWithLoggedOnUsers', 'AUPowerManagement') },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'; Names = @('ExcludeWUDriversInQualityUpdate', 'DeferFeatureUpdates', 'DeferFeatureUpdatesPeriodInDays', 'DeferQualityUpdates', 'DeferQualityUpdatesPeriodInDays', 'SetUpdateNotificationLevel', 'UpdateNotificationLevel') },
    @{ Path = 'HKLM:\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings'; Names = @('BranchReadinessLevel', 'DeferFeatureUpdatesPeriodInDays', 'DeferQualityUpdatesPeriodInDays') },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Device Metadata'; Names = @('PreventDeviceMetadataFromNetwork') },
    @{ Path = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DriverSearching'; Names = @('DontPromptForWindowsUpdate', 'DontSearchWindowsUpdate', 'DriverUpdateWizardWuSearchEnabled') },
    @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config'; Names = @('DODownloadMode') },
    @{ Path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer'; Names = @('SettingsPageVisibility') }
)
$serviceNames = @('BITS', 'wuauserv', 'UsoSvc')
$backupKey = 'SOFTWARE\TweakHub\WindowsUpdate'
$backupName = 'ServiceTaskBackup'
$taskPaths = @(
    '\Microsoft\Windows\InstallService\*',
    '\Microsoft\Windows\UpdateOrchestrator\*',
    '\Microsoft\Windows\UpdateAssistant\*',
    '\Microsoft\Windows\WaaSMedic\*',
    '\Microsoft\Windows\WindowsUpdate\*',
    '\Microsoft\WindowsUpdate\*'
)

# One read of all tasks: every wildcard -TaskPath call enumerates them again (about 0.8 s each).
function Test-UpdateTaskPath([string]$Path) {
    @($taskPaths | Where-Object { $Path -like $_ }).Count -gt 0
}

function Get-UpdateTasks {
    Get-ScheduledTask | Where-Object { Test-UpdateTaskPath $_.TaskPath }
}

# One CIM query for all services instead of one per name.
function Get-UpdateServices {
    $found = @{}
    Get-CimInstance Win32_Service -Filter (($serviceNames | ForEach-Object { "Name='$_'" }) -join ' OR ') |
        ForEach-Object { $found[$_.Name] = $_ }
    $found
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

function Assert-UpdateBackup($Backup) {
    if ($null -eq $Backup -or $Backup.Services -isnot [array] -or $Backup.Tasks -isnot [array]) {
        throw 'Invalid Windows Update backup.'
    }
    foreach ($item in $Backup.Services) {
        if ($item.Name -isnot [string] -or $item.Name -notin $serviceNames -or
            $item.StartMode -isnot [string] -or $item.StartMode -notin 'Auto', 'Manual', 'Disabled' -or
            $item.State -isnot [string] -or $item.State -notin 'Running', 'Stopped') {
            throw 'Invalid service in the Windows Update backup.'
        }
    }
    foreach ($item in $Backup.Tasks) {
        if ($item.TaskPath -isnot [string] -or -not (Test-UpdateTaskPath $item.TaskPath) -or
            $item.TaskName -isnot [string] -or [string]::IsNullOrWhiteSpace($item.TaskName) -or $item.Enabled -isnot [bool]) {
            throw 'Invalid scheduled task in the Windows Update backup.'
        }
    }
}

function Save-Backup {
    $key = $machine.CreateSubKey($backupKey)
    try {
        $saved = $key.GetValue($backupName)
        if ($null -ne $saved) {
            $backup = ConvertFrom-Json -InputObject $saved
            Assert-UpdateBackup $backup
            return $backup
        }

        $found = Get-UpdateServices
        $services = @(foreach ($name in $serviceNames) {
            $service = $found[$name]
            if ($service) { [pscustomobject]@{ Name = $name; StartMode = $service.StartMode; State = $service.State } }
        })
        $tasks = @(Get-UpdateTasks | ForEach-Object {
            [pscustomobject]@{ TaskPath = $_.TaskPath; TaskName = $_.TaskName; Enabled = [bool]$_.Settings.Enabled }
        })
        $backup = [pscustomobject]@{ Services = $services; Tasks = $tasks }
        Assert-UpdateBackup $backup
        $key.SetValue($backupName, (ConvertTo-Json -InputObject $backup -Depth 4), 'String')
        return $backup
    } finally {
        $key.Dispose()
    }
}

function Restore-Backup {
    $key = $machine.OpenSubKey($backupKey, $true)
    if ($null -eq $key) { throw 'The Windows Update machine backup is missing.' }
    try {
        $saved = $key.GetValue($backupName)
        if ($null -eq $saved) { throw 'The Windows Update machine backup is missing.' }
        $backup = ConvertFrom-Json -InputObject $saved
        Assert-UpdateBackup $backup

        $found = Get-UpdateServices
        $tasks = @{}
        Get-UpdateTasks | ForEach-Object { $tasks[$_.TaskPath + $_.TaskName] = $_ }

        foreach ($item in $backup.Services) {
            $service = $found[$item.Name]
            if (-not $service -or $service.StartMode -ne $item.StartMode) {
                $startup = if ($item.StartMode -eq 'Auto') { 'Automatic' } else { $item.StartMode }
                Set-Service -Name $item.Name -StartupType $startup
            }
            if ($service.State -ne $item.State) {
                if ($item.State -eq 'Running') { Start-Service -Name $item.Name }
                else { Stop-Service -Name $item.Name -Force }
            }
        }
        foreach ($item in $backup.Tasks) {
            $task = $tasks[$item.TaskPath + $item.TaskName]
            if ($task -and [bool]$task.Settings.Enabled -ne $item.Enabled) {
                if ($item.Enabled) { $task | Enable-ScheduledTask | Out-Null }
                else { $task | Disable-ScheduledTask | Out-Null }
            }
        }

        $key.DeleteValue($backupName)
    } finally {
        $key.Dispose()
    }
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
    # New-Item -Force on an existing key deletes its values and subkeys: create the key only when it is missing.
    if (-not (Test-Path -LiteralPath $Path)) { New-Item -Path $Path -Force | Out-Null }
    New-ItemProperty -Path $Path -Name $Name -PropertyType DWord -Value $Value -Force | Out-Null
}

function Set-UpdateAvailability($Backup) {
    Set-Service -Name BITS -StartupType Manual
    Set-Service -Name wuauserv -StartupType Manual
    Set-Service -Name UsoSvc -StartupType Automatic
    Start-Service -Name UsoSvc -ErrorAction SilentlyContinue
    # Windows ships some update tasks disabled (WakeUpAndScanForUpdates wakes the PC): re-enable only those that
    # were on before TweakHub first changed anything. Save-Backup always runs first, so the backup exists here.
    $before = @{}
    foreach ($task in $Backup.Tasks) { $before[$task.TaskPath + $task.TaskName] = $task.Enabled }
    Get-UpdateTasks | Where-Object { $before[$_.TaskPath + $_.TaskName] -ne $false } |
        Enable-ScheduledTask -ErrorAction SilentlyContinue | Out-Null
}

function Get-DetectedPreset {
    $found = Get-UpdateServices
    $services = @{}
    foreach ($name in $serviceNames) { $services[$name] = $found[$name].StartMode }
    $tasks = @(Get-UpdateTasks)
    $allTasksDisabled = $tasks.Count -eq 0 -or @($tasks | Where-Object { $_.Settings.Enabled }).Count -eq 0
    $availabilityIsDefault = $services.BITS -eq 'Manual' -and $services.wuauserv -eq 'Manual' -and $services.UsoSvc -eq 'Auto'
    # Not "every task enabled": Windows ships some disabled and protected (UpdateOrchestrator\Schedule Work).
    $noDisabledTasks = -not $allTasksDisabled

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
        (Get-RegistryValue $au 'AUOptions') -eq 2 -and
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
    return
}
$machine = [Microsoft.Win32.RegistryKey]::OpenBaseKey('LocalMachine', 'Registry64')
if ($Preset -eq 'Restore') {
    Restore-Backup
    Write-Output 'PRESET:RESTORED'
    return
}

$backup = Save-Backup
Clear-ManagedPolicies

switch ($Preset) {
    'Default' {
        Set-UpdateAvailability $backup
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
        Set-UpdateAvailability $backup
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
        # Security updates are not deferred, but nothing downloads or installs by itself: 2 = notify before download.
        # Clear-ManagedPolicies already removed the notification-level policies, so the notifications stay on.
        Set-Policy $au 'AUOptions' 2
        Set-Policy $au 'NoAutoRebootWithLoggedOnUsers' 1
        Set-Policy $au 'AUPowerManagement' 0
    }
}

Write-Output ('PRESET:' + $Preset)
