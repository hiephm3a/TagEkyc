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
    'raw-export-delivery-correction-a3-fence-mutant.trx'=1
    'raw-export-delivery-correction-a4-valid-actor-mutant.trx'=1
    'raw-export-delivery-correction-c5-authorize-scope-mutant.trx'=1
    'raw-export-delivery-correction-c5-b3-b4-c1-c4-http-chain.trx'=1
    'raw-export-delivery-correction-c5-c1-c4-managed-chain-a2.trx'=1
    'raw-export-delivery-correction-c5-c1-c4-managed-chain.trx'=1
    'raw-export-delivery-correction-c5-full-restored.trx'=25
    'raw-export-delivery-correction-c5-scope-successor-a2.trx'=1
    'raw-export-delivery-correction-c5-scope-successor-a3.trx'=1
    'raw-export-delivery-correction-c5-scope-successor-baseline.trx'=1
    'raw-export-delivery-correction-credential-migration-roundtrip.trx'=1
    'raw-export-delivery-correction-f1-baseline.trx'=1
    'raw-export-delivery-correction-f1-client-isolation-mutant.trx'=1
    'raw-export-delivery-correction-http-routes.trx'=2
    'raw-export-delivery-correction-migration-discovery-a2.trx'=1
    'raw-export-delivery-correction-migration-discovery.trx'=1
    'raw-export-delivery-correction-restored-joined.trx'=6
    'raw-export-delivery-correction-restored-joined-v2.trx'=6
    'raw-export-delivery-correction-standalone-client-decrypt.trx'=1
    'raw-export-delivery-correction-c5-unit.trx'=1
    'raw-export-delivery-same-job-e2e.trx'=1
    'raw-export-delivery-same-job-e2e-a2.trx'=1
    'raw-export-delivery-same-job-e2e-a3.trx'=1
    'raw-export-delivery-same-job-e2e-a4.trx'=1
    'raw-export-delivery-same-job-e2e-a5.trx'=1
    'raw-export-delivery-same-job-e2e-a6.trx'=1
    'raw-export-delivery-same-job-e2e-final.trx'=1
    'raw-export-delivery-sdk-unit-final.trx'=4
    'raw-export-delivery-c5-profile-final.trx'=3
    'raw-export-delivery-consent-class-mutant.trx'=1
    'raw-export-delivery-restored-final.trx'=1
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
    'raw-export-delivery-correction-a3-fence-mutant.trx' = @('EVIDENCE_RED','Increasing only the durable candidate fencing token reaches the B4 comparator and fails with RAW_EXPORT_JOB_FENCE_STALE.','raw-export-delivery-correction-restored-joined.trx')
    'raw-export-delivery-correction-a4-valid-actor-mutant.trx' = @('EVIDENCE_RED','Replacing only the durable principal with a different valid GUID passes input validation and makes acquisition return null.','raw-export-delivery-correction-restored-joined.trx')
    'raw-export-delivery-correction-f1-client-isolation-mutant.trx' = @('EVIDENCE_RED','Removing only the ClientApplicationId arm from the B4 read SQL changes a cross-client read from NotFound to Found.','raw-export-delivery-correction-restored-joined.trx')
    'raw-export-delivery-correction-c5-authorize-scope-mutant.trx' = @('EVIDENCE_RED','Removing only business.raw-export.authorize from the successor scope set fails the exact four-scope contract.','raw-export-delivery-correction-c5-scope-successor-a3.trx')
    'raw-export-delivery-correction-c5-scope-successor-baseline.trx' = @('SUPERSEDED_RED','The first successor migration used the entity property name UpdatedAt instead of the mapped UpdatedAtUtc database column.','raw-export-delivery-correction-c5-scope-successor-a3.trx')
    'raw-export-delivery-correction-c5-scope-successor-a2.trx' = @('SUPERSEDED_RED','The second successor attempt carried one incorrect predecessor digest literal and therefore did not rewrite every managed-credential function.','raw-export-delivery-correction-c5-scope-successor-a3.trx')
    'raw-export-delivery-correction-c5-c1-c4-managed-chain.trx' = @('PRODUCT_DEFECT_PREDECESSOR','The C5 authentication policy still recognized only the two delivery scopes, so the newly issued four-scope credential fell through to disabled-client policy.','raw-export-delivery-correction-c5-c1-c4-managed-chain-a2.trx')
    'raw-export-delivery-correction-migration-discovery.trx' = @('SUPERSEDED_RED','The migration discovery assertion still named the prior migration as the last current migration.','raw-export-delivery-correction-migration-discovery-a2.trx')
    'raw-export-delivery-correction-c5-full-restored.trx' = @('KNOWN_TEST_DEBT','C528 still assumes that a rollback may cross a later A3 guarded migration. That compatibility debt is named and is not evidence for this delivery correction.','raw-export-delivery-correction-credential-migration-roundtrip.trx')
    'raw-export-delivery-same-job-e2e.trx' = @('PRODUCT_DEFECT_PREDECESSOR','The first joined run exposed legacy consent resolution selecting the first consent row instead of the row for the source raw class.','raw-export-delivery-same-job-e2e-final.trx')
    'raw-export-delivery-same-job-e2e-a2.trx' = @('PRODUCT_DEFECT_PREDECESSOR','The reordered fixture reproduced the same multi-class consent defect on LiveSelfieImage.','raw-export-delivery-same-job-e2e-final.trx')
    'raw-export-delivery-same-job-e2e-a3.trx' = @('SUPERSEDED_RED','The first migration rewrite did not match PostgreSQL normalized function text and failed closed before the joined assertion.','raw-export-delivery-same-job-e2e-final.trx')
    'raw-export-delivery-same-job-e2e-a4.trx' = @('SUPERSEDED_FIXTURE_RED','The diagnostic predecessor still used a no-retain policy rather than the existing encrypted-packet policy fixture.','raw-export-delivery-same-job-e2e-final.trx')
    'raw-export-delivery-same-job-e2e-a5.trx' = @('PRODUCT_DEFECT_PREDECESSOR','The joined run reached C3 and exposed that the SDK decoded the server base64url package digest as hexadecimal.','raw-export-delivery-same-job-e2e-final.trx')
    'raw-export-delivery-consent-class-mutant.trx' = @('EVIDENCE_RED','Changing only the consent class comparison from equality to inequality makes the same-job joined proof fail at R3 staging with SourceRetentionNotAuthorized.','raw-export-delivery-restored-final.trx')
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
        $scope = if ($relative -match 'TestResults/raw-export-delivery(?:-correction)?/') { 'RAW_EXPORT_DELIVERY_SLICE' } else { 'WHOLE_REPO_HISTORICAL_INVENTORY' }
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

$inventoryPath=Join-Path $OutputDirectory 'raw_export_delivery_whole_repo_trx_inventory_v2.tsv'
$failedPath=Join-Path $OutputDirectory 'raw_export_delivery_failed_run_census_v2.tsv'
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
