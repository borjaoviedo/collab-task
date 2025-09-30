param(
  [Parameter(Mandatory = $true)]
  [ValidateSet('up','down','rebuild','logs','health')]
  [string]$cmd,
  [string]$ProjectName = 'collabtask',
  [int]$ApiPort = 8080  # used only in demo mode
)

# Resolve Compose files (base + prod overlay)
$Infra = (Resolve-Path (Join-Path $PSScriptRoot '..\..\infra')).Path
$files = @(
  '--project-directory', $Infra,
  '-f', (Join-Path $Infra 'compose.yaml'),
  '-f', (Join-Path $Infra 'compose.prod.yaml')
)
$profiles = @('--profile','prod')
$project  = @('--project-name', $ProjectName)

function Cleanup-ComposeLeftovers {
  param([string]$Name)
  # Remove containers, networks, and volumes left by a given compose project
  $label = "com.docker.compose.project=$Name"
  $c = docker ps -aq --filter "label=$label"; if ($c) { docker rm -f $c | Out-Null }
  $n = docker network ls -q --filter "label=$label"; if ($n) { docker network rm $n | Out-Null }
  $v = docker volume ls -q --filter "label=$label"; if ($v) { docker volume rm $v | Out-Null }
}

function Wait-Health {
  param([int]$Retries = 60, [int]$DelaySec = 2)
  # Prefer Docker health status to avoid requiring a public port
  $svc = "api"
  for ($i = 1; $i -le $Retries; $i++) {
    $cid = docker compose $files $project ps -q $svc
    if (-not $cid) { Start-Sleep -Seconds $DelaySec; continue }
    $status = docker inspect --format='{{json .State.Health.Status}}' $cid 2>$null
    if ($status -and $status -match 'healthy') { return $true }
    Start-Sleep -Seconds $DelaySec
  }
  # Fallback to HTTP if the API port is exposed (demo mode)
  try {
    $res = Invoke-RestMethod -Uri ("http://localhost:{0}/health" -f $ApiPort) -TimeoutSec 2
    if ($res.status -eq 'ok') { return $true }
  } catch { }
  throw "API did not become healthy"
}

switch ($cmd) {
  'up' {
    # Start stack in background and wait for readiness
    docker compose $files $profiles $project up -d
    Wait-Health | Out-Null
  }
  'down' {
    # Stop and remove all resources from the project
    docker compose $files $project down -v --remove-orphans
    Cleanup-ComposeLeftovers -Name $ProjectName
  }
  'rebuild' {
    # Force a clean rebuild of the API image
    docker compose $files $project build --no-cache api
  }
  'logs' {
    # Stream API logs
    docker compose $files $project logs -f api
  }
  'health' {
    # Report current health via Docker or HTTP fallback
    Wait-Health | Out-Null
    '{ "status": "ok" }'
  }
}
