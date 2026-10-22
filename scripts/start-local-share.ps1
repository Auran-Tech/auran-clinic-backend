$ErrorActionPreference = 'Stop'
$backend = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$composeFiles = @('-f', 'docker-compose.yml', '-f', 'docker-compose.share.yml')

Push-Location $backend
try {
  docker compose @composeFiles up -d --build --no-deps frontend
  if ($LASTEXITCODE -ne 0) { throw 'Could not build the same-origin frontend.' }

  $ready = Invoke-WebRequest -Uri 'http://127.0.0.1:4173/health/ready' -SkipHttpErrorCheck
  if ([int]$ready.StatusCode -ne 200) { throw "Local readiness failed: HTTP $([int]$ready.StatusCode)" }

  Write-Host 'Starting public temporary Auran test link. Copy the trycloudflare.com URL from the logs below.'
  Write-Host 'Anyone with the URL can open the sign-in page. Share only test accounts and synthetic data.'
  Write-Host 'The URL stops working when this command or your laptop stops. Press Ctrl+C to stop sharing.'
  docker compose @composeFiles --profile share up --menu=false --no-color --no-deps share
  if ($LASTEXITCODE -ne 0) { throw 'The tunnel stopped unexpectedly.' }
}
finally {
  docker compose @composeFiles --profile share stop share | Out-Null
  Pop-Location
}
