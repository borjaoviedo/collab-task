#!/usr/bin/env bash
set -euo pipefail

orig_dir="$(pwd)"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
api_dir="$(cd "$script_dir/../../api" && pwd)"

cleanup() { cd "$orig_dir"; }
trap cleanup EXIT

cd "$api_dir"
dotnet build -c Debug

# Unit tests
unit_projects=$(find . -name '*.Tests.csproj' ! -name 'Infrastructure.Tests*.csproj' | sort || true)
if [ -z "$unit_projects" ]; then
  echo "No unit test projects found." >&2
else
  for p in $unit_projects; do
    echo "==> dotnet test (unit) $p"
    dotnet test "$p" -c Debug --no-build "$@"
  done
fi

# Infra tests
infra_projects=$(find . -name 'Infrastructure.Tests*.csproj' | sort || true)
if [ -z "$infra_projects" ]; then
  echo "No infrastructure test projects found." >&2
else
  for p in $infra_projects; do
    echo "==> dotnet test (infra) $p"
    dotnet test "$p" -c Debug --no-build "$@"
  done
fi
