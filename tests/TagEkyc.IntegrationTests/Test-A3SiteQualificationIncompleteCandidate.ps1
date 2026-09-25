param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$tools = Join-Path $repoRoot 'tools'
$scratch = Join-Path ([IO.Path]::GetTempPath()) "tagekyc-site-incomplete-$([Guid]::NewGuid().ToString('N'))"

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

try {
    [IO.Directory]::CreateDirectory($scratch) | Out-Null
    $candidate = Join-Path $scratch 'candidate.json'
    $target = Join-Path $scratch 'installed.json'
    $configuration = Join-Path $scratch 'site-configuration.json'

    $probeOutput = @(& (Join-Path $tools 'Test-A3SiteRawIngressTransport.ps1') `
        -SelfTest -SelfTestOutputRecordPath $candidate)
    Assert-True ($probeOutput -contains 'SITE_ENDPOINT_TRANSPORT_QUALIFICATION=MEASUREMENT_INCOMPLETE') `
        'Probe generator did not report MEASUREMENT_INCOMPLETE.'
    Assert-True (-not ($probeOutput -contains 'SITE_ENDPOINT_TRANSPORT_QUALIFICATION=PASS')) `
        'Probe generator retained false PASS output.'
    $record = Get-Content -LiteralPath $candidate -Raw | ConvertFrom-Json
    Assert-True (@($record.PSObject.Properties).Count -eq 15) 'Candidate must contain exactly 15 fields.'
    Assert-True ([string]$record.status -ceq 'MEASUREMENT_INCOMPLETE') `
        'Candidate status must be MEASUREMENT_INCOMPLETE.'

    $prepareOutput = @(& (Join-Path $tools 'Invoke-A3SiteRawIngressTransportQualification.ps1') `
        -Mode PrepareSite -SiteId 'synthetic-site' `
        -EndpointOrigin 'https://synthetic.invalid:8443' `
        -DeploymentRevision 'synthetic-deployment-1' `
        -RecordPath $target -ConfigurationPath $configuration)
    Assert-True ($prepareOutput -contains 'SITE_CONFIGURATION=PREPARED_NOT_QUALIFIED') `
        'PrepareSite did not preserve prepared-not-qualified behavior.'

    $sentinel = [Text.Encoding]::UTF8.GetBytes('existing-target-must-survive')
    [IO.File]::WriteAllBytes($target, $sentinel)

    $invokePath = Join-Path $tools 'Invoke-A3SiteRawIngressTransportQualification.ps1'
    $tokens = $null
    $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($invokePath, [ref]$tokens, [ref]$errors)
    Assert-True ($errors.Count -eq 0) 'Invoke tool did not parse.'
    $function = @($ast.FindAll({ param($node)
        $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
        $node.Name -ceq 'Complete-ProbeSiteCandidate'
    }, $true))
    Assert-True ($function.Count -eq 1) 'Exact ProbeSite completion function was not found.'
    . ([ScriptBlock]::Create($function[0].Extent.Text))

    $diagnosticOutput = @(Complete-ProbeSiteCandidate $configuration $candidate $false)
    Assert-True ($diagnosticOutput -contains
        'SITE_QUALIFICATION_INSTALLATION=BLOCKED_MEASUREMENT_INCOMPLETE') `
        'ProbeSite did not report the incomplete candidate as blocked.'

    $blockedOutput = @()
    $blockedReason = $null
    try {
        $blockedOutput = @(Complete-ProbeSiteCandidate $configuration $candidate $true)
    } catch {
        $blockedReason = $_.Exception.Message
        $blockedOutput += $_.Exception.ErrorRecord.TargetObject
    }
    Assert-True ($blockedReason -ceq 'SITE_QUALIFICATION_INSTALL_BLOCKED_MEASUREMENT_INCOMPLETE') `
        'ProbeSite InstallOnPass did not block the incomplete candidate.'
    Assert-True ([Convert]::ToBase64String([IO.File]::ReadAllBytes($target)) -ceq
        [Convert]::ToBase64String($sentinel)) `
        'ProbeSite InstallOnPass replaced the target qualification.'

    $installerReason = $null
    try {
        & (Join-Path $tools 'Install-A3SiteRawIngressTransportQualification.ps1') `
            -ConfigurationPath $configuration -CandidateRecordPath $candidate
    } catch { $installerReason = $_.Exception.Message }
    Assert-True ($installerReason -ceq 'SITE_QUALIFICATION_RECORD_DOES_NOT_MATCH_CONFIGURATION') `
        'Direct installer did not reject the incomplete candidate.'
    Assert-True ([Convert]::ToBase64String([IO.File]::ReadAllBytes($target)) -ceq
        [Convert]::ToBase64String($sentinel)) `
        'Direct installer replaced the target qualification.'

    Write-Output 'PROBE_SITE_PASS_MINTING=BLOCKED'
    Write-Output 'CANDIDATE_STATUS=MEASUREMENT_INCOMPLETE'
    Write-Output 'CANDIDATE_FIELD_COUNT=15'
    Write-Output 'PROBE_SITE_INSTALL_ON_PASS=BLOCKED'
    Write-Output 'DIRECT_INSTALLER=BLOCKED'
    Write-Output 'TARGET_QUALIFICATION_REPLACED=NO'
    Write-Output 'PREPARE_SITE=PASS'
}
finally {
    if ($scratch.StartsWith([IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $scratch)) {
        Remove-Item -LiteralPath $scratch -Recurse -Force
    }
}
