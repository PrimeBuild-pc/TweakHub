param([string]$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path)
$ErrorActionPreference = 'Stop'
# Expects the Store payload (dotnet publish -p:StorePackage=true) in .\payload\App and the probe scripts in .\payload\Probe.
$vt = $PSScriptRoot
$sdk = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.26100.0\x64"
$publisher = 'CN=ACD03D0F-7A52-4C93-ADF8-A07702180E72'
$repoManifest = (Join-Path $Repo 'packaging\msix\AppxManifest.xml')
$base = ((Get-Content $repoManifest -Raw) -replace "`r?`n", "`r`n").Replace('__VERSION__', '1.8.2.0')

# Assets from the repo icon (same as Build-Msix.ps1, simplified).
$assets = Join-Path $vt 'payload\Assets'
New-Item -ItemType Directory -Force $assets | Out-Null
Add-Type -AssemblyName System.Drawing
$ico = New-Object System.Drawing.Icon((Join-Path $Repo 'ico.ico'), 256, 256)
$src = $ico.ToBitmap()
foreach ($s in 44, 50, 150) { $b = New-Object System.Drawing.Bitmap($src, $s, $s); $b.Save("$assets\Logo$s.png", [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose() }

$flexProps = @'
    <virtualization:FileSystemWriteVirtualization>
      <virtualization:ExcludedDirectories>
        <virtualization:ExcludedDirectory>$(KnownFolder:LocalAppData)\TweakHub</virtualization:ExcludedDirectory>
      </virtualization:ExcludedDirectories>
    </virtualization:FileSystemWriteVirtualization>
    <virtualization:RegistryWriteVirtualization>
      <virtualization:ExcludedKeys>
        <virtualization:ExcludedKey>HKEY_CURRENT_USER\Software\Microsoft\GameBar</virtualization:ExcludedKey>
__EXTRA_KEY__      </virtualization:ExcludedKeys>
    </virtualization:RegistryWriteVirtualization>
'@
$desktop6 = @'
    <desktop6:RegistryWriteVirtualization>disabled</desktop6:RegistryWriteVirtualization>
    <desktop6:FileSystemWriteVirtualization>disabled</desktop6:FileSystemWriteVirtualization>
'@
$unvirt = '    <rescap:Capability Name="unvirtualizedResources" />' + "`r`n"

function New-Variant($name, [scriptblock]$edit) {
    $m = $base.Replace('Name="LorenzoMassafra.TweakHub"', "Name=`"TweakHub.VT.$name`"").Replace('<DisplayName>TweakHub</DisplayName>', "<DisplayName>TweakHub VT $name</DisplayName>")
    $m = & $edit $m
    $dir = Join-Path $vt "stage-$name"
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
    Copy-Item (Join-Path $vt 'payload') $dir -Recurse
    [IO.File]::WriteAllText("$dir\AppxManifest.xml", $m, [Text.UTF8Encoding]::new($false))
    $msix = Join-Path $vt "TweakHub-VT-$name.msix"
    & "$sdk\makeappx.exe" pack /d $dir /p $msix /o 2>&1 | Out-File (Join-Path $vt "makeappx-$name.log")
    "{0,-12} makeappx exit {1}" -f $name, $LASTEXITCODE
    if ($LASTEXITCODE -eq 0) { $msix }
}

$built = @()
# Disabled = the submitted Store manifest (package-wide virtualization off + unvirtualizedResources).
$built += New-Variant 'Disabled' { param($m) $m }
# Default = standard MSIX virtualization: no desktop6 switches, no unvirtualizedResources.
$built += New-Variant 'Default' { param($m) $m.Replace($desktop6, '').Replace($unvirt, '') }
# Flex = Flexible Virtualization: only LocalAppData\TweakHub and one HKCU key excluded.
$built += New-Variant 'Flex' { param($m) $m.Replace($desktop6, $flexProps.Replace('__EXTRA_KEY__', '')).Replace('xmlns:desktop6=', 'xmlns:virtualization="http://schemas.microsoft.com/appx/manifest/virtualization/windows10" xmlns:desktop6=').Replace('IgnorableNamespaces="uap uap10 desktop6 rescap"', 'IgnorableNamespaces="uap uap10 desktop6 rescap virtualization"') }
# FlexHklm = same, plus one HKLM key: expected to be rejected by the schema.
$hklm = '        <virtualization:ExcludedKey>HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows</virtualization:ExcludedKey>' + "`r`n"
New-Variant 'FlexHklm' { param($m) $m.Replace($desktop6, $flexProps.Replace('__EXTRA_KEY__', $hklm)).Replace('xmlns:desktop6=', 'xmlns:virtualization="http://schemas.microsoft.com/appx/manifest/virtualization/windows10" xmlns:desktop6=').Replace('IgnorableNamespaces="uap uap10 desktop6 rescap"', 'IgnorableNamespaces="uap uap10 desktop6 rescap virtualization"') } | Out-Null

$built = @($built | Where-Object { $_ -like '*.msix' })
$cert = New-SelfSignedCertificate -Type Custom -Subject $publisher -KeyUsage DigitalSignature -KeyAlgorithm RSA -KeyLength 2048 `
    -CertStoreLocation 'Cert:\CurrentUser\My' -NotAfter (Get-Date).AddDays(7) -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
try {
    Export-Certificate -Cert $cert -FilePath (Join-Path $vt 'vt.cer') -Force | Out-Null
    foreach ($p in $built) { & "$sdk\signtool.exe" sign /fd SHA256 /s My /sha1 $cert.Thumbprint $p | Out-Null; "signed $p exit $LASTEXITCODE" }
} finally { Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -DeleteKey -Force }
