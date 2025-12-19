# WinGet Installation Script (multi-method)
# Attempts three methods to install Microsoft.WinGet.Client

try {
    $version = winget --version 2>$null
    if ($version) {
        Write-Output "ALREADY_INSTALLED:$version"
        return
    }
} catch {
    Write-Output "NOT_INSTALLED"
}

$success = $false
$method = ""

# Method 1: PowerShell registration
try {
    Write-Host "Trying Method 1: PowerShell registration..."
    Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe
    Start-Sleep -Seconds 3
    $testResult = winget --version 2>$null
    if ($testResult) {
        $success = $true
        $method = "PowerShell registration"
        Write-Host "Method 1 successful"
    }
} catch {
    Write-Host "Method 1 failed: $($_.Exception.Message)"
}

# Method 2: Direct download
if (-not $success) {
    try {
        Write-Host "Trying Method 2: Direct download..."
        $downloadPath = "$env:TEMP\winget.msixbundle"
        Invoke-WebRequest -Uri "https://aka.ms/getwinget" -OutFile $downloadPath -UseBasicParsing
        Add-AppxPackage $downloadPath
        Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        $testResult = winget --version 2>$null
        if ($testResult) {
            $success = $true
            $method = "Direct download"
            Write-Host "Method 2 successful"
        }
    } catch {
        Write-Host "Method 2 failed: $($_.Exception.Message)"
    }
}

# Method 3: GitHub latest release
if (-not $success) {
    try {
        Write-Host "Trying Method 3: GitHub latest release..."
        $url = "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"
        $downloadPath = "$env:TEMP\winget-installer.msixbundle"
        Invoke-WebRequest -Uri $url -OutFile $downloadPath -UseBasicParsing
        Add-AppxPackage $downloadPath
        Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        $testResult = winget --version 2>$null
        if ($testResult) {
            $success = $true
            $method = "GitHub release"
            Write-Host "Method 3 successful"
        }
    } catch {
        Write-Host "Method 3 failed: $($_.Exception.Message)"
    }
}

if ($success) {
    $version = winget --version
    Write-Output ("SUCCESS:{0}:{1}" -f $method, $version)
} else {
    Write-Output "FAILED:All installation methods failed"
}
