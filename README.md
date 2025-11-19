# MoneyFex Modular Monolithic Architecture

## Overview

This project is a migration of the existing MoneyFex transaction management system into a modular monolithic architecture. The system handles four types of transactions:
- **Bank Account Deposits** - Bank-to-bank transfers
- **Mobile Money Transfers** - Mobile wallet transactions
- **Cash Pickups** - Cash pickup transactions
- **KiiBank Transfers** - KiiBank-specific transfers

## Quick Start

### 1. Setup Database
```powershell
# Windows
.\scripts\setup-database.ps1

# Linux/Mac
./scripts/setup-database.sh
```

### 2. Run Web Application
```powershell
# Windows
.\scripts\run-web.ps1

# Linux/Mac
./scripts/run-web.sh
```

**Note:** If you get file lock errors during build, stop the project first:
```powershell
# Windows
.\scripts\stop-projects.ps1

# Linux/Mac
./scripts/stop-projects.sh
```

### 3. Access Application
- **Web App**: `https://localhost:5003`

## Project Structure

```
MoneyFex.Modular/
├── MoneyFex.Core/              # Domain entities and interfaces
├── MoneyFex.Infrastructure/    # Data access and services
├── MoneyFex.Web/               # .NET Core MVC (Frontend)
├── Database/                    # PostgreSQL schema and migrations
├── docs/                       # Documentation files
└── scripts/                     # Setup and run scripts
```

## Documentation

All documentation files are located in the `docs/` folder:
- **docs/QUICK_START.md** - Quick start guide (5 minutes)
- **docs/ACCESS_GUIDE.md** - How to access Swagger and Web App
- **docs/ARCHITECTURE.md** - Architecture details
- **docs/MIGRATION_GUIDE.md** - Database migration guide
- **docs/SETUP_CHECKLIST.md** - Setup verification checklist

## Scripts

All setup and run scripts are located in the `scripts/` folder:
- **scripts/setup.ps1** / **scripts/setup.sh** - Main setup script
- **scripts/run-web.ps1** / **scripts/run-web.sh** - Run Web application
- **scripts/setup-database.ps1** / **scripts/setup-database.sh** - Database setup
- **scripts/stop-projects.ps1** / **scripts/stop-projects.sh** - Stop running projects

## Architecture

### Architecture Layers

1. **Core Layer** (`MoneyFex.Core`)
   - Domain entities
   - Business interfaces
   - Enumerations
   - Domain logic

2. **Infrastructure Layer** (`MoneyFex.Infrastructure`)
   - Entity Framework Core DbContext
   - Repository implementations
   - Service implementations
   - Data access logic

3. **Web Layer** (`MoneyFex.Web`)
   - MVC controllers
   - Views
   - Frontend logic

## Database Schema

### Normalized Design

The database schema has been normalized to reduce redundancy:

- **Base Transaction Table**: Common fields for all transaction types
- **Transaction-Specific Tables**: Bank deposits, mobile transfers, cash pickups, KiiBank transfers
- **Reference Tables**: Countries, banks, mobile wallet operators, senders, staff
- **Auxiliary Tables**: Override values for auxiliary agents
- **Management Tables**: Reinitialize transactions, card payment information

## Setup Instructions

### Prerequisites

- .NET 9 SDK
- PostgreSQL 12 or higher
- Visual Studio 2022 or VS Code

### Database Setup

1. **Install PostgreSQL**
   ```bash
   # On Windows, download from https://www.postgresql.org/download/windows/
   # On Linux/Mac, use package manager
   ```

2. **Run Database Setup Script**
   ```powershell
   # Windows
   .\scripts\setup-database.ps1
   
   # Linux/Mac
   ./scripts/setup-database.sh
   ```

3. **Update Connection String**
   - Edit `MoneyFex.Web/appsettings.json`
   - Update the `ConnectionStrings:DefaultConnection` value with your PostgreSQL credentials

### Application Setup

1. **Run Setup Script**
   ```powershell
   # Windows
   .\scripts\setup.ps1
   
   # Linux/Mac
   ./scripts/setup.sh
   ```

2. **Run Web Application**
   ```powershell
   # Windows
   .\scripts\run-web.ps1
   
   # Linux/Mac
   ./scripts/run-web.sh
   ```

## Design Decisions

### 1. Modular Monolithic Architecture

**Rationale**: 
- Maintains simplicity of a monolithic application
- Allows for clear module boundaries
- Easier to understand and maintain
- Can be split into microservices later if needed

### 2. Normalized Database Schema

**Rationale**:
- Reduces data redundancy
- Improves data integrity
- Easier to maintain and update
- Better query performance with proper indexing

### 3. Repository Pattern

**Rationale**:
- Separates data access logic from business logic
- Makes testing easier
- Provides flexibility to change data access implementation
- Follows SOLID principles

### 4. Service Layer

**Rationale**:
- Encapsulates business logic
- Provides a clean API for controllers
- Allows for transaction management
- Enables cross-cutting concerns

### 5. PostgreSQL

**Rationale**:
- Open-source and cost-effective
- Excellent performance
- Strong ACID compliance
- Good support for complex queries

## Migration from Legacy System

### Key Changes

1. **Database Normalization**
   - Removed redundant fields
   - Created base transaction table
   - Separated transaction-specific details

2. **Code Organization**
   - Clear separation of concerns
   - Modular structure
   - Dependency injection

3. **Technology Stack**
   - Upgraded to .NET 9
   - PostgreSQL instead of SQL Server
   - Modern async/await patterns

4. **Direct Database Access**
   - Web application uses Entity Framework Core directly
   - No separate API layer needed
   - Simplified architecture

## Testing

### Unit Tests

```bash
dotnet test
```

### Integration Tests

```bash
dotnet test --filter Category=Integration
```

## Deployment

### Development

```bash
dotnet run --project MoneyFex.Web
```

### Production

```bash
dotnet publish -c Release
```

## Future Enhancements

1. **Authentication & Authorization**
   - JWT token-based authentication
   - Role-based access control

2. **Caching**
   - Redis for frequently accessed data
   - Response caching

3. **Logging**
   - Structured logging with Serilog
   - Application Insights integration

4. **Monitoring**
   - Health checks
   - Performance monitoring

5. **API Versioning**
   - Version management
   - Backward compatibility

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

[Your License Here]

## Support

For issues and questions, please refer to:
- **docs/QUICK_START.md** - Quick start guide
- **docs/ACCESS_GUIDE.md** - Access instructions
- **docs/ARCHITECTURE.md** - Architecture details
- **docs/MIGRATION_GUIDE.md** - Migration guide

---

**Project Status**: ✅ Complete and Ready for Use

