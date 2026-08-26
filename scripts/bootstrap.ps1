param(
    [string]$RootPath = (Join-Path (Get-Location) "Auran.Clinic")
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK was not found. Install .NET 8 SDK first."
}

if (Test-Path $RootPath) {
    throw "Target path already exists: $RootPath"
}

New-Item -ItemType Directory -Path $RootPath | Out-Null
Set-Location $RootPath

dotnet new sln -n Auran.Clinic

dotnet new webapi -n Auran.Clinic.Api -o src/Auran.Clinic.Api -f net8.0 --use-controllers
dotnet new classlib -n Auran.Clinic.Application -o src/Auran.Clinic.Application -f net8.0
dotnet new classlib -n Auran.Clinic.Domain -o src/Auran.Clinic.Domain -f net8.0
dotnet new classlib -n Auran.Clinic.Infrastructure -o src/Auran.Clinic.Infrastructure -f net8.0
dotnet new xunit -n Auran.Clinic.UnitTests -o tests/Auran.Clinic.UnitTests -f net8.0
dotnet new xunit -n Auran.Clinic.IntegrationTests -o tests/Auran.Clinic.IntegrationTests -f net8.0

dotnet sln Auran.Clinic.sln add src/Auran.Clinic.Api/Auran.Clinic.Api.csproj
dotnet sln Auran.Clinic.sln add src/Auran.Clinic.Application/Auran.Clinic.Application.csproj
dotnet sln Auran.Clinic.sln add src/Auran.Clinic.Domain/Auran.Clinic.Domain.csproj
dotnet sln Auran.Clinic.sln add src/Auran.Clinic.Infrastructure/Auran.Clinic.Infrastructure.csproj
dotnet sln Auran.Clinic.sln add tests/Auran.Clinic.UnitTests/Auran.Clinic.UnitTests.csproj
dotnet sln Auran.Clinic.sln add tests/Auran.Clinic.IntegrationTests/Auran.Clinic.IntegrationTests.csproj

dotnet add src/Auran.Clinic.Application/Auran.Clinic.Application.csproj reference src/Auran.Clinic.Domain/Auran.Clinic.Domain.csproj
dotnet add src/Auran.Clinic.Infrastructure/Auran.Clinic.Infrastructure.csproj reference src/Auran.Clinic.Application/Auran.Clinic.Application.csproj src/Auran.Clinic.Domain/Auran.Clinic.Domain.csproj
dotnet add src/Auran.Clinic.Api/Auran.Clinic.Api.csproj reference src/Auran.Clinic.Application/Auran.Clinic.Application.csproj src/Auran.Clinic.Infrastructure/Auran.Clinic.Infrastructure.csproj

dotnet add tests/Auran.Clinic.UnitTests/Auran.Clinic.UnitTests.csproj reference src/Auran.Clinic.Application/Auran.Clinic.Application.csproj src/Auran.Clinic.Domain/Auran.Clinic.Domain.csproj
dotnet add tests/Auran.Clinic.IntegrationTests/Auran.Clinic.IntegrationTests.csproj reference src/Auran.Clinic.Api/Auran.Clinic.Api.csproj

Write-Host "Auran Clinic solution scaffold created successfully." -ForegroundColor Green
Write-Host "Use the repository files as the source of truth for package versions and project configuration." -ForegroundColor DarkGray
