# Elevated, in the guest: trust the throw-away test certificate and install the three test packages.
$r = 'C:\vt\results'; New-Item -ItemType Directory -Force $r | Out-Null
Start-Transcript -LiteralPath "$r\setup.txt" -Force | Out-Null
Import-Certificate -FilePath C:\vt\vt.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Select-Object Thumbprint, Subject
[Environment]::OSVersion.VersionString
(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion').DisplayVersion
Get-AppxPackage TweakHub.VT.* | Remove-AppxPackage
foreach ($n in 'Disabled', 'Default', 'Flex') {
    try { Add-AppxPackage -Path "C:\vt\TweakHub-VT-$n.msix" -ErrorAction Stop; "installed $n" } catch { "INSTALL FAILED $n : $($_.Exception.Message)" }
}
Get-AppxPackage TweakHub.VT.* | Select-Object Name, PackageFamilyName, InstallLocation | Format-List
Stop-Transcript | Out-Null

