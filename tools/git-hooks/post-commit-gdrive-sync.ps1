param(
    [string]$Profile = "main",
    [string]$Config = ".gdrive/config.local.json",
    [string]$Commit = "HEAD"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $repoRoot
try {
    dotnet run --project tools\TagEkyc.GDriveSync -- sync-changed --profile $Profile --config $Config --commit $Commit
}
finally {
    Pop-Location
}
