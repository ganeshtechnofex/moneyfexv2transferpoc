# Data Migration Guide: Legacy to Modular Architecture

## Overview

This document provides a comprehensive guide for migrating data from the legacy FAXER.PORTAL project (SQL Server) to the new MoneyFex.Modular project (PostgreSQL). The migration follows an ETL (Extract, Transform, Load) process.

## Prerequisites

1. **Source Database**: SQL Server (Legacy - MoneyFexDB)
   - Connection String: `Server=DESKTOP-AID3TE5;Database=MoneyFexDB;User ID=sa;Password=riddhasoft`
   - Database Name: `MoneyFexDB`

2. **Target Database**: PostgreSQL (New - moneyfex_db)
   - Connection String: `Host=localhost;Port=5432;Database=moneyfex_db;Username=postgres;Password=technofex`
   - Database Name: `moneyfex_db`

3. **Tools Required**:
   - SQL Server Management Studio (SSMS) or Azure Data Studio
   - PostgreSQL client (psql) or pgAdmin
   - .NET Core 9 SDK
   - Entity Framework Core tools

## Migration Strategy

### Phase 1: Reference Data Migration
1. Countries
2. Banks
3. Mobile Wallet Operators
4. Staff

### Phase 2: User Data Migration
1. Senders (FaxerInformation)
2. Sender Logins
3. Recipients
4. Receiver Details

### Phase 3: Transaction Data Migration
1. Transactions (Base)
2. Bank Account Deposits
3. Mobile Money Transfers
4. Cash Pickups
5. KiiBank Transfers

### Phase 4: Supporting Data Migration
1. Card Payment Information
2. Reinitialize Transactions
3. Auxiliary Agent Details

## Table Mapping

### Core Reference Tables

| Legacy Table | Legacy Entity | New Table | New Entity | Notes |
|-------------|--------------|-----------|------------|-------|
| Country | Country | countries | Country | Direct mapping |
| Bank | Bank | banks | Bank | Direct mapping |
| MobileWalletOperator | MobileWalletOperator | mobile_wallet_operators | MobileWalletOperator | Direct mapping |
| StaffInformation | StaffInformation | staff | Staff | Simplified mapping |

### User Tables

| Legacy Table | Legacy Entity | New Table | New Entity | Notes |
|-------------|--------------|-----------|------------|-------|
| FaxerInformation | FaxerInformation | senders | Sender | Simplified mapping |
| FaxerLogin | FaxerLogin | sender_logins | SenderLogin | Direct mapping |
| Recipients | Recipients | recipients | Recipient | Simplified mapping |
| ReceiversDetails | ReceiversDetails | receiver_details | ReceiverDetail | Direct mapping |

### Transaction Tables

| Legacy Table | Legacy Entity | New Table | New Entity | Notes |
|-------------|--------------|-----------|------------|-------|
| BankAccountDeposit | BankAccountDeposit | transactions + bank_account_deposits | Transaction + BankAccountDeposit | Split into base + detail |
| MobileMoneyTransfer | MobileMoneyTransfer | transactions + mobile_money_transfers | Transaction + MobileMoneyTransfer | Split into base + detail |
| FaxingNonCardTransaction | FaxingNonCardTransaction | transactions + cash_pickups | Transaction + CashPickup | Split into base + detail |
| (KiiBank) | (Various) | transactions + kiibank_transfers | Transaction + KiiBankTransfer | New structure |

## Field Mapping

### FaxerInformation → Sender

| Legacy Field | Legacy Type | New Field | New Type | Transformation |
|-------------|------------|-----------|----------|----------------|
| Id | int | Id | int | Direct |
| FirstName | string | FirstName | string | Direct |
| MiddleName | string | MiddleName | string | Direct |
| LastName | string | LastName | string | Direct |
| Email | string | Email | string | Direct |
| PhoneNumber | string | PhoneNumber | string | Direct |
| AccountNo | string | AccountNo | string | Direct |
| Address1 | string | Address1 | string | Direct |
| Address2 | string | Address2 | string | Direct |
| City | string | City | string | Direct |
| State | string | State | string | Direct |
| Country | string | CountryCode | string | Map to country_code |
| PostalCode | string | PostalCode | string | Direct |
| IsBusiness | bool | IsBusiness | bool | Direct |
| CreatedDate | DateTime? | CreatedAt | DateTime | Use CreatedDate or current time |
| IsDeleted | bool | IsActive | bool | Invert: IsActive = !IsDeleted |

### BankAccountDeposit → Transaction + BankAccountDeposit

**Transaction Table:**
| Legacy Field | New Field | Transformation |
|-------------|-----------|----------------|
| TransactionId | Id | Generate new ID |
| ReceiptNo | ReceiptNo | Direct |
| TransactionDate | TransactionDate | Direct |
| SenderId | SenderId | Direct |
| SendingCountry | SendingCountryCode | Direct |
| ReceivingCountry | ReceivingCountryCode | Direct |
| SendingCurrency | SendingCurrency | Direct |
| ReceivingCurrency | ReceivingCurrency | Direct |
| SendingAmount | SendingAmount | Direct |
| ReceivingAmount | ReceivingAmount | Direct |
| Fee | Fee | Direct |
| TotalAmount | TotalAmount | Direct |
| ExchangeRate | ExchangeRate | Direct |
| PaymentReference | PaymentReference | Direct |
| SenderPaymentMode | SenderPaymentMode | Map enum |
| Status | Status | Map enum (BankDepositStatus → TransactionStatus) |
| PayingStaffId | PayingStaffId | Direct |
| Apiservice | ApiService | Map enum |

**BankAccountDeposit Table:**
| Legacy Field | New Field | Transformation |
|-------------|-----------|----------------|
| TransactionId | TransactionId | Link to new Transaction.Id |
| BankId | BankId | Direct |
| BankName | BankName | Direct |
| BankCode | BankCode | Direct |
| ReceiverAccountNo | ReceiverAccountNo | Direct |
| ReceiverName | ReceiverName | Direct |
| ReceiverCity | ReceiverCity | Direct |
| IsManualDeposit | IsManualDeposit | Direct |
| IsManualApproveNeeded | IsManualApprovalNeeded | Direct |
| ManuallyApproved | IsManuallyApproved | Direct |
| IsEuropeTransfer | IsEuropeTransfer | Direct |
| IsTransactionDuplicated | IsTransactionDuplicated | Direct |
| DuplicateTransactionReceiptNo | DuplicateTransactionReceiptNo | Direct |

### MobileMoneyTransfer → Transaction + MobileMoneyTransfer

**Transaction Table:**
| Legacy Field | New Field | Transformation |
|-------------|-----------|----------------|
| Id | Id | Generate new ID |
| ReceiptNo | ReceiptNo | Direct |
| TransactionDate | TransactionDate | Direct |
| SenderId | SenderId | Direct |
| SendingCountry | SendingCountryCode | Direct |
| ReceivingCountry | ReceivingCountryCode | Direct |
| SendingCurrency | SendingCurrency | Direct |
| ReceivingCurrency | ReceivingCurrency | Direct |
| SendingAmount | SendingAmount | Direct |
| ReceivingAmount | ReceivingAmount | Direct |
| Fee | Fee | Direct |
| TotalAmount | TotalAmount | Direct |
| ExchangeRate | ExchangeRate | Direct |
| PaymentReference | PaymentReference | Direct |
| SenderPaymentMode | SenderPaymentMode | Map enum |
| Status | Status | Map enum (MobileMoneyTransferStatus → TransactionStatus) |
| PayingStaffId | PayingStaffId | Direct |
| Apiservice | ApiService | Map enum |

**MobileMoneyTransfer Table:**
| Legacy Field | New Field | Transformation |
|-------------|-----------|----------------|
| Id | TransactionId | Link to new Transaction.Id |
| WalletOperatorId | WalletOperatorId | Direct |
| PaidToMobileNo | PaidToMobileNo | Direct |
| ReceiverName | ReceiverName | Direct |
| ReceiverCity | ReceiverCity | Direct |

### FaxingNonCardTransaction → Transaction + CashPickup

**Transaction Table:**
| Legacy Field | New Field | Transformation |
|-------------|-----------|----------------|
| Id | Id | Generate new ID |
| ReceiptNumber | ReceiptNo | Direct |
| TransactionDate | TransactionDate | Direct |
| SenderId | SenderId | Direct |
| SendingCountry | SendingCountryCode | Direct |
| ReceivingCountry | ReceivingCountryCode | Direct |
| SendingCurrency | SendingCurrency | Direct |
| ReceivingCurrency | ReceivingCurrency | Direct |
| FaxingAmount | SendingAmount | Direct |
| ReceivingAmount | ReceivingAmount | Direct |
| FaxingFee | Fee | Direct |
| TotalAmount | TotalAmount | Direct |
| ExchangeRate | ExchangeRate | Direct |
| PaymentReference | PaymentReference | Direct |
| SenderPaymentMode | SenderPaymentMode | Map enum |
| FaxingStatus | Status | Map enum (FaxingStatus → TransactionStatus) |
| PayingStaffId | PayingStaffId | Direct |
| Apiservice | ApiService | Map enum |

**CashPickup Table:**
| Legacy Field | New Field | Transformation |
|-------------|-----------|----------------|
| Id | TransactionId | Link to new Transaction.Id |
| MFCN | MFCN | Direct |
| RecipientId | RecipientId | Direct |
| NonCardRecieverId | NonCardReceiverId | Direct |

## Enum Mappings

### TransactionStatus Mapping

| Legacy Enum | Legacy Value | New Enum | New Value |
|------------|--------------|----------|-----------|
| MobileMoneyTransferStatus.InProgress | 1 | TransactionStatus.InProgress | 0 |
| MobileMoneyTransferStatus.Paid | 2 | TransactionStatus.Paid | 1 |
| MobileMoneyTransferStatus.Cancel | 3 | TransactionStatus.Cancelled | 2 |
| MobileMoneyTransferStatus.Failed | 0 | TransactionStatus.Failed | 3 |
| MobileMoneyTransferStatus.PaymentPending | 4 | TransactionStatus.PaymentPending | 4 |
| MobileMoneyTransferStatus.IdCheckInProgress | 5 | TransactionStatus.IdCheckInProgress | 5 |
| MobileMoneyTransferStatus.FullRefund | 10 | TransactionStatus.FullRefund | 7 |
| MobileMoneyTransferStatus.PartailRefund | 11 | TransactionStatus.PartialRefund | 8 |
| MobileMoneyTransferStatus.Held | 8 | TransactionStatus.Held | 13 |
| MobileMoneyTransferStatus.Paused | 12 | TransactionStatus.Paused | 14 |
| FaxingStatus.Received | 1 | TransactionStatus.Received | 11 |
| FaxingStatus.Completed | 6 | TransactionStatus.Completed | 12 |
| FaxingStatus.NotReceived | 0 | TransactionStatus.NotReceived | 10 |
| BankDepositStatus.Confirm | 3 | TransactionStatus.Paid | 1 |
| BankDepositStatus.PaymentPending | 6 | TransactionStatus.PaymentPending | 4 |
| BankDepositStatus.InProgressFC | 15 | TransactionStatus.InProgress | 0 |

### PaymentMode Mapping

| Legacy Enum | Legacy Value | New Enum | New Value |
|------------|--------------|----------|-----------|
| SenderPaymentMode.Card | 0 | PaymentMode.Card | 0 |
| SenderPaymentMode.BankAccount | 1 | PaymentMode.BankAccount | 1 |
| SenderPaymentMode.MobileWallet | 2 | PaymentMode.MobileWallet | 2 |
| SenderPaymentMode.Cash | 3 | PaymentMode.Cash | 3 |

### TransactionModule Mapping

| Legacy Enum | Legacy Value | New Enum | New Value |
|------------|--------------|----------|-----------|
| OperatingUserType.Sender | 0 | TransactionModule.Sender | 0 |
| OperatingUserType.CardUser | 1 | TransactionModule.CardUser | 1 |
| OperatingUserType.BusinessMerchant | 2 | TransactionModule.BusinessMerchant | 2 |
| OperatingUserType.Agent | 3 | TransactionModule.Agent | 3 |
| OperatingUserType.Admin | 4 | TransactionModule.AdminStaff | 4 |
| OperatingUserType.KiiPayBusiness | 5 | TransactionModule.KiiPayBusiness | 5 |
| OperatingUserType.KiiPayPersonal | 6 | TransactionModule.KiiPayPersonal | 6 |

## Migration Steps

### Step 1: Prepare Source Database

1. **Backup Legacy Database**
   ```sql
   BACKUP DATABASE MoneyFexDB 
   TO DISK = 'C:\Backups\MoneyFexDB_BeforeMigration.bak'
   WITH FORMAT, COMPRESSION;
   ```

2. **Verify Data Integrity**
   - Check for orphaned records
   - Verify foreign key relationships
   - Identify missing reference data

### Step 2: Prepare Target Database

1. **Create New Database**
   ```sql
   CREATE DATABASE moneyfex_db;
   ```

2. **Run Migrations**
   ```bash
   cd MoneyFex.Modular/MoneyFex.Web
   dotnet ef database update
   ```

3. **Verify Schema**
   - Ensure all tables are created
   - Verify indexes and constraints

### Step 3: Run Migration Tool

1. **Configure Connection Strings**
   - Update `appsettings.json` with both source and target connections
   - Or use environment variables

2. **Run Migration Service**
   ```bash
   cd MoneyFex.Modular
   dotnet run --project MoneyFex.Infrastructure/MigrationTool/MigrationTool.csproj
   ```

3. **Monitor Progress**
   - Check logs for errors
   - Verify record counts
   - Validate data integrity

### Step 4: Data Validation

1. **Record Count Verification**
   ```sql
   -- Legacy
   SELECT COUNT(*) FROM FaxerInformation;
   SELECT COUNT(*) FROM BankAccountDeposit;
   SELECT COUNT(*) FROM MobileMoneyTransfer;
   SELECT COUNT(*) FROM FaxingNonCardTransaction;
   
   -- New
   SELECT COUNT(*) FROM senders;
   SELECT COUNT(*) FROM transactions WHERE transaction_module = 0; -- BankDeposit
   SELECT COUNT(*) FROM transactions WHERE transaction_module = 0; -- MobileWallet
   SELECT COUNT(*) FROM transactions WHERE transaction_module = 0; -- CashPickup
   ```

2. **Data Sampling**
   - Compare random samples of migrated data
   - Verify field mappings
   - Check enum conversions

3. **Referential Integrity**
   - Verify foreign key relationships
   - Check for orphaned records
   - Validate country/bank mappings

### Step 5: Post-Migration Tasks

1. **Update Sequences**
   ```sql
   -- PostgreSQL sequences need to be updated after migration
   SELECT setval('senders_id_seq', (SELECT MAX(id) FROM senders));
   SELECT setval('transactions_id_seq', (SELECT MAX(id) FROM transactions));
   SELECT setval('banks_id_seq', (SELECT MAX(id) FROM banks));
   ```

2. **Rebuild Indexes**
   ```sql
   REINDEX DATABASE moneyfex_db;
   ```

3. **Update Statistics**
   ```sql
   ANALYZE;
   ```

## Migration Tool Usage

The migration tool (`DataMigrationService`) provides:

1. **Incremental Migration**: Migrate data in batches
2. **Resume Capability**: Resume from last checkpoint
3. **Validation**: Automatic data validation
4. **Logging**: Comprehensive migration logs
5. **Rollback**: Ability to rollback if needed

### Configuration

```json
{
  "MigrationSettings": {
    "SourceConnectionString": "Server=DESKTOP-AID3TE5;Database=MoneyFexDB;User ID=sa;Password=riddhasoft",
    "TargetConnectionString": "Host=localhost;Port=5432;Database=moneyfex_db;Username=postgres;Password=technofex",
    "BatchSize": 1000,
    "EnableValidation": true,
    "EnableLogging": true,
    "ResumeFromCheckpoint": false
  }
}
```

### Running Migration

```bash
# Full migration
dotnet run --project MigrationTool -- --mode full

# Incremental migration
dotnet run --project MigrationTool -- --mode incremental --batch-size 500

# Validation only
dotnet run --project MigrationTool -- --mode validate
```

## Troubleshooting

### Common Issues

1. **Connection Timeout**
   - Increase timeout in connection string
   - Check network connectivity
   - Verify firewall rules

2. **Data Type Mismatches**
   - Check enum mappings
   - Verify date/time formats
   - Handle NULL values

3. **Foreign Key Violations**
   - Migrate reference data first
   - Check for missing parent records
   - Handle orphaned records

4. **Performance Issues**
   - Reduce batch size
   - Add indexes on source database
   - Use parallel processing for independent tables

### Error Handling

The migration tool includes:
- Automatic retry for transient errors
- Detailed error logging
- Checkpoint creation for resume capability
- Data validation reports

## Rollback Plan

If migration fails:

1. **Stop Migration Process**
   - Cancel running migration
   - Save current checkpoint

2. **Restore Target Database**
   ```sql
   DROP DATABASE moneyfex_db;
   CREATE DATABASE moneyfex_db;
   ```

3. **Fix Issues**
   - Review error logs
   - Correct data issues in source
   - Update migration configuration

4. **Resume Migration**
   - Use checkpoint to resume
   - Or start fresh with fixes

## Post-Migration Verification

### Data Integrity Checks

```sql
-- Verify sender counts
SELECT 
    (SELECT COUNT(*) FROM FaxerInformation WHERE IsDeleted = 0) as LegacyActive,
    (SELECT COUNT(*) FROM senders WHERE is_active = true) as NewActive;

-- Verify transaction counts
SELECT 
    (SELECT COUNT(*) FROM BankAccountDeposit) as LegacyBankDeposits,
    (SELECT COUNT(*) FROM bank_account_deposits) as NewBankDeposits;

-- Verify amounts match
SELECT 
    SUM(SendingAmount) as LegacyTotal,
    (SELECT SUM(sending_amount) FROM transactions) as NewTotal
FROM BankAccountDeposit;
```

### Business Logic Validation

1. **Transaction Totals**
   - Verify sum of all transactions matches
   - Check fee calculations
   - Validate exchange rates

2. **User Relationships**
   - Verify sender-transaction links
   - Check recipient associations
   - Validate staff assignments

3. **Reference Data**
   - Verify all countries migrated
   - Check bank mappings
   - Validate wallet operators

## Best Practices

1. **Test First**: Always test migration on a copy of production data
2. **Backup**: Create full backups before migration
3. **Incremental**: Use incremental migration for large datasets
4. **Validate**: Run validation checks after each phase
5. **Monitor**: Monitor system resources during migration
6. **Document**: Document any manual interventions
7. **Verify**: Thoroughly verify data after migration

## Support

For issues or questions:
1. Check migration logs in `logs/migration.log`
2. Review error reports in `reports/`
3. Consult this guide for common issues
4. Contact development team for assistance

## Appendix

### A. SQL Scripts

See `MoneyFex.Modular/Database/MigrationScripts/` for:
- Data extraction scripts
- Transformation queries
- Validation scripts

### B. Configuration Files

See `MoneyFex.Modular/MoneyFex.Infrastructure/MigrationTool/` for:
- Migration configuration
- Connection string templates
- Enum mapping definitions

### C. Logs and Reports

Migration generates:
- `logs/migration.log`: Detailed migration log
- `reports/validation-report.json`: Data validation report
- `reports/migration-summary.json`: Migration summary

