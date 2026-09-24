param(
    [string]$ServerRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$AgentRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\TagEkyc.CaptureAgent')).Path,
    [string]$OutputDirectory = (Join-Path $ServerRoot 'docs\tips\tip_88c1_secure_raw_source_sealed_assembly')
)

$ErrorActionPreference = 'Stop'

function Clean([object]$value) {
    if ($null -eq $value) { return '' }
    return (([string]$value) -replace '[\t\r\n]+', ' ' -replace ' {2,}', ' ').Trim()
}

function Relative([string]$root, [string]$path) {
    return [IO.Path]::GetRelativePath($root, $path).Replace('\', '/')
}

function Read-Trx([IO.FileInfo]$file) {
    [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw
    $counters = $xml.TestRun.ResultSummary.Counters
    $failed = @($xml.SelectNodes("//*[local-name()='UnitTestResult'][@outcome='Failed']"))
    $first = $failed | Select-Object -First 1
    $message = if ($null -ne $first) {
        $executionId = [string]$first.executionId
        $result = $xml.SelectSingleNode("//*[local-name()='UnitTestResult'][@executionId='$executionId']")
        if ($null -ne $result.Output.ErrorInfo.Message) { $result.Output.ErrorInfo.Message } else { $result.Output.StdOut }
    } else { '' }
    $skipped = [int]$counters.notExecuted + [int]$counters.notRunnable + [int]$counters.inconclusive
    [pscustomobject]@{
        Total = [int]$counters.total
        Passed = [int]$counters.passed
        Failed = [int]$counters.failed
        Skipped = $skipped
        FailingTests = Clean (($failed | ForEach-Object testName) -join '; ')
        FirstFailure = Clean $message
    }
}

$prior = @{}
Get-ChildItem -LiteralPath $OutputDirectory -File -Filter '*failed_run_census*.tsv' |
    Where-Object Name -ne 'a3_p04_site_qualification_failed_run_census_v1.tsv' |
    ForEach-Object {
        $source = $_.Name
        try { $rows = @(Import-Csv -LiteralPath $_.FullName -Delimiter "`t") } catch { return }
        foreach ($row in $rows) {
            $pathProperty = $row.PSObject.Properties | Where-Object Name -CMatch '^(path|Path)$' | Select-Object -First 1
            $classProperty = $row.PSObject.Properties | Where-Object Name -CMatch '^(classification|Classification)$' | Select-Object -First 1
            if ($null -eq $pathProperty -or $null -eq $classProperty -or
                [string]::IsNullOrWhiteSpace($pathProperty.Value) -or [string]::IsNullOrWhiteSpace($classProperty.Value)) { continue }
            $name = [IO.Path]::GetFileName([string]$pathProperty.Value)
            if (-not $prior.ContainsKey($name)) {
                $prior[$name] = [pscustomobject]@{ Classification = [string]$classProperty.Value; Source = $source }
            } elseif ($prior[$name].Classification -cne [string]$classProperty.Value) {
                throw "PRIOR_CENSUS_CLASSIFICATION_CONFLICT trx=$name"
            }
        }
    }

$expected = @{
    'a3-p04-contract-a1.trx' = 3
    'a3-p04-contract-a2.trx' = 2
    'a3-p04-contract-a3.trx' = 2
    'a3-p04-contract-a4.trx' = 1
    'a3-p04-contract-a5.trx' = 3
    'a3-p04-startup-a1.trx' = 23
    'a3-p04-startup-a2.trx' = 3
    'a3-p04-startup-a3.trx' = 12
    'a3-p04-startup-a5.trx' = 24
    'a3-p04-sentinel-a1.trx' = 88
    'a3-p04-sentinel-a2.trx' = 107
    'a3-p04-sentinel-a4.trx' = 107
}

$inventory = [Collections.Generic.List[object]]::new()
$failedRows = [Collections.Generic.List[object]]::new()
$roots = @(
    [pscustomobject]@{ Repo = 'server'; Root = $ServerRoot },
    [pscustomobject]@{ Repo = 'agent'; Root = $AgentRoot }
)
$failedId = 0
foreach ($entry in $roots) {
    $files = @(Get-ChildItem -LiteralPath $entry.Root -Recurse -File -Filter '*.trx' -ErrorAction SilentlyContinue |
        Where-Object FullName -NotMatch '\\(bin|obj|\.git)\\' | Sort-Object FullName)
    foreach ($file in $files) {
        $trx = Read-Trx $file
        $relative = Relative $entry.Root $file.FullName
        $name = $file.Name
        $isCurrent = $relative -match 'TestResults/a3-p04/' -or $name -like 'a3-site-qualification-a[12]-*'
        $expectedCount = if ($expected.ContainsKey($name)) { [int]$expected[$name] } elseif ($name -like 'a3-site-qualification-a[12]-*') { 1 } else { $trx.Total }
        $countDisposition = if ($trx.Total -ne $expectedCount) { 'DIAGNOSTIC_COUNT_MISMATCH' }
            elseif ($trx.Skipped -gt 0) { 'DIAGNOSTIC_SKIP_PRESENT' }
            elseif ($trx.Failed -gt 0) { 'FAILED_RECORDED' }
            else { 'PASS_EXACT_COUNT' }
        if ($name -eq 'a3-p04-sentinel-a2.trx') { $countDisposition = 'EXCLUDED_DIAGNOSTIC_PARTIAL_SENTINEL' }
        $inventory.Add([pscustomobject][ordered]@{
            repo = $entry.Repo
            path = $relative
            sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            total = $trx.Total
            passed = $trx.Passed
            failed = $trx.Failed
            skipped = $trx.Skipped
            expected_cases = $expectedCount
            count_disposition = $countDisposition
            package_scope = if ($isCurrent) { 'P04_SITE_QUALIFICATION_SLICE' } else { 'WHOLE_REPO_HISTORICAL_INVENTORY' }
        })
        if ($trx.Failed -eq 0) { continue }

        $failedId++
        $classification = $null
        $classificationSource = $null
        $reason = $null
        $successor = ''
        switch -Wildcard ($name) {
            'a3-p04-contract-a2.trx' { $classification='EVIDENCE_RED'; $classificationSource='CURRENT_SLICE'; $reason='Removing only the Content-Encoding arm makes only its named case fail.'; $successor='a3-p04-contract-a5.trx' }
            'a3-p04-contract-a3.trx' { $classification='EVIDENCE_RED'; $classificationSource='CURRENT_SLICE'; $reason='Removing only the Trailer arm makes only its named case fail.'; $successor='a3-p04-contract-a5.trx' }
            'a3-p04-contract-a4.trx' { $classification='EVIDENCE_RED'; $classificationSource='CURRENT_SLICE'; $reason='Moving framing rejection after authentication commits one nonce and fails the independent database assertion.'; $successor='a3-p04-contract-a5.trx' }
            'a3-p04-startup-a2.trx' { $classification='EVIDENCE_RED'; $classificationSource='CURRENT_SLICE'; $reason='Removing only the site-qualification startup gate makes all missing stale and FAIL cases stop throwing.'; $successor='a3-p04-startup-a5.trx' }
            'a3-p04-startup-a3.trx' { $classification='EXCLUDED_DIAGNOSTIC'; $classificationSource='CURRENT_SLICE'; $reason='The current generated seal correctly became INVALID after current source and evidence bytes drifted; the test preserves the stronger INCOMPLETE expectation and was not weakened.'; $successor='a3-p04-startup-a5.trx' }
            'a3-site-qualification-a2-*' { $classification='EVIDENCE_RED_TEST_TOPOLOGY'; $classificationSource='CURRENT_SLICE'; $reason='The F3 intermediary-generated 100 releases raw body before durable B/R1, so the executable site qualification must fail.'; $successor='' }
            default {
                if ($prior.ContainsKey($name)) {
                    $classification = $prior[$name].Classification
                    $classificationSource = $prior[$name].Source
                    $reason = 'Historical failed run retained with its prior canonical failed-run classification; it is not used by the current package.'
                } else {
                    $classification = 'EXCLUDED_HISTORICAL_OUTSIDE_SLICE'
                    $classificationSource = 'WHOLE_REPO_SWEEP'
                    $reason = 'Historical failed run is outside the bounded P04/site-qualification slice and is inventoried without being used as current evidence.'
                }
            }
        }
        $failedRows.Add([pscustomobject][ordered]@{
            id = ('P04FR{0:D4}' -f $failedId)
            repo = $entry.Repo
            path = $relative
            sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            total = $trx.Total
            passed = $trx.Passed
            failed = $trx.Failed
            skipped = $trx.Skipped
            failing_tests = $trx.FailingTests
            first_assertion_or_exception = $trx.FirstFailure
            classification = $classification
            classification_source = $classificationSource
            successor_trx = $successor
            reason = $reason
        })
    }
}

$inventoryPath = Join-Path $OutputDirectory 'a3_p04_site_qualification_whole_repo_trx_inventory_v1.tsv'
$failedPath = Join-Path $OutputDirectory 'a3_p04_site_qualification_failed_run_census_v1.tsv'
$inventory | Export-Csv -LiteralPath $inventoryPath -Delimiter "`t" -NoTypeInformation -Encoding utf8
$failedRows | Export-Csv -LiteralPath $failedPath -Delimiter "`t" -NoTypeInformation -Encoding utf8

"trx_total=$($inventory.Count)"
"trx_server=$(@($inventory | Where-Object repo -EQ server).Count)"
"trx_agent=$(@($inventory | Where-Object repo -EQ agent).Count)"
"failed_total=$($failedRows.Count)"
"failed_unclassified=$(@($failedRows | Where-Object { [string]::IsNullOrWhiteSpace($_.classification) }).Count)"
"count_mismatch=$(@($inventory | Where-Object count_disposition -EQ DIAGNOSTIC_COUNT_MISMATCH).Count)"
"inventory_sha256=$((Get-FileHash -LiteralPath $inventoryPath -Algorithm SHA256).Hash)"
"failed_census_sha256=$((Get-FileHash -LiteralPath $failedPath -Algorithm SHA256).Hash)"
