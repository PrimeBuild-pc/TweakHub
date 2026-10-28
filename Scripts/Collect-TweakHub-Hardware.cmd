@echo off
setlocal
set "TWEAKHUB_COLLECTOR=%~f0"
set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if exist "%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe" set "PS=%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
"%PS%" -NoLogo -NoProfile -Command "$ErrorActionPreference='Stop'; try { $source=[IO.File]::ReadAllText($env:TWEAKHUB_COLLECTOR); $payload=[regex]::Split($source,'(?m)^# POWERSHELL PAYLOAD\r?$')[1]; & ([scriptblock]::Create($payload)) } catch { Write-Host ('Raccolta fallita: ' + $_.Exception.GetType().Name) -ForegroundColor Red; exit 1 }"
set "RESULT=%ERRORLEVEL%"
echo.
if not "%RESULT%"=="0" echo Impossibile completare la raccolta. Comunica questo errore allo sviluppatore.
pause
exit /b %RESULT%
# POWERSHELL PAYLOAD
$ErrorActionPreference = 'Stop'
$script:tokens = @{}
$script:issues = [System.Collections.Generic.List[object]]::new()

function Get-DeviceToken([string]$Id) {
    if ([string]::IsNullOrWhiteSpace($Id)) { return $null }
    if (-not $script:tokens.ContainsKey($Id)) {
        $script:tokens[$Id] = 'Device{0:D5}' -f ($script:tokens.Count + 1)
    }
    return $script:tokens[$Id]
}

function Get-HardwareIds($Ids) {
    # Never retain the instance/serial segment, including on unusual driver output.
    @($Ids | ForEach-Object {
        if ([string]$_ -match '^(PCI|ACPI|USB|HID|HDAUDIO|DISPLAY)\\([^\\]+)') {
            '{0}\{1}' -f $Matches[1], $Matches[2]
        }
    } | Sort-Object -Unique)
}

function Read-Cim([string]$Class, [string[]]$Fields) {
    try {
        @(Get-CimInstance -ClassName $Class -OperationTimeoutSec 30 -ErrorAction Stop |
            Select-Object -Property $Fields)
    } catch {
        $script:issues.Add([pscustomobject]@{ Section = $Class; Error = $_.Exception.GetType().Name })
    }
}

function Read-OptionalRegistry([string]$Path, [string]$Section) {
    try { Get-ItemProperty -LiteralPath $Path -ErrorAction Stop }
    catch {
        if ($_.CategoryInfo.Category -ne 'ObjectNotFound') {
            $script:issues.Add([pscustomobject]@{ Section = $Section; Error = $_.Exception.GetType().Name })
        }
    }
}

Write-Host 'TweakHub - Raccolta hardware (sola lettura)' -ForegroundColor Cyan
Write-Host 'Non serve aprire TweakHub o ThreadPilot. Non serve avviare come amministratore.'
Write-Host 'Raccoglie modelli hardware, BIOS, driver, topologia dispositivi e impostazioni MSI/affinita.'
Write-Host 'Non raccoglie account, nome PC, IP, MAC, seriali, file personali o processi.'
Write-Host 'Gli ID originali vengono sostituiti da riferimenti validi solo in questo report.'
Write-Host 'Crea un JSON sul Desktop. Non scarica, non invia e non modifica impostazioni.'
Write-Host 'Controlla il JSON prima di condividerlo: la configurazione hardware puo identificare un PC.'
if ((Read-Host 'Vuoi procedere? Scrivi S e premi Invio') -notmatch '^(s|si|y|yes)$') {
    Write-Host 'Annullato. Nessun report creato.'
    return
}

Write-Host 'Raccolta in corso. Attendi qualche minuto...'
$report = [ordered]@{
    SchemaVersion = 1
    CollectorVersion = '1.0.0'
    CollectedAtUtc = [DateTime]::UtcNow.ToString('o')
    Elevated = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
    Notes = @(
        'Read-only snapshot; no applications need to be running.'
        'Device tokens are local to this report; original instance IDs are not exported.'
        'Names use driver device descriptions, not user-assigned friendly names.'
        'Null or missing properties mean unavailable, not unsupported or disabled.'
        'PCIe speed values are Windows generation codes (1=2.5, 2=5, 3=8, 4=16, 5=32, 6=64 GT/s). Idle links can downshift.'
        'No vendor GPU APIs or USB IOCTLs: negotiated USB speeds and some GPU links are unavailable.'
        'No latency measurements or motherboard lane-sharing validation; consult board manuals.'
    )
    OperatingSystem = @(Read-Cim 'Win32_OperatingSystem' @('Caption', 'Version', 'BuildNumber', 'OSArchitecture'))
    Computer = @(Read-Cim 'Win32_ComputerSystem' @('Manufacturer', 'Model', 'SystemType', 'TotalPhysicalMemory'))
    Motherboard = @(Read-Cim 'Win32_BaseBoard' @('Manufacturer', 'Product', 'Version'))
    Bios = @(Read-Cim 'Win32_BIOS' @('Manufacturer', 'SMBIOSBIOSVersion', 'ReleaseDate'))
    Cpu = @(Read-Cim 'Win32_Processor' @('Name', 'Manufacturer', 'NumberOfCores', 'NumberOfLogicalProcessors', 'MaxClockSpeed', 'SocketDesignation'))
    Memory = @(Read-Cim 'Win32_PhysicalMemory' @('Manufacturer', 'PartNumber', 'Capacity', 'Speed', 'ConfiguredClockSpeed', 'SMBIOSMemoryType', 'DeviceLocator', 'BankLabel'))
    Graphics = @(Read-Cim 'Win32_VideoController' @('Name', 'VideoProcessor', 'DriverVersion', 'DriverDate'))
    Disks = @(Read-Cim 'Win32_DiskDrive' @('Model', 'InterfaceType', 'MediaType', 'Size', 'FirmwareRevision'))
}

$devices = @(Get-PnpDevice -ErrorAction Stop | Sort-Object InstanceId)
if ($devices.Count -eq 0) { throw 'Empty PnP inventory' }
$properties = @{}
$keys = @(
    'DEVPKEY_Device_DeviceDesc', 'DEVPKEY_Device_Parent', 'DEVPKEY_Device_HardwareIds',
    'DEVPKEY_Device_Service', 'DEVPKEY_Device_DriverInfPath', 'DEVPKEY_Device_DriverVersion',
    'DEVPKEY_Device_DriverProvider', 'DEVPKEY_Device_DriverDate', 'DEVPKEY_Device_IsPresent',
    'DEVPKEY_Device_ProblemCode', 'DEVPKEY_Device_BusNumber', 'DEVPKEY_Device_Address',
    'DEVPKEY_Device_LocationPaths', 'DEVPKEY_PciDevice_CurrentLinkSpeed',
    'DEVPKEY_PciDevice_CurrentLinkWidth', 'DEVPKEY_PciDevice_MaxLinkSpeed', 'DEVPKEY_PciDevice_MaxLinkWidth'
)
# Unsupported/missing properties are normal; count other failures without leaking instance IDs in messages.
$propertyErrors = @()
$devices | Get-PnpDeviceProperty -KeyName $keys -ErrorAction SilentlyContinue -ErrorVariable propertyErrors |
    ForEach-Object {
        if (-not $properties.ContainsKey($_.InstanceId)) { $properties[$_.InstanceId] = @{} }
        $properties[$_.InstanceId][$_.KeyName] = $_.Data
    }
$propertyErrors | Group-Object { [string]$_.CategoryInfo.Category } | ForEach-Object {
    $script:issues.Add([pscustomobject]@{ Section = 'PnpProperties'; Category = $_.Name; Count = $_.Count })
}
$report.Devices = @($devices | ForEach-Object {
    $device = $_
    $id = [string]$device.InstanceId
    $token = Get-DeviceToken $id
    $p = $properties[$id]
    if (-not $p) { $p = @{} }
    $bus = ($id -split '\\', 2)[0]
    $hardwareBus = $bus -in @('PCI', 'ACPI', 'USB', 'HID', 'HDAUDIO', 'DISPLAY', 'SCSI', 'IDE')
    $name = ''
    if ($hardwareBus) { $name = [string]$p['DEVPKEY_Device_DeviceDesc'] }
    $present = $p['DEVPKEY_Device_IsPresent']
    if ($null -eq $present) { $present = $device.Present }
    $problem = $p['DEVPKEY_Device_ProblemCode']
    if ($null -eq $problem) { $problem = $device.ConfigManagerErrorCode }
    $interrupts = $null
    if ($bus -eq 'PCI') {
        $base = "HKLM:\SYSTEM\CurrentControlSet\Enum\$id\Device Parameters\Interrupt Management"
        $msi = Read-OptionalRegistry "$base\MessageSignaledInterruptProperties" "$token/MSI"
        $affinity = Read-OptionalRegistry "$base\Affinity Policy" "$token/Affinity"
        $interrupts = [ordered]@{
            MSISupported = $msi.MSISupported
            MessageNumberLimit = $msi.MessageNumberLimit
            DevicePriority = $affinity.DevicePriority
            DevicePolicy = $affinity.DevicePolicy
            AssignmentSetOverrideBytes = @($affinity.AssignmentSetOverride | ForEach-Object { [int]$_ })
        }
    }
    [pscustomobject]@{
        Id = $token
        Parent = Get-DeviceToken ([string]$p['DEVPKEY_Device_Parent'])
        Bus = $(if ($bus -match '^[A-Z0-9_]{1,32}$') { $bus } else { 'OTHER' })
        Name = $name
        Class = [string]$device.Class
        Status = [string]$device.Status
        Present = $present
        Problem = $problem
        HardwareIds = @(Get-HardwareIds $p['DEVPKEY_Device_HardwareIds'])
        Service = $p['DEVPKEY_Device_Service']
        Inf = $p['DEVPKEY_Device_DriverInfPath']
        DriverVersion = $p['DEVPKEY_Device_DriverVersion']
        DriverProvider = $p['DEVPKEY_Device_DriverProvider']
        DriverDate = $p['DEVPKEY_Device_DriverDate']
        BusNumber = $p['DEVPKEY_Device_BusNumber']
        Address = $p['DEVPKEY_Device_Address']
        LocationPaths = @($p['DEVPKEY_Device_LocationPaths'] | Where-Object { $_ -match '^(PCIROOT|ACPI)\(' })
        LinkGen = $p['DEVPKEY_PciDevice_CurrentLinkSpeed']
        LinkWidth = $p['DEVPKEY_PciDevice_CurrentLinkWidth']
        MaxLinkGen = $p['DEVPKEY_PciDevice_MaxLinkSpeed']
        MaxLinkWidth = $p['DEVPKEY_PciDevice_MaxLinkWidth']
        Interrupts = $interrupts
    }
})
$report.DeviceCount = $report.Devices.Count
$report.Issues = @($script:issues.ToArray())
$json = $report | ConvertTo-Json -Depth 12
$null = $json | ConvertFrom-Json -ErrorAction Stop
$desktop = [Environment]::GetFolderPath('DesktopDirectory')
if (-not $desktop -or -not [IO.Directory]::Exists($desktop)) { throw 'Desktop unavailable' }
$output = Join-Path $desktop ('TweakHub-Hardware-{0}-{1}.json' -f (Get-Date -Format 'yyyyMMdd-HHmmss'), [guid]::NewGuid().ToString('N').Substring(0, 8))
[IO.File]::WriteAllText($output, $json, [Text.UTF8Encoding]::new($false))
Write-Host ''
Write-Host "Report creato: $output" -ForegroundColor Green
Write-Host "Dispositivi: $($report.DeviceCount). Segnalazioni di raccolta: $($report.Issues.Count)."
Write-Host 'Le proprieta mancanti sono normali: il report segnala i limiti della raccolta.'
Write-Host 'Controlla il file e condividi SOLO questo JSON con lo sviluppatore, preferibilmente in privato.'
