#!/usr/bin/env bash
set -euo pipefail
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API_DIR="${DIR}/../api"
cd "$API_DIR"

dotnet build -c Debug

mapfile -t PROJECTS < <(find . -type f -name "*.csproj" \
  | grep -E "/[^/]*\.Tests\.csproj$" \
  | grep -v -E "/Infrastructure\.Tests.*\.csproj$")

if [ "${#PROJECTS[@]}" -eq 0 ]; then
  echo "No unit test projects found." >&2
  exit 1
fi

for p in "${PROJECTS[@]}"; do
  echo "==> dotnet test $p"
  dotnet test "$p" -c Debug --no-build "$@"
done
