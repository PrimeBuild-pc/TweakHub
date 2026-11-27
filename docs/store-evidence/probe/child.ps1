# Elevated child started by probe.ps1 through UAC (runas), the way TweakHub starts reg.exe / powershell.exe.
param([string]$V, [string]$DataDir, [string]$Script)
$log = "C:\vt\results\probe-$V.txt"
function L($m) { Add-Content -LiteralPath $log -Value ("[child {0:HH:mm:ss}] {1}" -f (Get-Date), $m) }
Add-Type -Namespace C -Name N -MemberDefinition '[DllImport("kernel32.dll", CharSet=CharSet.Unicode)] public static extern int GetCurrentPackageFullName(ref int len, System.Text.StringBuilder name);'
$len = 256; $sb = New-Object System.Text.StringBuilder 256
$rc = [C.N]::GetCurrentPackageFullName([ref]$len, $sb)
L "package identity: rc=$rc name=$($sb.ToString())"
L ("integrity: " + ((whoami /groups | Select-String 'Mandatory Label') -replace '\s+', ' '))
L "LOCALAPPDATA as seen by child: $env:LOCALAPPDATA"
# HKLM write, as RegistryService.RunElevatedReg does with reg.exe.
& reg.exe add 'HKLM\SOFTWARE\TweakHubProbe' /v $V /t REG_DWORD /d 1 /f | Out-Null
L "reg.exe add HKLM\SOFTWARE\TweakHubProbe\$V exit=$LASTEXITCODE"
# Read the backup record the UI wrote before the change.
$backup = Join-Path $DataDir "backup-$V.json"
L ("child sees UI backup record: " + (Test-Path -LiteralPath $backup))
# Write the undo record the UI must find later.
Set-Content -LiteralPath (Join-Path $DataDir "undo-$V.json") -Value "{`"variant`":`"$V`"}"
L "child wrote undo record: $DataDir\undo-$V.json"
# Run the script file the UI wrote to TEMP.
L ("child sees UI temp script: " + (Test-Path -LiteralPath $Script))
if (Test-Path -LiteralPath $Script) { & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Script }
exit 0
