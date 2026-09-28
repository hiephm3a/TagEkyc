param(
    [Parameter(ParameterSetName = 'Probe', Mandatory = $true)][string]$SiteId,
    [Parameter(ParameterSetName = 'Probe', Mandatory = $true)][string]$EndpointOrigin,
    [Parameter(ParameterSetName = 'Probe', Mandatory = $true)][string]$DeploymentRevision,
    [Parameter(ParameterSetName = 'Probe', Mandatory = $true)][string]$OutputRecordPath,
    [Parameter(ParameterSetName = 'Probe')][ValidateRange(1, 168)][int]$ValidForHours = 8,
    [Parameter(ParameterSetName = 'Probe')][ValidateRange(5, 120)][int]$TimeoutSeconds = 30,
    [Parameter(ParameterSetName = 'Probe')][string]$MeasurementReportPath,
    [Parameter(ParameterSetName = 'SelfTest', Mandatory = $true)][switch]$SelfTest,
    [Parameter(ParameterSetName = 'SelfTest')][string]$SelfTestOutputRecordPath
)

$ErrorActionPreference = 'Stop'

function Read-CrlfLine([System.IO.Stream]$Stream, [int]$Maximum) {
    $bytes = [System.Collections.Generic.List[byte]]::new()
    while ($bytes.Count -le $Maximum) {
        $value = $Stream.ReadByte()
        if ($value -lt 0) { throw 'SITE_PROBE_CONNECTION_CLOSED' }
        if ($value -eq 13) {
            if ($Stream.ReadByte() -ne 10) { throw 'SITE_PROBE_RESPONSE_INVALID' }
            return [Text.Encoding]::ASCII.GetString($bytes.ToArray())
        }
        if ($value -lt 32 -or $value -gt 126) { throw 'SITE_PROBE_RESPONSE_INVALID' }
        $bytes.Add([byte]$value)
    }
    throw 'SITE_PROBE_RESPONSE_TOO_LARGE'
}

function Read-Response([System.IO.Stream]$Stream) {
    $statusLine = Read-CrlfLine $Stream 2048
    if ($statusLine -notmatch '^HTTP/1\.1 ([0-9]{3})(?: .*)?$') {
        throw 'SITE_PROBE_RESPONSE_INVALID'
    }
    $status = [int]$Matches[1]
    $headers = @{}
    $bytes = $statusLine.Length + 2
    $fields = 0
    while ($true) {
        $line = Read-CrlfLine $Stream 8192
        $bytes += $line.Length + 2
        if ($bytes -gt 8192) { throw 'SITE_PROBE_RESPONSE_TOO_LARGE' }
        if ($line.Length -eq 0) { break }
        if (++$fields -gt 32 -or $line -notmatch '^([A-Za-z0-9-]+):[ ]*(.*)$') {
            throw 'SITE_PROBE_RESPONSE_INVALID'
        }
        $name = $Matches[1].ToLowerInvariant()
        if ($headers.ContainsKey($name)) { throw 'SITE_PROBE_DUPLICATE_RESPONSE_HEADER' }
        $headers[$name] = $Matches[2]
    }
    [pscustomobject]@{ Status = $status; Headers = $headers }
}

function Read-ExactBody([System.IO.Stream]$Stream, [int]$Length) {
    $result = [byte[]]::new($Length)
    $offset = 0
    while ($offset -lt $Length) {
        $read = $Stream.Read($result, $offset, $Length - $offset)
        if ($read -le 0) { throw 'SITE_PROBE_RESPONSE_BODY_INCOMPLETE' }
        $offset += $read
    }
    $result
}

function Assert-QualifyingFinal([System.IO.Stream]$Stream) {
    $response = Read-Response $Stream
    if ($response.Status -ge 100 -and $response.Status -lt 200) {
        if ($response.Status -eq 100) { throw 'SITE_QUALIFICATION_EARLY_CONTINUE_OBSERVED' }
        throw 'SITE_QUALIFICATION_UNEXPECTED_INTERIM_RESPONSE'
    }
    $parsedLength = 0
    if ($response.Status -notin @(403, 503) -or
        -not $response.Headers.ContainsKey('content-length') -or
        $response.Headers.ContainsKey('transfer-encoding') -or
        $response.Headers.ContainsKey('content-encoding') -or
        -not [int]::TryParse($response.Headers['content-length'], [ref]$parsedLength)) {
        throw 'SITE_PROBE_FINAL_RESPONSE_INVALID'
    }
    $length = $parsedLength
    if ($length -lt 1 -or $length -gt 65536) { throw 'SITE_PROBE_FINAL_RESPONSE_INVALID' }
    $body = [Text.Encoding]::UTF8.GetString((Read-ExactBody $Stream $length)) | ConvertFrom-Json
    $allowed = @(
        'CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_INVALID',
        'CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_EXPIRED',
        'ACCESS_DENIED')
    if ([string]$body.code -cnotin $allowed) { throw 'SITE_PROBE_NOT_CAPTURE_RUNTIME_RAW_INGRESS' }
    [pscustomobject]@{ Status = $response.Status; Code = [string]$body.code }
}

function Read-SyntheticAgentMeasurements([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'SITE_QUALIFICATION_MEASUREMENT_REPORT_NOT_FOUND'
    }
    $measurement = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $allowedTopLevel = @('SchemaVersion','Status','Qualifies','Reports')
    if (@($measurement.PSObject.Properties | Where-Object { $_.Name -cnotin $allowedTopLevel }).Count -ne 0 -or
        $measurement.SchemaVersion -ne 1 -or [string]$measurement.Status -cne 'MEASURED' -or
        $null -eq $measurement.Reports -or @($measurement.Reports).Count -lt 1) {
        throw 'SITE_QUALIFICATION_MEASUREMENT_REPORT_INVALID'
    }
    $allowedReport = @(
        'QualificationRunId','QualificationSuiteId','SiteId','EndpointOrigin','DeploymentRevision',
        'State','Mode','CreatedAtUtc','ExpiresAtUtc','ConsumedAtUtc',
        'BrokerHeldAtUtc','BrokerCommittedAtUtc','RawPostCount',
        'ServerApplicationBodyReadsWhileBrokerHeld','ClientTransportEntryCount',
        'ContentBytesCopied','AgentBodyBytesSentWhileBrokerHeld','ObservedContinue',
        'ContinueObservedBeforeBrokerCommit','ApplicationPrebufferObserved',
        'FinalResponseObserved','AgentObservationCompleted','ServerObservationCompleted',
        'EvidenceComplete','HiddenRetryObserved','KestrelContinueRelayedAfterCommit')
    foreach ($report in @($measurement.Reports)) {
        $properties = @($report.PSObject.Properties.Name)
        if ($properties.Count -ne $allowedReport.Count -or
            @($properties | Where-Object { $_ -cnotin $allowedReport }).Count -ne 0 -or
            [Guid]$report.QualificationRunId -eq [Guid]::Empty -or
            [Guid]$report.QualificationSuiteId -eq [Guid]::Empty -or
            [string]$report.State -cne 'BrokerCommitted' -or
            [string]$report.Mode -cnotin @('FullBodyHeldCommit','LostFinalNoRetry') -or
            [int]$report.RawPostCount -lt 0 -or
            [int]$report.ServerApplicationBodyReadsWhileBrokerHeld -lt 0 -or
            [int]$report.ClientTransportEntryCount -lt 0 -or
            [long]$report.ContentBytesCopied -le 0 -or
            [long]$report.AgentBodyBytesSentWhileBrokerHeld -lt 0) {
            throw 'SITE_QUALIFICATION_MEASUREMENT_REPORT_INVALID'
        }
    }
    $measurement
}

function Write-SiteQualificationCandidate(
    [string]$CandidateSiteId,
    [string]$CandidateEndpointOrigin,
    [string]$CandidateDeploymentRevision,
    [string]$CandidateOutputRecordPath,
    [int]$CandidateValidForHours,
    [object]$SyntheticMeasurements,
    [bool]$EarlyContinueObserved) {
    $observed = [DateTimeOffset]::UtcNow
    $reports = if ($null -eq $SyntheticMeasurements) { @() } else { @($SyntheticMeasurements.Reports) }
    $now = [DateTimeOffset]::UtcNow
    $suiteIds = @($reports | ForEach-Object { [Guid]$_.QualificationSuiteId } | Select-Object -Unique)
    $complete = $reports.Count -eq 2 -and
        @($reports | Where-Object { $_.Mode -ceq 'FullBodyHeldCommit' }).Count -eq 1 -and
        @($reports | Where-Object { $_.Mode -ceq 'LostFinalNoRetry' }).Count -eq 1 -and
        $suiteIds.Count -eq 1 -and
        @($reports | Where-Object {
            [string]$_.SiteId -cne $CandidateSiteId -or
            [string]$_.EndpointOrigin -cne $CandidateEndpointOrigin -or
            [string]$_.DeploymentRevision -cne $CandidateDeploymentRevision -or
            -not [bool]$_.EvidenceComplete -or -not [bool]$_.AgentObservationCompleted -or
            -not [bool]$_.ServerObservationCompleted -or
            [DateTimeOffset]$_.CreatedAtUtc -gt $now -or [DateTimeOffset]$_.ExpiresAtUtc -le $now
        }).Count -eq 0
    $agentHeldSends = 0
    $serverHeldReads = 0
    $rawPostCount = 1
    $kestrelAfterCommit = $true
    $prebuffer = $false
    $hiddenRetry = $false
    $fullBodyFinal = $false
    $lostFinalMissing = $false
    if ($complete) {
        foreach ($report in $reports) {
            if ($report.AgentBodyBytesSentWhileBrokerHeld -gt 0) { $agentHeldSends++ }
            $serverHeldReads += [int]$report.ServerApplicationBodyReadsWhileBrokerHeld
            if ([int]$report.RawPostCount -ne 1) { $rawPostCount = [int]$report.RawPostCount }
            $kestrelAfterCommit = $kestrelAfterCommit -and
                [bool]$report.KestrelContinueRelayedAfterCommit -and [bool]$report.ObservedContinue -and
                -not [bool]$report.ContinueObservedBeforeBrokerCommit
            $prebuffer = $prebuffer -or [bool]$report.ApplicationPrebufferObserved
            $hiddenRetry = $hiddenRetry -or [bool]$report.HiddenRetryObserved -or
                [int]$report.ClientTransportEntryCount -ne 1
            if ($report.Mode -ceq 'FullBodyHeldCommit') {
                $fullBodyFinal = [bool]$report.FinalResponseObserved
            } else {
                $lostFinalMissing = -not [bool]$report.FinalResponseObserved
            }
        }
    }
    $qualifies = $complete -and -not $EarlyContinueObserved -and $agentHeldSends -eq 0 -and
        $serverHeldReads -eq 0 -and $rawPostCount -eq 1 -and $kestrelAfterCommit -and
        -not $prebuffer -and -not $hiddenRetry -and $fullBodyFinal -and $lostFinalMissing
    $record = [ordered]@{
        formatVersion = 1
        qualificationId = "a3-site-$([Guid]::NewGuid().ToString('N'))"
        siteId = $CandidateSiteId
        endpointOrigin = $CandidateEndpointOrigin
        deploymentRevision = $CandidateDeploymentRevision
        status = if (-not $complete) { 'MEASUREMENT_INCOMPLETE' } elseif ($qualifies) { 'PASS' } else { 'FAIL' }
        observedAtUtc = $observed.ToString('O')
        validUntilUtc = $observed.AddHours($CandidateValidForHours).ToString('O')
        agentBodySendsWhileBOrR1Held = $agentHeldSends
        serverApplicationBodyReadsWhileBOrR1Held = $serverHeldReads
        rawPostCount = $rawPostCount
        kestrelContinueRelayedAfterCommit = $kestrelAfterCommit
        earlyOrIntermediaryContinueObserved = $EarlyContinueObserved
        applicationPrebufferObserved = $prebuffer
        hiddenRetryObserved = $hiddenRetry
    }
    $target = [IO.Path]::GetFullPath($CandidateOutputRecordPath)
    [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
    [IO.File]::WriteAllText($target, ($record | ConvertTo-Json -Compress),
        [Text.UTF8Encoding]::new($false))
    [pscustomobject]@{ Path = $target; Status = [string]$record.status }
}

if ($SelfTest) {
    $valid = "HTTP/1.1 503 Service Unavailable`r`nContent-Type: application/json; charset=utf-8`r`nContent-Length: 94`r`n`r`n" +
        '{"code":"CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_INVALID","correlationId":"q"}'
    $validBytes = [Text.Encoding]::ASCII.GetBytes($valid)
    # Rebuild Content-Length exactly so the self-test cannot pass on a partial body.
    $body = '{"code":"CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_INVALID","correlationId":"q"}'
    $validBytes = [Text.Encoding]::ASCII.GetBytes("HTTP/1.1 503 Service Unavailable`r`nContent-Type: application/json; charset=utf-8`r`nContent-Length: $([Text.Encoding]::UTF8.GetByteCount($body))`r`n`r`n$body")
    $stream = [IO.MemoryStream]::new($validBytes)
    $accepted = Assert-QualifyingFinal $stream
    if ($accepted.Status -ne 503) { throw 'SITE_PROBE_SELF_TEST_VALID_FINAL_FAILED' }

    $early = [Text.Encoding]::ASCII.GetBytes("HTTP/1.1 100 Continue`r`n`r`n")
    $stream = [IO.MemoryStream]::new($early)
    try { Assert-QualifyingFinal $stream; throw 'SITE_PROBE_SELF_TEST_EARLY_CONTINUE_WAS_ACCEPTED' }
    catch { if ($_.Exception.Message -cne 'SITE_QUALIFICATION_EARLY_CONTINUE_OBSERVED') { throw } }
    if (-not [string]::IsNullOrWhiteSpace($SelfTestOutputRecordPath)) {
        $selfTestRecord = Write-SiteQualificationCandidate `
            'synthetic-site' 'https://synthetic.invalid:8443' 'synthetic-deployment-1' `
            $SelfTestOutputRecordPath 8 $null $false
        Write-Output 'SITE_ENDPOINT_TRANSPORT_QUALIFICATION=MEASUREMENT_INCOMPLETE'
        Write-Output "QUALIFICATION_RECORD=$($selfTestRecord.Path)"
        Write-Output "QUALIFICATION_RECORD_SHA256=$((Get-FileHash -LiteralPath $selfTestRecord.Path -Algorithm SHA256).Hash)"
    }
    Write-Output 'SITE_PROBE_SELF_TEST=PASS'
    return
}

if ([string]::IsNullOrWhiteSpace($SiteId) -or $SiteId -ceq 'development-loopback' -or
    [string]::IsNullOrWhiteSpace($DeploymentRevision)) { throw 'SITE_PROBE_IDENTITY_REQUIRED' }
$origin = $null
if (-not [Uri]::TryCreate($EndpointOrigin, [UriKind]::Absolute, [ref]$origin) -or
    $origin.Scheme -cne 'https' -or $origin.AbsolutePath -cne '/' -or
    -not [string]::IsNullOrEmpty($origin.Query) -or -not [string]::IsNullOrEmpty($origin.Fragment) -or
    -not [string]::IsNullOrEmpty($origin.UserInfo)) { throw 'SITE_PROBE_HTTPS_ORIGIN_INVALID' }
$normalizedOrigin = $origin.GetLeftPart([UriPartial]::Authority)
$deadline = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds($TimeoutSeconds))
$tcp = [Net.Sockets.TcpClient]::new()
try {
    $tcp.ConnectAsync($origin.IdnHost, $origin.Port, $deadline.Token).GetAwaiter().GetResult()
    $tls = [Net.Security.SslStream]::new($tcp.GetStream(), $false)
    try {
        $tls.ReadTimeout = $TimeoutSeconds * 1000
        $tls.WriteTimeout = $TimeoutSeconds * 1000
        $protocols = [Collections.Generic.List[Net.Security.SslApplicationProtocol]]::new()
        $protocols.Add([Net.Security.SslApplicationProtocol]::Http11)
        $options = [Net.Security.SslClientAuthenticationOptions]::new()
        $options.TargetHost = $origin.IdnHost
        $options.ApplicationProtocols = $protocols
        $options.CertificateRevocationCheckMode = [Security.Cryptography.X509Certificates.X509RevocationMode]::Online
        $tls.AuthenticateAsClientAsync($options, $deadline.Token).GetAwaiter().GetResult()
        if ($tls.NegotiatedApplicationProtocol -ne [Net.Security.SslApplicationProtocol]::Http11) {
            throw 'SITE_PROBE_HTTP11_ALPN_REQUIRED'
        }
        $request = "POST /api/ekyc/raw-export/source-ingress HTTP/1.1`r`n" +
            "Host: $($origin.Authority)`r`nContent-Type: image/jpeg`r`nContent-Length: 1`r`n" +
            "Expect: 100-continue`r`nConnection: close`r`n" +
            "X-TagEkyc-Api-Key: site-qualification-probe`r`n`r`n"
        $requestBytes = [Text.Encoding]::ASCII.GetBytes($request)
        $tls.Write($requestBytes, 0, $requestBytes.Length)
        $tls.Flush()
        # Deliberately never write the one-byte body. A conforming path returns
        # the application final response; a terminating intermediary that emits
        # 100 on its own is detected before any body can leave this process.
        $earlyContinue = $false
        $final = $null
        try { $final = Assert-QualifyingFinal $tls }
        catch {
            if ($_.Exception.Message -cne 'SITE_QUALIFICATION_EARLY_CONTINUE_OBSERVED') { throw }
            $earlyContinue = $true
        }
        $measurements = Read-SyntheticAgentMeasurements $MeasurementReportPath
        $target = Write-SiteQualificationCandidate $SiteId $normalizedOrigin `
            $DeploymentRevision $OutputRecordPath $ValidForHours $measurements $earlyContinue
        Write-Output "SITE_ENDPOINT_TRANSPORT_QUALIFICATION=$($target.Status)"
        Write-Output "SITE_ID=$SiteId"
        Write-Output "ENDPOINT_ORIGIN=$normalizedOrigin"
        Write-Output 'TLS_PLATFORM_VALIDATION=PASS'
        Write-Output 'ALPN_HTTP11=PASS'
        Write-Output "EARLY_CONTINUE_OBSERVED=$(if ($earlyContinue) { 'YES' } else { 'NO' })"
        Write-Output 'BODY_BYTES_SENT=0'
        if ($null -ne $final) { Write-Output "FINAL_CODE=$($final.Code)" }
        Write-Output "QUALIFICATION_RECORD=$($target.Path)"
        Write-Output "QUALIFICATION_RECORD_SHA256=$((Get-FileHash -LiteralPath $target.Path -Algorithm SHA256).Hash)"
    }
    finally { if ($null -ne $tls) { $tls.Dispose() } }
}
finally {
    $tcp.Dispose()
    $deadline.Dispose()
}
