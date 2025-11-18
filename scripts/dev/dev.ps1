param(
  [Parameter(Mandatory = $true)]
  [ValidateSet('up','down','rebuild','logs','health')]
  [string]$cmd,
  [string]$ProjectName = 'collabtask',
  [int]$ApiPort = 8080
)

# Resolve Compose files (base + dev overlay)
$Infra = (Resolve-Path (Join-Path $PSScriptRoot '..\..\infra')).Path
$files = @(
  '--project-directory', $Infra,
  '-f', (Join-Path $Infra 'compose.yaml'),
  '-f', (Join-Path $Infra 'compose.dev.yaml')
)
$profiles = @('--profile','dev')
$project  = @('--project-name', $ProjectName)

# Load env file from repo root
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$EnvFile = (Join-Path $RepoRoot '.env.dev')
$envargs = @('--env-file', $EnvFile)

function Cleanup-ComposeLeftovers {
  param([string]$Name)
  # Remove containers, networks, and volumes left by a given compose project
  $label = "com.docker.compose.project=$Name"
  $c = docker ps -aq --filter "label=$label"; if ($c) { docker rm -f $c | Out-Null }
  $n = docker network ls -q --filter "label=$label"; if ($n) { docker network rm $n | Out-Null }
  $v = docker volume ls -q --filter "label=$label"; if ($v) { docker volume rm $v | Out-Null }
}

function Wait-HealthHttp {
  param([int]$Port = 8080, [int]$Retries = 60, [int]$DelaySec = 2)
  # Poll the app health endpoint until it reports status 'Healthy' or timeout
  for ($i = 1; $i -le $Retries; $i++) {
    try {
      $res = Invoke-RestMethod -Uri ("http://localhost:{0}/health" -f $Port) -TimeoutSec 2
      if ($res.status -eq 'Healthy') { return $true }
    } catch { }
    Start-Sleep -Seconds $DelaySec
  }
  throw "API did not become healthy on http://localhost:$Port/health"
}

switch ($cmd) {
  'up' {
    # Start stack in background and wait for API readiness
    docker compose $files $profiles $project $envargs up -d
    Wait-HealthHttp -Port $ApiPort | Out-Null
  }
  'down' {
    # Stop and remove all resources from the project
    docker compose $files $project $envargs down -v --remove-orphans
    Cleanup-ComposeLeftovers -Name $ProjectName
  }
  'rebuild' {
    # Force a clean rebuild of the API image
    docker compose $files $project $envargs build --no-cache api
  }
  'logs' {
    # Stream API logs
    docker compose $files $project $envargs logs -f api
  }
  'health' {
    # Print current health status from the API endpoint
    Invoke-RestMethod -Uri ("http://localhost:{0}/health" -f $ApiPort) | ConvertTo-Json -Compress
  }
}
