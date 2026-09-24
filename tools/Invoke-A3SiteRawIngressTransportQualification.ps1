param(
    [ValidateSet('DevelopmentHarness','PrepareSite','ProbeSite')][string]$Mode = 'DevelopmentHarness',
    [string]$SiteId = 'development-loopback',
    [string]$EndpointOrigin,
    [string]$DeploymentRevision = 'development-harness-v1',
    [string]$RecordPath = 'TestResults/a3-site-qualification/development-transport-qualification.json',
    [string]$ConfigurationPath = 'TestResults/a3-site-qualification/site-configuration.json',
    [ValidateRange(0,10080)][int]$ExpiryWarningMinutes = 1440,
    [switch]$InstallOnPass
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Resolve-OperationalPath([string]$Path) {
    if ([IO.Path]::IsPathFullyQualified($Path)) { return [IO.Path]::GetFullPath($Path) }
    [IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

if ($Mode -ceq 'PrepareSite') {
    if ([string]::IsNullOrWhiteSpace($SiteId) -or $SiteId -ceq 'development-loopback' -or
        [string]::IsNullOrWhiteSpace($DeploymentRevision) -or [string]::IsNullOrWhiteSpace($EndpointOrigin)) {
        throw 'SITE_CONFIGURATION_IDENTITY_REQUIRED'
    }
    $origin = $null
    if (-not [Uri]::TryCreate($EndpointOrigin, [UriKind]::Absolute, [ref]$origin) -or
        $origin.Scheme -cne 'https' -or $origin.AbsolutePath -cne '/' -or
        -not [string]::IsNullOrEmpty($origin.Query) -or -not [string]::IsNullOrEmpty($origin.Fragment)) {
        throw 'SITE_CONFIGURATION_HTTPS_ORIGIN_INVALID'
    }
    $config = [ordered]@{
        'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualificationRecordPath' =
            (Resolve-OperationalPath $RecordPath)
        'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:SiteId' = $SiteId
        'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:EndpointOrigin' =
            $origin.GetLeftPart([UriPartial]::Authority)
        'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:DeploymentRevision' = $DeploymentRevision
        'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:ExpiryWarningMinutes' = $ExpiryWarningMinutes
    }
    $target = Resolve-OperationalPath $ConfigurationPath
    [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
    [IO.File]::WriteAllText($target, ($config | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    Write-Output 'SITE_CONFIGURATION=PREPARED_NOT_QUALIFIED'
    Write-Output "SITE_CONFIGURATION_PATH=$target"
    Write-Output 'SITE_QUALIFICATION_RECORD=REQUIRED_BEFORE_ACTIVATION'
    return
}

if ($Mode -ceq 'ProbeSite') {
    $configurationFile = Resolve-OperationalPath $ConfigurationPath
    if (-not (Test-Path -LiteralPath $configurationFile -PathType Leaf)) {
        throw 'SITE_CONFIGURATION_NOT_FOUND'
    }
    $configuration = Get-Content -LiteralPath $configurationFile -Raw | ConvertFrom-Json -AsHashtable
    $prefix = 'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:'
    $candidate = Resolve-OperationalPath $RecordPath
    & (Join-Path $PSScriptRoot 'Test-A3SiteRawIngressTransport.ps1') `
        -SiteId $configuration[$prefix + 'SiteId'] `
        -EndpointOrigin $configuration[$prefix + 'EndpointOrigin'] `
        -DeploymentRevision $configuration[$prefix + 'DeploymentRevision'] `
        -OutputRecordPath $candidate
    if ($InstallOnPass) {
        & (Join-Path $PSScriptRoot 'Install-A3SiteRawIngressTransportQualification.ps1') `
            -ConfigurationPath $configurationFile -CandidateRecordPath $candidate
    } else {
        Write-Output 'SITE_QUALIFICATION_INSTALLATION=PENDING_EXPLICIT_INSTALL_ON_PASS'
    }
    return
}

if ($SiteId -cne 'development-loopback' -or $DeploymentRevision -notmatch '^development-harness-') {
    throw 'DEVELOPMENT_HARNESS_CANNOT_IMPERSONATE_SITE'
}
$runner = Join-Path $repoRoot 'tests/TagEkyc.IntegrationTests/Run-A3IsolatedTlsLinux.ps1'
$record = Resolve-OperationalPath $RecordPath
$filter = 'FullyQualifiedName~ExpectFence_IntermediaryCannotQualifyBySendingItsOwnContinue'
if (Test-Path -LiteralPath $record) { Remove-Item -LiteralPath $record -Force }
& $runner -TestFilter $filter -ResultPrefix 'a3-site-qualification-a1' `
    -QualificationRecordPath $record -QualificationSiteId $SiteId `
    -QualificationDeploymentRevision $DeploymentRevision
if (-not (Test-Path -LiteralPath $record -PathType Leaf)) {
    throw 'Development harness did not emit its machine-readable PASS record.'
}
$evidence = Get-Content -LiteralPath $record -Raw | ConvertFrom-Json
if ($evidence.status -cne 'PASS' -or $evidence.siteId -cne 'development-loopback' -or
    $evidence.agentBodySendsWhileBOrR1Held -ne 0 -or
    $evidence.serverApplicationBodyReadsWhileBOrR1Held -ne 0 -or $evidence.rawPostCount -ne 1 -or
    -not $evidence.kestrelContinueRelayedAfterCommit -or
    $evidence.earlyOrIntermediaryContinueObserved -or $evidence.applicationPrebufferObserved -or
    $evidence.hiddenRetryObserved) { throw 'Development qualification record is not a qualifying PASS.' }
& $runner -TestFilter $filter -ResultPrefix 'a3-site-qualification-a2' `
    -EarlyContinueMutation -ExpectedTestFailure
$negative = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tests/TagEkyc.IntegrationTests/TestResults/a3-expect-fence') `
    -Filter 'a3-site-qualification-a2-*.trx' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -eq $negative) { throw 'F3 negative-control TRX was not retained.' }
[xml]$negativeTrx = Get-Content -LiteralPath $negative.FullName -Raw
if ([string]$negativeTrx.TestRun.ResultSummary.Output.StdOut -notmatch
    'intermediary-generated 100 released raw body before durable B/R1') {
    throw 'F3 failed for an unexpected reason; qualification is not discriminating.'
}
Write-Output 'DEVELOPMENT_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION=PASS'
Write-Output "QUALIFICATION_RECORD=$record"
Write-Output "QUALIFICATION_RECORD_SHA256=$((Get-FileHash -LiteralPath $record -Algorithm SHA256).Hash)"
Write-Output 'PRODUCTION_SITE_QUALIFIED=NO'
Write-Output "F3_NEGATIVE_CONTROL_TRX=$($negative.FullName)"
