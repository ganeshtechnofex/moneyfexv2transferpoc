# Data Migration Quick Start Guide

## Quick Overview

This guide provides step-by-step instructions to migrate data from the legacy SQL Server database to the new PostgreSQL database.

## Prerequisites

1. **Source Database Access**
   - SQL Server connection to legacy database
   - Database: `MoneyFexDB`
   - Connection: `Server=DESKTOP-AID3TE5;Database=MoneyFexDB;User ID=sa;Password=riddhasoft`

2. **Target Database Access**
   - PostgreSQL connection to new database
   - Database: `moneyfex_db`
   - Connection: `Host=localhost;Port=5432;Database=moneyfex_db;Username=postgres;Password=technofex`

3. **.NET Core 9 SDK** installed

## Step 1: Configure Connection Strings

Edit `MoneyFex.Infrastructure/MigrationTool/appsettings.json`:

```json
{
  "MigrationSettings": {
    "SourceConnectionString": "Server=YOUR_SQL_SERVER;Database=MoneyFexDB;User ID=sa;Password=YOUR_PASSWORD;TrustServerCertificate=true",
    "TargetConnectionString": "Host=localhost;Port=5432;Database=moneyfex_db;Username=postgres;Password=YOUR_PASSWORD",
    "BatchSize": 1000,
    "EnableValidation": true,
    "EnableLogging": true
  }
}
```

## Step 2: Run Migration

### Windows (PowerShell)

```powershell
cd MoneyFex.Modular
.\scripts\run-migration.ps1 full
```

### Linux/Mac (Bash)

```bash
cd MoneyFex.Modular
chmod +x scripts/run-migration.sh
./scripts/run-migration.sh full
```

### Manual Execution

```bash
cd MoneyFex.Modular/MoneyFex.Infrastructure/MigrationTool
dotnet restore
dotnet build
dotnet run -- --mode full
```

## Step 3: Validate Migration

After migration completes, run validation:

```bash
# Windows
.\scripts\run-migration.ps1 validate

# Linux/Mac
./scripts/run-migration.sh validate
```

## Migration Modes

### Full Migration
Migrates all data from legacy to new database:
```bash
.\scripts\run-migration.ps1 full
```

### Validation Only
Validates data without migrating:
```bash
.\scripts\run-migration.ps1 validate
```

### Incremental Migration
Migrates data in batches (useful for large datasets):
```bash
.\scripts\run-migration.ps1 incremental 500
```

## What Gets Migrated

### Phase 1: Reference Data
- ✅ Countries
- ✅ Banks
- ✅ Mobile Wallet Operators
- ✅ Staff

### Phase 2: User Data
- ✅ Senders (FaxerInformation)
- ✅ Sender Logins
- ✅ Recipients
- ✅ Receiver Details

### Phase 3: Transaction Data
- ✅ Bank Account Deposits
- ✅ Mobile Money Transfers
- ✅ Cash Pickups

## Monitoring Progress

Migration logs are written to:
- `logs/migration.log` - Detailed migration log
- Console output - Real-time progress

## Troubleshooting

### Connection Issues

**SQL Server Connection Failed**
- Verify SQL Server is running
- Check firewall rules
- Verify credentials in appsettings.json
- Try adding `TrustServerCertificate=true` to connection string

**PostgreSQL Connection Failed**
- Verify PostgreSQL is running
- Check if database exists: `CREATE DATABASE moneyfex_db;`
- Verify credentials in appsettings.json
- Check pg_hba.conf for authentication settings

### Data Issues

**Foreign Key Violations**
- Ensure reference data (countries, banks) is migrated first
- Check for missing parent records in source database

**Enum Mapping Errors**
- Review enum mappings in `DataMigrationService.cs`
- Check migration logs for specific enum values that failed

### Performance Issues

**Slow Migration**
- Reduce batch size in appsettings.json
- Add indexes on source database
- Run during off-peak hours
- Consider incremental migration

## Post-Migration Tasks

1. **Update Sequences**
   ```sql
   SELECT setval('senders_id_seq', (SELECT MAX(id) FROM senders));
   SELECT setval('transactions_id_seq', (SELECT MAX(id) FROM transactions));
   ```

2. **Verify Data Counts**
   ```sql
   -- Compare record counts
   SELECT 'senders' as table_name, COUNT(*) FROM senders
   UNION ALL
   SELECT 'transactions', COUNT(*) FROM transactions;
   ```

3. **Check Data Integrity**
   - Verify foreign key relationships
   - Check for orphaned records
   - Validate transaction totals

## Rollback

If migration fails and you need to start over:

1. **Drop target database tables** (or recreate database)
2. **Fix issues** in source data or configuration
3. **Re-run migration**

## Support

For detailed information, see:
- `docs/DATA_MIGRATION_GUIDE.md` - Comprehensive migration guide
- `logs/migration.log` - Detailed migration logs

## Next Steps

After successful migration:
1. ✅ Verify data integrity
2. ✅ Update application connection strings
3. ✅ Test application with migrated data
4. ✅ Deploy to production

