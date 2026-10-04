[CmdletBinding()]
param(
    [string]$Target = 'C:\CCCD\TagEkyc-SMID50-Candidate-20261004',
    [string]$VendorRoot = 'C:\CCCD\CCCDID50',
    [Parameter(Mandatory = $true)]
    [string]$CandidateManifestSha256
)

$ErrorActionPreference = 'Stop'
$harness = Join-Path $Target 'Smid50CurrentPathQualification.v03.exe'
$helper = Join-Path $Target 'TagEkyc.CaptureAgent.Smid50Service.v03.exe'
$sdk = Join-Path $Target 'sdk'
$manifest = Join-Path $Target 'smid50-sdk-manifest.json'
$resultPath = Join-Path $Target 'smid50-v03-qualification-result.json'
$script:EnumerationErrors = [Collections.Generic.List[string]]::new()

function Get-Sha256([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
}

function Add-EnumerationError([string]$Phase, [string]$Path, [Exception]$Exception) {
    $script:EnumerationErrors.Add("$Phase|$Path|$($Exception.GetType().FullName)")
}

function Get-LogDirectories([string]$Phase) {
    $roots = @($Target, $VendorRoot, 'C:\CCCD') | Select-Object -Unique
    $directories = foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) { continue }
        try {
            Get-ChildItem -LiteralPath $root -Directory -Recurse -ErrorAction Stop |
                Where-Object { $_.Name -in @('log', 'logd') }
        }
        catch {
            Add-EnumerationError "$Phase-directory-enumeration" $root $_.Exception
        }
    }
    @($directories | Sort-Object FullName -Unique)
}

function Get-LogCensus([string]$Phase) {
    $rows = foreach ($directory in Get-LogDirectories $Phase) {
        try {
            $files = @(Get-ChildItem -LiteralPath $directory.FullName -File -Recurse -ErrorAction Stop)
        }
        catch {
            Add-EnumerationError "$Phase-file-enumeration" $directory.FullName $_.Exception
            continue
        }
        foreach ($file in $files) {
            try {
                [pscustomobject]@{
                    Path = $file.FullName
                    Length = $file.Length
                    LastWriteTimeUtc = $file.LastWriteTimeUtc.ToString('O')
                    Sha256 = Get-Sha256 $file.FullName
                }
            }
            catch {
                [pscustomobject]@{
                    Path = $file.FullName
                    Length = $file.Length
                    LastWriteTimeUtc = $file.LastWriteTimeUtc.ToString('O')
                    Sha256 = 'UNREADABLE'
                }
            }
        }
    }
    @($rows | Sort-Object Path -Unique)
}

function Find-Sequence([byte[]]$Haystack, [byte[]]$Needle) {
    if ($Needle.Length -eq 0 -or $Haystack.Length -lt $Needle.Length) { return $false }
    for ($offset = 0; $offset -le $Haystack.Length - $Needle.Length; $offset++) {
        $equal = $true
        for ($index = 0; $index -lt $Needle.Length; $index++) {
            if ($Haystack[$offset + $index] -ne $Needle[$index]) { $equal = $false; break }
        }
        if ($equal) { return $true }
    }
    return $false
}

function Get-SensitiveNeedles {
    $outputDirectory = Join-Path $VendorRoot 'win-x86\smid50-output'
    if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) { return @() }
    try {
        $artifacts = @(Get-ChildItem -LiteralPath $outputDirectory -File -ErrorAction Stop)
    }
    catch {
        Add-EnumerationError 'sensitive-reference-enumeration' $outputDirectory $_.Exception
        return @()
    }
    $needles = foreach ($artifact in $artifacts) {
        $bytes = [IO.File]::ReadAllBytes($artifact.FullName)
        try {
            if ($bytes.Length -eq 0) { continue }
            $segments = [Collections.Generic.List[byte[]]]::new()
            if ($bytes.Length -lt 32) {
                $segments.Add([byte[]]$bytes.Clone())
            }
            else {
                $offsets = @(0, [Math]::Max(0, [int](($bytes.Length - 32) / 2)), ($bytes.Length - 32))
                foreach ($offset in $offsets | Select-Object -Unique) {
                    $segment = New-Object byte[] 32
                    [Array]::Copy($bytes, $offset, $segment, 0, 32)
                    $segments.Add($segment)
                }
            }
            foreach ($segment in $segments) {
                [pscustomobject]@{ Artifact = $artifact.Name; Kind = 'binary-segment'; Bytes = $segment }
                $hex = [BitConverter]::ToString($segment).Replace('-', '')
                [pscustomobject]@{ Artifact = $artifact.Name; Kind = 'hex-segment'; Bytes = [Text.Encoding]::ASCII.GetBytes($hex) }
                [pscustomobject]@{ Artifact = $artifact.Name; Kind = 'base64-segment'; Bytes = [Text.Encoding]::ASCII.GetBytes([Convert]::ToBase64String($segment)) }
            }
        }
        finally {
            [Array]::Clear($bytes, 0, $bytes.Length)
        }
    }
    @($needles)
}

function Test-ChangedLogs([object[]]$Changed, [object[]]$Needles) {
    $scans = foreach ($file in $Changed) {
        if ($file.Sha256 -eq 'UNREADABLE') {
            [pscustomobject]@{
                Path = $file.Path; Length = $file.Length; Sha256 = $file.Sha256
                PayloadNeedleMatch = $null; PayloadNeedleMatchCount = $null
                MrzLikeTextObserved = $null; KeywordOnlyObservation = $null
            }
            continue
        }
        $bytes = [IO.File]::ReadAllBytes($file.Path)
        try {
            $matches = @($Needles | Where-Object { Find-Sequence $bytes $_.Bytes })
            $text = [Text.Encoding]::UTF8.GetString($bytes)
            $mrzLike = [regex]::IsMatch($text, '(?m)(P<|I<)[A-Z0-9<]{20,}') -or
                [regex]::IsMatch($text, '(?m)[A-Z0-9<]{30,44}')
            $keyword = [regex]::IsMatch($text, '(?i)\b(MRZ|document[ _-]?number|date[ _-]?of[ _-]?birth|DG1|DG2|DG13|DG14|DG15|SOD|APDU|signature)\b')
            [pscustomobject]@{
                Path = $file.Path
                Length = $file.Length
                Sha256 = $file.Sha256
                PayloadNeedleMatch = $matches.Count -gt 0
                PayloadNeedleMatchCount = $matches.Count
                MatchedArtifactNames = @($matches.Artifact | Sort-Object -Unique)
                MrzLikeTextObserved = $mrzLike
                KeywordOnlyObservation = $keyword
            }
        }
        finally {
            [Array]::Clear($bytes, 0, $bytes.Length)
        }
    }
    @($scans)
}

foreach ($required in @($harness, $helper, $manifest)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "QUALIFICATION_ARTIFACT_MISSING=$required" }
}

$startedAt = [DateTimeOffset]::UtcNow
$pre = Get-LogCensus 'before'
$helperSha = Get-Sha256 $helper
$manifestSha = Get-Sha256 $manifest
$harnessSha = Get-Sha256 $harness

$stdout = Join-Path $Target 'smid50-v03-harness.stdout.tmp'
$stderr = Join-Path $Target 'smid50-v03-harness.stderr.tmp'
Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue

$process = Start-Process -FilePath $harness -ArgumentList @(
    $helper,
    $helperSha,
    $sdk,
    $manifest,
    $manifestSha,
    $CandidateManifestSha256
) -WorkingDirectory $Target -RedirectStandardOutput $stdout -RedirectStandardError $stderr -Wait -PassThru -WindowStyle Hidden

$completedAt = [DateTimeOffset]::UtcNow
$post = Get-LogCensus 'after'
$preByPath = @{}
foreach ($row in $pre) { $preByPath[$row.Path] = $row }
$changed = @($post | Where-Object {
    -not $preByPath.ContainsKey($_.Path) -or
    $preByPath[$_.Path].Sha256 -ne $_.Sha256 -or
    $preByPath[$_.Path].Length -ne $_.Length
})
$needles = Get-SensitiveNeedles
$scans = Test-ChangedLogs $changed $needles
$safeStdout = Get-Content -LiteralPath $stdout -Raw -ErrorAction SilentlyContinue
$safeStderr = Get-Content -LiteralPath $stderr -Raw -ErrorAction SilentlyContinue
if ($null -eq $safeStdout) { $safeStdout = '' }
if ($null -eq $safeStderr) { $safeStderr = '' }

$finalLogDirectories = Get-LogDirectories 'final'
$enumerationErrors = @($script:EnumerationErrors | Sort-Object -Unique)
$result = [ordered]@{
    FormatVersion = 1
    StartedAtUtc = $startedAt
    CompletedAtUtc = $completedAt
    MachineName = $env:COMPUTERNAME
    WindowsUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    CurrentPath = 'Smid50ReaderProcessClient->Smid50CccdReader->x86-helper->physical-reader'
    HarnessExitCode = $process.ExitCode
    HarnessSha256 = $harnessSha
    HelperSha256 = $helperSha
    SdkManifestSha256 = $manifestSha
    CandidateManifestSha256 = $CandidateManifestSha256.ToUpperInvariant()
    SdkConfigSha256 = Get-Sha256 (Join-Path $sdk 'config_V5.yaml')
    HarnessOutput = $safeStdout.Trim()
    HarnessError = $safeStderr.Trim()
    LogDirectoryPaths = @($finalLogDirectories.FullName)
    EnumerationErrorCount = $enumerationErrors.Count
    EnumerationErrors = $enumerationErrors
    LogFileCountBefore = $pre.Count
    LogFileCountAfter = $post.Count
    ChangedLogFileCount = $changed.Count
    ChangedLogFiles = $scans
    SensitiveReferenceArtifactCount = @($needles.Artifact | Sort-Object -Unique).Count
    AnyPayloadNeedleMatch = @($scans | Where-Object PayloadNeedleMatch).Count -gt 0
    AnyMrzLikeTextObserved = @($scans | Where-Object MrzLikeTextObserved).Count -gt 0
    UnreadableChangedLogCount = @($scans | Where-Object { $_.Sha256 -eq 'UNREADABLE' }).Count
}

$json = $result | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText($resultPath, $json, [Text.UTF8Encoding]::new($false))
Remove-Item -LiteralPath $stdout, $stderr -Force -ErrorAction SilentlyContinue

$json
if ($process.ExitCode -ne 0) { exit $process.ExitCode }
if ($result.EnumerationErrorCount -ne 0 -or $result.AnyPayloadNeedleMatch -or $result.AnyMrzLikeTextObserved -or $result.UnreadableChangedLogCount -ne 0) { exit 5 }
exit 0
