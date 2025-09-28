param([Parameter(ValueFromRemainingArguments = $true)] [string[]]$Args)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiDir = Join-Path $scriptDir "..\api"
Set-Location $apiDir

dotnet build -c Debug

$projects = Get-ChildItem -Recurse -Filter "Infrastructure.Tests*.csproj"

if (-not $projects) { Write-Error "No infrastructure test projects found."; exit 1 }

foreach ($p in $projects) {
  Write-Host "==> dotnet test $($p.FullName)"
  dotnet test $p.FullName -c Debug --no-build @Args
}
