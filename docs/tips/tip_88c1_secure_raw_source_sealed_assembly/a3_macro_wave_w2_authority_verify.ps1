param([string]$ServerRoot = 'D:\Task\Remote Signing\TagEkyc')

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $ServerRoot).Path
$doc = Join-Path $root 'docs\tips\tip_88c1_secure_raw_source_sealed_assembly'
$partition = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_partition_v1.tsv'))
$audit = @(Import-Csv -Delimiter "`t" -LiteralPath (Join-Path $doc 'a3_macro_wave_w2_authority_audit.tsv'))
$wave = @($partition | Where-Object MacroWave -CEQ 'W2_AUTHORITY_DECISION')

if ($wave.Count -ne 14) { throw "W2_ROW_COUNT_INVALID expected=14 actual=$($wave.Count)" }
if ($audit.Count -ne 14) { throw "W2_AUDIT_COUNT_INVALID expected=14 actual=$($audit.Count)" }

$partitionRows = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $wave) {
    if (-not $partitionRows.Add($row.RowId)) { throw "W2_PARTITION_DUPLICATE row=$($row.RowId)" }
    if ($row.ExitCondition -cne 'WIRE_PRODUCT|PARK_WITH_A3') { throw "W2_EXIT_INVALID row=$($row.RowId)" }
}

$auditRows = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($row in $audit) {
    if (-not $auditRows.Add($row.RowId)) { throw "W2_AUDIT_DUPLICATE row=$($row.RowId)" }
    if ($row.Decision -cne 'AWAIT_HOMEOWNER_SCOPE_DECISION') { throw "W2_DECISION_INVALID row=$($row.RowId)" }
    foreach ($field in @('Family','AuthoritySource','CurrentGap','ImplementationExit')) {
        if ([string]::IsNullOrWhiteSpace($row.$field)) { throw "W2_FIELD_EMPTY row=$($row.RowId) field=$field" }
    }
}

$unassigned = @($partitionRows | Where-Object { -not $auditRows.Contains($_) })
$extra = @($auditRows | Where-Object { -not $partitionRows.Contains($_) })
if ($unassigned.Count -ne 0) { throw "W2_UNASSIGNED count=$($unassigned.Count) rows=$($unassigned -join ',')" }
if ($extra.Count -ne 0) { throw "W2_EXTRA count=$($extra.Count) rows=$($extra -join ',')" }

$families = @($audit | Group-Object Family | Sort-Object Name)
"w2_rows=$($wave.Count)"
"audit_rows=$($audit.Count)"
"duplicate=0"
"unassigned=$($unassigned.Count)"
"extra=$($extra.Count)"
"decisions_pending_homeowner=$(@($audit | Where-Object Decision -CEQ 'AWAIT_HOMEOWNER_SCOPE_DECISION').Count)"
foreach ($family in $families) { "family_$($family.Name)=$($family.Count)" }
