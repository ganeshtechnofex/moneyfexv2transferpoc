# Stop Projects Script for Windows PowerShell
Write-Host "Stopping MoneyFex projects..." -ForegroundColor Yellow
Write-Host ""

# Find and stop MoneyFex.Web processes
$webProcesses = Get-Process -Name "MoneyFex.Web" -ErrorAction SilentlyContinue
if ($webProcesses) {
    Write-Host "Stopping MoneyFex.Web processes..." -ForegroundColor Yellow
    $webProcesses | Stop-Process -Force
    Write-Host "MoneyFex.Web stopped" -ForegroundColor Green
} else {
    Write-Host "No MoneyFex.Web processes found" -ForegroundColor Gray
}

# Also check for dotnet processes that might be running the projects
Write-Host ""
Write-Host "Checking for related dotnet processes..." -ForegroundColor Yellow
$allDotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
$relatedProcesses = @()

foreach ($proc in $allDotnetProcesses) {
    try {
        $cmdLine = (Get-WmiObject Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
        if ($cmdLine -like "*MoneyFex.Web*") {
            $relatedProcesses += $proc
        }
    } catch {
        # Ignore errors
    }
}

if ($relatedProcesses.Count -gt 0) {
    Write-Host "Found related dotnet processes, stopping..." -ForegroundColor Yellow
    $relatedProcesses | Stop-Process -Force
    Write-Host "Related dotnet processes stopped" -ForegroundColor Green
} else {
    Write-Host "No related dotnet processes found" -ForegroundColor Gray
}

# Wait for file locks to release
Write-Host ""
Write-Host "Waiting for file locks to release..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Processes Stopped" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now:" -ForegroundColor Yellow
Write-Host "  1. Rebuild the solution: dotnet build" -ForegroundColor White
Write-Host "  2. Restart the Web project:" -ForegroundColor White
Write-Host "     .\scripts\run-web.ps1" -ForegroundColor Gray
Write-Host "     Or: cd MoneyFex.Web && dotnet run" -ForegroundColor Gray
Write-Host ""
