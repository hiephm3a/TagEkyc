param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$docDir = Join-Path $RepositoryRoot 'docs/tips/tip_88c1_secure_raw_source_sealed_assembly'
$partitionPath = Join-Path $docDir 'a3_activation_scope_partition_v1.tsv'
$ownershipPath = Join-Path $docDir 'a3_outcome_ownership_table_v1.tsv'
$ledgerPath = Join-Path $docDir 'tip_88c1_c6b_a3_proof_reconciliation_v0_2.md'
$utf8 = [Text.UTF8Encoding]::new($false)

$rows = [ordered]@{
    'P29 A3_S02_O29_ExactOutcomeShapeStatusAndResidue' = @('AuthorityInvalid', 'C112 + C113')
    'P30 A3_S02_O30_ExactOutcomeShapeStatusAndResidue' = @('SelectionNone', 'C102')
    'P31 A3_S02_O31_ExactOutcomeShapeStatusAndResidue' = @('SelectionAmbiguous', 'P31-selection-ambiguous')
    'P32 A3_S02_O32_ExactOutcomeShapeStatusAndResidue' = @('SourceBindingInvalid', 'P32-binding-conflict')
    'P33 A3_S02_O33_ExactOutcomeShapeStatusAndResidue' = @('SourceUnavailable', 'dedicated P33 test')
    'P34 A3_S02_O34_ExactOutcomeShapeStatusAndResidue' = @('SourceIntegrityInvalid', 'P34-integrity-evidence')
    'P35 A3_S02_O35_ExactOutcomeShapeStatusAndResidue' = @('AssemblyPrepareFailed', 'P35-prepare-abort')
    'P36 A3_S02_O36_ExactOutcomeShapeStatusAndResidue' = @('AssemblyClassSetMismatch', 'P36-class-set-abort')
}

# The active partition is intentionally empty after the exact eight-row ratification.
[IO.File]::WriteAllText(
    $partitionPath,
    "RowId`tCurrentStatus`tMechanism`tSharedGuard`tExitCondition`n",
    $utf8)

$lines = [IO.File]::ReadAllLines($ownershipPath, $utf8)
$header = $lines[0].Split("`t")
$index = @{}
for ($i = 0; $i -lt $header.Count; $i++) { $index[$header[$i]] = $i }
$required = @(
    'RowId','Authority','AuthorityStatus','ExistingProof','ProofStrength','ProofBoundary',
    'RestoreEvidence','RestoredGate','AuditDisposition','ExactGap','ActivationBlockingBasis','Notes'
)
foreach ($name in $required) {
    if (-not $index.ContainsKey($name)) { throw "OWNERSHIP_COLUMN_MISSING name=$name" }
}

$decisionCitation = 'server/docs/tips/tip_88c1_secure_raw_source_sealed_assembly/a3_p29_p36_ratification_scope_decision_v1.md:5'
$proofSource = 'server/tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs'
$restoreTrx = 'server/tests/TagEkyc.IntegrationTests/TestResults/a3-p29-p36/a3-p29-p36-durable-worker-final-a14.trx'
$updated = 0
for ($lineIndex = 1; $lineIndex -lt $lines.Count; $lineIndex++) {
    if ([string]::IsNullOrWhiteSpace($lines[$lineIndex])) { continue }
    $values = $lines[$lineIndex].Split("`t")
    $rowId = $values[$index.RowId]
    if (-not $rows.Contains($rowId)) { continue }

    if ($values[$index.Authority] -notlike "*$decisionCitation*") {
        $values[$index.Authority] += ";$decisionCitation"
    }
    $values[$index.AuthorityStatus] = 'HOMEOWNER_RATIFIED'
    $values[$index.ExistingProof] = "$($rows[$rowId][1])@$proofSource"
    $values[$index.ProofStrength] = 'MUTATION_DISCRIMINATED'
    $values[$index.ProofBoundary] = 'PRODUCTION_DURABLE_WORKER_JOIN'
    $values[$index.RestoreEvidence] = $restoreTrx
    $values[$index.RestoredGate] = '5/5_DURABLE_WORKER_JOIN+2/2_ADJACENT'
    $values[$index.AuditDisposition] = 'ALREADY_RATIFIED_COMPLETE'
    $values[$index.ExactGap] = "No remaining $($rowId.Split(' ')[0]) row-closure work; exact outcome $($rows[$rowId][0]) and durable PostgreSQL residue are joined through the production DurableWorker path."
    $values[$index.ActivationBlockingBasis] = 'NOT_TRACED'
    $values[$index.Notes] = 'Homeowner ratified P29-P36 explicitly at a6d8405 after independent review; deferred backlog and site qualification remain separate.'
    $lines[$lineIndex] = [string]::Join("`t", $values)
    $updated++
}
if ($updated -ne 8) { throw "OWNERSHIP_P29_P36_UPDATE_COUNT_INVALID actual=$updated" }
[IO.File]::WriteAllText($ownershipPath, [string]::Join("`n", $lines) + "`n", $utf8)

$ledgerLines = [Collections.Generic.List[string]]::new()
$ledgerLines.AddRange([IO.File]::ReadAllLines($ledgerPath, $utf8))
$ledgerUpdated = 0
for ($i = 0; $i -lt $ledgerLines.Count; $i++) {
    if ($ledgerLines[$i] -notmatch '^\| P(?<number>29|30|31|32|33|34|35|36) `') { continue }
    $number = $Matches.number
    $replacement = "| IMPLEMENTED / HOMEOWNER_RATIFIED: exact P$number production DurableWorker outcome/residue proof accepted at a6d8405; packet E6C815A7...21B9 and successor canonical-hash manifest apply; no product/SDK source changed. |"
    $ledgerLines[$i] = [regex]::Replace($ledgerLines[$i], '\| NOT IMPLEMENTED:.*\|$', $replacement)
    if ($ledgerLines[$i] -notmatch 'IMPLEMENTED / HOMEOWNER_RATIFIED') {
        throw "LEDGER_P29_P36_STATUS_REWRITE_FAILED row=P$number"
    }
    $ledgerUpdated++
}
if ($ledgerUpdated -ne 8) { throw "LEDGER_P29_P36_UPDATE_COUNT_INVALID actual=$ledgerUpdated" }

$marker = '### P29-P36 Homeowner ratification successor (2026-09-24)'
$ledger = [string]::Join("`n", $ledgerLines)
if (-not $ledger.Contains($marker)) {
    $ledger += @"


$marker

The Homeowner explicitly stated: "Tôi ratify P29-P36 tại a6d8405, gộp luôn hai điều nhỏ kia" and authorized the complete governance transaction from eight activation-open rows to zero. The ratification is limited to P29-P36. It does not ratify any of the thirty deferred candidate-only rows and does not imply that a hospital site has passed transport qualification.

Independent review accepted eight one-to-one outcome assertions, the real packet-job acquisition through `DurableRawExportAssemblyWorkSource`, the production `RawExportAssemblyHostedService`, the real orchestrator, and durable PostgreSQL recording. Two discriminating mutations separately proved exact outcome identity and durable residue. Restored gates are 5/5 joined plus 2/2 adjacent, with nine failed runs fully classified.

P29-P36 are therefore `IMPLEMENTED / HOMEOWNER_RATIFIED`. The activation partition becomes empty and the activation-evidence count becomes zero. Approved assembly topology remains `DurableWorker`. Site transport qualification policy version 1 remains required; with count zero, a missing, invalid, mismatched, failed, stale, or expired site record blocks raw ingress at the site gate rather than at the authority-open count gate.

No push, deployment, site qualification, deferred-row ratification, production-source change, or SDK-source change is authorized by this transaction.
"@
}
[IO.File]::WriteAllText($ledgerPath, $ledger.TrimEnd("`r", "`n") + "`n", $utf8)

& (Join-Path $docDir 'a3_activation_scope_backlog_verify.ps1') -Mode Build -DocDir $docDir
