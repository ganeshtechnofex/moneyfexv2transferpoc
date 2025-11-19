# Quick Start Guide - Migration Tool

## 🚀 Quick Steps

### 1. Configure Connection Strings

Edit `appsettings.json`:

```json
{
  "MigrationSettings": {
    "SourceConnectionString": "Server=YOUR_SQL_SERVER;Database=YOUR_DB;User ID=USER;Password=PASSWORD;TrustServerCertificate=true",
    "TargetConnectionString": "Host=YOUR_PG_HOST;Port=5432;Database=YOUR_DB;Username=USER;Password=PASSWORD"
  }
}
```

### 2. Build the Project

```bash
cd MigrationTool
dotnet build
```

### 3. Validate Connections (Optional but Recommended)

```bash
dotnet run --mode validate
```

### 4. Run Full Migration

```bash
dotnet run
```

or

```bash
dotnet run --mode full
```

## 📋 What Gets Migrated

**Phase 1 - Reference Data:**
- Countries, Banks, Mobile Wallet Operators, Staff

**Phase 2 - User Data:**
- Senders, Sender Logins, Recipients, Receiver Details

**Phase 3 - Transaction Data:**
- Bank Account Deposits, Mobile Money Transfers, Cash Pickups

## 📊 Check Results

After migration completes, check:
- Console output for summary
- `logs/migration.log` for detailed logs
- Target database for migrated records

## ⚠️ Common Issues

| Issue | Solution |
|-------|----------|
| Connection failed | Check connection strings in `appsettings.json` |
| Table not found | Run EF Core migrations on target database |
| Duplicate key | Migration handles this automatically with `ON CONFLICT DO UPDATE` |

## 📝 Notes

- Migration is **idempotent** - safe to run multiple times
- All data is migrated with proper field mappings
- Logs are saved to `logs/migration.log`
- Migration uses transactions for data integrity

For detailed information, see `MIGRATION_GUIDE.md`

