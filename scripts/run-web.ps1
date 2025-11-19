# Run Web Script for Windows PowerShell
Write-Host "Starting MoneyFex Web Application..." -ForegroundColor Cyan
Write-Host ""
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
cd "$projectRoot\MoneyFex.Web"
dotnet run
