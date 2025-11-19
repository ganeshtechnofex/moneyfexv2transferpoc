# MoneyFex Modular - Project Structure

## Folder Organization

```
MoneyFex.Modular/
├── MoneyFex.API/                    # Web API Project
│   ├── Controllers/                 # API Controllers
│   ├── Properties/                  # Launch settings
│   ├── Program.cs                   # Application entry point
│   └── appsettings.json             # Configuration
│
├── MoneyFex.Core/                   # Domain Layer
│   ├── Entities/                    # Domain entities
│   │   ├── Enums/                   # Enumerations
│   │   └── *.cs                     # Entity classes
│   └── Interfaces/                  # Repository and service interfaces
│
├── MoneyFex.Infrastructure/         # Infrastructure Layer
│   ├── Data/                        # DbContext
│   ├── Migrations/                  # EF Core migrations
│   ├── Repositories/                # Repository implementations
│   └── Services/                    # Service implementations
│
├── MoneyFex.Web/                     # MVC Web Project
│   ├── Controllers/                 # MVC Controllers
│   ├── Views/                       # Razor views
│   ├── wwwroot/                     # Static files (CSS, JS)
│   ├── Program.cs                   # Application entry point
│   └── appsettings.json             # Configuration
│
├── Database/                         # Database Scripts
│   └── Schema/                      # SQL schema files
│       └── 01_CreateDatabase.sql    # Main schema script
│
├── docs/                            # 📚 Documentation Files
│   ├── README.md                    # Documentation index
│   ├── QUICK_START.md               # Quick start guide
│   ├── ACCESS_GUIDE.md              # Access instructions
│   ├── ARCHITECTURE.md               # Architecture details
│   ├── MIGRATION_GUIDE.md           # Database migration guide
│   ├── SETUP_CHECKLIST.md           # Setup checklist
│   ├── PROJECT_SUMMARY.md           # Project summary
│   ├── COMPLETION_STATUS.md         # Completion status
│   ├── FINAL_SUMMARY.md             # Final summary
│   ├── RUN_PROJECTS.md              # Running projects guide
│   ├── RUNNING_STATUS.md            # Current status
│   └── START_PROJECTS.md            # Start instructions
│
├── scripts/                         # 🔧 Scripts
│   ├── README.md                    # Scripts documentation
│   ├── setup.ps1                    # Main setup (Windows)
│   ├── setup.sh                     # Main setup (Linux/Mac)
│   ├── setup-database.ps1           # Database setup (Windows)
│   ├── setup-database.sh            # Database setup (Linux/Mac)
│   ├── run-api.ps1                  # Run API (Windows)
│   ├── run-api.sh                   # Run API (Linux/Mac)
│   ├── run-web.ps1                  # Run Web (Windows)
│   └── run-web.sh                   # Run Web (Linux/Mac)
│
├── MoneyFex.Modular.sln             # Solution file
└── README.md                         # Main project README
```

## File Organization

### Documentation (`docs/`)
All markdown documentation files are organized here:
- Setup and getting started guides
- Architecture documentation
- Migration guides
- Status and running guides

### Scripts (`scripts/`)
All PowerShell and Bash scripts are organized here:
- Setup scripts for initial configuration
- Run scripts for starting applications
- Database setup scripts

### Database (`Database/`)
Database-related files:
- SQL schema scripts
- Migration SQL files

## Usage

### Running Scripts

All scripts should be run from the project root:

```powershell
# Windows
.\scripts\setup.ps1
.\scripts\run-api.ps1

# Linux/Mac
./scripts/setup.sh
./scripts/run-api.sh
```

### Reading Documentation

All documentation is in the `docs/` folder:

```powershell
# View documentation
code docs\QUICK_START.md
code docs\ACCESS_GUIDE.md
```

## Benefits of This Organization

1. **Clear Separation**: Documentation and scripts are separate from code
2. **Easy to Find**: All related files are grouped together
3. **Maintainable**: Easy to update and maintain
4. **Professional**: Clean project structure

---

**All files are now organized in their respective folders!**

