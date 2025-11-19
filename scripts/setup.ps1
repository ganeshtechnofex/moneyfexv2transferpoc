# MoneyFex Modular - Setup Script for Windows PowerShell
# This script helps automate the setup process

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
cd $projectRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MoneyFex Modular - Setup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is installed
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "✗ .NET SDK not found. Please install .NET 9 SDK." -ForegroundColor Red
    exit 1
}

# Check if PostgreSQL is available
Write-Host ""
Write-Host "Checking PostgreSQL..." -ForegroundColor Yellow
$psqlVersion = psql --version 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ PostgreSQL found: $psqlVersion" -ForegroundColor Green
} else {
    Write-Host "⚠ PostgreSQL not found in PATH. Please ensure PostgreSQL is installed." -ForegroundColor Yellow
    Write-Host "  You can still continue, but you'll need to run the SQL script manually." -ForegroundColor Yellow
}

# Restore NuGet packages
Write-Host ""
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to restore packages" -ForegroundColor Red
    exit 1
}

# Build the solution
Write-Host ""
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Solution built successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}

# Database setup instructions
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Setup Required" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Please run the following commands to set up the database:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Create database:" -ForegroundColor White
Write-Host "   psql -U postgres -c 'CREATE DATABASE moneyfex_db;'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Run schema script:" -ForegroundColor White
Write-Host "   psql -U postgres -d moneyfex_db -f Database\Schema\01_CreateDatabase.sql" -ForegroundColor Gray
Write-Host ""
Write-Host "   Or use the database setup script:" -ForegroundColor White
Write-Host "   .\scripts\setup-database.ps1" -ForegroundColor Gray
Write-Host ""

# Connection string reminder
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Connection String Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Please update connection string in:" -ForegroundColor Yellow
Write-Host "  - MoneyFex.Web\appsettings.json" -ForegroundColor White
Write-Host ""
Write-Host "Format:" -ForegroundColor Yellow
Write-Host '  "ConnectionStrings": {' -ForegroundColor Gray
Write-Host '    "DefaultConnection": "Host=localhost;Port=5432;Database=moneyfex_db;Username=postgres;Password=YOUR_PASSWORD"' -ForegroundColor Gray
Write-Host '  }' -ForegroundColor Gray
Write-Host ""

# Ready to run
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Ready to Run!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run the Web application:" -ForegroundColor Yellow
Write-Host "  .\scripts\run-web.ps1" -ForegroundColor White
Write-Host "  Or: cd MoneyFex.Web && dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "Web application will be available at: https://localhost:5003" -ForegroundColor Green
Write-Host ""
