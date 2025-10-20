param(
  [string]$ProjectPath = "api/src/Presentation",
  [string]$Url = "",
  [string]$OutFile = "contracts/openapi.json",
  [int]$TimeoutMs = 90000
)

$ErrorActionPreference = "Stop"

if (-not $Url -or $Url -eq "") {
  if ($env:OPENAPI_URL) {
    $Url = $env:OPENAPI_URL
  } else {
    $Url = "http://localhost:8080/swagger/v1/swagger.json"
  }
}
if ($env:OPENAPI_TIMEOUT_MS) {
  [int]$TimeoutMs = [int]$env:OPENAPI_TIMEOUT_MS
}

function Wait-Http200([string]$u, [int]$timeoutMs, [int]$intervalMs = 1000) {
  $deadline = [DateTime]::UtcNow.AddMilliseconds($timeoutMs)
  while ([DateTime]::UtcNow -lt $deadline) {
    try {
      $r = Invoke-WebRequest $u -UseBasicParsing -TimeoutSec 2
      if ($r.StatusCode -eq 200) { return }
    } catch { }
    Start-Sleep -Milliseconds $intervalMs
  }
  throw "Timeout waiting for $u"
}

Write-Host "Starting API via dotnet run..."
$proc = Start-Process dotnet -ArgumentList "run --project `"$ProjectPath`" --no-build --configuration Release --no-launch-profile --urls http://localhost:8080" -WindowStyle Hidden -PassThru

try {
  Wait-Http200 -u $Url -timeoutMs $TimeoutMs
  $dir = Split-Path -Parent $OutFile
  if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
  Invoke-WebRequest $Url -OutFile $OutFile -UseBasicParsing
  Write-Host "OpenAPI exported to $OutFile"
}
finally {
  if ($proc -and -not $proc.HasExited) {
    Stop-Process -Id $proc.Id -Force
  }
}
