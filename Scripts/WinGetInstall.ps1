# Source: https://learn.microsoft.com/windows/package-manager/winget/troubleshooting
$ErrorActionPreference = 'Stop'

function Test-WinGet {
    if (-not (Get-Command winget.exe -ErrorAction SilentlyContinue)) { return $false }
    & winget.exe source list --disable-interactivity | Out-Null
    if ($LASTEXITCODE -ne 0) { return $false }
    & winget.exe search --id Microsoft.PowerToys --exact --accept-source-agreements --disable-interactivity | Out-Null
    return $LASTEXITCODE -eq 0
}

if (Test-WinGet) {
    Write-Output "VERIFIED:WinGet $(& winget.exe --version) is working."
    return
}

Write-Output 'Registering Microsoft App Installer...'
try {
    Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe
} catch {
    Write-Warning $_
}
if (Test-WinGet) {
    Write-Output "VERIFIED:WinGet $(& winget.exe --version) was re-registered and is working."
    return
}

Write-Output 'Running the official WinGet repair module...'
Install-PackageProvider -Name NuGet -Force | Out-Null
Install-Module -Name Microsoft.WinGet.Client -Force -AllowClobber -Scope CurrentUser -Repository PSGallery
Import-Module Microsoft.WinGet.Client -Force
Repair-WinGetPackageManager -Force -Latest
if (Test-WinGet) {
    Write-Output "VERIFIED:WinGet $(& winget.exe --version) was repaired and is working."
    return
}

if (Get-Command winget.exe -ErrorAction SilentlyContinue) {
    Write-Output 'Resetting WinGet sources...'
    & winget.exe source reset --force --disable-interactivity
    if ($LASTEXITCODE -eq 0) { & winget.exe source update --disable-interactivity | Out-Null }
}
if (-not (Test-WinGet)) { throw 'WinGet repair finished, but package search still fails. Review the log and Windows network or policy settings.' }
Write-Output "VERIFIED:WinGet $(& winget.exe --version) and its sources are working."
