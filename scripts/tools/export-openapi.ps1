param(
  [string]$ProjectPath = "api/src/Presentation",
  [string]$Url = "http://localhost:8080/swagger/v1/swagger.json",
  [string]$OutFile = "contracts/openapi.json"
)

$ErrorActionPreference = "Stop"

function Wait-Http200([string]$u, [int]$retries = 60, [int]$delay = 1000) {
  for ($i=0; $i -lt $retries; $i++) {
    try {
      $r = Invoke-WebRequest $u -UseBasicParsing -TimeoutSec 2
      if ($r.StatusCode -eq 200) { return }
    } catch { }
    Start-Sleep -Milliseconds $delay
  }
  throw "Timeout waiting for $u"
}

Write-Host "Starting API via dotnet run..."
  $proc = Start-Process dotnet -ArgumentList "run --project `"$ProjectPath`" --no-build --urls http://localhost:8080" -WindowStyle Hidden -PassThru
  try {
    Wait-Http200 $Url
    Invoke-WebRequest $Url -OutFile $OutFile -UseBasicParsing
  } finally {
    if ($proc -and !$proc.HasExited) { Stop-Process -Id $proc.Id -Force }
  }

  Write-Host "OpenAPI exported to $OutFile"