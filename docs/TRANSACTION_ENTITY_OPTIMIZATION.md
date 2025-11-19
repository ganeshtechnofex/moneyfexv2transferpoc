# Transaction Entity Optimization Summary

## Overview
This document summarizes the comprehensive optimization of transaction-related entities based on deep analysis of the legacy project. All properties have been preserved to ensure no data loss during ETL migration.

## Optimization Strategy

### 1. **Normalization Approach**
- **Base Transaction Entity**: Contains all common properties used across ALL transaction types
- **Detail Entities**: Contain only transaction-type-specific properties
- **Result**: Eliminates duplication, reduces storage, improves maintainability

### 2. **Property Analysis**

#### Common Properties (Moved to Transaction Base)
These properties were found in ALL transaction types (BankAccountDeposit, MobileMoneyTransfer, CashPickup, KiiBankTransfer):

- `PayingStaffName` - Name of staff who processed payment
- `AgentCommission` - Commission for agent transactions
- `ExtraFee` - Additional fees
- `Margin` - Exchange rate margin
- `MFRate` - MoneyFex rate
- `TransferZeroSenderId` - TransferZero API sender ID
- `ReasonForTransfer` - Reason for the transfer
- `CardProcessorApi` - Card processor used
- `IsFromMobile` - Flag indicating mobile app transaction
- `TransactionUpdateDate` - Date transaction was last updated

#### Transaction-Specific Properties

**BankAccountDeposit:**
- Bank information (BankId, BankName, BankCode)
- Receiver details (ReceiverAccountNo, ReceiverName, ReceiverCity, ReceiverCountry, ReceiverMobileNo)
- Bank deposit flags (IsManualDeposit, IsManualApprovalNeeded, IsManuallyApproved, IsEuropeTransfer)
- Duplication tracking (IsTransactionDuplicated, DuplicateTransactionReceiptNo)
- Business flag (IsBusiness)
- Payment confirmation (HasMadePaymentToBankAccount)
- Transaction description

**MobileMoneyTransfer:**
- Wallet operator (WalletOperatorId)
- Mobile number (PaidToMobileNo)
- Receiver details (ReceiverName, ReceiverCity)

**CashPickup:**
- MFCN (MoneyFex Control Number)
- Recipient information (RecipientId, NonCardReceiverId)
- Identity card (RecipientIdentityCardId, RecipientIdentityCardNumber)
- Admin approval (IsApprovedByAdmin)
- Agent staff name

**KiiBankTransfer:**
- Account information (AccountNo, AccountOwnerName, AccountHolderPhoneNo)
- Bank information (BankId, BankBranchId, BankBranchCode)
- Transaction reference

## Entity Changes

### Transaction Entity (Base)
**Added Properties:**
- `PayingStaffName` (string?, max 200)
- `AgentCommission` (decimal?, precision 18,2)
- `ExtraFee` (decimal?, precision 18,2)
- `Margin` (decimal?, precision 18,2)
- `MFRate` (decimal?, precision 18,6)
- `TransferZeroSenderId` (string?, max 100)
- `ReasonForTransfer` (ReasonForTransfer?, enum)
- `CardProcessorApi` (CardProcessorApi?, enum)
- `IsFromMobile` (bool)
- `TransactionUpdateDate` (DateTime?)

**Enums Created:**
- `ReasonForTransfer`: Non, ForEducation, ToPayforServices, ForCharityDonation, ForanInvestment, ForFamilySupport, SendingToMyself
- `CardProcessorApi`: Select, TrustPayment, T365, WorldPay

### BankAccountDeposit Entity
**Added Properties:**
- `ReceiverCountry` (string?, max 3)
- `ReceiverMobileNo` (string?, max 50)
- `RecipientId` (int?)
- `IsBusiness` (bool)
- `HasMadePaymentToBankAccount` (bool)
- `TransactionDescription` (string?, max 500)

**Removed (Moved to Transaction):**
- All financial metadata (AgentCommission, ExtraFee, Margin, MFRate)
- TransferZeroSenderId
- PayingStaffName
- ReasonForTransfer
- CardProcessorApi
- IsFromMobile
- TransactionUpdateDate

### MobileMoneyTransfer Entity
**Added Properties:**
- `RecipientId` (int?)

**Removed (Moved to Transaction):**
- All financial metadata
- TransferZeroSenderId
- PayingStaffName
- ReasonForTransfer
- CardProcessorApi
- IsFromMobile

### CashPickup Entity
**Added Properties:**
- `RecipientIdentityCardId` (int?)
- `RecipientIdentityCardNumber` (string?, max 100)
- `IsApprovedByAdmin` (bool)
- `AgentStaffName` (string?, max 200)

**Removed (Moved to Transaction):**
- All financial metadata
- TransferZeroSenderId
- ReasonForTransfer
- CardProcessorApi
- IsFromMobile

### KiiBankTransfer Entity
**Added Properties:**
- `AccountOwnerName` (string?, max 200)
- `AccountHolderPhoneNo` (string?, max 50)
- `BankId` (int?)
- `BankBranchId` (int?)
- `BankBranchCode` (string?, max 50)
- `TransactionReference` (string?, max 100)

## Database Configuration Updates

### Transaction Table
- Added enum conversions for ReasonForTransfer and CardProcessorApi
- Added max length constraints for string properties
- Added precision for decimal properties

### Detail Tables
- Added foreign key relationships to Recipient where applicable
- Added max length constraints for new string properties
- Configured proper cascade/restrict behaviors

## Migration Notes

### Properties Preserved for ETL
All legacy properties have been preserved in the new structure:
- Properties moved to Transaction base are accessible via `Transaction.PropertyName`
- Transaction-specific properties remain in detail entities
- All foreign key relationships maintained
- All indexes and constraints preserved

### Data Migration Path
1. **Transaction Base**: Map common properties from all legacy transaction types
2. **BankAccountDeposit**: Map bank-specific properties from legacy BankAccountDeposit
3. **MobileMoneyTransfer**: Map mobile-specific properties from legacy MobileMoneyTransfer
4. **CashPickup**: Map cash pickup properties from legacy FaxingNonCardTransaction
5. **KiiBankTransfer**: Map KiiBank properties from legacy KiiBank entities

## Benefits

1. **Reduced Duplication**: Common properties stored once in Transaction table
2. **Improved Query Performance**: Single table for common fields reduces joins
3. **Easier Maintenance**: Changes to common properties in one place
4. **Better Normalization**: Follows database normalization best practices
5. **ETL Safety**: All properties preserved, no data loss risk
6. **Type Safety**: Enums provide compile-time safety
7. **Scalability**: Easy to add new transaction types

## Next Steps

1. Create migration: `AddMissingTransactionProperties`
2. Test migration on development database
3. Update ETL scripts to map to new structure
4. Update repository/service layers to use new structure
5. Update stored procedures/queries to use new structure

