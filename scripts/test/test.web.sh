#!/usr/bin/env bash
set -euo pipefail

orig_dir="$(pwd)"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
web_dir="$(cd "$script_dir/../../web" && pwd)"

cleanup() { cd "$orig_dir"; }
trap cleanup EXIT

cd "$web_dir"

if [ -f "pnpm-lock.yaml" ]; then
  corepack enable >/dev/null 2>&1 || true
  pnpm install --frozen-lockfile
  pnpm test:coverage
else
  npm ci
  npm run test:coverage
fi
