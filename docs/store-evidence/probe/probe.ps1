# Runs INSIDE the package (Invoke-CommandInDesktopPackage), medium integrity, like TweakHub's UI process.
# Performs the same kinds of writes TweakHub performs, then starts one elevated child the way TweakHub does (runas).
param([Parameter(Mandatory)][string]$V)
$ErrorActionPreference = 'Continue'
$log = "C:\vt\results\probe-$V.txt"
New-Item -ItemType Directory -Force C:\vt\results | Out-Null
function L($m) { Add-Content -LiteralPath $log -Value ("[probe {0:HH:mm:ss}] {1}" -f (Get-Date), $m) }
Add-Type -Namespace P -Name N -MemberDefinition '[DllImport("kernel32.dll", CharSet=CharSet.Unicode)] public static extern int GetCurrentPackageFullName(ref int len, System.Text.StringBuilder name);'
$len = 256; $sb = New-Object System.Text.StringBuilder 256
$rc = [P.N]::GetCurrentPackageFullName([ref]$len, $sb)
L "package identity: rc=$rc name=$($sb.ToString())"
L ("integrity: " + ((whoami /groups | Select-String 'Mandatory Label') -replace '\s+', ' '))

# 1. HKCU tweak written directly by the UI process (RegistryService writes HKCU without elevation).
foreach ($k in @('HKCU:\Software\Microsoft\GameBar', 'HKCU:\Control Panel\Mouse')) {
    try { New-ItemProperty -LiteralPath $k -Name "TweakHubProbe_$V" -Value 1 -PropertyType DWord -Force -ErrorAction Stop | Out-Null; L "HKCU write OK: $k\TweakHubProbe_$V" }
    catch { L "HKCU write FAILED: $k : $($_.Exception.Message)" }
}
# 2. Backup/undo record written by the UI process (registry-backup.json lives here in the Store build).
$data = Join-Path $env:LOCALAPPDATA 'TweakHub\Store\Data'
New-Item -ItemType Directory -Force $data | Out-Null
Set-Content -LiteralPath (Join-Path $data "backup-$V.json") -Value "{`"variant`":`"$V`",`"old`":0}"
L "UI wrote backup: $data\backup-$V.json"
# 3. Script file handed to the elevated child (TweakHub writes the hash-checked script to TEMP).
$script = Join-Path $env:TEMP "tweakhub-probe-$V.ps1"
Set-Content -LiteralPath $script -Value "'child ran script from TEMP' | Out-File C:\vt\results\script-ran-$V.txt"
L "UI wrote temp script: $script"
# 4. Elevated child, started like PowerShellService/RegistryService.RunElevatedReg (ShellExecute runas).
$child = Join-Path $PSScriptRoot 'child.ps1'
try {
    $p = Start-Process powershell.exe -Verb RunAs -Wait -PassThru -WindowStyle Hidden -ArgumentList ('-NoProfile -ExecutionPolicy Bypass -File "{0}" -V {1} -DataDir "{2}" -Script "{3}"' -f $child, $V, $data, $script)
    L "elevated child exit code: $($p.ExitCode)"
} catch { L "elevated child FAILED to start: $($_.Exception.Message)" }
# 5. The UI reads back the undo record the child wrote.
$undo = Join-Path $data "undo-$V.json"
L ("UI sees child's undo record: " + (Test-Path -LiteralPath $undo))
L 'probe done'

