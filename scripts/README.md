# MoneyFex Modular - Scripts

This folder contains all setup and run scripts for the MoneyFex Modular project.

## Scripts Overview

### Setup Scripts

#### Windows PowerShell
- **setup.ps1** - Main setup script (checks prerequisites, restores packages, builds solution)
- **setup-database.ps1** - Database setup script (creates database and runs schema)

#### Linux/Mac (Bash)
- **setup.sh** - Main setup script (checks prerequisites, restores packages, builds solution)
- **setup-database.sh** - Database setup script (creates database and runs schema)

### Run Scripts

#### Windows PowerShell
- **run-web.ps1** - Quick script to run the Web project
- **stop-projects.ps1** - Stop all running projects

#### Linux/Mac (Bash)
- **run-web.sh** - Quick script to run the Web project
- **stop-projects.sh** - Stop all running projects

## Usage

### Windows PowerShell

#### Initial Setup
```powershell
# Run main setup
.\scripts\setup.ps1

# Setup database
.\scripts\setup-database.ps1
```

#### Run Web Application
```powershell
.\scripts\run-web.ps1
```

#### Stop Projects
```powershell
# Stop all running projects
.\scripts\stop-projects.ps1
```

### Linux/Mac

#### Initial Setup
```bash
# Make scripts executable (first time only)
chmod +x scripts/*.sh

# Run main setup
./scripts/setup.sh

# Setup database
./scripts/setup-database.sh
```

#### Run Web Application
```bash
./scripts/run-web.sh
```

#### Stop Projects
```bash
# Stop all running projects
./scripts/stop-projects.sh
```

## Script Details

### setup.ps1 / setup.sh
- Checks .NET SDK installation
- Checks PostgreSQL availability
- Restores NuGet packages
- Builds the solution
- Shows next steps

### setup-database.ps1 / setup-database.sh
- Creates PostgreSQL database
- Runs schema script
- Shows connection string

### run-web.ps1 / run-web.sh
- Changes to Web directory
- Runs `dotnet run`
- Starts Web on https://localhost:5003

## Notes

- All scripts should be run from the project root directory
- Database scripts require PostgreSQL to be installed and running
- Run scripts require .NET 9 SDK to be installed
- Scripts will show helpful error messages if prerequisites are missing

---

**All scripts are organized in this folder for easy access!**

