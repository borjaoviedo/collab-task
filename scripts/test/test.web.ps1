#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'

$orig = Get-Location
try {
  $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
  $webDir = Resolve-Path (Join-Path $scriptDir '..\..\web')
  Set-Location $webDir

  if (Test-Path 'pnpm-lock.yaml') {
    corepack enable | Out-Null
    corepack pnpm install --frozen-lockfile
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    corepack pnpm run test:coverage
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  } else {
    npm ci
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm run test:coverage
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  }
}
finally {
  Set-Location $orig
}
