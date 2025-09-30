#!/usr/bin/env bash
set -euo pipefail

orig_dir="$(pwd)"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
api_dir="$(cd "$script_dir/../../api" && pwd)"

cleanup() {
  cd "$orig_dir"
}
trap cleanup EXIT

cd "$api_dir"
dotnet build -c Debug

projects=$(find . -name '*.Tests.csproj' ! -name 'Infrastructure.Tests*.csproj')

if [ -z "$projects" ]; then
  echo "No unit test projects found." >&2
  exit 1
fi

for p in $projects; do
  echo "==> dotnet test $p"
  dotnet test "$p" -c Debug --no-build "$@"
done
