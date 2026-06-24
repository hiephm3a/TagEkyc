<#
TIP-68 SoftHSM provisioning helper.

SOFTHSM2_MODULE is the canonical CI/other-machine entry point for selecting the
PKCS#11 module. Built-in paths are local developer fallbacks only.
#>

param(
    [string]$TokenDirectory = (Join-Path $env:TEMP "tagekyc-tip68-softhsm"),
    [string]$TokenLabel = "tagekyc-tip68",
    [string]$KeyLabel = "tagekyc-tip68-es256",
    [string]$KeyObjectIdHex = "74697036382d6573323536",
    [string]$Kid = "tagekyc-es256-2026-v1",
    [string]$UserPin = $env:TAG_EKYC_SOFTHSM_USER_PIN,
    [string]$SoPin = $env:TAG_EKYC_SOFTHSM_SO_PIN,
    [string]$ModulePath = $env:SOFTHSM2_MODULE,
    [string]$SoftHsmUtil = "softhsm2-util"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($UserPin)) {
    throw "Set TAG_EKYC_SOFTHSM_USER_PIN before provisioning."
}

if ([string]::IsNullOrWhiteSpace($SoPin)) {
    throw "Set TAG_EKYC_SOFTHSM_SO_PIN before provisioning."
}

if ([string]::IsNullOrWhiteSpace($ModulePath)) {
    $candidateModules = @(
        "C:\SoftHSM2\lib\softhsm2-x64.dll",
        "C:\Program Files\SoftHSM2\lib\softhsm2-x64.dll",
        "C:\Program Files (x86)\SoftHSM2\lib\softhsm2.dll",
        (Join-Path $env:TEMP "SoftHSM2-2.5.0-portable\SoftHSM2\lib\softhsm2-x64.dll"),
        "/usr/lib/softhsm/libsofthsm2.so",
        "/usr/lib/x86_64-linux-gnu/softhsm/libsofthsm2.so",
        "/opt/homebrew/lib/softhsm/libsofthsm2.so"
    )
    $ModulePath = $candidateModules | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($ModulePath) -or -not (Test-Path -LiteralPath $ModulePath)) {
    throw "SoftHSM2 module not found. Set SOFTHSM2_MODULE to the PKCS#11 module path."
}

$softHsmCommand = Get-Command $SoftHsmUtil -ErrorAction Stop

Remove-Item -LiteralPath $TokenDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $TokenDirectory | Out-Null

$configPath = Join-Path $TokenDirectory "softhsm2.conf"
@"
directories.tokendir = $TokenDirectory
objectstore.backend = file
log.level = ERROR
"@ | Set-Content -LiteralPath $configPath -Encoding ASCII

$env:SOFTHSM2_CONF = $configPath

& $softHsmCommand.Source --init-token --free --label $TokenLabel --so-pin $SoPin --pin $UserPin | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "SoftHSM token initialization failed."
}

Write-Host "SoftHSM TIP-68 token initialized."
Write-Host "SOFTHSM2_CONF=$configPath"
Write-Host "TagEkyc:EvidenceSigning:Backend=Pkcs11"
Write-Host "TagEkyc:EvidenceSigning:Pkcs11:LibraryPath=$ModulePath"
Write-Host "TagEkyc:EvidenceSigning:Pkcs11:TokenLabel=$TokenLabel"
Write-Host "TagEkyc:EvidenceSigning:Pkcs11:KeyLabel=$KeyLabel"
Write-Host "TagEkyc:EvidenceSigning:Pkcs11:KeyObjectId=$KeyObjectIdHex"
Write-Host "TagEkyc:EvidenceSigning:Pkcs11:Kid=$Kid"
Write-Host "TagEkyc:EvidenceSigning:Pkcs11:Pin=<redacted>"
Write-Host "TIP-68 fixture creates the EC P-256 keypair via Pkcs11Interop C_GenerateKeyPair."
