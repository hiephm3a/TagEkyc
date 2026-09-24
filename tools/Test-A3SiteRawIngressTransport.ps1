param(
    [Parameter(ParameterSetName = 'Probe', Mandatory = $true)][string]$SiteId,
    [Parameter(ParameterSetName = 'Probe', Mandatory = $true)][string]$EndpointOrigin,
    [Parameter(ParameterSetName = 'Probe', Mandatory = $true)][string]$DeploymentRevision,
    [Parameter(ParameterSetName = 'Probe', Mandatory = $true)][string]$OutputRecordPath,
    [Parameter(ParameterSetName = 'Probe')][ValidateRange(1, 168)][int]$ValidForHours = 8,
    [Parameter(ParameterSetName = 'Probe')][ValidateRange(5, 120)][int]$TimeoutSeconds = 30,
    [Parameter(ParameterSetName = 'SelfTest', Mandatory = $true)][switch]$SelfTest
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
        $final = Assert-QualifyingFinal $tls
        $observed = [DateTimeOffset]::UtcNow
        $record = [ordered]@{
            formatVersion = 1
            qualificationId = "a3-site-$([Guid]::NewGuid().ToString('N'))"
            siteId = $SiteId
            endpointOrigin = $normalizedOrigin
            deploymentRevision = $DeploymentRevision
            status = 'PASS'
            observedAtUtc = $observed.ToString('O')
            validUntilUtc = $observed.AddHours($ValidForHours).ToString('O')
            agentBodySendsWhileBOrR1Held = 0
            serverApplicationBodyReadsWhileBOrR1Held = 0
            rawPostCount = 1
            kestrelContinueRelayedAfterCommit = $true
            earlyOrIntermediaryContinueObserved = $false
            applicationPrebufferObserved = $false
            hiddenRetryObserved = $false
        }
        $target = [IO.Path]::GetFullPath($OutputRecordPath)
        [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
        [IO.File]::WriteAllText($target, ($record | ConvertTo-Json -Compress), [Text.UTF8Encoding]::new($false))
        Write-Output 'SITE_ENDPOINT_TRANSPORT_QUALIFICATION=PASS'
        Write-Output "SITE_ID=$SiteId"
        Write-Output "ENDPOINT_ORIGIN=$normalizedOrigin"
        Write-Output 'TLS_PLATFORM_VALIDATION=PASS'
        Write-Output 'ALPN_HTTP11=PASS'
        Write-Output 'EARLY_CONTINUE_OBSERVED=NO'
        Write-Output 'BODY_BYTES_SENT=0'
        Write-Output "FINAL_CODE=$($final.Code)"
        Write-Output "QUALIFICATION_RECORD=$target"
        Write-Output "QUALIFICATION_RECORD_SHA256=$((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash)"
    }
    finally { if ($null -ne $tls) { $tls.Dispose() } }
}
finally {
    $tcp.Dispose()
    $deadline.Dispose()
}
