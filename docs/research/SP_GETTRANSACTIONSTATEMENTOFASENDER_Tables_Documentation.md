# Stored Procedure: SP_GETTRANSACTIONSTATEMENTOFASENDER - Tables Documentation

## Overview
This document provides a comprehensive list of all tables and their properties used in the stored procedure `SP_GETTRANSACTIONSTATEMENTOFASENDER`. This procedure retrieves transaction statements for a sender across three transaction types: Bank Deposit, Mobile Wallet, and Cash Pickup.

---

## Transaction Service Types

### TransferType Values:
- **1** = Bank Deposit
- **2** = Mobile Wallet
- **3** = Cash Pickup

---

## Permanent Tables

### 1. BankAccountDeposit
**Purpose:** Stores bank account deposit transactions

**Service Type:** Bank Deposit (TransferType = 1)

**Join Type:** Primary table in UNION query

**Properties Used:**
- `Id` - Transaction identifier
- `TransactionDate` - Date of the transaction
- `TotalAmount` - Total transaction amount
- `Status` - Transaction status (0-13)
- `SendingCurrency` - Currency code for sending country
- `ReceivingCurrency` - Currency code for receiving country
- `ReceiverName` - Name of the receiver
- `ReceiptNo` - Receipt number/transaction identifier
- `Fee` - Transaction fee
- `SenderId` - Foreign key to FaxerInformation
- `SendingCountry` - Country code for sending country
- `ReceivingCountry` - Country code for receiving country

**Join Conditions:**
- `bankDeposit.SenderId = senderInfo.Id` (INNER JOIN)
- `bankDeposit.SendingCountry = sendingCountry.CountryCode` (INNER JOIN)
- `bankDeposit.ReceivingCountry = receivingCountry.CountryCode` (INNER JOIN)
- `bankDeposit.ReceiptNo = secureTradingLog.orderreference` (LEFT JOIN)

---

### 2. MobileMoneyTransfer
**Purpose:** Stores mobile money transfer transactions

**Service Type:** Mobile Wallet (TransferType = 2)

**Join Type:** Primary table in UNION query

**Properties Used:**
- `Id` - Transaction identifier
- `TransactionDate` - Date of the transaction
- `TotalAmount` - Total transaction amount
- `Status` - Transaction status (0-10)
- `SendingCurrency` - Currency code for sending country
- `ReceivingCurrency` - Currency code for receiving country
- `ReceiverName` - Name of the receiver
- `ReceiptNo` - Receipt number/transaction identifier
- `Fee` - Transaction fee
- `SenderId` - Foreign key to FaxerInformation
- `SendingCountry` - Country code for sending country
- `ReceivingCountry` - Country code for receiving country

**Join Conditions:**
- `mobile.SenderId = senderInfo.Id` (INNER JOIN)
- `mobile.SendingCountry = sendingCountry.CountryCode` (INNER JOIN)
- `mobile.ReceivingCountry = receivingCountry.CountryCode` (INNER JOIN)
- `mobile.ReceiptNo = secureTradingLog.orderreference` (LEFT JOIN)

---

### 3. FaxingNonCardTransaction
**Purpose:** Stores cash pickup/non-card transactions

**Service Type:** Cash Pickup (TransferType = 3)

**Join Type:** Primary table in UNION query

**Properties Used:**
- `Id` - Transaction identifier
- `TransactionDate` - Date of the transaction
- `TotalAmount` - Total transaction amount
- `FaxingStatus` - Transaction status (0-11)
- `SendingCurrency` - Currency code for sending country
- `ReceivingCurrency` - Currency code for receiving country
- `ReceiptNumber` - Receipt number/transaction identifier
- `FaxingFee` - Transaction fee
- `SenderId` - Foreign key to FaxerInformation
- `SendingCountry` - Country code for sending country
- `ReceivingCountry` - Country code for receiving country
- `NonCardRecieverId` - Foreign key to ReceiversDetails

**Join Conditions:**
- `cash.SenderId = senderInfo.Id` (INNER JOIN)
- `cash.SendingCountry = sendingCountry.CountryCode` (INNER JOIN)
- `cash.ReceivingCountry = receivingCountry.CountryCode` (INNER JOIN)
- `cash.ReceiptNumber = secureTradingLog.orderreference` (LEFT JOIN)
- `cashPickup.NonCardRecieverId = receiverDetails.Id` (LEFT JOIN)

---

### 4. SecureTradingApiResponseTransactionLog
**Purpose:** Stores Secure Trading API response transaction logs (card payment information)

**Service Type:** Used across all transaction types (Bank, Mobile, Cash)

**Join Type:** LEFT JOIN (optional - for card payment details)

**Join Conditions:**
- `bankDeposit.ReceiptNo = secureTradingLog.orderreference`
- `mobile.ReceiptNo = secureTradingLog.orderreference`
- `cashPickup.ReceiptNumber = secureTradingLog.orderreference`

**Properties Used:**
- `orderreference` - Order reference matching transaction receipt number
- `issuer` - Card issuer name
- `maskedpan` - Masked primary account number (last 4 digits extracted)

---

### 5. FaxerInformation
**Purpose:** Stores sender/customer information

**Service Type:** Used across all transaction types (Bank, Mobile, Cash)

**Join Type:** INNER JOIN (as `senderInfo`)

**Join Conditions:**
- `bankDeposit.SenderId = senderInfo.Id`
- `mobile.SenderId = senderInfo.Id`
- `cashPickup.SenderId = senderInfo.Id`

**Properties Used:**
- `Id` - Sender identifier
- `Address1` - Billing address of the sender

---

### 6. Country
**Purpose:** Stores country information (used twice for sending and receiving countries)

**Service Type:** Used across all transaction types (Bank, Mobile, Cash)

**Join Type:** INNER JOIN (as `sendingCountry` and `receivingCountry`)

**Join Conditions:**
- `bankDeposit.SendingCountry = sendingCountry.CountryCode`
- `bankDeposit.ReceivingCountry = receivingCountry.CountryCode`
- `mobile.SendingCountry = sendingCountry.CountryCode`
- `mobile.ReceivingCountry = receivingCountry.CountryCode`
- `cashPickup.SendingCountry = sendingCountry.CountryCode`
- `cashPickup.ReceivingCountry = receivingCountry.CountryCode`

**Properties Used:**
- `CountryCode` - Country code identifier
- `Currency` - Currency code

---

### 7. ReceiversDetails
**Purpose:** Stores receiver information for cash pickup transactions

**Service Type:** Cash Pickup (TransferType = 3)

**Join Type:** LEFT JOIN (as `receiverDetails`)

**Join Condition:** `cashPickup.NonCardRecieverId = receiverDetails.Id`

**Properties Used:**
- `Id` - Receiver identifier
- `FullName` - Full name of the receiver

---

## Temporary Tables

### 1. ##bankDeposit (Global Temporary Table)
**Purpose:** Intermediate storage for filtered transactions across all three types

**Created By:** Dynamic SQL execution

**Structure:**
- `Id` - Transaction ID
- `TransactionDate` - Transaction date
- `TransferType` - Transaction type (1=Bank, 2=Mobile, 3=Cash)

**Lifecycle:** Created and dropped within the procedure execution

**Data Sources:**
- BankAccountDeposit (TransferType = 1)
- MobileMoneyTransfer (TransferType = 2)
- FaxingNonCardTransaction (TransferType = 3)

---

### 2. #finalData (Local Temporary Table)
**Purpose:** Final filtered and paginated data before final result set

**Created By:** `SELECT INTO` statement

**Structure:**
- Inherits all columns from `##bankDeposit` table
- Contains paginated results based on `@PageNo` and `@PageSize` parameters
- Ordered by `TransactionDate DESC`

**Lifecycle:** Created and dropped within the procedure execution

---

## Table Relationships Summary

### Bank Deposit Transactions (TransferType = 1)
```
BankAccountDeposit (Primary)
├── INNER JOIN FaxerInformation (via SenderId)
├── INNER JOIN Country [sendingCountry] (via SendingCountry)
├── INNER JOIN Country [receivingCountry] (via ReceivingCountry)
└── LEFT JOIN SecureTradingApiResponseTransactionLog (via ReceiptNo)
```

### Mobile Wallet Transactions (TransferType = 2)
```
MobileMoneyTransfer (Primary)
├── INNER JOIN FaxerInformation (via SenderId)
├── INNER JOIN Country [sendingCountry] (via SendingCountry)
├── INNER JOIN Country [receivingCountry] (via ReceivingCountry)
└── LEFT JOIN SecureTradingApiResponseTransactionLog (via ReceiptNo)
```

### Cash Pickup Transactions (TransferType = 3)
```
FaxingNonCardTransaction (Primary)
├── INNER JOIN FaxerInformation (via SenderId)
├── INNER JOIN Country [sendingCountry] (via SendingCountry)
├── INNER JOIN Country [receivingCountry] (via ReceivingCountry)
├── LEFT JOIN ReceiversDetails (via NonCardRecieverId)
└── LEFT JOIN SecureTradingApiResponseTransactionLog (via ReceiptNumber)
```

---

## Status Code Mappings

### BankAccountDeposit.Status Values:
- `0` = 'In Progress'
- `1` = 'In Progress'
- `2` = 'Cancelled'
- `3` = 'Paid'
- `4` = 'In progress'
- `5` = 'Failed'
- `6` = 'Payment Pending'
- `7` = 'In progress (ID Check)'
- `8` = 'In progress (MoneyFex Bank Deposit)'
- `9` = 'In progress'
- `10` = 'Abnormal'
- `11` = 'Full Refund'
- `12` = 'Partial Refund'
- `13` = 'In Progress'
- `Default` = 'In Progress'

### MobileMoneyTransfer.Status Values:
- `0` = 'Failed'
- `1` = 'In Progress'
- `2` = 'Paid'
- `3` = 'Cancelled'
- `4` = 'Payment Pending'
- `5` = 'In Progress (ID Check)'
- `6` = 'In progress'
- `7` = 'In progress'
- `8` = 'In progress'
- `9` = 'Refund'
- `10` = 'Refund'
- `Default` = 'In Progress'

### FaxingNonCardTransaction.FaxingStatus Values:
- `0` = 'Not Received'
- `1` = 'Received'
- `2` = 'Cancelled'
- `3` = 'Refunded'
- `4` = 'In Progress'
- `5` = 'Completed'
- `6` = 'Payment Pending'
- `7` = 'In Progress (ID Check)'
- `8` = 'In progress'
- `9` = 'Refund'
- `10` = 'Refund'
- `11` = 'In Progress'
- `Default` = 'In Progress'

---

## Result Set Columns

The final result set includes the following columns (common across all transaction types):

- `TotalCount` - Total number of records matching the criteria
- `Id` - Transaction identifier
- `TranasactionId` - Transaction ID (same as Id)
- `Amount` - Total transaction amount
- `BillingAddress` - Sender's billing address (from FaxerInformation.Address1)
- `CardIssuer` - Card issuer name (from SecureTradingApiResponseTransactionLog, only for Bank and Cash)
- `Last4Digits` - Last 4 digits of card (from SecureTradingApiResponseTransactionLog.maskedpan)
- `Status` - Human-readable transaction status
- `Date` - Formatted transaction date (convert to varchar, format 106)
- `DateTime` - Transaction date/time
- `CountryCurrency` - Sending currency
- `ReceivingCurrency` - Receiving currency
- `Receiver` - Receiver name
- `ReceiptNo` - Receipt number
- `Fee` - Transaction fee
- `TransactionType` - Type of transaction ('Bank Deposit', 'Mobile Wallet', or 'Cash Pickup')

---

## Filtering Parameters

The procedure supports filtering by:
- `@SenderId` - Filter by specific sender ID (required, default: 1020)
- `@Year` - Filter by transaction year (optional, default: 0)
- `@ReceiptNo` - Filter by specific receipt number (optional, default: '')
- `@PageNo` - Page number for pagination (default: 1)
- `@PageSize` - Number of records per page (default: 10)

---

## Query Execution Flow

1. **Dynamic Query Building:**
   - Builds query for BankAccountDeposit transactions
   - Appends UNION ALL for MobileMoneyTransfer transactions
   - Appends UNION ALL for FaxingNonCardTransaction transactions
   - Applies filters based on @SenderId, @ReceiptNo, and @Year parameters

2. **Temporary Table Creation:**
   - Executes dynamic query to populate `##bankDeposit` global temp table
   - Counts total records

3. **Pagination:**
   - Creates `#finalData` local temp table with paginated results
   - Uses OFFSET/FETCH for pagination

4. **Final Result Set:**
   - UNION of three separate queries (Bank, Mobile, Cash)
   - Each query joins with `#finalData` to get only paginated records
   - Results ordered by DateTime DESC

5. **Cleanup:**
   - Drops temporary tables

---

## Total Count

**Permanent Tables:** 7
- BankAccountDeposit
- MobileMoneyTransfer
- FaxingNonCardTransaction
- SecureTradingApiResponseTransactionLog
- FaxerInformation
- Country (used twice but counted as one table)
- ReceiversDetails

**Temporary Tables:** 2
- ##bankDeposit (global temp table)
- #finalData (local temp table)

**Total Tables:** 9

---

## Service Type Distribution

### Bank Deposit Service (TransferType = 1)
- **Primary Table:** BankAccountDeposit
- **Related Tables:** FaxerInformation, Country (2x), SecureTradingApiResponseTransactionLog

### Mobile Wallet Service (TransferType = 2)
- **Primary Table:** MobileMoneyTransfer
- **Related Tables:** FaxerInformation, Country (2x), SecureTradingApiResponseTransactionLog

### Cash Pickup Service (TransferType = 3)
- **Primary Table:** FaxingNonCardTransaction
- **Related Tables:** FaxerInformation, Country (2x), ReceiversDetails, SecureTradingApiResponseTransactionLog

---

## Notes

1. The procedure uses dynamic SQL to build queries based on input parameters
2. Three different transaction types are combined using UNION ALL
3. Temporary tables are used for filtering and pagination across all transaction types
4. SecureTradingApiResponseTransactionLog provides optional card payment information
5. Country table is joined twice (sending and receiving) for all transaction types
6. Cash Pickup transactions have an additional join to ReceiversDetails for receiver information
7. The procedure returns a unified result set with consistent column structure across all transaction types
8. Pagination is applied before the final UNION to optimize performance

