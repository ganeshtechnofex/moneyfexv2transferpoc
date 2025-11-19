# MoneyFex Modular - Troubleshooting Guide

## Common Issues and Solutions

### File Lock Errors During Build

**Error:**
```
Could not copy "...MoneyFex.Infrastructure.dll" to "bin\Debug\net9.0\MoneyFex.Infrastructure.dll". 
The file is locked by: "MoneyFex.API (28064)"
```

**Cause:** Projects are running and have locked the DLL files, preventing rebuild.

**Solution:**

1. **Stop Running Projects**
   ```powershell
   # Use the stop script
   .\scripts\stop-projects.ps1
   
   # Or manually stop processes
   Stop-Process -Id 28064 -Force  # API process
   Stop-Process -Id 27892 -Force  # Web process
   ```

2. **Wait a few seconds** for files to unlock

3. **Rebuild**
   ```powershell
   dotnet build
   ```

4. **Restart Projects**
   ```powershell
   # Terminal 1
   .\scripts\run-api.ps1
   
   # Terminal 2
   .\scripts\run-web.ps1
   ```

### Database Connection Errors

**Error:**
```
An error occurred while migrating the database.
Connection refused
```

**Solutions:**

1. **Verify PostgreSQL is running**
   ```powershell
   psql -U postgres -c "SELECT version();"
   ```

2. **Check connection string** in `appsettings.json`
   - Verify password is correct
   - Verify database exists
   - Verify host and port

3. **Create database if missing**
   ```powershell
   psql -U postgres -c "CREATE DATABASE moneyfex_db;"
   ```

4. **Run schema script**
   ```powershell
   .\scripts\setup-database.ps1
   ```

### Port Already in Use

**Error:**
```
Failed to bind to address https://localhost:5001: address already in use
```

**Solutions:**

1. **Find process using the port**
   ```powershell
   netstat -ano | findstr ":5001"
   ```

2. **Stop the process**
   ```powershell
   Stop-Process -Id <PID> -Force
   ```

3. **Or change port** in `launchSettings.json`

### Migration Errors

**Error:**
```
An error occurred while migrating the database.
```

**Solutions:**

1. **Check database exists**
   ```sql
   \l  -- List databases in psql
   ```

2. **Check connection string** is correct

3. **Verify schema script ran** successfully

4. **Check PostgreSQL logs** for detailed errors

5. **Manual migration**
   ```powershell
   dotnet ef database update --project MoneyFex.Infrastructure --startup-project MoneyFex.API
   ```

### Build Errors

**Error:**
```
Build failed
```

**Solutions:**

1. **Clean and rebuild**
   ```powershell
   dotnet clean
   dotnet restore
   dotnet build
   ```

2. **Check for missing packages**
   ```powershell
   dotnet restore
   ```

3. **Check .NET SDK version**
   ```powershell
   dotnet --version  # Should be 9.x.x
   ```

### Swagger UI Not Loading

**Solutions:**

1. **Check API is running**
   - Look for "Now listening on: https://localhost:5001" in console

2. **Try HTTP instead of HTTPS**
   - `http://localhost:5000/swagger`

3. **Check browser console** (F12) for errors

4. **Verify Swagger is enabled** in `Program.cs`

### Web App Not Loading

**Solutions:**

1. **Check Web is running**
   - Look for "Now listening on: https://localhost:5003" in console

2. **Try HTTP instead of HTTPS**
   - `http://localhost:5002`

3. **Check browser console** (F12) for errors

4. **Verify database connection** is working

### Enum Conversion Errors

**Error:**
```
Cannot convert enum to integer
```

**Solution:** The DbContext has been updated to handle enum conversions. If you see this error:
1. Rebuild the solution
2. Remove and recreate migrations if needed

### Process Management

**Stop All Projects:**
```powershell
.\scripts\stop-projects.ps1
```

**Find Running Processes:**
```powershell
Get-Process | Where-Object { $_.ProcessName -like "*MoneyFex*" -or $_.ProcessName -eq "dotnet" }
```

**Kill Specific Process:**
```powershell
Stop-Process -Id <PID> -Force
```

## Quick Fixes

### Complete Reset

If nothing works, try a complete reset:

```powershell
# 1. Stop all processes
.\scripts\stop-projects.ps1

# 2. Clean solution
dotnet clean

# 3. Restore packages
dotnet restore

# 4. Rebuild
dotnet build

# 5. Restart projects
.\scripts\run-api.ps1
.\scripts\run-web.ps1
```

### Database Reset

If database has issues:

```powershell
# 1. Drop database
psql -U postgres -c "DROP DATABASE moneyfex_db;"

# 2. Recreate
.\scripts\setup-database.ps1

# 3. Restart projects (migrations will run automatically)
```

## Getting Help

1. **Check Logs**: Look at console output for detailed error messages
2. **Check Documentation**: 
   - `docs/QUICK_START.md` - Setup guide
   - `docs/ACCESS_GUIDE.md` - Access instructions
3. **Verify Prerequisites**: .NET 9 SDK, PostgreSQL installed and running

## Prevention Tips

1. **Always stop projects before rebuilding**
2. **Use the stop script**: `.\scripts\stop-projects.ps1`
3. **Check processes before building**: Verify no projects are running
4. **Use separate terminals**: Run API and Web in different terminals

---

**Most issues can be resolved by stopping processes and rebuilding!**

