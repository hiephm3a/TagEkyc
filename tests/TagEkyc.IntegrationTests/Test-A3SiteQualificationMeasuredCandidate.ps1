param([string]$ActualHostileReportPath)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$tools = Join-Path $repoRoot 'tools'
$scratch = Join-Path ([IO.Path]::GetTempPath()) "tagekyc-site-measured-$([Guid]::NewGuid().ToString('N'))"

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Clone-Value([object]$Value) {
    $Value | ConvertTo-Json -Depth 8 -Compress | ConvertFrom-Json
}

try {
    [IO.Directory]::CreateDirectory($scratch) | Out-Null
    $probePath = Join-Path $tools 'Test-A3SiteRawIngressTransport.ps1'
    $tokens = $null
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($probePath, [ref]$tokens, [ref]$errors)
    Assert-True ($errors.Count -eq 0) 'Probe tool did not parse.'
    foreach ($name in @('Read-SyntheticAgentMeasurements','Write-SiteQualificationCandidate')) {
        $function = @($ast.FindAll({ param($node)
            $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
            $node.Name -ceq $name
        }, $true))
        Assert-True ($function.Count -eq 1) "Exact probe function $name was not found."
        . ([ScriptBlock]::Create($function[0].Extent.Text))
    }
    $invokeTokens = $null
    $invokeErrors = $null
    $invokeAst = [Management.Automation.Language.Parser]::ParseFile(
        (Join-Path $tools 'Invoke-A3SiteRawIngressTransportQualification.ps1'),
        [ref]$invokeTokens, [ref]$invokeErrors)
    Assert-True ($invokeErrors.Count -eq 0) 'Invoke tool did not parse.'
    $completion = @($invokeAst.FindAll({ param($node)
        $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
        $node.Name -ceq 'Complete-ProbeSiteCandidate'
    }, $true))
    Assert-True ($completion.Count -eq 1) 'Exact ProbeSite completion function was not found.'
    . ([ScriptBlock]::Create($completion[0].Extent.Text))

    $now = [DateTimeOffset]::UtcNow
    $suiteId = [Guid]::NewGuid()
    $safeReport = [pscustomobject][ordered]@{
        QualificationRunId = [Guid]::NewGuid()
        QualificationSuiteId = $suiteId
        SiteId = 'synthetic-site'
        EndpointOrigin = 'https://synthetic.invalid:8443'
        DeploymentRevision = 'synthetic-revision'
        State = 'BrokerCommitted'
        Mode = 'FullBodyHeldCommit'
        CreatedAtUtc = $now.ToString('O')
        ExpiresAtUtc = $now.AddMinutes(1).ToString('O')
        ConsumedAtUtc = $now.ToString('O')
        BrokerHeldAtUtc = $now.ToString('O')
        BrokerCommittedAtUtc = $now.ToString('O')
        RawPostCount = 1
        ServerApplicationBodyReadsWhileBrokerHeld = 0
        ClientTransportEntryCount = 1
        ContentBytesCopied = 17
        AgentBodyBytesSentWhileBrokerHeld = 0
        ObservedContinue = $true
        ContinueObservedBeforeBrokerCommit = $false
        ApplicationPrebufferObserved = $false
        FinalResponseObserved = $true
        AgentObservationCompleted = $true
        ServerObservationCompleted = $true
        EvidenceComplete = $true
        HiddenRetryObserved = $false
        KestrelContinueRelayedAfterCommit = $true
    }
    $lostReport = Clone-Value $safeReport
    $lostReport.QualificationRunId = [Guid]::NewGuid()
    $lostReport.Mode = 'LostFinalNoRetry'
    $lostReport.FinalResponseObserved = $false
    $safe = [pscustomobject][ordered]@{
        SchemaVersion = 1
        Status = 'MEASURED'
        Qualifies = $true
        Reports = @($safeReport, $lostReport)
    }
    $safePath = Join-Path $scratch 'safe-measurement.json'
    [IO.File]::WriteAllText($safePath, ($safe | ConvertTo-Json -Depth 8 -Compress),
        [Text.UTF8Encoding]::new($false))
    $readSafe = Read-SyntheticAgentMeasurements $safePath
    $candidatePath = Join-Path $scratch 'candidate.json'
    $candidate = Write-SiteQualificationCandidate 'synthetic-site' `
        'https://synthetic.invalid:8443' 'synthetic-revision' $candidatePath 8 $readSafe $false
    $record = Get-Content -LiteralPath $candidate.Path -Raw | ConvertFrom-Json
    Assert-True (@($record.PSObject.Properties).Count -eq 15) 'PASS candidate must contain 15 fields.'
    Assert-True ([string]$record.status -ceq 'PASS') 'Safe measurements did not mint PASS.'

    $target = Join-Path $scratch 'installed.json'
    $configuration = Join-Path $scratch 'configuration.json'
    $config = [ordered]@{
        'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualificationRecordPath' = $target
        'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:SiteId' = 'synthetic-site'
        'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:EndpointOrigin' = 'https://synthetic.invalid:8443'
        'TagEkyc:CaptureRuntime:SiteRawIngressTransportQualification:DeploymentRevision' = 'synthetic-revision'
    }
    [IO.File]::WriteAllText($configuration, ($config | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    & (Join-Path $tools 'Install-A3SiteRawIngressTransportQualification.ps1') `
        -ConfigurationPath $configuration -CandidateRecordPath $candidate.Path | Out-Null
    Assert-True (Test-Path -LiteralPath $target -PathType Leaf) 'Safe PASS candidate was not installed.'
    $installedHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash

    $cases = @(
        @('agent-held', 'AgentBodyBytesSentWhileBrokerHeld', 1, $false, 'agentBodySendsWhileBOrR1Held', 1),
        @('server-held', 'ServerApplicationBodyReadsWhileBrokerHeld', 1, $false, 'serverApplicationBodyReadsWhileBOrR1Held', 1),
        @('raw-post', 'RawPostCount', 2, $false, 'rawPostCount', 2),
        @('continue-order', 'KestrelContinueRelayedAfterCommit', $false, $false, 'kestrelContinueRelayedAfterCommit', $false),
        @('early-continue', $null, $null, $true, 'earlyOrIntermediaryContinueObserved', $true),
        @('prebuffer', 'ApplicationPrebufferObserved', $true, $false, 'applicationPrebufferObserved', $true),
        @('hidden-retry', 'HiddenRetryObserved', $true, $false, 'hiddenRetryObserved', $true)
    )
    foreach ($case in $cases) {
        $bad = Clone-Value $safe
        if ($null -ne $case[1]) { $bad.Reports[0].($case[1]) = $case[2] }
        $bad.Qualifies = $false
        $badPath = Join-Path $scratch "$($case[0]).json"
        [IO.File]::WriteAllText($badPath, ($bad | ConvertTo-Json -Depth 8 -Compress),
            [Text.UTF8Encoding]::new($false))
        $readBad = Read-SyntheticAgentMeasurements $badPath
        $badCandidatePath = Join-Path $scratch "$($case[0])-candidate.json"
        $badCandidate = Write-SiteQualificationCandidate 'synthetic-site' `
            'https://synthetic.invalid:8443' 'synthetic-revision' $badCandidatePath 8 `
            $readBad ([bool]$case[3])
        $badRecord = Get-Content -LiteralPath $badCandidate.Path -Raw | ConvertFrom-Json
        Assert-True (@($badRecord.PSObject.Properties).Count -eq 15) "$($case[0]) changed schema shape."
        Assert-True ([string]$badRecord.status -ceq 'FAIL') "$($case[0]) did not fail qualification."
        Assert-True ($badRecord.($case[4]) -eq $case[5]) "$($case[0]) did not retain its bad value."
        $reason = $null
        try {
            & (Join-Path $tools 'Install-A3SiteRawIngressTransportQualification.ps1') `
                -ConfigurationPath $configuration -CandidateRecordPath $badCandidate.Path | Out-Null
        } catch { $reason = $_.Exception.Message }
        Assert-True ($reason -ceq 'SITE_QUALIFICATION_RECORD_DOES_NOT_MATCH_CONFIGURATION') `
            "$($case[0]) was not rejected by the direct installer."
        $probeReason = $null
        try { Complete-ProbeSiteCandidate $configuration $badCandidate.Path $true | Out-Null }
        catch { $probeReason = $_.Exception.Message }
        Assert-True ($probeReason -ceq 'SITE_QUALIFICATION_INSTALL_BLOCKED_FAIL') `
            "$($case[0]) was not blocked by ProbeSite -InstallOnPass."
        Assert-True ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ceq $installedHash) `
            "$($case[0]) replaced the installed qualification."
    }

    if (-not [string]::IsNullOrWhiteSpace($ActualHostileReportPath)) {
        $actualHostileSha256 = (Get-FileHash -LiteralPath $ActualHostileReportPath -Algorithm SHA256).Hash
        $reducerRunAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        $hostile = Get-Content -LiteralPath $ActualHostileReportPath -Raw | ConvertFrom-Json
        Assert-True ([bool]$hostile.ContinueObservedBeforeBrokerCommit) `
            'Actual F3 report did not retain early/intermediary Continue.'
        Assert-True (-not [bool]$hostile.KestrelContinueRelayedAfterCommit) `
            'Actual F3 report did not retain the bad Continue ordering.'
        $lostCompanion = Clone-Value $hostile
        $lostCompanion.QualificationRunId = [Guid]::NewGuid()
        $lostCompanion.Mode = 'LostFinalNoRetry'
        $lostCompanion.FinalResponseObserved = $false
        $hostileMeasurement = [pscustomobject][ordered]@{
            SchemaVersion = 1
            Status = 'MEASURED'
            Qualifies = $false
            Reports = @($hostile, $lostCompanion)
        }
        $hostileMeasurementPath = Join-Path $scratch 'actual-f3-measurement.json'
        [IO.File]::WriteAllText($hostileMeasurementPath,
            ($hostileMeasurement | ConvertTo-Json -Depth 8 -Compress),
            [Text.UTF8Encoding]::new($false))
        $readHostile = Read-SyntheticAgentMeasurements $hostileMeasurementPath
        $hostileCandidatePath = Join-Path $scratch 'actual-f3-candidate.json'
        $hostileCandidate = Write-SiteQualificationCandidate ([string]$hostile.SiteId) `
            ([string]$hostile.EndpointOrigin) ([string]$hostile.DeploymentRevision) `
            $hostileCandidatePath 8 $readHostile $false
        $hostileRecord = Get-Content -LiteralPath $hostileCandidate.Path -Raw | ConvertFrom-Json
        Assert-True ([string]$hostileRecord.status -ceq 'FAIL') `
            'Actual hostile F3 report did not reduce to FAIL.'
        Assert-True (-not [bool]$hostileRecord.earlyOrIntermediaryContinueObserved) `
            'Header-only early-Continue input was not held false for the discriminating proof.'
        Assert-True ([int]$hostileRecord.agentBodySendsWhileBOrR1Held -gt 0) `
            'Actual hostile full-body send measurement was lost by the reducer.'
        Assert-True ([bool]$hostileRecord.applicationPrebufferObserved) `
            'Actual hostile full-body prebuffer measurement was lost by the reducer.'
        Assert-True (-not [bool]$hostileRecord.kestrelContinueRelayedAfterCommit) `
            'Actual hostile F3 ordering was lost by the reducer.'
        Write-Output "ACTUAL_F3_INPUT_SHA256=$actualHostileSha256"
        Write-Output "ACTUAL_F3_REDUCER_RAN_AT_UTC=$reducerRunAtUtc"
        Write-Output 'ACTUAL_F3_HEADER_ONLY_EARLY_CONTINUE=false'
        Write-Output 'ACTUAL_F3_FULL_BODY_REPORT_TO_CANDIDATE=FAIL'
    }

    Write-Output 'MEASURED_CANDIDATE_POSITIVE=PASS'
    Write-Output 'MEASURED_CANDIDATE_FIELD_COUNT=15'
    Write-Output 'MEASUREMENT_NEGATIVE_GUARDS=7/7'
    Write-Output 'DIRECT_INSTALLER_BAD_MEASUREMENT=BLOCKED'
    Write-Output 'TARGET_QUALIFICATION_REPLACED=NO'
}
finally {
    if ($scratch.StartsWith([IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $scratch)) {
        Remove-Item -LiteralPath $scratch -Recurse -Force
    }
}
