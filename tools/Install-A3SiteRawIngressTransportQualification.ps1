param(
    [Parameter(Mandatory = $true)][string]$ConfigurationPath,
    [Parameter(Mandatory = $true)][string]$CandidateRecordPath
)

$ErrorActionPreference = 'Stop'
$configuration = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
$record = Get-Content -LiteralPath $CandidateRecordPath -Raw | ConvertFrom-Json
function Get-JsonProperty([object]$Value, [string]$Name) {
    $property = $Value.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    $property.Value
}
$prefix = 'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:'
$target = [string](Get-JsonProperty $configuration 'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualificationRecordPath')
if ([string]::IsNullOrWhiteSpace($target) -or -not [IO.Path]::IsPathRooted($target)) {
    throw 'SITE_QUALIFICATION_TARGET_PATH_INVALID'
}
if (@($record.PSObject.Properties).Count -ne 15 -or $record.status -cne 'PASS' -or
    $record.siteId -cne (Get-JsonProperty $configuration ($prefix + 'SiteId')) -or
    $record.endpointOrigin.TrimEnd('/') -cne ([string](Get-JsonProperty $configuration ($prefix + 'EndpointOrigin'))).TrimEnd('/') -or
    $record.deploymentRevision -cne (Get-JsonProperty $configuration ($prefix + 'DeploymentRevision')) -or
    [DateTimeOffset]$record.validUntilUtc -le [DateTimeOffset]::UtcNow -or
    $record.agentBodySendsWhileBOrR1Held -ne 0 -or
    $record.serverApplicationBodyReadsWhileBOrR1Held -ne 0 -or $record.rawPostCount -ne 1 -or
    -not $record.kestrelContinueRelayedAfterCommit -or
    $record.earlyOrIntermediaryContinueObserved -or $record.applicationPrebufferObserved -or
    $record.hiddenRetryObserved) { throw 'SITE_QUALIFICATION_RECORD_DOES_NOT_MATCH_CONFIGURATION' }
[IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
$temporary = "$target.$([Guid]::NewGuid().ToString('N')).tmp"
try {
    [IO.File]::WriteAllBytes($temporary, [IO.File]::ReadAllBytes((Resolve-Path $CandidateRecordPath)))
    if (Test-Path -LiteralPath $target -PathType Leaf) {
        [IO.File]::Replace($temporary, $target, $null, $true)
    } else {
        [IO.File]::Move($temporary, $target)
    }
} finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
}
Write-Output 'SITE_QUALIFICATION_INSTALLED=YES'
Write-Output "SITE_QUALIFICATION_RECORD=$target"
Write-Output "SITE_QUALIFICATION_RECORD_SHA256=$((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash)"
Write-Output 'RESTART_REQUIRED=NO'
