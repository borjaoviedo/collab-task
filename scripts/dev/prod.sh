#!/usr/bin/env bash
set -euo pipefail

# Usage: ./prod.sh [up|down|rebuild|logs|health] [--project-name NAME] [--api-port PORT]
CMD="${1:-}"; shift || true

PROJECT_NAME="collabtask"
API_PORT="8080" # used only in demo mode
ENV_FILE=""            # defaults to repo-root/.env.prod

# Parse simple flags
while [[ $# -gt 0 ]]; do
  case "$1" in
    --project-name) PROJECT_NAME="${2:-collabtask}"; shift 2;;
    --api-port) API_PORT="${2:-8080}"; shift 2;;
    --env-file)     ENV_FILE="${2:-}"; shift 2;;
    *) echo "Unknown arg: $1" >&2; exit 2;;
  esac
done

# Resolve Compose files (base + prod overlay)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
INFRA="$(cd "${SCRIPT_DIR}/../../infra" && pwd)"

# Default env-file to repo root
if [[ -z "${ENV_FILE}" ]]; then
  ENV_FILE="${REPO_ROOT}/.env.prod"
fi
if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Env file not found: ${ENV_FILE}" >&2
  exit 1
fi

FILES=(-f "${INFRA}/compose.yaml" -f "${INFRA}/compose.prod.yaml")
PROFILES=(--profile prod)
PROJECT=(--project-name "${PROJECT_NAME}")
ENVARGS=(--env-file "${ENV_FILE}")

cleanup_compose_leftovers() {
  # Remove containers, networks, and volumes left by a given compose project
  local label="com.docker.compose.project=${PROJECT_NAME}"
  mapfile -t cids < <(docker ps -aq --filter "label=${label}" || true)
  if [[ ${#cids[@]} -gt 0 ]]; then docker rm -f "${cids[@]}" >/dev/null 2>&1 || true; fi
  mapfile -t nids < <(docker network ls -q --filter "label=${label}" || true)
  if [[ ${#nids[@]} -gt 0 ]]; then docker network rm "${nids[@]}" >/dev/null 2>&1 || true; fi
  mapfile -t vids < <(docker volume ls -q --filter "label=${label}" || true)
  if [[ ${#vids[@]} -gt 0 ]]; then docker volume rm "${vids[@]}" >/dev/null 2>&1 || true; fi
}

wait_health() {
  # Prefer Docker health status to avoid requiring a public port
  local retries=60 delay=2 svc="api"
  for ((i=1; i<=retries; i++)); do
    cid="$(docker compose --project-directory "${INFRA}" "${FILES[@]}" "${PROJECT[@]}" "${ENVARGS[@]}" ps -q "${svc}" || true)"
    if [[ -n "${cid}" ]]; then
      status="$(docker inspect --format='{{.State.Health.Status}}' "${cid}" 2>/dev/null || true)"
      if [[ "${status}" == "healthy" ]]; then
        return 0
      fi
    fi
    sleep "${delay}"
  end
  # Fallback to HTTP if the API port is exposed (demo mode)
  if out="$(curl -fsS "http://localhost:${API_PORT}/health" 2>/dev/null || true)"; then
    if [[ "$out" == *'"status"'*":"*"ok"* ]]; then
      return 0
    fi
  fi
  echo "API did not become healthy" >&2
  return 1
}

case "${CMD}" in
  up)
    docker compose --project-directory "${INFRA}" "${FILES[@]}" "${PROFILES[@]}" "${PROJECT[@]}" "${ENVARGS[@]}" up -d
    wait_health
    ;;
  down)
    docker compose --project-directory "${INFRA}" "${FILES[@]}" "${PROJECT[@]}" "${ENVARGS[@]}" down -v --remove-orphans
    cleanup_compose_leftovers
    ;;
  rebuild)
    docker compose --project-directory "${INFRA}" "${FILES[@]}" "${PROJECT[@]}" "${ENVARGS[@]}" build --no-cache api
    ;;
  logs)
    docker compose --project-directory "${INFRA}" "${FILES[@]}" "${PROJECT[@]}" "${ENVARGS[@]}" logs -f api
    ;;
  health)
    if wait_health; then echo '{ "status": "ok" }'; fi
    ;;
  *)
    echo "Usage: $0 {up|down|rebuild|logs|health} [--project-name NAME] [--api-port PORT]" >&2
    exit 1
    ;;
esac
