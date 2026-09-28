param(
    [ValidateSet('DevelopmentHarness','PrepareSite','ProbeSite')][string]$Mode = 'DevelopmentHarness',
    [string]$SiteId = 'development-loopback',
    [string]$EndpointOrigin,
    [string]$DeploymentRevision = 'development-harness-v1',
    [string]$RecordPath = 'TestResults/a3-site-qualification/development-transport-qualification.json',
    [string]$CandidateRecordPath = 'TestResults/a3-site-qualification/site-transport-qualification-candidate.json',
    [string]$ConfigurationPath = 'TestResults/a3-site-qualification/site-configuration.json',
    [ValidateRange(0,10080)][int]$ExpiryWarningMinutes = 1440,
    [string]$SyntheticAgentExecutablePath,
    [switch]$InstallOnPass
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
function Resolve-OperationalPath([string]$Path) {
    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    [IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Get-JsonProperty([object]$Value, [string]$Name) {
    $property = $Value.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    $property.Value
}

function Assert-ProbeCandidateIsSeparate([string]$InstalledTarget, [string]$Candidate) {
    if ([string]::IsNullOrWhiteSpace($InstalledTarget) -or
        -not [IO.Path]::IsPathRooted($InstalledTarget) -or
        [string]::Equals([IO.Path]::GetFullPath($InstalledTarget), $Candidate,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw 'SITE_QUALIFICATION_CANDIDATE_MUST_NOT_BE_INSTALLED_TARGET'
    }
}

function Read-SyntheticAgentMeasurementOutput([string[]]$Lines, [int]$ExitCode) {
    $measurement = $null
    for ($index = $Lines.Count - 1; $index -ge 0; $index--) {
        $line = $Lines[$index]
        if (-not $line.TrimStart().StartsWith('{')) { continue }
        try {
            $parsed = $line | ConvertFrom-Json
            if ($parsed.SchemaVersion -eq 1 -and [string]$parsed.Status -ceq 'MEASURED') {
                $measurement = $parsed
                break
            }
        } catch { }
    }
    if ($null -eq $measurement -or (($ExitCode -eq 0) -ne [bool]$measurement.Qualifies)) {
        throw 'SITE_QUALIFICATION_SYNTHETIC_AGENT_MEASUREMENT_INVALID'
    }
    $measurement
}

function Complete-ProbeSiteCandidate(
    [string]$ConfigurationFile,
    [string]$CandidateRecord,
    [bool]$InstallCandidate) {
    $candidateValue = Get-Content -LiteralPath $CandidateRecord -Raw | ConvertFrom-Json
    if ([string]$candidateValue.status -cne 'PASS') {
        Write-Output "SITE_QUALIFICATION_INSTALLATION=BLOCKED_$([string]$candidateValue.status)"
        if ($InstallCandidate) {
            throw "SITE_QUALIFICATION_INSTALL_BLOCKED_$([string]$candidateValue.status)"
        }
        return
    }
    if ($InstallCandidate) {
        & (Join-Path $PSScriptRoot 'Install-A3SiteRawIngressTransportQualification.ps1') `
            -ConfigurationPath $ConfigurationFile -CandidateRecordPath $CandidateRecord
    } else {
        Write-Output 'SITE_QUALIFICATION_INSTALLATION=PENDING_EXPLICIT_INSTALL_ON_PASS'
    }
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
    $configuration = Get-Content -LiteralPath $configurationFile -Raw | ConvertFrom-Json
    $prefix = 'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:'
    $candidate = Resolve-OperationalPath $CandidateRecordPath
    $installedTarget = [string](Get-JsonProperty $configuration 'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualificationRecordPath')
    Assert-ProbeCandidateIsSeparate $installedTarget $candidate
    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        Remove-Item -LiteralPath $candidate -Force
    }
    $measurementPath = $null
    try {
        if (-not [string]::IsNullOrWhiteSpace($SyntheticAgentExecutablePath)) {
            if (-not [IO.Path]::IsPathRooted($SyntheticAgentExecutablePath) -or
                [IO.Path]::GetExtension($SyntheticAgentExecutablePath) -cne '.exe' -or
                -not (Test-Path -LiteralPath $SyntheticAgentExecutablePath -PathType Leaf)) {
                throw 'SITE_QUALIFICATION_SYNTHETIC_AGENT_EXECUTABLE_INVALID'
            }
            if ([string]::IsNullOrWhiteSpace($env:TAGEKYC_SITE_QUALIFICATION_API_KEY)) {
                throw 'SITE_QUALIFICATION_API_KEY_MISSING'
            }
            $start = [Diagnostics.ProcessStartInfo]::new()
            $start.FileName = $SyntheticAgentExecutablePath
            $start.Arguments = 'qualify-site'
            $start.UseShellExecute = $false
            $start.CreateNoWindow = $true
            $start.RedirectStandardOutput = $true
            $start.RedirectStandardError = $true
            $process = [Diagnostics.Process]::new()
            $process.StartInfo = $start
            if (-not $process.Start()) { throw 'SITE_QUALIFICATION_SYNTHETIC_AGENT_START_FAILED' }
            $stdout = $process.StandardOutput.ReadToEnd()
            $stderr = $process.StandardError.ReadToEnd()
            $process.WaitForExit()
            $agentExitCode = $process.ExitCode
            $process.Dispose()
            $agentOutput = @($stdout -split "`r?`n")
            try { $agentMeasurement = Read-SyntheticAgentMeasurementOutput $agentOutput $agentExitCode }
            catch { throw "SITE_QUALIFICATION_SYNTHETIC_AGENT_MEASUREMENT_INVALID exit=$agentExitCode stderr=$($stderr.Trim())" }
            $measurementPath = Join-Path ([IO.Path]::GetTempPath()) `
                "tagekyc-site-measurement-$([Guid]::NewGuid().ToString('N')).json"
            [IO.File]::WriteAllText($measurementPath,
                ($agentMeasurement | ConvertTo-Json -Depth 8 -Compress),
                [Text.UTF8Encoding]::new($false))
        }
        & (Join-Path $PSScriptRoot 'Test-A3SiteRawIngressTransport.ps1') `
            -SiteId (Get-JsonProperty $configuration ($prefix + 'SiteId')) `
            -EndpointOrigin (Get-JsonProperty $configuration ($prefix + 'EndpointOrigin')) `
            -DeploymentRevision (Get-JsonProperty $configuration ($prefix + 'DeploymentRevision')) `
            -OutputRecordPath $candidate -MeasurementReportPath $measurementPath
        Complete-ProbeSiteCandidate $configurationFile $candidate $InstallOnPass.IsPresent
    }
    finally {
        if ($null -ne $measurementPath -and
            $measurementPath.StartsWith([IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase) -and
            (Test-Path -LiteralPath $measurementPath -PathType Leaf)) {
            Remove-Item -LiteralPath $measurementPath -Force
        }
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
