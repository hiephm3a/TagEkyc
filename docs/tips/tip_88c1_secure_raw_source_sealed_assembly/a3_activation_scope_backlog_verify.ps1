param(
    [ValidateSet('Build','Verify')][string]$Mode = 'Verify',
    [string]$DocDir = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'
$oldPartition = @(Import-Csv -LiteralPath (Join-Path $DocDir 'a3_macro_wave_partition_v1.tsv') -Delimiter "`t")
$retained = @(Import-Csv -LiteralPath (Join-Path $DocDir 'a3_activation_scope_partition_v1.tsv') -Delimiter "`t")
$ownership = @(Import-Csv -LiteralPath (Join-Path $DocDir 'a3_outcome_ownership_table_v1.tsv') -Delimiter "`t")
$outputPath = Join-Path $DocDir 'a3_activation_scope_deferred_backlog_v1.tsv'

if ($oldPartition.Count -ne 46 -or $retained.Count -ne 8) {
    throw "ACTIVATION_SCOPE_INPUT_COUNT_INVALID prior=$($oldPartition.Count) retained=$($retained.Count)"
}
$priorIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$retainedIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $oldPartition) {
    if (-not $priorIds.Add($row.RowId)) { throw "ACTIVATION_SCOPE_PRIOR_DUPLICATE row=$($row.RowId)" }
}
foreach ($row in $retained) {
    if (-not $retainedIds.Add($row.RowId) -or -not $priorIds.Contains($row.RowId)) {
        throw "ACTIVATION_SCOPE_RETAINED_INVALID row=$($row.RowId)"
    }
}
$ownershipById = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
foreach ($row in $ownership) {
    if ($row.RowId -eq 'NONE') { continue }
    if (-not $ownershipById.TryAdd($row.RowId, $row)) {
        throw "ACTIVATION_SCOPE_OWNERSHIP_DUPLICATE row=$($row.RowId)"
    }
}
$header = 'RowId', 'PriorStatus', 'AuthorityStatus', 'SharedMechanism', 'Disposition',
    'ReasonClass', 'ProducerCount', 'ProducerReachableInActivated', 'ProofStrength'
$lines = [Collections.Generic.List[string]]::new()
$lines.Add([string]::Join("`t", $header))
$assembly = 0
$candidate = 0
$reconciled = 0
$ratifiedSuccessor = 0
$siteQualification = 0
foreach ($row in $oldPartition) {
    if (-not $ownershipById.ContainsKey($row.RowId)) {
        throw "ACTIVATION_SCOPE_OWNERSHIP_MISSING row=$($row.RowId)"
    }
    $owner = $ownershipById[$row.RowId]
    if ($retainedIds.Contains($row.RowId)) { continue }
    if ($row.RowId -ceq 'E01_Race_IssueWithdrawal' -and $owner.AuthorityStatus -ceq 'HOMEOWNER_RATIFIED') {
        $reconciled++
        continue
    }
    if ($row.RowId -ceq 'BP10 WindowCapacityIsBoundedAcrossAllExitPaths' -and
        $owner.AuthorityStatus -ceq 'HOMEOWNER_RATIFIED' -and
        $owner.AuditDisposition -ceq 'ALREADY_RATIFIED_COMPLETE') {
        $ratifiedSuccessor++
        continue
    }
    if ($row.RowId -ceq 'P05 A3_S02_O05_ExactOutcomeShapeStatusAndResidue' -and
        $owner.AuthorityStatus -ceq 'HOMEOWNER_RATIFIED' -and
        $owner.AuditDisposition -ceq 'ALREADY_RATIFIED_COMPLETE') {
        $ratifiedSuccessor++
        continue
    }
    if ($row.RowId -ceq 'P04 A3_S02_O04_ExactOutcomeShapeStatusAndResidue' -and
        $owner.AuthorityStatus -ceq 'HOMEOWNER_RATIFIED' -and
        $owner.AuditDisposition -ceq 'ALREADY_RATIFIED_COMPLETE') {
        $ratifiedSuccessor++
        continue
    }
    if ($owner.AuditDisposition -ceq 'SITE_QUALIFICATION_REQUIRED' -and
        $owner.ActivationBlockingBasis -ceq 'SITE_TRANSPORT_QUALIFICATION_POLICY_V1') {
        $siteQualification++
        continue
    }
    if ($owner.AuthorityStatus -ceq 'CANDIDATE_ONLY') {
        $reason = 'CANDIDATE_AUTHORITY_NOT_OPERATIVE_FOR_DELIVERY'
        $candidate++
    } else {
        throw "ACTIVATION_SCOPE_UNAUTHORIZED_DEFERRAL row=$($row.RowId) authority=$($owner.AuthorityStatus)"
    }
    $values = @($row.RowId, $row.CurrentStatus, $owner.AuthorityStatus,
        $owner.SharedMechanism, 'DEFERRED_BY_HOMEOWNER_SCOPE', $reason,
        $owner.ProducerCount, $owner.ProducerReachableInActivated, $owner.ProofStrength)
    foreach ($value in $values) {
        if ($value -match "[`t`r`n]") { throw "ACTIVATION_SCOPE_BACKLOG_VALUE_INVALID row=$($row.RowId)" }
    }
    $lines.Add([string]::Join("`t", $values))
}
if ($assembly -ne 0 -or $candidate -ne 30 -or $reconciled -ne 1 -or
    $ratifiedSuccessor -ne 3 -or $siteQualification -ne 4 -or $lines.Count -ne 31) {
    throw "ACTIVATION_SCOPE_DISPOSITION_COUNT_INVALID assembly=$assembly candidate=$candidate reconciled=$reconciled ratified_successor=$ratifiedSuccessor site_qualification=$siteQualification"
}
$content = [string]::Join("`n", $lines) + "`n"
$utf8 = [Text.UTF8Encoding]::new($false)
if ($Mode -ceq 'Build') {
    [IO.File]::WriteAllText($outputPath, $content, $utf8)
} elseif (-not [IO.File]::Exists($outputPath) -or
    [IO.File]::ReadAllText($outputPath, $utf8) -cne $content) {
    throw 'ACTIVATION_SCOPE_BACKLOG_CURRENT_BYTES_DRIFT'
}
"prior_open=46"
"reconciled_prior_ratification=$reconciled"
"ratified_successor=$ratifiedSuccessor"
"site_qualification_required=$siteQualification"
"deferred_assembly=$assembly"
"deferred_candidate=$candidate"
"retained_activation=$($retained.Count)"
"backlog_rows=$($lines.Count - 1)"
