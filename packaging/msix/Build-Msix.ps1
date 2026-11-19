[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [switch]$SignForTest,
    [switch]$UpdateLockFile
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$config = Get-Content (Join-Path $PSScriptRoot 'identity.json') -Raw | ConvertFrom-Json
$sdk = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin\$($config.WindowsSdk)\x64"
$makeappx = Join-Path $sdk 'makeappx.exe'
$signtool = Join-Path $sdk 'signtool.exe'
foreach ($tool in @($makeappx, $signtool)) {
    if (!(Test-Path -LiteralPath $tool)) { throw "Windows SDK tool missing: $tool" }
}
Push-Location $repo
try {
    $actualSdk = (& dotnet --version).Trim()
    if ($actualSdk -ne $config.DotnetSdk) { throw "Use .NET SDK $($config.DotnetSdk); selected SDK is $actualSdk" }
    $project = Join-Path $repo "$($config.App).csproj"
    $versionText = (& dotnet msbuild $project -nologo -getProperty:Version).Trim()
    if ($LASTEXITCODE -ne 0 -or $versionText -notmatch '^\d+\.\d+\.\d+$') { throw "Expected stable project version: $versionText" }
    $version = [version]"$versionText.0"
    if ($version.Major -eq 0 -or $version.Major -gt 65535 -or $version.Minor -gt 65535 -or $version.Build -gt 65535) { throw 'Invalid Store package version' }
    if (!$OutputDirectory) { $OutputDirectory = Join-Path $repo "artifacts\msix\$version" }
    if (![IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory = Join-Path $repo $OutputDirectory }
    $OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $stage = Join-Path $OutputDirectory ('stage-' + [guid]::NewGuid().ToString('N'))
    $app = Join-Path $stage 'App'
    New-Item -ItemType Directory -Force -Path $app | Out-Null
    $lock = Join-Path $PSScriptRoot 'packages.lock.json'
    if (!$UpdateLockFile -and !(Test-Path -LiteralPath $lock)) { throw 'Missing lock file; initialize explicitly with -UpdateLockFile' }
    $properties = @(
        '-p:StorePackage=true', '-p:SelfContained=true', '-p:PublishSingleFile=false', '-p:PublishTrimmed=false',
        '-p:PublishReadyToRun=false', '-p:IncludeNativeLibrariesForSelfExtract=false',
        '-p:TargetLatestRuntimePatch=true',
        '-p:RestorePackagesWithLockFile=true', "-p:NuGetLockFilePath=$lock",
        '-p:Deterministic=true', '-p:ContinuousIntegrationBuild=true',
        '-p:IncludeSourceRevisionInInformationalVersion=false', "-p:PathMap=$repo=/_/"
    )
    & dotnet restore $project -r win-x64 @properties "-p:RestoreLockedMode=$(!$UpdateLockFile)" "-p:RestoreForceEvaluate=$UpdateLockFile"
    if ($LASTEXITCODE -ne 0) { throw 'Store restore failed' }
    & dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -o $app @properties
    if ($LASTEXITCODE -ne 0) { throw 'Store Release publish failed' }
    $mainExe = Join-Path $app "$($config.App).exe"
    if ([version][Diagnostics.FileVersionInfo]::GetVersionInfo($mainExe).FileVersion -ne $version) { throw 'Executable/package version mismatch' }
    $embeddedManifest = Join-Path $OutputDirectory "$($config.App)-executable.manifest.xml"
    & (Join-Path $sdk 'mt.exe') -nologo "-inputresource:$mainExe;#1" "-out:$embeddedManifest"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect embedded UAC manifest' }
    [xml]$uac = Get-Content -LiteralPath $embeddedManifest -Raw
    $level = $uac.SelectSingleNode("//*[local-name()='requestedExecutionLevel']").GetAttribute('level')
    $expectedLevel = if ($config.App -eq 'ThreadPilot') { 'requireAdministrator' } else { 'asInvoker' }
    if ($level -ne $expectedLevel) { throw "Unexpected UAC launch level: $level" }
    $runtimeConfig = Get-Content (Join-Path $app "$($config.App).runtimeconfig.json") -Raw | ConvertFrom-Json
    $included = @($runtimeConfig.runtimeOptions.includedFrameworks)
    foreach ($framework in @('Microsoft.NETCore.App','Microsoft.WindowsDesktop.App')) {
        if (@($included | Where-Object { $_.name -eq $framework -and $_.version -eq $config.RuntimeFrameworkVersion }).Count -ne 1) {
            throw "Runtime pin mismatch for $framework; expected $($config.RuntimeFrameworkVersion)"
        }
    }
    # portable.flag is an EXE-channel marker and must never activate portable paths in MSIX.
    $portableFlag = Join-Path $app 'portable.flag'
    if (Test-Path -LiteralPath $portableFlag) { Remove-Item -LiteralPath $portableFlag }
    foreach ($notice in @('LICENSE', 'THIRD_PARTY_NOTICES.md')) {
        $source = Join-Path $repo $notice
        if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $app -Force }
    }
    if ($config.App -eq 'TweakHub') {
        # Keep packaged scripts read-only; all script context/download paths use the writable Store root.
        Copy-Item -LiteralPath (Join-Path $repo 'Scripts') -Destination $app -Recurse -Force
    }
    Add-Type -AssemblyName System.Drawing
    $assets = Join-Path $stage 'Assets'
    New-Item -ItemType Directory -Force -Path $assets | Out-Null
    # WPF decodes PNG-compressed ICO frames, including the existing 256px icons.
    Add-Type -AssemblyName PresentationCore
    $decoder = [Windows.Media.Imaging.BitmapDecoder]::Create([uri](Join-Path $repo $config.Icon),
        [Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat, [Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add(($decoder.Frames | Sort-Object PixelWidth -Descending | Select-Object -First 1))
    $icon = [IO.MemoryStream]::new()
    $encoder.Save($icon)
    $icon.Position = 0
    $sourceBitmap = [Drawing.Bitmap]::new($icon)
    try {
        foreach ($size in @(44, 50, 150)) {
            $bitmap = New-Object System.Drawing.Bitmap($size, $size)
            $graphics = [Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.Clear([Drawing.Color]::Transparent)
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $inset = [int][Math]::Round($size * 0.08)
                $graphics.DrawImage($sourceBitmap, $inset, $inset, $size - 2 * $inset, $size - 2 * $inset)
                $bitmap.Save((Join-Path $assets "Logo$size.png"), [Drawing.Imaging.ImageFormat]::Png)
            } finally { $graphics.Dispose(); $bitmap.Dispose() }
        }
    } finally { $sourceBitmap.Dispose(); $icon.Dispose() }
    $manifest = (Get-Content (Join-Path $PSScriptRoot 'AppxManifest.xml') -Raw).Replace('__VERSION__', $version.ToString())
    [IO.File]::WriteAllText((Join-Path $stage 'AppxManifest.xml'), $manifest, [Text.UTF8Encoding]::new($false))
    # Normalize payload timestamps; MakeAppx must see the same inputs across runs.
    Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object { $_.LastWriteTimeUtc = [datetime]'2000-01-01T00:00:00Z' }
    $stem = "$($config.App)_$($version)_x64"
    $store = Join-Path $OutputDirectory "$stem-Store.msix"
    & $makeappx pack /d $stage /p $store /o
    if ($LASTEXITCODE -ne 0) { throw 'MakeAppx schema/semantic validation failed' }
    & (Join-Path $PSScriptRoot 'Test-Package.ps1') -PackagePath $store
    $artifacts = @($store)
    if ($SignForTest) {
        $test = Join-Path $OutputDirectory "$stem-Test.msix"
        Copy-Item -LiteralPath $store -Destination $test -Force
        # My is a personal certificate store, not a trust store. No host root/trust installation.
        # The private key is non-exportable and removed in finally; only a public CER is delivered.
        $cert = New-SelfSignedCertificate -Type Custom -Subject $config.Publisher -KeyUsage DigitalSignature `
            -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable `
            -CertStoreLocation 'Cert:\CurrentUser\My' -NotAfter (Get-Date).AddYears(1) `
            -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
        try {
            $cer = Join-Path $OutputDirectory "$stem-Test.cer"
            Export-Certificate -Cert $cert -FilePath $cer -Force | Out-Null
            & $signtool sign /fd SHA256 /s My /sha1 $cert.Thumbprint $test
            if ($LASTEXITCODE -ne 0) { throw 'Test signing failed' }
            & (Join-Path $PSScriptRoot 'Test-Package.ps1') -PackagePath $test -CertificatePath $cer
            $artifacts += @($test, $cer)
            $certInfo = @{ Thumbprint = $cert.Thumbprint; NotAfter = $cert.NotAfter.ToString('o'); Timestamped = $false }
        } finally { Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($cert.Thumbprint)" -DeleteKey -Force }
    } else { $certInfo = $null }
    $hashes = foreach ($file in $artifacts) { $h = Get-FileHash -LiteralPath $file -Algorithm SHA256; "$($h.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($file))" }
    $hashes | Set-Content (Join-Path $OutputDirectory 'SHA256SUMS.txt') -Encoding ASCII
    $receipt = [ordered]@{
        App = $config.App; Version = $version.ToString(); Architecture = 'x64'; Configuration = 'Release'
        Identity = $config.Name; Publisher = $config.Publisher; PublisherDisplayName = $config.PublisherDisplayName; PackageFamilyName = $config.PackageFamilyName
        Capabilities = @('runFullTrust', 'allowElevation', 'unvirtualizedResources')
        MinimumWindows = '10.0.22000.0'; MaxVersionTested = '10.0.26100.0'
        DotnetSdk = $actualSdk; WindowsSdk = $config.WindowsSdk; RuntimeFrameworkVersion = $config.RuntimeFrameworkVersion
        Commit = (& git rev-parse HEAD); WorkingTree = @(& git status --short)
        StorePackage = $store; StoreSignature = 'Unsigned; Microsoft Store signs distributed packages'
        TestCertificate = $certInfo; Stage = $stage; Artifacts = $artifacts
    }
    $receipt | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $OutputDirectory 'build-receipt.json') -Encoding UTF8
    Write-Output "MSIX artifacts: $OutputDirectory"
} finally { Pop-Location }
