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

function Read-Trx([IO.FileInfo]$file) {
    [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw
    $counters = $xml.TestRun.ResultSummary.Counters
    $failed = @($xml.SelectNodes("//*[local-name()='UnitTestResult'][@outcome='Failed']"))
    $first = $failed | Select-Object -First 1
    $message = if ($null -ne $first) { $first.Output.ErrorInfo.Message } else { '' }
    [pscustomobject]@{
        Total = [int]$counters.total
        Passed = [int]$counters.passed
        Failed = [int]$counters.failed
        Skipped = [int]$counters.notExecuted + [int]$counters.notRunnable + [int]$counters.inconclusive
        FailingTests = Clean (($failed | ForEach-Object testName) -join '; ')
        FirstFailure = Clean $message
    }
}

$expected = @{
    'raw-export-delivery-a1.trx'=2; 'raw-export-delivery-a2.trx'=2
    'raw-export-delivery-a3.trx'=1; 'raw-export-delivery-a4.trx'=1
    'raw-export-delivery-a5.trx'=1; 'raw-export-delivery-a6.trx'=1
    'raw-export-delivery-a7.trx'=1; 'raw-export-delivery-a8.trx'=1
    'raw-export-delivery-a9.trx'=1; 'raw-export-delivery-a10.trx'=1
    'raw-export-delivery-a11.trx'=2; 'raw-export-delivery-a12.trx'=2
    'raw-export-delivery-a13.trx'=88; 'raw-export-delivery-a14.trx'=24
    'raw-export-delivery-a15.trx'=107; 'raw-export-delivery-a16.trx'=107
    'raw-export-delivery-a17.trx'=42; 'raw-export-delivery-a18.trx'=8
    'raw-export-delivery-a19.trx'=107; 'raw-export-delivery-a20.trx'=2
    'raw-export-delivery-a21.trx'=107
}

$currentFailures = @{
    'a3-raw-export-work-source-current.trx' = @('EXCLUDED_INFRASTRUCTURE_DIAGNOSTIC','Docker/PostgreSQL fixture was unavailable before the business assertion.','a3-raw-export-work-source-current-v6.trx')
    'a3-raw-export-work-source-current-v2.trx' = @('SUPERSEDED_FIXTURE_RED','Early durable-source fixture predecessor failed before the final bounded contract was established.','a3-raw-export-work-source-current-v6.trx')
    'a3-raw-export-work-source-current-v3.trx' = @('SUPERSEDED_FIXTURE_RED','Early durable-source fixture predecessor failed before the final bounded contract was established.','a3-raw-export-work-source-current-v6.trx')
    'a3-raw-export-work-source-current-v4.trx' = @('SUPERSEDED_FIXTURE_RED','Early durable-source fixture predecessor failed before the final bounded contract was established.','a3-raw-export-work-source-current-v6.trx')
    'a3-raw-export-work-source-current-v5.trx' = @('SUPERSEDED_FIXTURE_RED','Early durable-source fixture predecessor failed before the final bounded contract was established.','a3-raw-export-work-source-current-v6.trx')
    'raw-export-delivery-a3.trx' = @('EVIDENCE_RED','Changing only the stored fencing token to fence minus one makes durable acquisition fail.','raw-export-delivery-a12.trx')
    'raw-export-delivery-a4.trx' = @('EVIDENCE_RED','Replacing only the stored principal with an empty identity makes durable acquisition fail.','raw-export-delivery-a12.trx')
    'raw-export-delivery-a5.trx' = @('EVIDENCE_RED','Removing only the HTTP authentication stop changes the public result from 403 to 200.','raw-export-delivery-a12.trx')
    'raw-export-delivery-a6.trx' = @('SUPERSEDED_FIXTURE_RED','The first clean-migration fixture created unequal permit/job deadlines and hit the schema constraint before the target assertion.','raw-export-delivery-a9.trx')
    'raw-export-delivery-a10.trx' = @('EVIDENCE_RED','Removing only the JobExpiresAt predicate makes the SQL selector return an expired job.','raw-export-delivery-a12.trx')
    'raw-export-delivery-a14.trx' = @('SUPERSEDED_PARTIAL_SENTINEL','The build graph exposed only 24 of the required 107 Server cases and all were blocked by stale fixture assumptions.','raw-export-delivery-a21.trx')
    'raw-export-delivery-a15.trx' = @('SUPERSEDED_FIXTURE_RED','All 107 current-byte cases exposed missing site-qualification fixture setup and the obsolete expected-last-migration helper.','raw-export-delivery-a21.trx')
    'raw-export-delivery-a16.trx' = @('SUPERSEDED_FIXTURE_RED','The first fixture correction lacked the runtime gate/seal registration and left the Kestrel harness unqualified.','raw-export-delivery-a21.trx')
    'raw-export-delivery-a19.trx' = @('SUPERSEDED_FIXTURE_RED','The shared fixture correction left two terminal-projection TestServer cases without qualification services.','raw-export-delivery-a21.trx')
}

$inventory = [Collections.Generic.List[object]]::new()
$failedRows = [Collections.Generic.List[object]]::new()
$roots = @(
    [pscustomobject]@{ Repo='server'; Root=$ServerRoot },
    [pscustomobject]@{ Repo='agent'; Root=$AgentRoot }
)
$failedId = 0
foreach ($entry in $roots) {
    $files = @(Get-ChildItem -LiteralPath $entry.Root -Recurse -File -Filter '*.trx' -ErrorAction SilentlyContinue |
        Where-Object FullName -NotMatch '\\(bin|obj|\.git)\\' | Sort-Object FullName)
    foreach ($file in $files) {
        $trx = Read-Trx $file
        $relative = [IO.Path]::GetRelativePath($entry.Root,$file.FullName).Replace('\','/')
        $name = $file.Name
        $scope = if ($relative -match 'TestResults/raw-export-delivery/') { 'RAW_EXPORT_DELIVERY_SLICE' } else { 'WHOLE_REPO_HISTORICAL_INVENTORY' }
        $expectedCount = if ($expected.ContainsKey($name) -and $scope -eq 'RAW_EXPORT_DELIVERY_SLICE') { [int]$expected[$name] } else { $trx.Total }
        $inventory.Add([pscustomobject][ordered]@{
            repo=$entry.Repo; path=$relative
            sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            total=$trx.Total; passed=$trx.Passed; failed=$trx.Failed; skipped=$trx.Skipped
            expected_cases=$expectedCount
            count_disposition=if($trx.Total -ne $expectedCount){'DIAGNOSTIC_COUNT_MISMATCH'}elseif($trx.Skipped -gt 0){'DIAGNOSTIC_SKIP_PRESENT'}elseif($trx.Failed -gt 0){'FAILED_RECORDED'}else{'PASS_EXACT_COUNT'}
            package_scope=$scope
        })
        if ($trx.Failed -eq 0) { continue }
        $failedId++
        if ($scope -eq 'RAW_EXPORT_DELIVERY_SLICE' -and $currentFailures.ContainsKey($name)) {
            $data=$currentFailures[$name]; $classification=$data[0]; $reason=$data[1]; $successor=$data[2]; $source='CURRENT_SLICE'
        } else {
            $classification='EXCLUDED_HISTORICAL_OUTSIDE_SLICE'; $reason='Historical failed run is inventoried but is not used as evidence by this delivery packet.'; $successor=''; $source='WHOLE_REPO_SWEEP'
        }
        $failedRows.Add([pscustomobject][ordered]@{
            id=('RDEFR{0:D4}' -f $failedId); repo=$entry.Repo; path=$relative
            sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            total=$trx.Total; passed=$trx.Passed; failed=$trx.Failed; skipped=$trx.Skipped
            failing_tests=$trx.FailingTests; first_assertion_or_exception=$trx.FirstFailure
            classification=$classification; classification_source=$source; successor_trx=$successor; reason=$reason
        })
    }
}

$inventoryPath=Join-Path $OutputDirectory 'raw_export_delivery_whole_repo_trx_inventory_v1.tsv'
$failedPath=Join-Path $OutputDirectory 'raw_export_delivery_failed_run_census_v1.tsv'
$inventory | Export-Csv -LiteralPath $inventoryPath -Delimiter "`t" -NoTypeInformation -Encoding utf8
$failedRows | Export-Csv -LiteralPath $failedPath -Delimiter "`t" -NoTypeInformation -Encoding utf8

"trx_total=$($inventory.Count)"
"trx_server=$(@($inventory|Where-Object repo -EQ server).Count)"
"trx_agent=$(@($inventory|Where-Object repo -EQ agent).Count)"
"failed_total=$($failedRows.Count)"
"failed_unclassified=$(@($failedRows|Where-Object { [string]::IsNullOrWhiteSpace($_.classification) }).Count)"
"current_slice_failed=$(@($failedRows|Where-Object classification_source -EQ CURRENT_SLICE).Count)"
"count_mismatch=$(@($inventory|Where-Object count_disposition -EQ DIAGNOSTIC_COUNT_MISMATCH).Count)"
"inventory_sha256=$((Get-FileHash -LiteralPath $inventoryPath -Algorithm SHA256).Hash)"
"failed_census_sha256=$((Get-FileHash -LiteralPath $failedPath -Algorithm SHA256).Hash)"
