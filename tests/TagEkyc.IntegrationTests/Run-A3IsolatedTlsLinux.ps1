param(
    [string]$TestFilter = 'FullyQualifiedName~ExpectFence_RolledBackR1NeverReleasesAgentBody',
    [string]$ResultPrefix = 'a3-expect-strict-linux-rollback',
    [switch]$EarlyContinueMutation,
    [switch]$ExpectedTestFailure,
    [string]$QualificationRecordPath,
    [string]$QualificationSiteId = 'development-site',
    [string]$QualificationDeploymentRevision = 'development-1'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$parent = Split-Path $repoRoot -Parent
$nuget = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget\packages'
$compose = Join-Path $repoRoot 'docker-compose.persistence-tests.yml'

if (-not (Test-Path -LiteralPath $nuget -PathType Container)) {
    throw "NuGet package cache is missing: $nuget"
}
if ((docker ps -a --filter 'name=^/tagekyc-postgres-test$' --format '{{.ID}}').Length -ne 0) {
    throw 'Dedicated PostgreSQL container is already present; refusing to reuse or remove it.'
}
if ((docker network ls --filter 'name=^tagekyc_default$' --format '{{.ID}}').Length -ne 0) {
    throw 'Dedicated PostgreSQL network is already present; refusing to reuse or remove it.'
}

$started = $false
try {
    docker build --quiet -f (Join-Path $PSScriptRoot 'A3IsolatedTlsRunner.Dockerfile') -t tagekyc-a3-isolated-tls-runner $repoRoot
    if ($LASTEXITCODE -ne 0) { throw 'Isolated TLS runner image build failed.' }

    $started = $true
    docker compose -f $compose up -d --wait
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL fixture startup failed.' }

    $recordEnvironment = @()
    if ($QualificationRecordPath) {
        $resolvedRecord = [System.IO.Path]::GetFullPath($QualificationRecordPath)
        if (-not $resolvedRecord.StartsWith($repoRoot + [System.IO.Path]::DirectorySeparatorChar,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'Qualification record must stay inside the TagEkyc repository.'
        }
        $recordRelative = [System.IO.Path]::GetRelativePath($repoRoot, $resolvedRecord).Replace('\', '/')
        $recordEnvironment = @(
            '-e', "A3_SITE_QUALIFICATION_RECORD_PATH=/work/TagEkyc/$recordRelative",
            '-e', "A3_SITE_QUALIFICATION_SITE_ID=$QualificationSiteId",
            '-e', "A3_SITE_QUALIFICATION_DEPLOYMENT_REVISION=$QualificationDeploymentRevision")
    }
    $dockerArguments = @('run', '--rm', '--network', 'tagekyc_default',
        '-e', 'A3_ISOLATED_TLS_CONTAINER=1',
        '-e', "A3_TLS_FILTER=$TestFilter",
        '-e', "A3_TLS_RESULT_PREFIX=$ResultPrefix",
        '-e', "A3_F3_EARLY_CONTINUE=$([int]$EarlyContinueMutation.IsPresent)") +
        $recordEnvironment + @(
        '-v', '/var/run/docker.sock:/var/run/docker.sock',
        '-v', "${parent}:/work",
        '-v', "${nuget}:/root/.nuget/packages",
        '-w', '/work/TagEkyc',
        'tagekyc-a3-isolated-tls-runner',
        'bash', '/work/TagEkyc/tests/TagEkyc.IntegrationTests/Run-A3IsolatedTlsLinux.sh')
    & docker @dockerArguments
    $testExit = $LASTEXITCODE
    if ($ExpectedTestFailure) {
        if ($testExit -eq 0) { throw 'Expected the isolated TLS test to fail, but it passed.' }
    }
    elseif ($testExit -ne 0) { throw 'Isolated TLS test run failed; inspect its TRX.' }
}
finally {
    if ($started) {
        docker compose -f $compose down -v --remove-orphans
    }
}
