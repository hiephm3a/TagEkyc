$ErrorActionPreference = 'Stop'

$repo = (Get-Location).Path
$out = Join-Path $repo 'output/review/PRODUCTION_VERIFICATION_PERSISTENCE_PRINCIPAL_CORRECTION_V0_1'
$manifest = Join-Path $out 'MANIFEST.tsv'
$zip = Join-Path $repo 'output/review/PRODUCTION_VERIFICATION_PERSISTENCE_PRINCIPAL_CORRECTION_V0_1.zip'

$changed = @(& git diff --name-only -- . ':(exclude)docs/00_GDRIVE_FILE_INDEX.md')
$newSource = @(
    'src/TagEkyc.Infrastructure/Persistence/Migrations/20261002120000_ProductionApplicationPersistencePrincipal.cs',
    'src/TagEkyc.Infrastructure/Persistence/ApplicationPersistenceReadinessValidator.cs',
    'tests/TagEkyc.IntegrationTests/ApplicationPersistencePrincipalTests.cs'
)
$evidence = Get-ChildItem $out -File |
    Where-Object {
        $_.Name -notin @(
            'MANIFEST.tsv',
            'PACKET.md.sha256',
            'MANIFEST.tsv.sha256',
            'FAILED_RUN_CENSUS.tsv.sha256')
    } |
    ForEach-Object {
        $_.FullName.Substring($repo.Length + 1).Replace('\', '/')
    }
$paths = @(
    $changed
    $newSource
    $evidence
    'output/review/POSTGRESQL_LOSSLESS_BACKUP_RESTORE_QUALIFICATION_BLOCKER_V0_1/BLOCKER_REPORT.md'
) | Where-Object { $_ } | Sort-Object -Unique

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# hash_basis=WORKING_TREE_BYTES@HEAD_b02898d02c9204fb8afb7d381d32a31bfa3d322e')
$lines.Add("sha256`trepository`tpath")
foreach ($path in $paths) {
    $absolute = Join-Path $repo $path
    if (-not (Test-Path -LiteralPath $absolute)) {
        throw "MANIFEST_MISSING=$path"
    }
    $hash = (Get-FileHash -LiteralPath $absolute -Algorithm SHA256).Hash
    $lines.Add("$hash`tSERVER`t$path")
}
[System.IO.File]::WriteAllLines($manifest, $lines, [System.Text.UTF8Encoding]::new($false))

foreach ($name in @('PACKET.md', 'MANIFEST.tsv', 'FAILED_RUN_CENSUS.tsv')) {
    $hash = (Get-FileHash (Join-Path $out $name) -Algorithm SHA256).Hash
    [System.IO.File]::WriteAllText(
        (Join-Path $out "$name.sha256"),
        "$hash  $name`n",
        [System.Text.UTF8Encoding]::new($false))
}

Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip -CompressionLevel Optimal
$zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash
[System.IO.File]::WriteAllText(
    "$zip.sha256",
    "$zipHash  PRODUCTION_VERIFICATION_PERSISTENCE_PRINCIPAL_CORRECTION_V0_1.zip`n",
    [System.Text.UTF8Encoding]::new($false))

"MANIFEST_DATA=$($lines.Count - 2)"
"PACKET_SHA=$((Get-FileHash (Join-Path $out 'PACKET.md') -Algorithm SHA256).Hash)"
"MANIFEST_SHA=$((Get-FileHash $manifest -Algorithm SHA256).Hash)"
"CENSUS_SHA=$((Get-FileHash (Join-Path $out 'FAILED_RUN_CENSUS.tsv') -Algorithm SHA256).Hash)"
"ZIP_SHA=$zipHash"
