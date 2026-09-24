param(
    [string]$TablePath = (Join-Path $PSScriptRoot 'a3_outcome_ownership_table_v1.tsv'),
    [string]$CataloguePath = (Join-Path $PSScriptRoot 'tip_88c1_c6b_a3_broker_composition_dispatch_v0_5.md'),
    [string]$PartitionPath = (Join-Path $PSScriptRoot 'a3_macro_wave_partition_v1.tsv')
)

$ErrorActionPreference = 'Stop'
$serverRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$agentRoot = (Resolve-Path (Join-Path $serverRoot '..\TagEkyc.CaptureAgent')).Path
$required = @('RecordKind','OutcomeCode','RowId','RequirementSummary','Phase','Authority','AuthorityStatus','ProductionProducers','ProducerCount','ProducerReachableInActivated','ReachabilityEvidence','SharedMechanism','ExistingProof','ProofStrength','ProofBoundary','RestoreEvidence','RestoredGate','AuditDisposition','ExactGap','ActivationBlockingBasis','Notes')
$authorityStates = @('HOMEOWNER_RATIFIED','OPERATIVE_IMPLEMENTATION_AUTHORITY','CANDIDATE_ONLY','DERIVED_ASSURANCE','NOT_TRACED')
$proofStates = @('MUTATION_DISCRIMINATED','GREEN_ONLY','COMPONENT_ONLY','NONE')
$boundaries = @('SHIPPING_JOINED','PRODUCTION_COMPONENT','FIXTURE_OR_SYNTHETIC','NONE')
$dispositions = @('IMPLEMENTATION_REQUIRED','TEST_REQUIRED','AUTHORITY_DECISION_REQUIRED','DEFERRED_UNREACHABLE','HARDENING_NOT_ACTIVATION_BLOCKER','ALREADY_RATIFIED_COMPLETE','CANDIDATE_PENDING_RATIFICATION','SITE_QUALIFICATION_REQUIRED')
$restoreStates = @('BYTE_EXACT_RESTORED','NOT_APPLICABLE','NOT_TRACED')

function Fail([string]$message) { throw "A3_OUTCOME_AUDIT_INVALID $message" }
function CheckCitation([string]$value, [string]$field, [string]$key, [string[]]$sentinels) {
    if ($value -in $sentinels) { return }
    foreach ($item in ($value -split ';')) {
        if ($item -notmatch '^(server|agent)/(.+):([1-9][0-9]*)$') { Fail "MALFORMED_CITATION row=$key field=$field value=$item" }
        $root = if ($Matches[1] -eq 'server') { $serverRoot } else { $agentRoot }
        $path = Join-Path $root ($Matches[2] -replace '/', '\')
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { Fail "CITATION_FILE_MISSING row=$key field=$field value=$item" }
        $lineCount = @(Get-Content -LiteralPath $path).Count
        if ([int]$Matches[3] -gt $lineCount) { Fail "CITATION_LINE_MISSING row=$key field=$field value=$item" }
    }
}

$header = (Get-Content -LiteralPath $TablePath -TotalCount 1 -Encoding UTF8) -split "`t"
if (($header -join '|') -cne ($required -join '|')) { Fail 'HEADER_MISMATCH' }
$rows = @(Import-Csv -LiteralPath $TablePath -Delimiter "`t" -Encoding UTF8)
$catalogueLines = @(Get-Content -LiteralPath $CataloguePath -Encoding UTF8)
$catalogue = @($catalogueLines | ForEach-Object { if ($_ -match '^\|\s*(O[0-9]{2})\s*\|\s*([^|]+)\|') { [pscustomobject]@{ Id=$Matches[1]; Code=$Matches[2].Trim() } } })
$partition = @(Import-Csv -LiteralPath $PartitionPath -Delimiter "`t" -Encoding UTF8)
if (@($catalogue.Id | Sort-Object -Unique).Count -ne $catalogue.Count) { Fail 'DUPLICATE_CATALOGUE_ID' }
if (@($catalogue.Code | Sort-Object -Unique).Count -ne $catalogue.Count) { Fail 'DUPLICATE_CATALOGUE_OUTCOME' }
if (@($partition.RowId | Sort-Object -Unique).Count -ne $partition.Count) { Fail 'DUPLICATE_PARTITION_ROW' }

foreach ($r in $rows) {
    $key = "$($r.RecordKind)/$($r.OutcomeCode)/$($r.RowId)"
    foreach ($column in $required) { if ([string]::IsNullOrWhiteSpace($r.$column)) { Fail "BLANK_FIELD row=$key field=$column" } }
    if ($r.RecordKind -notin @('OUTCOME_CODE','PARTITION_ONLY_INVARIANT')) { Fail "UNKNOWN_RECORD_KIND row=$key" }
    if ($r.AuthorityStatus -notin $authorityStates) { Fail "UNKNOWN_AUTHORITY_STATUS row=$key" }
    if ($r.ProofStrength -notin $proofStates) { Fail "UNKNOWN_PROOF_STRENGTH row=$key" }
    if ($r.ProofBoundary -notin $boundaries) { Fail "UNKNOWN_PROOF_BOUNDARY row=$key" }
    if ($r.RestoreEvidence -notin $restoreStates) { Fail "UNKNOWN_RESTORE_EVIDENCE row=$key" }
    if ($r.AuditDisposition -notin $dispositions) { Fail "UNKNOWN_DISPOSITION row=$key" }
    if ($r.AuditDisposition -eq 'ALREADY_RATIFIED_COMPLETE' -and $r.AuthorityStatus -cne 'HOMEOWNER_RATIFIED') { Fail "UNRATIFIED_COMPLETE row=$key" }
    if ($r.AuditDisposition -eq 'AUTHORITY_DECISION_REQUIRED' -and $r.ExactGap -cnotmatch '^(Decide|Determine|Choose|Reconcile)(\s|$)') { Fail "DECISION_WORDING row=$key" }
    if ($r.AuditDisposition -eq 'SITE_QUALIFICATION_REQUIRED' -and
        ($r.ExactGap -cne 'NONE' -or $r.ActivationBlockingBasis -cne 'SITE_TRANSPORT_QUALIFICATION_POLICY_V1')) {
        Fail "SITE_QUALIFICATION_CLASSIFICATION_INVALID row=$key"
    }
    if ($r.ProducerReachableInActivated -notin @('YES','NO','UNKNOWN')) { Fail "UNKNOWN_REACHABILITY row=$key" }
    if ($r.RecordKind -eq 'PARTITION_ONLY_INVARIANT' -and $r.OutcomeCode -cne 'NONE') { Fail "PARTITION_ONLY_HAS_CODE row=$key" }
    if ($r.RecordKind -eq 'PARTITION_ONLY_INVARIANT' -and $r.RowId -eq 'NONE') { Fail "PARTITION_ONLY_HAS_NO_ROW row=$key" }
    if ($r.RecordKind -eq 'OUTCOME_CODE' -and $r.OutcomeCode -eq 'NONE') { Fail "OUTCOME_WITHOUT_CODE row=$key" }
    if ($r.RowId -ne 'NONE' -and $r.RowId -cnotin $partition.RowId) { Fail "UNKNOWN_PARTITION_ROW row=$key" }
    if ($r.ProducerCount -notmatch '^(0|[1-9][0-9]*)$') { Fail "MALFORMED_PRODUCER_COUNT row=$key" }
    $producers = if ($r.ProductionProducers -eq 'NOT_TRACED') { @() } else { @($r.ProductionProducers -split ';') }
    if ([int]$r.ProducerCount -ne $producers.Count) { Fail "PRODUCER_COUNT_MISMATCH row=$key" }
    if ($r.ProducerReachableInActivated -eq 'YES' -and $producers.Count -eq 0) { Fail "YES_WITHOUT_PRODUCER row=$key" }
    CheckCitation $r.Authority 'Authority' $key @('NOT_TRACED')
    CheckCitation $r.ProductionProducers 'ProductionProducers' $key @('NOT_TRACED')
    CheckCitation $r.ReachabilityEvidence 'ReachabilityEvidence' $key @('NOT_TRACED')
    CheckCitation $r.ActivationBlockingBasis 'ActivationBlockingBasis' $key @('NOT_TRACED')
    if ($r.ExistingProof -ne 'NONE') {
        foreach ($p in ($r.ExistingProof -split ';')) {
            if ($p -notmatch '^([^@]+)@(.+)$') { Fail "MALFORMED_PROOF row=$key value=$p" }
            $testName = $Matches[1]
            $citation = $Matches[2]
            CheckCitation $citation 'ExistingProof' $key @()
            if ($citation -notmatch '^(server|agent)/(.+):([1-9][0-9]*)$') { Fail "MALFORMED_PROOF_CITATION row=$key value=$p" }
            $proofRoot = if ($Matches[1] -eq 'server') { $serverRoot } else { $agentRoot }
            $proofPath = Join-Path $proofRoot ($Matches[2] -replace '/', '\')
            $proofLine = (Get-Content -LiteralPath $proofPath -Encoding UTF8)[([int]$Matches[3] - 1)]
            if ($proofLine -notlike "*$testName*") { Fail "PROOF_NAME_NOT_AT_CITATION row=$key value=$p" }
        }
    }
    if ($r.RestoredGate -ne 'NONE') {
        if ($r.RestoredGate -notmatch '^(server|agent)/(.+\.trx)$') { Fail "MALFORMED_RESTORED_GATE row=$key" }
        $gateRoot = if ($Matches[1] -eq 'server') { $serverRoot } else { $agentRoot }
        $gatePath = Join-Path $gateRoot ($Matches[2] -replace '/', '\')
        if (-not (Test-Path -LiteralPath $gatePath -PathType Leaf)) { Fail "RESTORED_GATE_MISSING row=$key" }
    }
    if (($r.ProofStrength -ne 'NONE' -or $r.ProducerCount -ne '0') -and $r.SharedMechanism -eq 'NONE') { Fail "BLANK_SHARED_MECHANISM row=$key" }
    if ($r.ProofStrength -eq 'NONE' -and $r.ExistingProof -ne 'NONE') { Fail "PROOF_STATE_MISMATCH row=$key" }
    if ($r.ProofBoundary -eq 'NONE' -and $r.ExistingProof -ne 'NONE') { Fail "PROOF_BOUNDARY_MISMATCH row=$key" }
    if ($r.AuditDisposition -eq 'CANDIDATE_PENDING_RATIFICATION') {
        if ($r.AuthorityStatus -notin @('HOMEOWNER_RATIFIED','OPERATIVE_IMPLEMENTATION_AUTHORITY') -or
            $r.ProducerReachableInActivated -ne 'YES' -or $r.ProofStrength -ne 'MUTATION_DISCRIMINATED' -or
            $r.ProofBoundary -ne 'SHIPPING_JOINED' -or $r.RestoreEvidence -ne 'BYTE_EXACT_RESTORED' -or
            $r.RestoredGate -eq 'NONE') { Fail "UNQUALIFIED_CANDIDATE row=$key" }
    }
    if ($r.AuditDisposition -eq 'IMPLEMENTATION_REQUIRED' -and $r.AuthorityStatus -notin @('HOMEOWNER_RATIFIED','OPERATIVE_IMPLEMENTATION_AUTHORITY')) { Fail "UNAUTHORIZED_IMPLEMENTATION row=$key" }
    if ($r.AuditDisposition -eq 'TEST_REQUIRED' -and ($r.AuthorityStatus -notin @('HOMEOWNER_RATIFIED','OPERATIVE_IMPLEMENTATION_AUTHORITY') -or $r.ProducerReachableInActivated -ne 'YES' -or $producers.Count -eq 0)) { Fail "UNQUALIFIED_TEST_REQUIRED row=$key" }
}

$outcomes = @($rows | Where-Object RecordKind -EQ 'OUTCOME_CODE')
$dupeCodes = @($outcomes | Group-Object OutcomeCode | Where-Object Count -NE 1)
if ($dupeCodes.Count -gt 0) { Fail "DUPLICATE_OUTCOME_CODE=$($dupeCodes.Name -join ',')" }
$missingCodes = @($catalogue.Code | Where-Object { $_ -cnotin $outcomes.OutcomeCode })
$extraCodes = @($outcomes.OutcomeCode | Where-Object { $_ -cnotin $catalogue.Code })
if ($missingCodes.Count -gt 0 -or $extraCodes.Count -gt 0) { Fail "CATALOGUE_MISMATCH missing=$($missingCodes -join ',') extra=$($extraCodes -join ',')" }
$unrepresented = @($partition.RowId | Where-Object { $_ -cnotin $rows.RowId })
if ($unrepresented.Count -gt 0) { Fail "UNREPRESENTED_PARTITION=$($unrepresented -join ',')" }
$partitionOnly = @($rows | Where-Object RecordKind -EQ 'PARTITION_ONLY_INVARIANT')
if (@($partitionOnly | Group-Object RowId | Where-Object Count -GT 1).Count -gt 0) { Fail 'DUPLICATE_PARTITION_ONLY_ROW' }
$outcomeMapped = @($outcomes | Where-Object RowId -NE 'NONE')
$mappedRows = @($outcomeMapped.RowId | Sort-Object -Unique)
$missingPartitionOnly = @($partition.RowId | Where-Object { $_ -cnotin $mappedRows -and $_ -cnotin $partitionOnly.RowId })
if ($missingPartitionOnly.Count -gt 0) { Fail "MISSING_PARTITION_ONLY=$($missingPartitionOnly -join ',')" }

"outcome_codes=$($catalogue.Count)"
"duplicate_outcome_codes=$($dupeCodes.Count)"
"partition_rows=$($partition.Count)"
"audit_records=$($rows.Count)"
"partition_only_rows=$($partitionOnly.Count)"
"codes_without_partition_row=$(@($outcomes | Where-Object RowId -EQ 'NONE').Count)"
"rows_mapping_multiple_codes=$(@($outcomeMapped | Group-Object RowId | Where-Object Count -GT 1).Count)"
"unrepresented_partition_rows=$($unrepresented.Count)"
foreach ($status in $authorityStates) { "authority_$status=$(@($rows | Where-Object AuthorityStatus -EQ $status).Count)" }
foreach ($state in $dispositions) { "disposition_$state=$(@($rows | Where-Object AuditDisposition -EQ $state).Count)" }
foreach ($state in @('YES','NO','UNKNOWN')) { "producer_reachable_$state=$(@($rows | Where-Object ProducerReachableInActivated -EQ $state).Count)" }
foreach ($state in $proofStates) { "proof_strength_$state=$(@($rows | Where-Object ProofStrength -EQ $state).Count)" }
foreach ($state in $boundaries) { "proof_boundary_$state=$(@($rows | Where-Object ProofBoundary -EQ $state).Count)" }
"no_producer=$(@($rows | Where-Object ProductionProducers -EQ 'NOT_TRACED').Count)"
"distinct_shared_mechanisms=$(@($rows.SharedMechanism | Where-Object { $_ -ne 'NONE' } | Sort-Object -Unique).Count)"
"test_required_shared_mechanisms=$(@($rows | Where-Object AuditDisposition -EQ 'TEST_REQUIRED' | Select-Object -ExpandProperty SharedMechanism -Unique).Count)"
$unstatedDispositions = @($rows | Where-Object { [string]::IsNullOrWhiteSpace($_.AuditDisposition) }).Count
"unstated_audit_dispositions=$unstatedDispositions"
