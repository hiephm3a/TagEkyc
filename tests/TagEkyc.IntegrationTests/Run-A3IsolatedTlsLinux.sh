#!/usr/bin/env bash
set -euo pipefail

if [[ "${A3_ISOLATED_TLS_CONTAINER:-}" != "1" ]]; then
  echo 'This runner must execute in the disposable A3 TLS container.' >&2
  exit 2
fi

scratch="$(mktemp -d /tmp/tagekyc-a3-tls.XXXXXXXX)"
crl_server_pid=''
cleanup() {
  if [[ -n "$crl_server_pid" ]]; then
    kill "$crl_server_pid" 2>/dev/null || true
    wait "$crl_server_pid" 2>/dev/null || true
  fi
  if [[ "$scratch" == /tmp/tagekyc-a3-tls.* && -d "$scratch" ]]; then
    rm -rf -- "$scratch"
  fi
}
trap cleanup EXIT

openssl req -x509 -newkey rsa:2048 -nodes -sha256 -days 1 \
  -keyout "$scratch/ca.key" -out "$scratch/ca.crt" \
  -subj "/CN=TagEkyc-A3-Container-$(date +%s)-$$" \
  -addext 'basicConstraints=critical,CA:TRUE' \
  -addext 'keyUsage=critical,keyCertSign,cRLSign' >/dev/null 2>&1
openssl req -new -newkey rsa:2048 -nodes \
  -keyout "$scratch/ip.key" -out "$scratch/ip.csr" \
  -subj '/CN=127.0.0.1' >/dev/null 2>&1
printf '%s\n' \
  'basicConstraints=critical,CA:FALSE' \
  'keyUsage=critical,digitalSignature,keyEncipherment' \
  'extendedKeyUsage=serverAuth' \
  'subjectAltName=IP:127.0.0.1' \
  'crlDistributionPoints=URI:http://127.0.0.1:8765/ca.crl' > "$scratch/ip.ext"
openssl x509 -req -in "$scratch/ip.csr" -CA "$scratch/ca.crt" \
  -CAkey "$scratch/ca.key" -set_serial 0x1001 -days 1 -sha256 \
  -extfile "$scratch/ip.ext" -out "$scratch/ip.crt" >/dev/null 2>&1
openssl pkcs12 -export -out "$scratch/ip.pfx" \
  -inkey "$scratch/ip.key" -in "$scratch/ip.crt" -certfile "$scratch/ca.crt" \
  -passout pass: >/dev/null 2>&1
mkdir "$scratch/newcerts"
: > "$scratch/index.txt"
printf '1002\n' > "$scratch/serial"
printf '1001\n' > "$scratch/crlnumber"
printf '%s\n' \
  '[ca]' \
  'default_ca=CA_default' \
  '[CA_default]' \
  "database=$scratch/index.txt" \
  "new_certs_dir=$scratch/newcerts" \
  "certificate=$scratch/ca.crt" \
  "private_key=$scratch/ca.key" \
  "serial=$scratch/serial" \
  "crlnumber=$scratch/crlnumber" \
  'default_md=sha256' \
  'default_crl_days=1' > "$scratch/ca.cnf"
openssl ca -config "$scratch/ca.cnf" -gencrl -out "$scratch/ca.pem.crl" >/dev/null 2>&1
openssl crl -in "$scratch/ca.pem.crl" -outform DER -out "$scratch/ca.crl"
python3 -m http.server 8765 --bind 127.0.0.1 --directory "$scratch" > "$scratch/http.log" 2>&1 &
crl_server_pid=$!
for attempt in 1 2 3 4 5; do
  if python3 -c 'import urllib.request; urllib.request.urlopen("http://127.0.0.1:8765/ca.crl", timeout=1).read()' 2>/dev/null; then
    break
  fi
  sleep 1
done
cat /etc/ssl/certs/ca-certificates.crt "$scratch/ca.crt" > "$scratch/bundle.pem"

export SSL_CERT_FILE="$scratch/bundle.pem"
export A3_ISOLATED_TLS_RUNNER=1
export A3_TEST_TLS_IP_PFX="$scratch/ip.pfx"
export A3_TEST_TLS_PFX_PASSWORD=''
export TAGEKYC_POSTGRES_TEST_CONNECTION_STRING="${TAGEKYC_POSTGRES_TEST_CONNECTION_STRING:-Host=tagekyc-postgres-test;Port=5432;Database=tagekyc_persistence_tests;Username=tagekyc;Password=tagekyc;Include Error Detail=true}"

project=/work/TagEkyc/tests/TagEkyc.IntegrationTests/TagEkyc.IntegrationTests.csproj
results=/work/TagEkyc/tests/TagEkyc.IntegrationTests/TestResults/a3-expect-fence
mkdir -p "$results"
result_tag="${A3_TLS_RESULT_TAG:-$(date +%Y%m%d%H%M%S)-$$}"
result_prefix="${A3_TLS_RESULT_PREFIX:-a3-expect-strict-linux-rollback}"
test_filter="${A3_TLS_FILTER:-FullyQualifiedName~ExpectFence_RolledBackR1NeverReleasesAgentBody}"
dotnet test "$project" -p:EnableCaptureAgentA3Acceptance=true \
  --filter "$test_filter" \
  --verbosity minimal --results-directory "$results" \
  --logger "trx;LogFileName=${result_prefix}-${result_tag}.trx"
