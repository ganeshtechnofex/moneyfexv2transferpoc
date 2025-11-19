# Stored Procedure: SP_Get_KiiBankTransactionStatement - Tables Documentation

## Overview
This document provides a comprehensive list of all tables and their properties used in the stored procedure `SP_Get_KiiBankTransactionStatement`. This procedure retrieves KiiBank transfer transaction statements with extensive filtering capabilities.

---

## Transaction Service Type

### TransactionServiceType Value:
- **7** = KiiBank Transaction

### TransferType Value:
- **7** = KiiBank

---

## Permanent Tables

### 1. KiiBankTransfer (kiiBankTransfer)
**Purpose:** Primary table storing KiiBank transfer transactions

**Service Type:** KiiBank (TransferType = 7, TransactionServiceType = 7)

**Join Type:** Primary table in main query

**Properties Used:**
- `Id` - Transaction identifier
- `TransactionDate` - Date of the transaction
- `Status` - Transaction status (0-10)
- `PaidFromModule` - Type of user who performed the transaction (0-5)
- `Fee` - Transaction fee
- `ReceiptNo` - Receipt number/transaction identifier
- `TransactionReference` - Transaction reference number
- `PaymentReference` - Payment reference number
- `SenderId` - Foreign key to FaxerInformation
- `SendingCountry` - Country code for sending country
- `ReceivingCountry` - Country code for receiving country
- `SendingCurrency` - Currency code for sending country
- `ReceivingCurrency` - Currency code for receiving country
- `ReceiverName` - Name of the receiver
- `AccountNo` - Account number of the receiver
- `SendingAmount` - Amount sent
- `ReceivingAmount` - Amount received
- `TotalAmount` - Total transaction amount
- `ExchangeRate` - Exchange rate applied
- `SenderPaymentMode` - Payment mode used by sender
- `IsComplianceApproved` - Flag indicating if compliance approved
- `IsComplianceNeededForTrans` - Flag indicating if compliance needed
- `RecipientId` - Recipient identifier
- `UpdateByStaffId` - Staff ID who last updated the transaction
- `PayingStaffId` - Staff ID who processed payment

**Join Conditions:**
- `kiiBankTransfer.SenderId = senderInfo.Id` (INNER JOIN)
- `kiiBankTransfer.UpdateByStaffId = staffInfo.Id` (LEFT JOIN)
- `kiiBankTransfer.SendingCountry = sendingCountry.CountryCode` (INNER JOIN)
- `kiiBankTransfer.ReceivingCountry = receivingCountry.CountryCode` (INNER JOIN)
- `kiiBankTransfer.Id = creditDebitCardInfo.CardTransactionId` (LEFT JOIN, with creditDebitCardInfo.TransferType = 3)

---

### 2. FaxerInformation (senderInfo)
**Purpose:** Stores sender/customer information

**Service Type:** KiiBank

**Join Type:** INNER JOIN

**Join Condition:** `kiiBankTransfer.SenderId = senderInfo.Id`

**Properties Used:**
- `Id` - Sender identifier
- `FirstName` - First name of sender
- `MiddleName` - Middle name of sender
- `LastName` - Last name of sender
- `Email` - Email address of sender
- `AccountNo` - Account number (MFCode)
- `PhoneNumber` - Phone number of sender
- `IsBusiness` - Flag indicating if sender is a business

---

### 3. StaffInformation (staffInfo)
**Purpose:** Stores staff information for transaction updates

**Service Type:** KiiBank

**Join Type:** LEFT JOIN

**Join Condition:** `kiiBankTransfer.UpdateByStaffId = staffInfo.Id`

**Properties Used:**
- `Id` - Staff identifier
- `FirstName` - First name of staff member
- `MiddleName` - Middle name of staff member
- `LastName` - Last name of staff member

---

### 4. FaxerLogin (senderLogin)
**Purpose:** Stores sender login information

**Service Type:** KiiBank

**Join Type:** LEFT JOIN

**Join Condition:** `senderInfo.Id = senderLogin.FaxerId`

**Properties Used:**
- `FaxerId` - Foreign key to FaxerInformation.Id
- `IsActive` - Flag indicating if sender account is active

---

### 5. Country (sendingCountry, receivingCountry)
**Purpose:** Stores country information (used twice for sending and receiving countries)

**Service Type:** KiiBank

**Join Type:** INNER JOIN (as `sendingCountry` and `receivingCountry`)

**Join Conditions:**
- `kiiBankTransfer.SendingCountry = sendingCountry.CountryCode`
- `kiiBankTransfer.ReceivingCountry = receivingCountry.CountryCode`

**Properties Used:**
- `CountryCode` - Country code identifier
- `CountryName` - Name of the country
- `Currency` - Currency code
- `CurrencySymbol` - Currency symbol

---

### 6. CardTopUpCreditDebitInformation (creditDebitCardInfo)
**Purpose:** Stores credit/debit card information for card payments

**Service Type:** KiiBank

**Join Type:** LEFT JOIN

**Join Conditions:**
- `creditDebitCardInfo.TransferType = 3`
- `kiiBankTransfer.Id = creditDebitCardInfo.CardTransactionId`

**Properties Used:**
- `TransferType` - Transfer type (must be 3 for KiiBank transfers)
- `CardTransactionId` - Foreign key to transaction ID
- `CardNumber` - Card number (masked)

---

## Temporary Tables

### 1. ##Mobile (Global Temporary Table)
**Purpose:** Intermediate storage for filtered KiiBank transactions

**Created By:** Dynamic SQL execution (`SELECT INTO ##Mobile`)

**Structure:**
- `Id` - Transaction ID
- `TransactionDate` - Transaction date
- `TransferType` - Transaction type (always 7 for KiiBank)
- `TransactionServiceType` - Service type (always 7)
- `StatusName` - Human-readable status name
- `TransactionPerformedBy` - Who performed the transaction
- `HasFee` - Flag indicating if transaction has fee

**Lifecycle:** Created and dropped within the procedure execution

**Data Source:** KiiBankTransfer (with join to FaxerInformation)

**Filtering Applied:**
- Search string (ReceiptNo or TransactionReference)
- SenderId
- StaffId (UpdateByStaffId)
- SendingCountry
- ReceivingCountry
- SenderName (from FaxerInformation)
- ReceiverName (from KiiBankTransfer)
- SendingCurrency
- ReceivingCurrency
- SenderEmail
- MFCode (AccountNo)
- PhoneNumber
- Date range (FromDate, ToDate)
- Status (StatusName)
- SearchByStatus (StatusName)
- ResponsiblePerson (TransactionPerformedBy)
- TransactionWithAndWithoutFee (HasFee)
- PayoutProvider (commented out in code, not currently used)

---

### 2. #finalData (Local Temporary Table)
**Purpose:** Final filtered and paginated data before final result set

**Created By:** `SELECT INTO #finalData` statement

**Structure:**
- Inherits all columns from `##Mobile` table
- Contains paginated results based on `@PageNum` and `@PageSize` parameters
- Ordered by `TransactionDate DESC`

**Lifecycle:** Created and dropped within the procedure execution

**Join Condition:** `finaldata.TransferType = 7 AND finaldata.Id = kiiBankTransfer.Id`

---

## Status Code Mappings

### KiiBankTransfer.Status Values:
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

### PaidFromModule Values:
- `0` = 'Sender'
- `1` = '' (empty)
- `2` = '' (empty)
- `3` = 'Agent'
- `4` = 'Admin Staff'
- `5` = '' (empty)
- `Default` = '' (empty)

---

## Result Set Columns

The final result set includes the following columns:

- `TotalCount` - Total number of records matching the criteria
- `Id` - Transaction identifier
- `AccountNumber` - Receiver's account number (from AccountNo)
- `SenderTelephoneNo` - Sender's phone number (from senderInfo.PhoneNumber, default: '')
- `ReceiverName` - Name of receiver (from ReceiverName, default: 'No Name')
- `ReceiverCountry` - Name of receiving country (from receivingCountry.CountryName)
- `IsManualApprovalNeeded` - Always 0 (BIT)
- `HasFee` - Flag indicating if transaction has fee (1 if Fee > 0, else 0)
- `Status` - Always 5
- `statusOfMobileWallet` - Always 0
- `StatusofMobileTransfer` - Always 0
- `StatusOfBankDepoist` - Always 0
- `StatusOfKiiBank` - Transaction status code (from Status)
- `StatusName` - Human-readable status name
- `TransactionType` - Always 'KiiBank'
- `ReceivingCurrencySymbol` - Currency symbol for receiving country
- `ReceivingCurrrency` - Receiving currency code (from kiiBankTransfer or receivingCountry)
- `SendingCurrency` - Sending currency code (from kiiBankTransfer or sendingCountry)
- `SendingCurrencySymbol` - Currency symbol for sending country
- `Fee` - Transaction fee (from Fee)
- `GrossAmount` - Gross amount (from SendingAmount)
- `ReceivingAmount` - Receiving amount (from ReceivingAmount)
- `TotalAmount` - Total amount (from TotalAmount)
- `ExchangeRate` - Exchange rate (from ExchangeRate)
- `SenderPaymentMode` - Payment mode used by sender
- `CardNumber` - Card number (from creditDebitCardInfo.CardNumber, default: '')
- `Date` - Formatted transaction date (convert to varchar, format 106)
- `TransactionServiceType` - Always 7
- `TransactionDate` - Transaction date/time
- `Reference` - Payment reference (from PaymentReference, default: '')
- `PaymentReference` - Payment reference (from PaymentReference, default: '')
- `BankCode` - Always empty string
- `WalletName` - Always empty string
- `BankName` - Always empty string
- `TransactionIdentifier` - Receipt number (from ReceiptNo, default: '')
- `FaxerAccountNo` - Sender account number (from senderInfo.AccountNo)
- `FaxerCountry` - Sending country name (from sendingCountry.CountryName)
- `SenderName` - Full name of sender (concatenated from FirstName, MiddleName, LastName)
- `IsManualBankDeposit` - Always 0 (BIT)
- `senderId` - Sender ID
- `IsRetryAbleCountry` - Always 0 (BIT)
- `IsBusiness` - Flag indicating if sender is a business (from senderInfo.IsBusiness)
- `IsAbnormalTransaction` - Flag indicating abnormal transaction (1 if Status = 7, else 0)
- `IsEuropeTransfer` - Always 0 (BIT)
- `IsAwaitForApproval` - Compliance approval flag (calculated based on IsComplianceApproved and Status)
- `PaidFromModule` - Operating user type (from PaidFromModule)
- `AgentStaffId` - Staff ID who processed payment (from PayingStaffId)
- `TransferReference` - Transfer reference (from TransactionReference, default: '')
- `IsDuplicatedTransaction` - Always 0 (BIT)
- `DuplicatedTransactionReceiptNo` - Receipt number (from ReceiptNo)
- `RecipientId` - Recipient ID
- `SenderCountryCode` - Sending country code
- `SenderEmail` - Sender email (from senderInfo.Email, default: '')
- `ReceivingCountryCode` - Receiving country code
- `TransactionPerformedBy` - Who performed the transaction (mapped from PaidFromModule)
- `TransactionUpdatedById` - Staff ID who updated transaction (from UpdateByStaffId)
- `TransactionUpdatedByName` - Name of staff who updated transaction (concatenated from staffInfo FirstName, MiddleName, LastName)
- `PayoutType` - Always 'Automatic'
- `IsSenderActive` - Flag indicating if sender account is active (from senderLogin.IsActive)

**Note:** This procedure does NOT include the following columns that are present in other similar procedures:
- `ReInitializedReceiptNo`
- `IsReInitializedTransaction`
- `ReInitializeStaffName`
- `ReInitializedDateTime`
- `PayoutProviderName`
- `ApiService`

---

## Filtering Parameters

The procedure supports extensive filtering by the following parameters:

- `@senderId` - Filter by specific sender ID (default: 0)
- `@SendingCountry` - Filter by sending country code (default: '')
- `@ReceivingCountry` - Filter by receiving country code (default: '')
- `@SenderName` - Filter by sender name (partial match, default: '')
- `@SenderEmail` - Filter by sender email (default: '')
- `@DateRange` - Date range filter (default: '', not used in current implementation)
- `@FromDate` - Filter transactions from this date (format: YYYY/MM/DD, default: '')
- `@ToDate` - Filter transactions to this date (format: YYYY/MM/DD, default: '')
- `@ReceiverName` - Filter by receiver name (partial match, default: '')
- `@searchString` - Search by ReceiptNo or TransactionReference (default: '')
- `@Status` - Filter by status name (partial match, default: '')
- `@PhoneNumber` - Filter by sender phone number (default: '')
- `@SendingCurrency` - Filter by sending currency code (default: '')
- `@ReceivingCurrency` - Filter by receiving currency code (default: '')
- `@TransactionWithAndWithoutFee` - Filter by fee presence (BIT value as string, default: '')
- `@ResponsiblePerson` - Filter by who performed transaction (default: '')
- `@SearchByStatus` - Additional status filter (partial match, default: '')
- `@MFCode` - Filter by sender account number (default: '')
- `@PageNum` - Page number for pagination (default: 1)
- `@PageSize` - Number of records per page (default: 10000)
- `@IsBusiness` - Filter by business flag (default: 0, not used in current implementation)
- `@IsRegisteredByAuxAgent` - Filter by auxiliary agent registration (default: 0, not used in current implementation)
- `@PayoutProvider` - Filter by payout provider name (default: '', commented out in code, not currently used)
- `@StaffId` - Filter by staff ID who updated transaction (default: 0)

---

## Query Execution Flow

1. **Dynamic Query Building:**
   - Builds base query for KiiBankTransfer with join to FaxerInformation
   - Applies filters based on input parameters
   - Creates temporary table `##Mobile` with filtered results

2. **Additional Filtering:**
   - Applies additional filters on `##Mobile` table:
     - Status (StatusName)
     - SearchByStatus (StatusName)
     - ResponsiblePerson (TransactionPerformedBy)
     - TransactionWithAndWithoutFee (HasFee)
     - PayoutProvider (commented out, not currently used)

3. **Pagination:**
   - Creates `#finalData` local temp table with paginated results
   - Uses OFFSET/FETCH for pagination based on `@PageNum` and `@PageSize`
   - Ordered by `TransactionDate DESC`

4. **Final Result Set:**
   - Joins `#finalData` with KiiBankTransfer and all related tables
   - Retrieves comprehensive transaction details with all related information
   - Results ordered by `TransactionDate DESC`

5. **Cleanup:**
   - Drops temporary tables `##Mobile` and `#finalData`

---

## Table Relationships Summary

### KiiBank Transfer Transaction Flow
```
KiiBankTransfer (Primary)
├── INNER JOIN FaxerInformation (via SenderId)
│   └── LEFT JOIN FaxerLogin (via FaxerId)
├── INNER JOIN Country [sendingCountry] (via SendingCountry)
├── INNER JOIN Country [receivingCountry] (via ReceivingCountry)
├── LEFT JOIN StaffInformation (via UpdateByStaffId)
└── LEFT JOIN CardTopUpCreditDebitInformation (via Id, with TransferType = 3)
```

---

## Total Count

**Permanent Tables:** 6
- KiiBankTransfer
- FaxerInformation
- StaffInformation
- FaxerLogin
- Country (used twice but counted as one table)
- CardTopUpCreditDebitInformation

**Temporary Tables:** 2
- ##Mobile (global temp table)
- #finalData (local temp table)

**Total Tables:** 8

---

## Special Notes

1. **Dynamic SQL:** The procedure uses dynamic SQL to build the initial query based on input parameters, which allows for flexible filtering.

2. **No Auxiliary Agent Table:** Unlike other similar procedures (Bank and Cash), this procedure does NOT use an auxiliary agent detail table. All transaction values come directly from `KiiBankTransfer`.

3. **No Reinitialization Tracking:** This procedure does NOT track reinitialized transactions. The result set does not include reinitialization-related columns.

4. **Compliance Approval Logic:** The `IsAwaitForApproval` flag is calculated as:
   - `0` if `IsComplianceApproved = 1` OR `Status = 3` (Cancelled)
   - Otherwise, uses `IsComplianceNeededForTrans` value

5. **Card Payment Information:** Card details are retrieved from `CardTopUpCreditDebitInformation` only when `TransferType = 3`, which represents KiiBank transfer payment method type.

6. **Pagination:** Pagination is applied after initial filtering but before the final detailed join, optimizing performance by limiting the number of records that need to be joined with all related tables.

7. **Status Mapping:** The procedure maps numeric status codes to human-readable status names in multiple places (both in the temp table creation and final result set).

8. **PaidFromModule:** The `PaidFromModule` field determines who performed the transaction (Sender, Agent, Admin Staff, etc.) and is used both for filtering and display.

9. **Abnormal Transaction:** Transactions with `Status = 7` are marked as abnormal (`IsAbnormalTransaction = 1`). This is different from Bank Deposit transactions where Status = 10 is abnormal.

10. **Payout Type:** The `PayoutType` is always 'Automatic' for KiiBank transfers (unlike Bank Deposit which can be 'Manual' or 'Automatic').

11. **No API Service Field:** This procedure does not include an `ApiService` field in the result set, unlike other similar procedures.

12. **No Payout Provider:** The `PayoutProvider` filtering is commented out in the code and not currently used. The result set does not include a `PayoutProviderName` column.

13. **Unused Parameters:** Some parameters like `@IsBusiness` and `@IsRegisteredByAuxAgent` are defined but not currently used in the filtering logic.

14. **Date Format:** Date filtering uses format 111 (YYYY/MM/DD) for comparison, and the result set uses format 106 (DD Mon YYYY) for display.

15. **Temporary Table Naming:** Despite being named `##Mobile`, this temporary table is used for KiiBank transactions (TransferType = 7), not mobile wallet transactions (TransferType = 2). This appears to be legacy naming.

16. **Status 8 Formatting:** Status code 8 maps to 'In progress ' (with a trailing space) in the final result set, but 'In progress' (without trailing space) in the temp table. This inconsistency may be intentional or a typo.

17. **Simplified Structure:** Compared to Bank and Cash transaction procedures, this procedure has a simpler structure with fewer joins and no auxiliary agent override logic.

