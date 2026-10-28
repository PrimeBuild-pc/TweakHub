$ErrorActionPreference = 'Stop'
$path = Join-Path $PSScriptRoot '..\Scripts\Collect-TweakHub-Hardware.cmd'
$source = [IO.File]::ReadAllText($path)
$parts = [regex]::Split($source, '(?m)^# POWERSHELL PAYLOAD\r?$')
if ($parts.Count -ne 2) { throw 'Expected exactly one payload marker' }
$tokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseInput($parts[1], [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
$ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] }, $false) |
    ForEach-Object { . ([scriptblock]::Create($_.Extent.Text)) }

function Assert($Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$script:tokens = @{}
$script:issues = [System.Collections.Generic.List[object]]::new()
$raw = 'USB\VID_1234&PID_5678\PRIVATE-SERIAL'
$first = Get-DeviceToken $raw
Assert ($first -eq (Get-DeviceToken $raw.ToLowerInvariant())) 'Tokens must be case insensitive and stable within a report'
Assert ($first -ne (Get-DeviceToken 'PCI\VEN_1234&DEV_5678\OTHER')) 'Different devices need different tokens'
Assert ($null -eq (Get-DeviceToken '')) 'Missing parents must remain null'
Assert ($first -notmatch 'PRIVATE|VID_1234') 'Token leaks original identity'
$ids = @(Get-HardwareIds @($raw, 'USB\VID_1234&PID_5678', 'BTHENUM\PRIVATE-NAME', 'SWD\PRIVATE-ACCOUNT'))
Assert ($ids.Count -eq 1) 'Hardware IDs must exclude sensitive buses and deduplicate'
Assert ($ids[0] -eq 'USB\VID_1234&PID_5678') 'Hardware ID must retain model but remove serial'
Assert (@(Get-HardwareIds $null).Count -eq 0) 'Missing hardware IDs must produce an empty list'

# A failed section must not abort the report or export raw exception messages.
function Get-CimInstance { throw 'PRIVATE-PC\PRIVATE-USER: access denied' }
$result = @(Read-Cim 'FakeClass' @('Model'))
Assert ($result.Count -eq 0) 'Failed CIM section must be empty'
Assert ($script:issues.Count -eq 1) 'Failed CIM section must be reported'
Assert (($script:issues | ConvertTo-Json) -notmatch 'PRIVATE') 'Issue leaks exception text'
Write-Host 'Hardware collector tests passed (payload syntax, tokens, hardware IDs, safe section failures).'
