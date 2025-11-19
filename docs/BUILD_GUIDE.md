# MoneyFex Modular - Build Guide

## ⚠️ Important: Always Stop Projects Before Building

**File lock errors occur when you try to build while projects are running.** The running processes lock the DLL files, preventing the build from copying new versions.

## Quick Fix for File Lock Errors

### Step 1: Stop Running Projects

```powershell
# Use the stop script (recommended)
.\scripts\stop-projects.ps1

# Or manually stop by process ID
Stop-Process -Id <PID> -Force
```

### Step 2: Wait a Few Seconds

Give the system time to release file locks:
```powershell
Start-Sleep -Seconds 3
```

### Step 3: Build

```powershell
dotnet build
```

### Step 4: Restart Projects

```powershell
# Terminal 1 - API
.\scripts\run-api.ps1

# Terminal 2 - Web
.\scripts\run-web.ps1
```

## Complete Workflow

### When You Need to Rebuild

```powershell
# 1. Stop all projects
.\scripts\stop-projects.ps1

# 2. Clean (optional, for fresh build)
dotnet clean

# 3. Restore packages (if needed)
dotnet restore

# 4. Build
dotnet build

# 5. Restart projects
.\scripts\run-api.ps1    # Terminal 1
.\scripts\run-web.ps1    # Terminal 2
```

## Common Build Scenarios

### Scenario 1: Making Code Changes

1. **Stop projects**: `.\scripts\stop-projects.ps1`
2. **Make your code changes**
3. **Build**: `dotnet build`
4. **Restart**: `.\scripts\run-api.ps1` and `.\scripts\run-web.ps1`

### Scenario 2: Adding New Packages

1. **Stop projects**: `.\scripts\stop-projects.ps1`
2. **Add package**: `dotnet add package <PackageName>`
3. **Restore**: `dotnet restore`
4. **Build**: `dotnet build`
5. **Restart projects**

### Scenario 3: Creating Migrations

1. **Stop projects**: `.\scripts\stop-projects.ps1`
2. **Create migration**: 
   ```powershell
   dotnet ef migrations add MigrationName --project MoneyFex.Infrastructure --startup-project MoneyFex.API
   ```
3. **Build**: `dotnet build`
4. **Restart projects** (migrations apply automatically)

### Scenario 4: Clean Build

1. **Stop projects**: `.\scripts\stop-projects.ps1`
2. **Clean**: `dotnet clean`
3. **Restore**: `dotnet restore`
4. **Build**: `dotnet build`
5. **Restart projects**

## Finding Running Processes

### Check What's Running

```powershell
# Find MoneyFex processes
Get-Process | Where-Object { 
    $_.ProcessName -like "*MoneyFex*" -or 
    $_.ProcessName -eq "dotnet" 
} | Select-Object Id, ProcessName, Path
```

### Find Process by Port

```powershell
# Find what's using port 5001 (API)
netstat -ano | findstr ":5001"

# Find what's using port 5003 (Web)
netstat -ano | findstr ":5003"
```

## Prevention Tips

1. **Always stop before building**: Use `.\scripts\stop-projects.ps1`
2. **Use separate terminals**: Keep API and Web in different terminals
3. **Check before building**: Verify no processes are running
4. **Use the scripts**: They handle path resolution correctly

## Troubleshooting

### If Stop Script Doesn't Work

1. **Manual stop by process ID:**
   ```powershell
   # Find process ID
   Get-Process | Where-Object { $_.ProcessName -like "*MoneyFex*" }
   
   # Stop by ID
   Stop-Process -Id <PID> -Force
   ```

2. **Kill all dotnet processes** (use with caution):
   ```powershell
   Get-Process dotnet | Stop-Process -Force
   ```

3. **Use Task Manager:**
   - Open Task Manager (Ctrl+Shift+Esc)
   - Find "MoneyFex.API" or "MoneyFex.Web"
   - End Task

### If Files Still Locked

1. **Wait longer**: Sometimes it takes a few seconds
2. **Close IDE**: Visual Studio/VS Code might have files locked
3. **Restart computer**: Last resort if nothing else works

## Best Practices

1. ✅ **Stop projects before building**
2. ✅ **Use the provided scripts**
3. ✅ **Keep projects in separate terminals**
4. ✅ **Wait a few seconds after stopping**
5. ✅ **Check for running processes before building**

## Quick Reference

| Action | Command |
|--------|---------|
| Stop projects | `.\scripts\stop-projects.ps1` |
| Build | `dotnet build` |
| Clean build | `dotnet clean && dotnet build` |
| Run API | `.\scripts\run-api.ps1` |
| Run Web | `.\scripts\run-web.ps1` |
| Check processes | `Get-Process | Where-Object { $_.ProcessName -like "*MoneyFex*" }` |

---

**Remember: Always stop projects before building to avoid file lock errors!**

