param(
    [string]$Profile = "main",
    [string]$Config = ".gdrive/config.local.json"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$hookPath = Join-Path $repoRoot ".git\hooks\post-commit"
$scriptPath = "tools/git-hooks/post-commit-gdrive-sync.ps1"

$hook = @"
#!/bin/sh
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$scriptPath" -Profile "$Profile" -Config "$Config"
"@

Set-Content -LiteralPath $hookPath -Value $hook -NoNewline -Encoding ASCII
Write-Host "Installed post-commit hook: $hookPath"
