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
    'P29 A3_S02_O29_ExactOutcomeShapeStatusAndResidue' = @('W2-G-ASSEMBLY-CALLER+O29', 'Joined DurableWorker proof must show authority-invalid recording before any source/provider read.')
    'P30 A3_S02_O30_ExactOutcomeShapeStatusAndResidue' = @('W2-G-O30', 'Joined DurableWorker proof must show the zero-eligible-source outcome with no binding or provider read.')
    'P31 A3_S02_O31_ExactOutcomeShapeStatusAndResidue' = @('W2-G-O31', 'Joined DurableWorker proof must show the multiple-eligible-source ambiguity outcome without selection mutation or provider read.')
    'P32 A3_S02_O32_ExactOutcomeShapeStatusAndResidue' = @('W2-G-O32', 'Joined DurableWorker proof must show the frozen-selection conflict outcome with the mapping unchanged.')
    'P33 A3_S02_O33_ExactOutcomeShapeStatusAndResidue' = @('W2-G-O33', 'Joined DurableWorker proof must show selected-source unavailability before assembly/provider read.')
    'P34 A3_S02_O34_ExactOutcomeShapeStatusAndResidue' = @('W2-G-O34', 'Joined DurableWorker proof must show source-integrity failure with quarantine/no-egress residue.')
    'P35 A3_S02_O35_ExactOutcomeShapeStatusAndResidue' = @('W2-G-O35', 'Joined DurableWorker proof must show C2 prepare failure with exact abort and no assembly identity.')
    'P36 A3_S02_O36_ExactOutcomeShapeStatusAndResidue' = @('W2-G-O36', 'Joined DurableWorker proof must show class-set mismatch with no sealed assembly identity.')
}

$partition = [Collections.Generic.List[string]]::new()
$partition.Add("RowId`tCurrentStatus`tMechanism`tSharedGuard`tExitCondition")
foreach ($entry in $rows.GetEnumerator()) {
    $partition.Add("$($entry.Key)`tNOT_IMPLEMENTED`tASSEMBLY-WORK-SOURCE`t$($entry.Value[0])`tEXACT_DURABLE_WORKER_JOIN_AND_HOMEOWNER_RATIFICATION")
}
[IO.File]::WriteAllText($partitionPath, [string]::Join("`n", $partition) + "`n", $utf8)

$lines = [IO.File]::ReadAllLines($ownershipPath, $utf8)
$header = $lines[0].Split("`t")
$index = @{}
for ($i = 0; $i -lt $header.Count; $i++) { $index[$header[$i]] = $i }
$required = @('RowId','Authority','ProductionProducers','ProducerCount','ProducerReachableInActivated',
    'ReachabilityEvidence','AuditDisposition','ExactGap','ActivationBlockingBasis','Notes')
foreach ($name in $required) {
    if (-not $index.ContainsKey($name)) { throw "OWNERSHIP_COLUMN_MISSING name=$name" }
}
$updated = 0
for ($lineIndex = 1; $lineIndex -lt $lines.Count; $lineIndex++) {
    if ([string]::IsNullOrWhiteSpace($lines[$lineIndex])) { continue }
    $values = $lines[$lineIndex].Split("`t")
    $rowId = $values[$index.RowId]
    if (-not $rows.Contains($rowId)) { continue }
    $values[$index.Authority] += ';server/docs/tips/tip_88c1_secure_raw_source_sealed_assembly/raw_export_delivery_activation_topology_decision_v1.md:5'
    $values[$index.ProductionProducers] = 'DurableRawExportAssemblyWorkSource@server/src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyRuntimeInfrastructure.cs:142'
    $values[$index.ProducerCount] = '1'
    $values[$index.ProducerReachableInActivated] = 'YES'
    $values[$index.ReachabilityEvidence] = 'server/src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyServiceCollectionExtensions.cs:41;server/src/TagEkyc.Api/Program.cs:70;server/tests/TagEkyc.IntegrationTests/RawExportDeliverySameJobEndToEndTests.cs'
    $values[$index.AuditDisposition] = 'TEST_REQUIRED'
    $values[$index.ExactGap] = $rows[$rowId][1]
    $values[$index.ActivationBlockingBasis] = 'ROW_PROOF_AND_HOMEOWNER_RATIFICATION'
    $values[$index.Notes] = 'Production durable work source and same-Job delivery success path now exist. Raw-export delivery ratification does not ratify this exact failure row.'
    $lines[$lineIndex] = [string]::Join("`t", $values)
    $updated++
}
if ($updated -ne 8) { throw "OWNERSHIP_ASSEMBLY_UPDATE_COUNT_INVALID actual=$updated" }
[IO.File]::WriteAllText($ownershipPath, [string]::Join("`n", $lines) + "`n", $utf8)

$ledger = [IO.File]::ReadAllText($ledgerPath, $utf8)
$marker = '### Raw-export delivery ratification and DurableWorker activation-scope successor (2026-09-24)'
if (-not $ledger.Contains($marker)) {
    $append = @"

$marker

The Homeowner explicitly ratified the raw-export delivery slice at TagEkyc commit `97b35fa` and SignFlow relocation commit `44a16b0`, authorized the assembly topology transition to `DurableWorker`, and authorized a separate governance re-freeze. This decision accepts the delivered SDK/control-plane/job/work-source/assembly/package/download/decrypt success path. It does **not** silently ratify the eight exact P29-P36 outcome rows; the accepted delivery packets explicitly excluded that inference.

Because Activated now includes assembly, the earlier `DEFER_ASSEMBLY_GATE` basis is no longer true. P29-P36 therefore leave the deferred backlog and return to the active partition under the shared `ASSEMBLY-WORK-SOURCE` mechanism. The canonical ownership registry records the production durable work source as reachable but keeps every row `TEST_REQUIRED` with `ROW_PROOF_AND_HOMEOWNER_RATIFICATION`. The thirty `CANDIDATE_ONLY` rows remain deferred unchanged.

The successor activation arithmetic is **0 previous activation-open + 8 reactivated assembly rows = 8 authority-open**. Approved assembly topology is `DurableWorker`; an Activated deployment must configure that exact topology. The format-2 successor seal remains nonzero and must fail with `CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE` before routes start. Site transport qualification policy version 1 remains mandatory after the assembly rows are eventually closed. No production activation or push is authorized by this transaction.
"@
    [IO.File]::WriteAllText($ledgerPath, $ledger.TrimEnd("`r", "`n") + $append + "`n", $utf8)
}

& (Join-Path $docDir 'a3_activation_scope_backlog_verify.ps1') -Mode Build -DocDir $docDir
