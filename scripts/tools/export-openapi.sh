#!/usr/bin/env bash

set -euo pipefail

PROJECT_PATH="api/src/Presentation"
URL="http://localhost:8080/swagger/v1/swagger.json"
OUT_FILE="contracts/openapi.json"

RETRIES=60
DELAY_MS=1000

usage() {
  echo "Usage: $0 [-p <project_path>] [-u <url>] [-o <outfile>]"
  echo "  -p   Path to the .NET project (default: ${PROJECT_PATH})"
  echo "  -u   Swagger JSON URL (default: ${URL})"
  echo "  -o   Output file path (default: ${OUT_FILE})"
}

while getopts ":p:u:o:h" opt; do
  case "${opt}" in
    p) PROJECT_PATH="${OPTARG}" ;;
    u) URL="${OPTARG}" ;;
    o) OUT_FILE="${OPTARG}" ;;
    h) usage; exit 0 ;;
    \?) echo "Invalid option: -${OPTARG}" >&2; usage; exit 1 ;;
  esac
done

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required" >&2
  exit 1
fi

echo "Starting API via dotnet run..."
dotnet run --project "${PROJECT_PATH}" --no-build --urls http://localhost:8080 >/dev/null 2>&1 &
API_PID=$!

cleanup() {
  if kill -0 "${API_PID}" >/dev/null 2>&1; then
    kill "${API_PID}" >/dev/null 2>&1 || true
    sleep 0.2
    if kill -0 "${API_PID}" >/dev/null 2>&1; then
      kill -9 "${API_PID}" >/dev/null 2>&1 || true
    fi
  fi
}
trap cleanup EXIT

wait_http_200() {
  local u="${1}"
  local retries="${2}"
  local delay_ms="${3}"

  for ((i=0; i<retries; i++)); do
    status="$(curl -s -o /dev/null -w "%{http_code}" --max-time 2 "${u}" || echo "000")"
    if [[ "${status}" == "200" ]]; then
      return 0
    fi
    sleep "$(awk "BEGIN {print ${delay_ms}/1000}")"
  done
  echo "Timeout waiting for ${u}" >&2
  return 1
}

wait_http_200 "${URL}" "${RETRIES}" "${DELAY_MS}"

mkdir -p "$(dirname "${OUT_FILE}")"
curl -sS "${URL}" -o "${OUT_FILE}"

echo "OpenAPI exported to ${OUT_FILE}"
