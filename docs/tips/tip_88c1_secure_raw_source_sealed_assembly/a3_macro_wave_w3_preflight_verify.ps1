$ErrorActionPreference = 'Stop'

$tip = Split-Path -Parent $MyInvocation.MyCommand.Path
$partition = Import-Csv (Join-Path $tip 'a3_macro_wave_partition_v1.tsv') -Delimiter "`t"
$matrix = Import-Csv (Join-Path $tip 'a3_macro_wave_w3_execution_matrix_v1.tsv') -Delimiter "`t"

$expected = @(
    'P01 A3_S02_O01_ExactOutcomeShapeStatusAndResidue',
    'P06 A3_S02_O06_ExactOutcomeShapeStatusAndResidue',
    'P07 A3_S02_O07_ExactOutcomeShapeStatusAndResidue',
    'P08 A3_S02_O08_ExactOutcomeShapeStatusAndResidue',
    'P11 A3_S02_O11_ExactOutcomeShapeStatusAndResidue',
    'P14 A3_S02_O14_ExactOutcomeShapeStatusAndResidue',
    'P16 A3_S02_O16_ExactOutcomeShapeStatusAndResidue'
)
$wave = @($partition | Where-Object MacroWave -eq 'W3_ADMISSION_OUTCOMES')
$matrixRows = @($matrix.RowId)

if ($wave.Count -ne 7 -or $matrix.Count -ne 7) { throw "W3_ROW_COUNT_MISMATCH partition=$($wave.Count) matrix=$($matrix.Count)" }
$missingPartition = @($expected | Where-Object { $_ -notin $wave.RowId })
$missingMatrix = @($expected | Where-Object { $_ -notin $matrixRows })
$extraMatrix = @($matrixRows | Where-Object { $_ -notin $expected })
$duplicates = @($matrixRows | Group-Object | Where-Object Count -ne 1)
if ($missingPartition.Count -or $missingMatrix.Count -or $extraMatrix.Count -or $duplicates.Count) {
    throw "W3_BIJECTION_FAILED missing_partition=$($missingPartition.Count) missing_matrix=$($missingMatrix.Count) extra=$($extraMatrix.Count) duplicate=$($duplicates.Count)"
}
foreach ($row in $matrix) {
    foreach ($field in 'Family','ProductionPath','SharedScenario','PositiveControl','MutationGuard','BodyExpectation','ResidueExpectation','ExitCondition') {
        if ([string]::IsNullOrWhiteSpace($row.$field)) { throw "W3_REQUIRED_FIELD_EMPTY row=$($row.RowId) field=$field" }
    }
}

"w3_partition_rows=$($wave.Count)"
"w3_matrix_rows=$($matrix.Count)"
"w3_duplicate=$($duplicates.Count)"
"w3_missing=0"
"w3_extra=$($extraMatrix.Count)"
"w3_families=$(@($matrix.Family | Sort-Object -Unique).Count)"
"w3_product_gap_rows=$(@($matrix | Where-Object ExitCondition -like 'PRODUCT_GAP*').Count)"
