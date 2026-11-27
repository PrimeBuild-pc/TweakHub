# NOT elevated, in the guest. Clean state, run the probe inside one test package, wait, then verify from outside.
param([Parameter(Mandatory)][string]$V)
$pkg = Get-AppxPackage "TweakHub.VT.$V"
# Clean start: no TweakHub data folder, no probe values, no previous result files for this variant.
Remove-Item (Join-Path $env:LOCALAPPDATA 'TweakHub') -Recurse -Force -ErrorAction SilentlyContinue
foreach ($k in 'HKCU:\Software\Microsoft\GameBar', 'HKCU:\Control Panel\Mouse') { Remove-ItemProperty $k -Name "TweakHubProbe_$V" -ErrorAction SilentlyContinue }
Remove-Item "C:\vt\results\*-$V.txt", "$env:TEMP\tweakhub-probe-$V.ps1" -ErrorAction SilentlyContinue
"local TweakHub folder before probe: $(Test-Path (Join-Path $env:LOCALAPPDATA 'TweakHub'))"
$probe = Join-Path $pkg.InstallLocation 'Probe\probe.ps1'
Invoke-CommandInDesktopPackage -PackageFamilyName $pkg.PackageFamilyName -AppId App -Command powershell.exe `
    -Args "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$probe`" -V $V"
"started probe $V; approve the UAC prompt"
$log = "C:\vt\results\probe-$V.txt"
for ($i = 0; $i -lt 120 -and !((Test-Path $log) -and (Select-String -LiteralPath $log -Pattern 'probe done' -Quiet)); $i++) { Start-Sleep 1 }
& C:\vt\verify.ps1 -V $V -Pfn $pkg.PackageFamilyName
Get-ChildItem (Join-Path $env:LOCALAPPDATA "Packages\$($pkg.PackageFamilyName)") -Recurse -File -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like "*-$V.*" } | ForEach-Object { Add-Content "C:\vt\results\verify-$V.txt" "found in package folder: $($_.FullName)" }
Clear-Host
Get-Content "C:\vt\results\verify-$V.txt"
