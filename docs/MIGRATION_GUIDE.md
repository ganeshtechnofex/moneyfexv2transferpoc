# Database Migration Guide

## Overview

This guide explains the database schema changes and migration process from the legacy system to the new normalized PostgreSQL schema.

## Schema Changes

### 1. Normalization

#### Before (Legacy)
- Each transaction type had its own table with all fields
- Redundant fields across tables
- Inconsistent naming conventions

#### After (New)
- Base `transactions` table with common fields
- Transaction-specific tables for unique fields
- Consistent naming and structure

### 2. Key Improvements

1. **Base Transaction Table**
   - All common transaction fields in one place
   - Consistent status and module tracking
   - Unified payment and compliance information

2. **Transaction-Specific Tables**
   - Only unique fields per transaction type
   - Foreign key relationship to base transaction
   - Cleaner, more maintainable structure

3. **Reference Tables**
   - Normalized country information
   - Separate bank and wallet operator tables
   - Consistent sender information

4. **Auxiliary Agent Tables**
   - Separate override tables
   - Clear relationship to transactions
   - Maintains audit trail

## Migration Steps

### Step 1: Backup Existing Database

```sql
-- Backup existing database
pg_dump -U postgres legacy_moneyfex_db > backup.sql
```

### Step 2: Create New Database

```sql
CREATE DATABASE moneyfex_db;
```

### Step 3: Run Schema Script

```bash
psql -U postgres -d moneyfex_db -f Database/Schema/01_CreateDatabase.sql
```

### Step 4: Data Migration

Create a migration script to transfer data from legacy tables:

```sql
-- Example: Migrate BankAccountDeposit data
INSERT INTO transactions (
    transaction_date, receipt_no, sender_id, 
    sending_country_code, receiving_country_code,
    sending_currency, receiving_currency,
    sending_amount, receiving_amount, fee, total_amount,
    exchange_rate, payment_reference, sender_payment_mode,
    transaction_module, status, api_service, transfer_reference,
    recipient_id, is_compliance_needed, is_compliance_approved,
    compliance_approved_by, compliance_approved_at,
    paying_staff_id, updated_by_staff_id
)
SELECT 
    TransactionDate, ReceiptNo, SenderId,
    SendingCountry, ReceivingCountry,
    SendingCurrency, ReceivingCurrency,
    SendingAmount, ReceivingAmount, Fee, TotalAmount,
    ExchangeRate, PaymentReference, 
    CASE SenderPaymentMode 
        WHEN 'Card' THEN 'Card'::payment_mode
        WHEN 'BankAccount' THEN 'BankAccount'::payment_mode
        ELSE 'Card'::payment_mode
    END,
    CASE PaidFromModule
        WHEN 0 THEN 'Sender'::transaction_module
        WHEN 3 THEN 'Agent'::transaction_module
        WHEN 4 THEN 'AdminStaff'::transaction_module
        ELSE 'Sender'::transaction_module
    END,
    CASE Status
        WHEN 0 THEN 'InProgress'::transaction_status
        WHEN 1 THEN 'InProgress'::transaction_status
        WHEN 2 THEN 'Cancelled'::transaction_status
        WHEN 3 THEN 'Paid'::transaction_status
        WHEN 5 THEN 'Failed'::transaction_status
        WHEN 6 THEN 'PaymentPending'::transaction_status
        WHEN 7 THEN 'IdCheckInProgress'::transaction_status
        WHEN 10 THEN 'Abnormal'::transaction_status
        WHEN 11 THEN 'FullRefund'::transaction_status
        WHEN 12 THEN 'PartialRefund'::transaction_status
        ELSE 'InProgress'::transaction_status
    END,
    CASE Apiservice
        WHEN 0 THEN 'VGG'::api_service
        WHEN 1 THEN 'TransferZero'::api_service
        WHEN 2 THEN 'EmergentApi'::api_service
        WHEN 3 THEN 'MTN'::api_service
        WHEN 4 THEN 'Zenith'::api_service
        WHEN 9 THEN 'Magma'::api_service
        ELSE 'Wari'::api_service
    END,
    TransferReference, RecipientId,
    IsComplianceNeededForTrans, IsComplianceApproved,
    ComplianceApprovedBy, ComplianceApprovedDate,
    PayingStaffId, UpdateByStaffId
FROM legacy_bank_account_deposit;

-- Insert into bank_account_deposits
INSERT INTO bank_account_deposits (
    transaction_id, bank_id, bank_name, bank_code,
    receiver_account_no, receiver_name, receiver_city,
    is_manual_deposit, is_manual_approval_needed,
    is_manually_approved, is_europe_transfer,
    is_transaction_duplicated, duplicate_transaction_receipt_no
)
SELECT 
    t.id, bd.BankId, bd.BankName, bd.BankCode,
    bd.ReceiverAccountNo, bd.ReceiverName, bd.ReceiverCity,
    bd.IsManualDeposit, bd.IsManualApproveNeeded,
    bd.ManuallyApproved, bd.IsEuropeTransfer,
    bd.IsTransactionDuplicated, bd.DuplicateTransactionReceiptNo
FROM legacy_bank_account_deposit bd
INNER JOIN transactions t ON t.receipt_no = bd.ReceiptNo;
```

### Step 5: Verify Data

```sql
-- Check transaction counts
SELECT COUNT(*) FROM transactions;
SELECT COUNT(*) FROM bank_account_deposits;
SELECT COUNT(*) FROM mobile_money_transfers;
SELECT COUNT(*) FROM cash_pickups;
SELECT COUNT(*) FROM kiibank_transfers;

-- Verify relationships
SELECT COUNT(*) 
FROM transactions t
LEFT JOIN bank_account_deposits bd ON t.id = bd.transaction_id
WHERE bd.transaction_id IS NULL AND t.receipt_no LIKE 'BD%';
```

## Field Mappings

### Transaction Status Mapping

| Legacy Status | New Status Enum |
|--------------|-----------------|
| 0, 1, 4, 9, 13 | InProgress |
| 2 | Cancelled |
| 3 | Paid |
| 5 | Failed |
| 6 | PaymentPending |
| 7 | IdCheckInProgress |
| 8 | InProgress (with note) |
| 10 | Abnormal |
| 11 | FullRefund |
| 12 | PartialRefund |

### Module Mapping

| Legacy PaidFromModule | New TransactionModule |
|----------------------|----------------------|
| 0 | Sender |
| 1, 2, 5 | Sender (default) |
| 3 | Agent |
| 4 | AdminStaff |

### API Service Mapping

| Legacy Apiservice | New ApiService |
|------------------|----------------|
| 0 | VGG |
| 1 | TransferZero |
| 2 | EmergentApi |
| 3 | MTN |
| 4 | Zenith |
| 9 | Magma |
| NULL/Other | Wari |

## Removed Fields

The following fields were removed as they were not used in transaction processing:

- Unused audit fields
- Redundant status fields
- Deprecated payment method fields
- Legacy integration fields

## Indexes

The new schema includes optimized indexes:

- Transaction receipt numbers (unique)
- Transaction dates (for date range queries)
- Sender IDs (for sender-based queries)
- Status (for status filtering)
- Payment references (for search)

## Performance Considerations

1. **Pagination**: All list endpoints support pagination
2. **Indexes**: Strategic indexes on frequently queried fields
3. **Foreign Keys**: Proper foreign key constraints for data integrity
4. **Enums**: PostgreSQL enums for better performance and type safety

## Rollback Plan

If migration fails:

1. Keep legacy database intact
2. New database can be dropped and recreated
3. Migration scripts can be rerun after fixes
4. No data loss from legacy system

## Testing

After migration:

1. Verify all transaction types are migrated
2. Check relationships are intact
3. Test API endpoints
4. Verify auxiliary agent overrides
5. Check reinitialize transactions

## Support

For migration issues, refer to:
- Database logs
- Application logs
- Migration script output

