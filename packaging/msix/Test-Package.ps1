[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagePath,
    [string]$CertificatePath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing
$config = Get-Content (Join-Path $PSScriptRoot 'identity.json') -Raw | ConvertFrom-Json
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
function Read-XmlEntry([string]$name) {
    $entry = $zip.GetEntry($name)
    if (!$entry) { throw "Missing $name" }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { return [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
}
try {
    $manifest = Read-XmlEntry 'AppxManifest.xml'
    if ($manifest.Package.Identity.Name -ne $config.Name -or $manifest.Package.Identity.Publisher -cne $config.Publisher) { throw 'Partner Center identity mismatch' }
    if ($manifest.Package.Identity.ProcessorArchitecture -ne 'x64') { throw 'Package is not x64' }
    if ($manifest.Package.Properties.PublisherDisplayName -cne $config.PublisherDisplayName) { throw 'Publisher display name mismatch' }
    if ([version]$manifest.Package.Identity.Version -lt [version]'1.0.0.0' -or ([version]$manifest.Package.Identity.Version).Revision -ne 0) { throw 'Invalid Store version' }
    $caps = @($manifest.Package.Capabilities.Capability | ForEach-Object { $_.Name })
    if (@(Compare-Object $caps @('runFullTrust','allowElevation','unvirtualizedResources')).Count) { throw 'Unexpected capabilities' }
    $ns = [Xml.XmlNamespaceManager]::new($manifest.NameTable)
    $ns.AddNamespace('f','http://schemas.microsoft.com/appx/manifest/foundation/windows10')
    $ns.AddNamespace('d','http://schemas.microsoft.com/appx/manifest/desktop/windows10/6')
    foreach ($property in @('RegistryWriteVirtualization','FileSystemWriteVirtualization')) {
        if ($manifest.SelectSingleNode("/f:Package/f:Properties/d:$property", $ns).InnerText -ne 'disabled') { throw "$property must be disabled" }
    }
    $application = $manifest.Package.Applications.Application
    $exeEntry = $zip.GetEntry($application.Executable.Replace('\','/'))
    if (!$exeEntry) { throw 'Start-menu executable missing' }
    # The apphost must be an AMD64 PE, not a renamed portable archive or an AnyCPU executable.
    $stream = $exeEntry.Open(); $memory = [IO.MemoryStream]::new()
    try { $stream.CopyTo($memory); $bytes = $memory.ToArray() } finally { $stream.Dispose(); $memory.Dispose() }
    $pe = [BitConverter]::ToInt32($bytes, 0x3c)
    if ([BitConverter]::ToUInt32($bytes, $pe) -ne 0x4550 -or [BitConverter]::ToUInt16($bytes, $pe + 4) -ne 0x8664) { throw 'Apphost is not x64 PE' }
    foreach ($dll in @('coreclr.dll','hostfxr.dll','hostpolicy.dll','System.Private.CoreLib.dll','PresentationFramework.dll','wpfgfx_cor3.dll','vcruntime140_cor3.dll')) {
        if (!$zip.GetEntry("App/$dll")) { throw "Self-contained dependency missing: $dll" }
    }
    if ($zip.GetEntry('App/portable.flag')) { throw 'Portable marker leaked into Store package' }
    if ($config.App -eq 'TweakHub') {
        foreach ($script in @('WinGetInstall.ps1','WindowsUpdatePreset.ps1')) {
            if (!$zip.GetEntry("App/Scripts/$script")) { throw "Bundled script missing: $script" }
        }
    }
    foreach ($size in @(44,50,150)) {
        $stream = $zip.GetEntry("Assets/Logo$size.png").Open()
        $image = [Drawing.Image]::FromStream($stream)
        try { if ($image.Width -ne $size -or $image.Height -ne $size) { throw 'Invalid logo dimensions' } }
        finally { $image.Dispose(); $stream.Dispose() }
    }
    # Verify every uncompressed 64-KiB SHA-256 block, including manifest and native/runtime payloads.
    $map = Read-XmlEntry 'AppxBlockMap.xml'
    if ($map.BlockMap.HashMethod -ne 'http://www.w3.org/2001/04/xmlenc#sha256') { throw 'Unexpected block hash method' }
    $sha = [Security.Cryptography.SHA256]::Create()
    $mapped = @{}
    try {
        foreach ($file in $map.BlockMap.File) {
            $name = $file.Name.Replace('\','/')
            $entry = $zip.GetEntry($name)
            if (!$entry -or $entry.Length -ne [long]$file.Size) { throw "Missing or truncated payload: $name" }
            $mapped[$name] = $true
            $stream = $entry.Open()
            try {
                foreach ($block in $file.SelectNodes("*[local-name()='Block']")) {
                    $buffer = New-Object byte[] 65536
                    $count = 0
                    while ($count -lt $buffer.Length) {
                        $read = $stream.Read($buffer, $count, $buffer.Length - $count)
                        if (!$read) { break }; $count += $read
                    }
                    $hash = [Convert]::ToBase64String($sha.ComputeHash($buffer,0,$count))
                    if ($hash -cne $block.Hash) { throw "Block hash mismatch: $name" }
                }
                if ($stream.ReadByte() -ne -1) { throw "Unmapped bytes: $name" }
            } finally { $stream.Dispose() }
        }
    } finally { $sha.Dispose() }
    foreach ($entry in $zip.Entries) {
        if (!$mapped.ContainsKey($entry.FullName) -and $entry.FullName -notin @('AppxBlockMap.xml','[Content_Types].xml','AppxSignature.p7x','AppxMetadata/CodeIntegrity.cat')) { throw "Unmapped entry: $($entry.FullName)" }
    }
    $signature = $zip.GetEntry('AppxSignature.p7x')
    if ($CertificatePath) {
        if (!$signature) { throw 'Test package is unsigned' }
        Add-Type -AssemblyName System.Security
        $stream = $signature.Open(); $memory = [IO.MemoryStream]::new()
        try { $stream.CopyTo($memory); $bytes = $memory.ToArray() } finally { $stream.Dispose(); $memory.Dispose() }
        if ([Text.Encoding]::ASCII.GetString($bytes,0,4) -ne 'PKCX') { throw 'Invalid Appx signature header' }
        $cms = [Security.Cryptography.Pkcs.SignedCms]::new()
        $cms.Decode($bytes[4..($bytes.Length-1)])
        $cms.CheckSignature($true)
        $cert = [Security.Cryptography.X509Certificates.X509Certificate2]::new((Resolve-Path -LiteralPath $CertificatePath).Path)
        if ($cms.SignerInfos.Count -ne 1 -or $cms.SignerInfos[0].Certificate.Thumbprint -ne $cert.Thumbprint -or $cert.Subject -cne $config.Publisher) { throw 'Test signer mismatch' }
        # APPX SIP binds the signed content to the package's footprint hashes (AXPC/AXCD/AXCT/AXBM).
        $auth = Get-AuthenticodeSignature -LiteralPath $PackagePath
        if ($auth.Status.ToString() -ne 'UnknownError' -or $auth.StatusMessage -notmatch '800B0109|root certificate|radice') {
            if ($auth.Status.ToString() -ne 'Valid' -and $auth.Status.ToString() -ne 'NotTrusted') { throw "Unexpected SIP validation status: $($auth.Status): $($auth.StatusMessage)" }
        }
        Write-Output "Test CMS signature and payload hashes valid; host trust intentionally absent. Signer: $($cert.Thumbprint)"
    } elseif ($signature) { throw 'Store artifact must remain unsigned' }
    Write-Output "Validated $($config.Name) $($manifest.Package.Identity.Version): x64, identity, capabilities, logos, self-contained runtime, $($mapped.Count) block-mapped files."
} finally { $zip.Dispose() }
