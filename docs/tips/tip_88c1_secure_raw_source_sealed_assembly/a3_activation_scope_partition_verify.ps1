param([string]$DocDir = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
$oldPartition = @(Import-Csv -LiteralPath (Join-Path $DocDir 'a3_macro_wave_partition_v1.tsv') -Delimiter "`t")
$current = @(Import-Csv -LiteralPath (Join-Path $DocDir 'a3_activation_scope_partition_v1.tsv') -Delimiter "`t")
$ownershipPath = Join-Path $DocDir 'a3_outcome_ownership_table_v1.tsv'
$ownership = @(Import-Csv -LiteralPath $ownershipPath -Delimiter "`t")
$backlog = @(Import-Csv -LiteralPath (Join-Path $DocDir 'a3_activation_scope_deferred_backlog_v1.tsv') -Delimiter "`t")
$ledgerPath = Join-Path $DocDir 'tip_88c1_c6b_a3_proof_reconciliation_v0_2.md'

& (Join-Path $DocDir 'a3_activation_scope_backlog_verify.ps1') -Mode Verify -DocDir $DocDir | Out-Null

function Normalize-RowId([string]$value) {
    return (($value -replace '`', '') -replace '\s+', ' ').Trim()
}
$proofOpen = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
foreach ($line in Get-Content -LiteralPath $ledgerPath) {
    if ($line -notmatch '^\|\s*(?<row>.+?)\s*\|.*\|\s*(?<status>NOT IMPLEMENTED|CANDIDATE_PENDING_RATIFICATION)') { continue }
    $id = Normalize-RowId $Matches.row
    if (-not $proofOpen.TryAdd($id, $Matches.status)) { throw "SCOPE_DUPLICATE_LEDGER_OPEN_ROW row=$id" }
}
$prior = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $oldPartition) {
    if (-not $prior.Add((Normalize-RowId $row.RowId))) { throw "SCOPE_DUPLICATE_PRIOR_ROW row=$($row.RowId)" }
}
$reconciled = 'E01_Race_IssueWithdrawal'
$ratifiedSuccessors = @(
    'BP10 WindowCapacityIsBoundedAcrossAllExitPaths',
    'P05 A3_S02_O05_ExactOutcomeShapeStatusAndResidue',
    'P04 A3_S02_O04_ExactOutcomeShapeStatusAndResidue',
    'P29 A3_S02_O29_ExactOutcomeShapeStatusAndResidue',
    'P30 A3_S02_O30_ExactOutcomeShapeStatusAndResidue',
    'P31 A3_S02_O31_ExactOutcomeShapeStatusAndResidue',
    'P32 A3_S02_O32_ExactOutcomeShapeStatusAndResidue',
    'P33 A3_S02_O33_ExactOutcomeShapeStatusAndResidue',
    'P34 A3_S02_O34_ExactOutcomeShapeStatusAndResidue',
    'P35 A3_S02_O35_ExactOutcomeShapeStatusAndResidue',
    'P36 A3_S02_O36_ExactOutcomeShapeStatusAndResidue'
)
$siteQualified = @(
    'A3_AgentRawExpectDoesNotSendBeforeCommittedR1',
    'BP03 BrokerCommitObservedBeforeFirstRawRead',
    'BP13 ThreeExpectTransportMutationsFailQualification',
    'Agent raw HTTP Expect → durable server B/R1 before first body byte'
)
if ($oldPartition.Count -ne 46 -or -not $prior.Contains($reconciled) -or
    $proofOpen.Count -ne 30 -or $proofOpen.ContainsKey($reconciled)) {
    throw 'SCOPE_E01_RECONCILIATION_INVALID'
}
foreach ($id in $ratifiedSuccessors) {
    if (-not $prior.Contains($id) -or $proofOpen.ContainsKey($id)) {
        throw "SCOPE_RATIFIED_SUCCESSOR_PROOF_INVALID row=$id"
    }
}
foreach ($id in $siteQualified) {
    if (-not $prior.Contains($id) -or $proofOpen.ContainsKey($id)) {
        throw "SCOPE_SITE_QUALIFIED_PROOF_INVALID row=$id"
    }
}
$ledger = Get-Content -LiteralPath $ledgerPath -Raw
if ($ledger -notmatch '(?m)^\| `E01_Race_IssueWithdrawal` \|.+IMPLEMENTED / HOMEOWNER_RATIFIED') {
    throw 'SCOPE_E01_RATIFICATION_ANCHOR_MISSING'
}
if ($ledger -notmatch '(?m)^\| BP10 `WindowCapacityIsBoundedAcrossAllExitPaths` \|.+IMPLEMENTED / HOMEOWNER_RATIFIED' -or
    $ledger -notmatch '### BP10 Homeowner ratification successor') {
    throw 'SCOPE_BP10_RATIFICATION_ANCHOR_MISSING'
}
if ($ledger -notmatch '(?m)^\| P05 `A3_S02_O05_ExactOutcomeShapeStatusAndResidue` \|.+IMPLEMENTED / HOMEOWNER_RATIFIED' -or
    $ledger -notmatch '### P05 Homeowner ratification successor') {
    throw 'SCOPE_P05_RATIFICATION_ANCHOR_MISSING'
}
if ($ledger -notmatch '(?m)^\| P04 `A3_S02_O04_ExactOutcomeShapeStatusAndResidue` \|.+IMPLEMENTED / HOMEOWNER_RATIFIED' -or
    $ledger -notmatch '### P04 corrected-contract Homeowner ratification successor') {
    throw 'SCOPE_P04_RATIFICATION_ANCHOR_MISSING'
}
if ($ledger -notmatch '### P29-P36 Homeowner ratification successor') {
    throw 'SCOPE_P29_P36_RATIFICATION_ANCHOR_MISSING'
}
if ([regex]::Matches($ledger, 'PRODUCT COMPLETE / SITE QUALIFICATION REQUIRED').Count -lt 4 -or
    $ledger -notmatch '### Expect product-complete / site-qualification successor') {
    throw 'SCOPE_SITE_QUALIFICATION_LEDGER_ANCHOR_MISSING'
}
foreach ($id in $prior) {
    if ($id -cne $reconciled -and $id -cnotin $ratifiedSuccessors -and
        $id -cnotin $siteQualified -and -not $proofOpen.ContainsKey($id)) {
        throw "SCOPE_PRIOR_PROOF_STATUS_LOST row=$id"
    }
}

$owners = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
foreach ($row in $ownership) {
    if ($row.RowId -eq 'NONE') { continue }
    if (-not $owners.TryAdd((Normalize-RowId $row.RowId), $row)) {
        throw "SCOPE_OWNERSHIP_DUPLICATE row=$($row.RowId)"
    }
}
$expected = [ordered]@{}
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $current) {
    $id = Normalize-RowId $row.RowId
    if (-not $seen.Add($id) -or -not $expected.Contains($id) -or
        -not $owners.ContainsKey($id) -or -not $proofOpen.ContainsKey($id) -or
        $row.CurrentStatus -cne 'NOT_IMPLEMENTED' -or
        $owners[$id].AuthorityStatus -cne $expected[$id][0] -or
        $owners[$id].SharedMechanism -cne $expected[$id][1] -or
        $row.Mechanism -cne $expected[$id][1]) {
        throw "SCOPE_RETAINED_ROW_INVALID row=$id"
    }
}
if ($seen.Count -ne 0) { throw "SCOPE_RETAINED_COUNT_INVALID actual=$($seen.Count)" }
foreach ($id in $ratifiedSuccessors) {
    if (-not $owners.ContainsKey($id) -or
        $owners[$id].AuthorityStatus -cne 'HOMEOWNER_RATIFIED' -or
        $owners[$id].AuditDisposition -cne 'ALREADY_RATIFIED_COMPLETE' -or
        $seen.Contains($id)) {
        throw "SCOPE_RATIFIED_SUCCESSOR_STATUS_INVALID row=$id"
    }
}
foreach ($id in $siteQualified) {
    if (-not $owners.ContainsKey($id) -or
        $owners[$id].AuditDisposition -cne 'SITE_QUALIFICATION_REQUIRED' -or
        $owners[$id].ActivationBlockingBasis -cne 'SITE_TRANSPORT_QUALIFICATION_POLICY_V1' -or
        $seen.Contains($id)) {
        throw "SCOPE_SITE_QUALIFICATION_STATUS_INVALID row=$id"
    }
}
$backlogSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $backlog) {
    $id = Normalize-RowId $row.RowId
    if (-not $backlogSet.Add($id) -or $seen.Contains($id) -or
        $id -ceq $reconciled -or $id -cin $ratifiedSuccessors -or $id -cin $siteQualified -or
        $row.Disposition -cne 'DEFERRED_BY_HOMEOWNER_SCOPE') {
        throw "SCOPE_BACKLOG_ROW_INVALID row=$id"
    }
}
foreach ($id in $prior) {
    if ($id -cne $reconciled -and $id -cnotin $ratifiedSuccessors -and $id -cnotin $siteQualified -and
        -not $seen.Contains($id) -and -not $backlogSet.Contains($id)) {
        throw "SCOPE_UNASSIGNED_PRIOR_ROW row=$id"
    }
}
if ($backlogSet.Count -ne 30 -or
    $seen.Count + $backlogSet.Count + 1 + $ratifiedSuccessors.Count + $siteQualified.Count -ne $prior.Count) {
    throw 'SCOPE_PARTITION_ARITHMETIC_INVALID'
}

"prior_open=$($prior.Count)"
"reconciled_prior_ratification=1"
"ratified_successor=$($ratifiedSuccessors.Count)"
"site_qualification_required=$($siteQualified.Count)"
"proof_open_in_ledger=$($proofOpen.Count)"
"deferred_by_homeowner_scope=$($backlogSet.Count)"
"official_open_rows=$($seen.Count)"
"partition_rows=$($current.Count)"
"assigned_once=$($seen.Count)"
"duplicate=0"
"unassigned=0"
"extra=0"
"mechanism_TRANSPORT-HEADER-GUARD=0"
"mechanism_ADMISSION-CAPABILITY=0"
"mechanism_TRANSPORT-EXPECT-FENCE=0"
"mechanism_ADMISSION-CAPACITY=0"
"mechanism_ASSEMBLY-WORK-SOURCE=0"
"ownership_registry_sha256=$((Get-FileHash -LiteralPath $ownershipPath -Algorithm SHA256).Hash)"
