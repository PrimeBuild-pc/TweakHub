# Runs OUTSIDE any package (normal desktop process): what Windows, Explorer and other apps actually see.
param([Parameter(Mandatory)][string]$V, [Parameter(Mandatory)][string]$Pfn)
$out = "C:\vt\results\verify-$V.txt"
function L($m) { Add-Content -LiteralPath $out -Value $m }
L "=== verify $V ($Pfn) at $(Get-Date -Format s) ==="
foreach ($k in @('HKCU:\Software\Microsoft\GameBar', 'HKCU:\Control Panel\Mouse')) {
    $val = (Get-ItemProperty -LiteralPath $k -ErrorAction SilentlyContinue)."TweakHubProbe_$V"
    L "real hive  $k\TweakHubProbe_$V = $(if ($null -eq $val) { '<missing>' } else { $val })"
}
$hklm = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\TweakHubProbe' -ErrorAction SilentlyContinue).$V
L "real hive  HKLM\SOFTWARE\TweakHubProbe\$V = $(if ($null -eq $hklm) { '<missing>' } else { $hklm })"
$real = Join-Path $env:LOCALAPPDATA 'TweakHub\Store\Data'
$cache = Join-Path $env:LOCALAPPDATA "Packages\$Pfn\LocalCache\Local\TweakHub\Store\Data"
foreach ($f in @("backup-$V.json", "undo-$V.json")) {
    L "file $f : real path=$(Test-Path (Join-Path $real $f))  package LocalCache=$(Test-Path (Join-Path $cache $f))"
}
$tempReal = Join-Path $env:TEMP "tweakhub-probe-$V.ps1"
$tempCache = Join-Path $env:LOCALAPPDATA "Packages\$Pfn\LocalCache\Local\Temp\tweakhub-probe-$V.ps1"
L "temp script: real TEMP=$(Test-Path $tempReal)  package LocalCache Temp=$(Test-Path $tempCache)"
L "child ran the temp script: $(Test-Path "C:\vt\results\script-ran-$V.txt")"
$hive = Join-Path $env:LOCALAPPDATA "Packages\$Pfn\SystemAppData\Helium\User.dat"
L "private package hive exists: $(Test-Path $hive)"
Get-Content "C:\vt\results\probe-$V.txt" -ErrorAction SilentlyContinue | ForEach-Object { L "  $_" }

