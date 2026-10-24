param(
    [switch] $ConfirmLocal
)

$ErrorActionPreference = 'Stop'

if (-not $ConfirmLocal -or
    [string]::IsNullOrWhiteSpace($env:AURAN_DEMO_EMAIL) -or
    [string]::IsNullOrWhiteSpace($env:AURAN_DEMO_PASSWORD)) {
    throw 'Use -ConfirmLocal with AURAN_DEMO_EMAIL and AURAN_DEMO_PASSWORD for the disposable local clinic.'
}

if ($env:AURAN_DEMO_EMAIL -notmatch '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+$') {
    throw 'The demo administrator email is invalid.'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$settings = @{}
foreach ($line in Get-Content (Join-Path $repositoryRoot '.env')) {
    if ($line -match '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') {
        $settings[$Matches[1]] = $Matches[2]
    }
}

if ([string]::IsNullOrWhiteSpace($settings['MSSQL_SA_PASSWORD'])) {
    throw 'The local Docker SQL password is missing from backend .env.'
}

$loginBody = @{
    email = $env:AURAN_DEMO_EMAIL
    password = $env:AURAN_DEMO_PASSWORD
} | ConvertTo-Json

$login = Invoke-RestMethod `
    -Method Post `
    -Uri 'http://127.0.0.1:5085/api/auth/login' `
    -ContentType 'application/json' `
    -Body $loginBody

$dashboard = Invoke-RestMethod `
    -Uri 'http://127.0.0.1:5085/api/reporting/dashboard' `
    -Headers @{ Authorization = 'Bearer ' + $login.data.accessToken }

$localDate = [string] $dashboard.data.localDate
if ($localDate -notmatch '^\d{4}-\d{2}-\d{2}$') {
    throw 'The dashboard API did not return a valid clinic-local date.'
}

$query = Get-Content `
    -Raw `
    -LiteralPath (Join-Path $PSScriptRoot 'seed-dashboard-history.sql')

Push-Location $repositoryRoot
try {
    $query | docker compose `
        -f docker-compose.yml `
        -f docker-compose.share.yml `
        exec -T sqlserver `
        /opt/mssql-tools18/bin/sqlcmd `
        -b `
        -S localhost `
        -U sa `
        -P $settings['MSSQL_SA_PASSWORD'] `
        -C `
        -d AuranClinic `
        -v "ClinicEmail=$($env:AURAN_DEMO_EMAIL)" "LocalDate=$localDate"

    if ($LASTEXITCODE -ne 0) {
        throw 'The local dashboard history seed failed.'
    }
}
finally {
    Pop-Location
}
