param([Parameter(ValueFromRemainingArguments = $true)] [string[]]$Args)

$ErrorActionPreference = "Stop"
$origDir = Get-Location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiDir = Join-Path $scriptDir "..\..\api"

try {
  Set-Location $apiDir

  dotnet build -c Debug

  $projects = Get-ChildItem -Recurse -Filter "Infrastructure.Tests*.csproj"

  if (-not $projects) { Write-Error "No infrastructure test projects found."; exit 1 }

  foreach ($p in $projects) {
    Write-Host "==> dotnet test $($p.FullName)"
    dotnet test $p.FullName -c Debug --no-build @Args
  }
}
finally {
  Set-Location $origDir
}
