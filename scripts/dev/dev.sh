#!/usr/bin/env bash
set -euo pipefail

# Usage: ./dev.sh [up|down|rebuild|logs|health] [--project-name NAME] [--api-port PORT]
CMD="${1:-}"; shift || true

PROJECT_NAME="collabtask"
API_PORT="8080"

# Parse simple flags
while [[ $# -gt 0 ]]; do
  case "$1" in
    --project-name) PROJECT_NAME="${2:-collabtask}"; shift 2;;
    --api-port) API_PORT="${2:-8080}"; shift 2;;
    *) echo "Unknown arg: $1" >&2; exit 2;;
  esac
done

# Resolve Compose files (base + dev overlay)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA="$(cd "${SCRIPT_DIR}/../../infra" && pwd)"

FILES=(-f "${INFRA}/compose.yaml" -f "${INFRA}/compose.dev.yaml")
PROFILES=(--profile dev)
PROJECT=(--project-name "${PROJECT_NAME}")

cleanup_compose_leftovers() {
  # Remove containers, networks, and volumes left by a given compose project
  local label="com.docker.compose.project=${PROJECT_NAME}"
  # Containers
  mapfile -t cids < <(docker ps -aq --filter "label=${label}" || true)
  if [[ ${#cids[@]} -gt 0 ]]; then docker rm -f "${cids[@]}" >/dev/null 2>&1 || true; fi
  # Networks
  mapfile -t nids < <(docker network ls -q --filter "label=${label}" || true)
  if [[ ${#nids[@]} -gt 0 ]]; then docker network rm "${nids[@]}" >/dev/null 2>&1 || true; fi
  # Volumes
  mapfile -t vids < <(docker volume ls -q --filter "label=${label}" || true)
  if [[ ${#vids[@]} -gt 0 ]]; then docker volume rm "${vids[@]}" >/dev/null 2>&1 || true; fi
}

wait_health_http() {
  # Poll the app health endpoint until it reports status ok or timeout
  local port="${1:-8080}"
  local retries=60 delay=2
  for ((i=1; i<=retries; i++)); do
    if out="$(curl -fsS "http://localhost:${port}/health" 2>/dev/null || true)"; then
      if [[ "$out" == *'"status"'*":"*"ok"* ]]; then
        return 0
      fi
    fi
    sleep "${delay}"
  done
  echo "API did not become healthy on http://localhost:${port}/health" >&2
  return 1
}

case "${CMD}" in
  up)
    docker compose --project-directory "${INFRA}" "${FILES[@]}" "${PROFILES[@]}" "${PROJECT[@]}" up -d
    wait_health_http "${API_PORT}"
    ;;
  down)
    docker compose --project-directory "${INFRA}" "${FILES[@]}" "${PROJECT[@]}" down -v --remove-orphans
    cleanup_compose_leftovers
    ;;
  rebuild)
    docker compose --project-directory "${INFRA}" "${FILES[@]}" "${PROJECT[@]}" build --no-cache api
    ;;
  logs)
    docker compose --project-directory "${INFRA}" "${FILES[@]}" "${PROJECT[@]}" logs -f api
    ;;
  health)
    curl -fsS "http://localhost:${API_PORT}/health" | jq -c '.' 2>/dev/null || curl -fsS "http://localhost:${API_PORT}/health"
    ;;
  *)
    echo "Usage: $0 {up|down|rebuild|logs|health} [--project-name NAME] [--api-port PORT]" >&2
    exit 1
    ;;
esac
