param([Parameter(ValueFromRemainingArguments = $true)] [string[]]$Args)

$ErrorActionPreference = "Stop"
$origDir = Get-Location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiDir = Join-Path $scriptDir "..\..\api"

try {
  Set-Location $apiDir

  dotnet build -c Debug

  # Unit tests: *.Tests.csproj except Infrastructure.Tests*.csproj
  $unit = Get-ChildItem -Recurse -Filter *.csproj |
    Where-Object { $_.Name -like "*.Tests.csproj" -and $_.Name -notlike "Infrastructure.Tests*.csproj" }

  if (-not $unit) { Write-Warning "No unit test projects found." }
  else {
    foreach ($p in $unit) {
      Write-Host "==> dotnet test (unit) $($p.FullName)"
      dotnet test $p.FullName -c Debug --no-build @Args
    }
  }

  # Infra tests: Infrastructure.Tests*.csproj
  $infra = Get-ChildItem -Recurse -Filter "Infrastructure.Tests*.csproj"

  if (-not $infra) { Write-Warning "No infrastructure test projects found." }
  else {
    foreach ($p in $infra) {
      Write-Host "==> dotnet test (infra) $($p.FullName)"
      dotnet test $p.FullName -c Debug --no-build @Args
    }
  }
}
finally {
  Set-Location $origDir
}
