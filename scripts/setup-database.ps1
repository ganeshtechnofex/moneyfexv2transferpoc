# Database Setup Script for Windows PowerShell
# This script helps set up the PostgreSQL database

param(
    [string]$Username = "postgres",
    [string]$Password = "",
    [string]$Host = "localhost",
    [int]$Port = 5432,
    [string]$Database = "moneyfex_db"
)

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MoneyFex Database Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if psql is available
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Host "✗ PostgreSQL (psql) not found in PATH" -ForegroundColor Red
    Write-Host "  Please ensure PostgreSQL is installed and psql is in your PATH" -ForegroundColor Yellow
    exit 1
}

Write-Host "Database Configuration:" -ForegroundColor Yellow
Write-Host "  Host: $Host" -ForegroundColor White
Write-Host "  Port: $Port" -ForegroundColor White
Write-Host "  Database: $Database" -ForegroundColor White
Write-Host "  Username: $Username" -ForegroundColor White
Write-Host ""

# Set PGPASSWORD if provided
if ($Password) {
    $env:PGPASSWORD = $Password
}

# Create database
Write-Host "Creating database '$Database'..." -ForegroundColor Yellow
$createDbQuery = "CREATE DATABASE $Database;"
if ($Password) {
    $env:PGPASSWORD = $Password
    $result = & psql -h $Host -p $Port -U $Username -c $createDbQuery 2>&1
} else {
    $result = & psql -h $Host -p $Port -U $Username -c $createDbQuery 2>&1
}

if ($LASTEXITCODE -eq 0 -or $result -match "already exists") {
    Write-Host "✓ Database '$Database' is ready" -ForegroundColor Green
} else {
    Write-Host "⚠ Database creation result: $result" -ForegroundColor Yellow
}

# Run schema script
Write-Host ""
Write-Host "Running schema script..." -ForegroundColor Yellow
$schemaPath = Join-Path $projectRoot "Database\Schema\01_CreateDatabase.sql"

if (Test-Path $schemaPath) {
    if ($Password) {
        $env:PGPASSWORD = $Password
        & psql -h $Host -p $Port -U $Username -d $Database -f $schemaPath
    } else {
        & psql -h $Host -p $Port -U $Username -d $Database -f $schemaPath
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Schema script executed successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ Schema script execution failed" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✗ Schema script not found at: $schemaPath" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Setup Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Connection String:" -ForegroundColor Yellow
Write-Host "Host=$Host;Port=$Port;Database=$Database;Username=$Username;Password=YOUR_PASSWORD" -ForegroundColor Gray
Write-Host ""
Write-Host "Update this in:" -ForegroundColor Yellow
Write-Host "  - MoneyFex.Web\appsettings.json" -ForegroundColor White
Write-Host ""
