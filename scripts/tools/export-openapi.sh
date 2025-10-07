#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="${PROJECT_PATH:-api/src/Presentation}"
URL="${OPENAPI_URL:-http://localhost:8080/swagger/v1/swagger.json}"
OUT_FILE="${OUT_FILE:-contracts/openapi.json}"
TIMEOUT_MS="${OPENAPI_TIMEOUT_MS:-90000}"

# flags opcionales: --project, --url, --out, --timeout-ms
while [[ $# -gt 0 ]]; do
  case "$1" in
    --project) PROJECT_PATH="$2"; shift 2 ;;
    --url) URL="$2"; shift 2 ;;
    --out|--out-file) OUT_FILE="$2"; shift 2 ;;
    --timeout-ms) TIMEOUT_MS="$2"; shift 2 ;;
    *) echo "Unknown arg: $1" >&2; exit 1 ;;
  esac
done

echo "Starting API via dotnet run..."
dotnet run --project "$PROJECT_PATH" --no-build --urls "http://localhost:8080" >/dev/null 2>&1 &
PID=$!

cleanup(){ if kill -0 "$PID" 2>/dev/null; then kill "$PID" 2>/dev/null || true; fi; }
trap cleanup EXIT

wait_http200() {
  local u="$1" timeout_ms="$2" start now
  start=$(date +%s%3N 2>/dev/null || echo $(( $(date +%s)*1000 )))
  while true; do
    if curl -fsS --max-time 2 "$u" >/dev/null 2>&1; then return 0; fi
    now=$(date +%s%3N 2>/dev/null || echo $(( $(date +%s)*1000 )))
    if (( now - start >= timeout_ms )); then echo "Timeout waiting for $u" >&2; return 1; fi
    sleep 1
  done
}

wait_http200 "$URL" "$TIMEOUT_MS"
mkdir -p "$(dirname "$OUT_FILE")"
curl -fsS "$URL" -o "$OUT_FILE"
echo "OpenAPI exported to $OUT_FILE"
