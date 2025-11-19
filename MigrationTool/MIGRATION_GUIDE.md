# MoneyFex Data Migration Tool - User Guide

## Overview
This tool migrates data from the legacy SQL Server database to the new PostgreSQL database. It handles all reference data, user data, and transaction data with proper field mappings and data type conversions.

---

## Prerequisites

### 1. Database Access
- **Source Database (SQL Server)**: Access to the legacy SQL Server database
- **Target Database (PostgreSQL)**: Access to the new PostgreSQL database
- Both databases should be accessible from your machine

### 2. Required Software
- .NET 9.0 SDK (or later)
- PostgreSQL client libraries (included in the tool)
- SQL Server client libraries (included in the tool)

### 3. Database Schema
- The target PostgreSQL database should have all tables created (via EF Core migrations)
- Ensure the database schema matches the `MoneyFexDbContext` configuration

---

## Configuration

### Step 1: Update appsettings.json

Edit the `appsettings.json` file in the `MigrationTool` directory:

```json
{
  "MigrationSettings": {
    "SourceConnectionString": "Server=YOUR_SQL_SERVER;Database=YOUR_DB;User ID=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true",
    "TargetConnectionString": "Host=YOUR_PG_HOST;Port=5432;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD",
    "BatchSize": 1000,
    "EnableValidation": true,
    "EnableLogging": true,
    "ResumeFromCheckpoint": false,
    "LogPath": "logs/migration.log"
  }
}
```

### Connection String Examples

**SQL Server (Source):**
```
Server=localhost;Database=MoneyFexLegacyDB;User ID=sa;Password=YourPassword;TrustServerCertificate=true
```

**PostgreSQL (Target):**
```
Host=localhost;Port=5432;Database=moneyfex_db;Username=postgres;Password=YourPassword
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `SourceConnectionString` | SQL Server connection string (REQUIRED) | - |
| `TargetConnectionString` | PostgreSQL connection string (REQUIRED) | - |
| `BatchSize` | Number of records to process in each batch | 1000 |
| `EnableValidation` | Enable data validation during migration | true |
| `EnableLogging` | Enable detailed logging | true |
| `ResumeFromCheckpoint` | Resume from last checkpoint (future feature) | false |
| `LogPath` | Path to log file | logs/migration.log |

---

## Running the Migration

### Step 1: Build the Project

Open a terminal/command prompt in the project root and run:

```bash
cd MigrationTool
dotnet build
```

### Step 2: Run the Migration

#### Full Migration (Recommended for first run)
Migrates all data from source to target database:

```bash
dotnet run
```

Or explicitly specify full mode:
```bash
dotnet run --mode full
```

#### Validation Mode
Validates database connections and shows record counts without migrating:

```bash
dotnet run --mode validate
```

#### Incremental Migration
For migrating in batches (uses BatchSize from config):

```bash
dotnet run --mode incremental --batch-size 500
```

---

## Migration Process

The migration runs in **3 phases**:

### Phase 1: Reference Data
1. **Countries** - Country codes, names, currencies
2. **Banks** - Bank information
3. **Mobile Wallet Operators** - Mobile wallet providers
4. **Staff** - Staff member information

### Phase 2: User Data
1. **Senders** - Sender (Faxer) information
2. **Sender Logins** - Active sender logins
3. **Recipients** - Recipient information
4. **Receiver Details** - Receiver detail information

### Phase 3: Transaction Data
1. **Bank Account Deposits** - Bank deposit transactions
2. **Mobile Money Transfers** - Mobile wallet transactions
3. **Cash Pickups** - Cash pickup transactions

---

## What to Expect

### Console Output
During migration, you'll see:
```
==========================================
MoneyFex Data Migration Tool
Legacy SQL Server -> New PostgreSQL
==========================================

Validating connection strings...
✓ Connection strings configured

Migration Mode: full

Starting FULL migration...
This will migrate all data from legacy to new database.

Migrating countries...
Migrated 150 countries
Migrating banks...
Migrated 45 banks
...
```

### Log File
Detailed logs are written to `logs/migration.log`:
```
[2024-01-15 10:30:45] INFO: Starting full database migration
[2024-01-15 10:30:46] INFO: Phase 1: Migrating reference data
[2024-01-15 10:30:47] INFO: Migrating countries...
[2024-01-15 10:30:48] INFO: Migrated 150 countries
...
```

### Final Results
After completion, you'll see:
```
==========================================
Migration Results
==========================================
Status: SUCCESS
Duration: 45.23 minutes
Start Time: 2024-01-15 10:30:45
End Time: 2024-01-15 11:16:08

Record Counts:
  Countries: 150
  Banks: 45
  Senders: 12,345
  Transactions: 98,765
  ...
```

---

## Troubleshooting

### Common Issues

#### 1. Connection String Errors
**Error**: `Unable to connect to database`

**Solutions**:
- Verify connection strings in `appsettings.json`
- Check network connectivity to databases
- Ensure database servers are running
- Verify credentials are correct
- For SQL Server, ensure `TrustServerCertificate=true` if using self-signed certificates

#### 2. Missing Tables
**Error**: `relation "table_name" does not exist`

**Solutions**:
- Ensure PostgreSQL database has all migrations applied
- Run EF Core migrations: `dotnet ef database update`
- Verify table names match the schema

#### 3. Data Type Mismatches
**Error**: `Invalid cast` or `Type mismatch`

**Solutions**:
- Check the migration service handles all data types correctly
- Review logs for specific field causing issues
- Verify enum mappings are correct

#### 4. Foreign Key Violations
**Error**: `Foreign key constraint violation`

**Solutions**:
- Ensure reference data (Countries, Banks, etc.) is migrated first
- Check that parent records exist before inserting child records
- Verify foreign key relationships in target database

#### 5. Duplicate Key Errors
**Error**: `Duplicate key value violates unique constraint`

**Solutions**:
- The migration uses `ON CONFLICT DO UPDATE` to handle duplicates
- If errors persist, check for data integrity issues in source database
- Review unique constraints in target database

### Getting Help

1. **Check Logs**: Review `logs/migration.log` for detailed error messages
2. **Validate Mode**: Run `--mode validate` to check database connectivity
3. **Test Connections**: Verify you can connect to both databases manually
4. **Review Configuration**: Double-check `appsettings.json` settings

---

## Best Practices

### Before Migration

1. **Backup Databases**: Always backup both source and target databases
2. **Test in Staging**: Run migration on staging/test environment first
3. **Validate Schema**: Ensure target database schema is up-to-date
4. **Check Disk Space**: Ensure sufficient disk space for logs and data
5. **Network Stability**: Use stable network connection

### During Migration

1. **Monitor Logs**: Watch the log file for any warnings or errors
2. **Don't Interrupt**: Let the migration complete without interruption
3. **Resource Monitoring**: Monitor database server resources (CPU, memory, disk)

### After Migration

1. **Verify Data**: Compare record counts between source and target
2. **Sample Checks**: Spot-check some records for accuracy
3. **Test Application**: Test the application with migrated data
4. **Keep Logs**: Save migration logs for reference

---

## Migration Modes Explained

### Full Migration (`--mode full`)
- Migrates all data from source to target
- Best for initial migration or complete refresh
- Processes all tables in order
- **Use when**: First time migration or complete data refresh

### Validation Mode (`--mode validate`)
- Tests database connectivity
- Shows record counts in both databases
- Does NOT migrate any data
- **Use when**: Testing connections or checking data volumes

### Incremental Migration (`--mode incremental`)
- Migrates data in batches
- Uses `BatchSize` from configuration
- Currently same as full migration (future enhancement)
- **Use when**: Large datasets that need batch processing

---

## Data Mapping

### Key Mappings

| Source (SQL Server) | Target (PostgreSQL) | Notes |
|---------------------|---------------------|-------|
| `FaxerInformation` | `senders` | Sender data |
| `BankAccountDeposit` | `transactions` + `bank_account_deposits` | Split into base transaction and deposit details |
| `MobileMoneyTransfer` | `transactions` + `mobile_money_transfers` | Split into base transaction and transfer details |
| `FaxingNonCardTransaction` | `transactions` + `cash_pickups` | Split into base transaction and pickup details |
| `Country` | `countries` | Country reference data |
| `Bank` | `banks` | Bank reference data |
| `MobileWalletOperator` | `mobile_wallet_operators` | Wallet operator reference data |

### Status Mapping

Legacy status values are mapped to new `TransactionStatus` enum:
- `Failed` → `Failed`
- `InProgress` → `InProgress`
- `Paid` → `Paid`
- `Cancelled` → `Cancelled`
- And more...

---

## Performance Tips

1. **Batch Size**: Adjust `BatchSize` based on your system:
   - Small datasets: 500-1000
   - Large datasets: 1000-5000
   - Very large: 5000-10000

2. **Database Indexes**: Ensure target database has proper indexes
3. **Network**: Use fast, stable network connection
4. **Resources**: Ensure sufficient database server resources
5. **Off-Peak Hours**: Run during off-peak hours for production

---

## Example Workflow

```bash
# 1. Navigate to MigrationTool directory
cd MigrationTool

# 2. Update appsettings.json with your connection strings

# 3. Validate connections first
dotnet run --mode validate

# 4. If validation succeeds, run full migration
dotnet run --mode full

# 5. Check the results and logs
cat logs/migration.log
```

---

## Support

For issues or questions:
1. Check the log file: `logs/migration.log`
2. Review error messages in console output
3. Verify database connectivity and permissions
4. Ensure all prerequisites are met

---

## Notes

- The migration uses `ON CONFLICT DO UPDATE` to handle duplicate records
- All timestamps are converted to UTC
- Enum values are properly mapped between legacy and new systems
- Null values are handled gracefully
- The migration is idempotent (can be run multiple times safely)

