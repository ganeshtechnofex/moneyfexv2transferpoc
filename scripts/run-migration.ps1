# Data Migration Script for Windows PowerShell
# Migrates data from legacy SQL Server to new PostgreSQL database

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MoneyFex Data Migration Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET is installed
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET SDK is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Navigate to migration tool directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
$migrationToolPath = Join-Path $projectRoot "MoneyFex.Infrastructure\MigrationTool"

if (-not (Test-Path $migrationToolPath)) {
    Write-Host "ERROR: Migration tool directory not found: $migrationToolPath" -ForegroundColor Red
    exit 1
}

Set-Location $migrationToolPath

# Check if appsettings.json exists
if (-not (Test-Path "appsettings.json")) {
    Write-Host "ERROR: appsettings.json not found in migration tool directory" -ForegroundColor Red
    Write-Host "Please create appsettings.json with connection strings" -ForegroundColor Yellow
    exit 1
}

# Parse command line arguments
$mode = "full"
if ($args.Count -gt 0) {
    $mode = $args[0]
}

Write-Host "Migration Mode: $mode" -ForegroundColor Yellow
Write-Host ""

# Create logs directory
$logsDir = Join-Path $projectRoot "logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir | Out-Null
    Write-Host "✓ Created logs directory" -ForegroundColor Green
}

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore packages" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Packages restored" -ForegroundColor Green
Write-Host ""

# Build project
Write-Host "Building migration tool..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build successful" -ForegroundColor Green
Write-Host ""

# Run migration
Write-Host "Starting migration..." -ForegroundColor Yellow
Write-Host ""

switch ($mode) {
    "full" {
        dotnet run --configuration Release -- --mode full
    }
    "validate" {
        dotnet run --configuration Release -- --mode validate
    }
    "incremental" {
        $batchSize = if ($args.Count -gt 1) { $args[1] } else { "1000" }
        dotnet run --configuration Release -- --mode incremental --batch-size $batchSize
    }
    default {
        Write-Host "Usage: .\run-migration.ps1 [full|validate|incremental] [batch-size]" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Modes:" -ForegroundColor Cyan
        Write-Host "  full        - Migrate all data (default)" -ForegroundColor White
        Write-Host "  validate    - Validate data without migrating" -ForegroundColor White
        Write-Host "  incremental - Migrate data in batches" -ForegroundColor White
        exit 1
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Migration Completed Successfully" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Check logs/migration.log for details" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Migration Failed" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Check logs/migration.log for error details" -ForegroundColor Yellow
    exit 1
}

