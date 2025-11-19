# Stored Procedure: SP_Get_BankTransactionStatement - Tables Documentation

## Overview
This document provides a comprehensive list of all tables and their properties used in the stored procedure `SP_Get_BankTransactionStatement`. This procedure retrieves bank deposit transaction statements with extensive filtering capabilities.

---

## Transaction Service Type

### TransactionServiceType Value:
- **6** = Bank Deposit Transaction

### TransferType Value:
- **1** = Bank Deposit

---

## Permanent Tables

### 1. BankAccountDeposit (bankAccountDeposit)
**Purpose:** Primary table storing bank account deposit transactions

**Service Type:** Bank Deposit (TransferType = 1, TransactionServiceType = 6)

**Join Type:** Primary table in main query

**Properties Used:**
- `Id` - Transaction identifier
- `TransactionDate` - Date of the transaction
- `Status` - Transaction status (0-13)
- `PaidFromModule` - Type of user who performed the transaction (0-5)
- `Fee` - Transaction fee
- `ReceiptNo` - Receipt number/transaction identifier
- `PaymentReference` - Payment reference number
- `SenderId` - Foreign key to FaxerInformation
- `SendingCountry` - Country code for sending country
- `ReceivingCountry` - Country code for receiving country
- `SendingCurrency` - Currency code for sending country
- `ReceivingCurrency` - Currency code for receiving country
- `ReceiverName` - Name of the receiver
- `ReceiverCity` - City of the receiver
- `ReceiverAccountNo` - Account number of the receiver
- `SendingAmount` - Amount sent
- `ReceivingAmount` - Amount received
- `TotalAmount` - Total transaction amount
- `ExchangeRate` - Exchange rate applied
- `SenderPaymentMode` - Payment mode used by sender
- `IsManualApproveNeeded` - Flag indicating if manual approval is needed
- `IsComplianceApproved` - Flag indicating if compliance approved
- `IsComplianceNeededForTrans` - Flag indicating if compliance needed
- `Apiservice` - API service used (0-4)
- `TransferReference` - Transfer reference number
- `IsTransactionDuplicated` - Flag indicating if transaction is duplicated
- `RecipientId` - Recipient identifier
- `UpdateByStaffId` - Staff ID who last updated the transaction
- `PayingStaffId` - Staff ID who processed payment
- `BankId` - Foreign key to Bank
- `BankCode` - Bank code
- `BankName` - Bank name
- `IsEuropeTransfer` - Flag indicating if it's a Europe transfer
- `IsManualDeposit` - Flag indicating if it's a manual deposit

**Join Conditions:**
- `bankAccountDeposit.SenderId = senderInfo.Id` (INNER JOIN)
- `bankAccountDeposit.BankId = bankInfo.Id` (LEFT JOIN)
- `bankAccountDeposit.UpdateByStaffId = staffInfo.Id` (LEFT JOIN)
- `bankAccountDeposit.Id = auxAgnetBankAccountDepoist.BankAccountDepositId` (LEFT JOIN)
- `bankAccountDeposit.SendingCountry = sendingCountry.CountryCode` (INNER JOIN)
- `bankAccountDeposit.ReceivingCountry = receivingCountry.CountryCode` (INNER JOIN)
- `bankAccountDeposit.Id = creditDebitCardInfo.CardTransactionId` (LEFT JOIN, with creditDebitCardInfo.TransferType = 4)
- `bankAccountDeposit.ReceiptNo = ReInTrans.NewReceiptNo` (LEFT JOIN)

---

### 2. FaxerInformation (senderInfo)
**Purpose:** Stores sender/customer information

**Service Type:** Bank Deposit

**Join Type:** INNER JOIN

**Join Condition:** `bankAccountDeposit.SenderId = senderInfo.Id`

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

### 3. Bank (bankInfo)
**Purpose:** Stores bank information for payout providers

**Service Type:** Bank Deposit

**Join Type:** LEFT JOIN

**Join Condition:** `bankAccountDeposit.BankId = bankInfo.Id`

**Properties Used:**
- `Id` - Bank identifier
- `Name` - Name of the bank/payout provider

**Note:** Used in both the initial filtering query and final result set. The bank name is used as PayoutProviderName when `IsEuropeTransfer = 0`, otherwise `BankName` from BankAccountDeposit is used.

---

### 4. StaffInformation (staffInfo)
**Purpose:** Stores staff information for transaction updates

**Service Type:** Bank Deposit

**Join Type:** LEFT JOIN

**Join Condition:** `bankAccountDeposit.UpdateByStaffId = staffInfo.Id`

**Properties Used:**
- `Id` - Staff identifier
- `FirstName` - First name of staff member
- `MiddleName` - Middle name of staff member
- `LastName` - Last name of staff member

---

### 5. AuxAgentBankAccountDepositDetail (auxAgnetBankAccountDepoist)
**Purpose:** Stores auxiliary agent bank account deposit details (overrides main transaction values)

**Service Type:** Bank Deposit

**Join Type:** LEFT JOIN

**Join Condition:** `auxAgnetBankAccountDepoist.BankAccountDepositId = bankAccountDeposit.Id`

**Properties Used:**
- `BankAccountDepositId` - Foreign key to BankAccountDeposit.Id
- `Fee` - Fee amount (overrides bankAccountDeposit.Fee)
- `SendingAmount` - Sending amount (overrides bankAccountDeposit.SendingAmount)
- `ReceivingAmount` - Receiving amount (overrides bankAccountDeposit.ReceivingAmount)
- `TotalAmount` - Total amount (overrides bankAccountDeposit.TotalAmount)
- `ExchangeRate` - Exchange rate (overrides bankAccountDeposit.ExchangeRate)

**Note:** These values take precedence over the main transaction values when present.

---

### 6. FaxerLogin (senderLogin)
**Purpose:** Stores sender login information

**Service Type:** Bank Deposit

**Join Type:** LEFT JOIN

**Join Condition:** `senderInfo.Id = senderLogin.FaxerId`

**Properties Used:**
- `FaxerId` - Foreign key to FaxerInformation.Id
- `IsActive` - Flag indicating if sender account is active

---

### 7. Country (sendingCountry, receivingCountry)
**Purpose:** Stores country information (used twice for sending and receiving countries)

**Service Type:** Bank Deposit

**Join Type:** INNER JOIN (as `sendingCountry` and `receivingCountry`)

**Join Conditions:**
- `bankAccountDeposit.SendingCountry = sendingCountry.CountryCode`
- `bankAccountDeposit.ReceivingCountry = receivingCountry.CountryCode`

**Properties Used:**
- `CountryCode` - Country code identifier
- `CountryName` - Name of the country
- `Currency` - Currency code
- `CurrencySymbol` - Currency symbol

---

### 8. CardTopUpCreditDebitInformation (creditDebitCardInfo)
**Purpose:** Stores credit/debit card information for card payments

**Service Type:** Bank Deposit

**Join Type:** LEFT JOIN

**Join Conditions:**
- `creditDebitCardInfo.TransferType = 4`
- `bankAccountDeposit.Id = creditDebitCardInfo.CardTransactionId`

**Properties Used:**
- `TransferType` - Transfer type (must be 4 for bank deposits)
- `CardTransactionId` - Foreign key to transaction ID
- `CardNumber` - Card number (masked)

---

### 9. ReinitializeTransaction (ReInTrans)
**Purpose:** Stores information about reinitialized transactions

**Service Type:** Bank Deposit

**Join Type:** LEFT JOIN

**Join Condition:** `bankAccountDeposit.ReceiptNo = ReInTrans.NewReceiptNo`

**Properties Used:**
- `NewReceiptNo` - New receipt number after reinitialization
- `ReceiptNo` - Original receipt number
- `CreatedByName` - Name of staff who created the reinitialization
- `Date` - Date of reinitialization

---

## Temporary Tables

### 1. ##BankDeposit (Global Temporary Table)
**Purpose:** Intermediate storage for filtered bank deposit transactions

**Created By:** Dynamic SQL execution (`SELECT INTO ##BankDeposit`)

**Structure:**
- `Id` - Transaction ID
- `TransactionDate` - Transaction date
- `TransferType` - Transaction type (always 1 for Bank Deposit)
- `TransactionServiceType` - Service type (always 6)
- `StatusName` - Human-readable status name
- `TransactionPerformedBy` - Who performed the transaction
- `HasFee` - Flag indicating if transaction has fee
- `PayoutProviderName` - Payout provider name (from Bank.Name)

**Lifecycle:** Created and dropped within the procedure execution

**Data Source:** BankAccountDeposit (with joins to FaxerInformation and Bank)

**Filtering Applied:**
- Search string (ReceiptNo or PaymentReference)
- SenderId
- StaffId (UpdateByStaffId)
- SendingCountry
- ReceivingCountry
- SenderName (from FaxerInformation)
- ReceiverName (from BankAccountDeposit)
- SendingCurrency
- ReceivingCurrency
- SenderEmail
- MFCode (AccountNo)
- PhoneNumber
- Date range (FromDate, ToDate)
- PayoutProvider (Bank.Name)
- Status (StatusName)
- SearchByStatus (StatusName)
- ResponsiblePerson (TransactionPerformedBy)
- TransactionWithAndWithoutFee (HasFee)

---

### 2. #finalData (Local Temporary Table)
**Purpose:** Final filtered and paginated data before final result set

**Created By:** `SELECT INTO #finalData` statement

**Structure:**
- Inherits all columns from `##BankDeposit` table
- Contains paginated results based on `@PageNum` and `@PageSize` parameters
- Ordered by `TransactionDate DESC`

**Lifecycle:** Created and dropped within the procedure execution

**Join Condition:** `bankAccountDeposit.Id = data.Id AND data.TransferType = 1`

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
- `10` = ' Abnormal' (note: includes leading space in code)
- `11` = 'Full Refund'
- `12` = 'Partial Refund'
- `13` = 'In Progress'
- `Default` = 'In Progress'

### PaidFromModule Values:
- `0` = 'Sender'
- `1` = '' (empty)
- `2` = '' (empty)
- `3` = 'Agent'
- `4` = 'Admin Staff'
- `5` = '' (empty)
- `Default` = '' (empty)

### Apiservice Values:
- `0` = 'VGG'
- `1` = 'TransferZero'
- `2` = 'EmergentApi'
- `3` = 'MTN'
- `4` = 'Zenith'
- `Default` = 'Wari'

---

## Result Set Columns

The final result set includes the following columns:

- `TotalCount` - Total number of records matching the criteria
- `Id` - Transaction identifier
- `AccountNumber` - Receiver's account number (from ReceiverAccountNo)
- `SenderTelephoneNo` - Sender's phone number (from senderInfo.PhoneNumber, default: '')
- `ReceiverName` - Name of receiver (from ReceiverName)
- `ReceiverCity` - City of receiver (from ReceiverCity)
- `ReceiverCountry` - Name of receiving country (from receivingCountry.CountryName)
- `IsManualApprovalNeeded` - Manual approval flag (from IsManualApproveNeeded)
- `HasFee` - Flag indicating if transaction has fee (1 if Fee > 0, else 0)
- `Status` - Always 5
- `statusOfMobileWallet` - Always 0
- `StatusofMobileTransfer` - Always 0
- `StatusOfBankDepoist` - Transaction status code (from Status)
- `StatusName` - Human-readable status name
- `TransactionType` - Always 'Bank Deposit'
- `ReceivingCurrencySymbol` - Currency symbol for receiving country
- `ReceivingCurrrency` - Receiving currency code (from bankAccountDeposit or receivingCountry)
- `SendingCurrency` - Sending currency code (from bankAccountDeposit or sendingCountry)
- `SendingCurrencySymbol` - Currency symbol for sending country
- `Fee` - Transaction fee (from auxAgnetBankAccountDepoist.Fee or bankAccountDeposit.Fee)
- `GrossAmount` - Gross amount (from auxAgnetBankAccountDepoist.SendingAmount or bankAccountDeposit.SendingAmount)
- `ReceivingAmount` - Receiving amount (from auxAgnetBankAccountDepoist.SendingAmount or bankAccountDeposit.ReceivingAmount) **Note:** Uses SendingAmount from aux table instead of ReceivingAmount - verify if this is correct
- `TotalAmount` - Total amount (from auxAgnetBankAccountDepoist.TotalAmount or bankAccountDeposit.TotalAmount)
- `ExchangeRate` - Exchange rate (from auxAgnetBankAccountDepoist.ExchangeRate or bankAccountDeposit.ExchangeRate)
- `SenderPaymentMode` - Payment mode used by sender
- `CardNumber` - Card number (from creditDebitCardInfo.CardNumber, default: '')
- `Date` - Formatted transaction date (convert to varchar, format 106)
- `TransactionServiceType` - Always 6
- `TransactionDate` - Transaction date/time
- `Reference` - Payment reference (from PaymentReference, default: '')
- `PaymentReference` - Payment reference (from PaymentReference, default: '')
- `BankCode` - Bank code (from BankCode)
- `WalletName` - Always empty string
- `BankName` - Bank name (from bankInfo.Name if IsEuropeTransfer = 0, else from BankName)
- `TransactionIdentifier` - Receipt number (from ReceiptNo, default: '')
- `FaxerAccountNo` - Sender account number (from senderInfo.AccountNo)
- `FaxerCountry` - Sending country name (from sendingCountry.CountryName)
- `SenderName` - Full name of sender (concatenated from FirstName, MiddleName, LastName)
- `IsManualBankDeposit` - Manual deposit flag (from IsManualDeposit)
- `senderId` - Sender ID
- `IsRetryAbleCountry` - Always 0 (BIT)
- `IsBusiness` - Flag indicating if sender is a business (from senderInfo.IsBusiness)
- `IsAbnormalTransaction` - Flag indicating abnormal transaction (1 if Status = 10, else 0)
- `IsEuropeTransfer` - Europe transfer flag (from IsEuropeTransfer)
- `IsAwaitForApproval` - Compliance approval flag (calculated based on IsComplianceApproved and Status)
- `PaidFromModule` - Operating user type (from PaidFromModule)
- `AgentStaffId` - Staff ID who processed payment (from PayingStaffId)
- `ApiService` - API service name (mapped from Apiservice)
- `TransferReference` - Transfer reference (from TransferReference, default: '')
- `IsDuplicatedTransaction` - Duplication flag (from IsTransactionDuplicated)
- `DuplicatedTransactionReceiptNo` - Receipt number (from ReceiptNo)
- `ReInitializedReceiptNo` - Original receipt number from reinitialization (from ReInTrans.ReceiptNo, default: '')
- `IsReInitializedTransaction` - Flag indicating if transaction was reinitialized (1 if ReInTrans.ReceiptNo IS NOT NULL, else 0)
- `ReInitializeStaffName` - Name of staff who reinitialized (from ReInTrans.CreatedByName, default: '')
- `ReInitializedDateTime` - Date/time of reinitialization (from ReInTrans.Date, formatted, default: '')
- `RecipientId` - Recipient ID
- `SenderCountryCode` - Sending country code
- `SenderEmail` - Sender email (from senderInfo.Email, default: '')
- `ReceivingCountryCode` - Receiving country code
- `TransactionPerformedBy` - Who performed the transaction (mapped from PaidFromModule)
- `PayoutProviderName` - Payout provider name (from bankInfo.Name if IsEuropeTransfer = 0, else from BankName)
- `TransactionUpdatedById` - Staff ID who updated transaction (from UpdateByStaffId)
- `TransactionUpdatedByName` - Name of staff who updated transaction (concatenated from staffInfo FirstName, MiddleName, LastName)
- `PayoutType` - Payout type ('Manual' if IsManualDeposit = 0, else 'Automatic')
- `IsSenderActive` - Flag indicating if sender account is active (from senderLogin.IsActive)

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
- `@searchString` - Search by ReceiptNo or PaymentReference (default: '')
- `@Status` - Filter by status name (partial match, default: '')
- `@PhoneNumber` - Filter by sender phone number (default: '')
- `@SendingCurrency` - Filter by sending currency code (default: '')
- `@ReceivingCurrency` - Filter by receiving currency code (default: '')
- `@TransactionWithAndWithoutFee` - Filter by fee presence (BIT value as string, default: '')
- `@ResponsiblePerson` - Filter by who performed transaction (default: '')
- `@SearchByStatus` - Additional status filter (partial match, default: '')
- `@MFCode` - Filter by sender account number (default: '')
- `@PageNum` - Page number for pagination (default: 1)
- `@PageSize` - Number of records per page (default: 10)
- `@IsBusiness` - Filter by business flag (default: 0, not used in current implementation)
- `@IsRegisteredByAuxAgent` - Filter by auxiliary agent registration (default: 0, not used in current implementation)
- `@PayoutProvider` - Filter by payout provider name (default: '')
- `@StaffId` - Filter by staff ID who updated transaction (default: 0)

---

## Query Execution Flow

1. **Dynamic Query Building:**
   - Builds base query for BankAccountDeposit with joins to FaxerInformation and Bank
   - Applies filters based on input parameters
   - Creates temporary table `##BankDeposit` with filtered results

2. **Additional Filtering:**
   - Applies additional filters on `##BankDeposit` table:
     - Status (StatusName)
     - SearchByStatus (StatusName)
     - ResponsiblePerson (TransactionPerformedBy)
     - TransactionWithAndWithoutFee (HasFee)
     - PayoutProvider (PayoutProviderName)

3. **Pagination:**
   - Creates `#finalData` local temp table with paginated results
   - Uses OFFSET/FETCH for pagination based on `@PageNum` and `@PageSize`
   - Ordered by `TransactionDate DESC`

4. **Final Result Set:**
   - Joins `#finalData` with BankAccountDeposit and all related tables
   - Retrieves comprehensive transaction details with all related information
   - Results ordered by `TransactionDate DESC`

5. **Cleanup:**
   - Drops temporary tables `##BankDeposit` and `#finalData`

---

## Table Relationships Summary

### Bank Deposit Transaction Flow
```
BankAccountDeposit (Primary)
├── INNER JOIN FaxerInformation (via SenderId)
│   └── LEFT JOIN FaxerLogin (via FaxerId)
├── INNER JOIN Country [sendingCountry] (via SendingCountry)
├── INNER JOIN Country [receivingCountry] (via ReceivingCountry)
├── LEFT JOIN Bank (via BankId)
├── LEFT JOIN StaffInformation (via UpdateByStaffId)
├── LEFT JOIN AuxAgentBankAccountDepositDetail (via BankAccountDepositId)
├── LEFT JOIN CardTopUpCreditDebitInformation (via Id, with TransferType = 4)
└── LEFT JOIN ReinitializeTransaction (via ReceiptNo = NewReceiptNo)
```

---

## Total Count

**Permanent Tables:** 9
- BankAccountDeposit
- FaxerInformation
- Bank
- StaffInformation
- AuxAgentBankAccountDepositDetail
- FaxerLogin
- Country (used twice but counted as one table)
- CardTopUpCreditDebitInformation
- ReinitializeTransaction

**Temporary Tables:** 2
- ##BankDeposit (global temp table)
- #finalData (local temp table)

**Total Tables:** 11

---

## Special Notes

1. **Dynamic SQL:** The procedure uses dynamic SQL to build the initial query based on input parameters, which allows for flexible filtering.

2. **Auxiliary Agent Overrides:** The `AuxAgentBankAccountDepositDetail` table provides override values for Fee, SendingAmount, ReceivingAmount, TotalAmount, and ExchangeRate. These values take precedence over the main transaction values when present.

3. **Reinitialization Tracking:** The procedure tracks reinitialized transactions through the `ReinitializeTransaction` table, providing information about the original receipt number and staff who performed the reinitialization.

4. **Compliance Approval Logic:** The `IsAwaitForApproval` flag is calculated as:
   - `0` if `IsComplianceApproved = 1` OR `Status = 2` (Cancelled)
   - Otherwise, uses `IsComplianceNeededForTrans` value

5. **Card Payment Information:** Card details are retrieved from `CardTopUpCreditDebitInformation` only when `TransferType = 4`, which represents bank deposit payment method type.

6. **Pagination:** Pagination is applied after initial filtering but before the final detailed join, optimizing performance by limiting the number of records that need to be joined with all related tables.

7. **Status Mapping:** The procedure maps numeric status codes to human-readable status names in multiple places (both in the temp table creation and final result set).

8. **PaidFromModule:** The `PaidFromModule` field determines who performed the transaction (Sender, Agent, Admin Staff, etc.) and is used both for filtering and display.

9. **Europe Transfer Logic:** For Europe transfers (`IsEuropeTransfer = 1`), the `BankName` and `PayoutProviderName` use the value from `BankAccountDeposit.BankName` instead of `Bank.Name`.

10. **Payout Type:** The `PayoutType` is determined by `IsManualDeposit`: 'Manual' if `IsManualDeposit = 0`, otherwise 'Automatic'.

11. **Abnormal Transaction:** Transactions with `Status = 10` are marked as abnormal (`IsAbnormalTransaction = 1`).

12. **Unused Parameters:** Some parameters like `@IsBusiness` and `@IsRegisteredByAuxAgent` are defined but not currently used in the filtering logic.

13. **Date Format:** Date filtering uses format 111 (YYYY/MM/DD) for comparison, and the result set uses format 106 (DD Mon YYYY) for display.

14. **ReceivingAmount Note:** In the final SELECT, `ReceivingAmount` uses `ISNULL(auxAgnetBankAccountDepoist.SendingAmount, bankAccountDeposit.ReceivingAmount)`. This appears to be a bug as it should likely use `auxAgnetBankAccountDepoist.ReceivingAmount` instead of `SendingAmount` from the aux table. Verify with business logic if this is intentional.

15. **Status 10 Formatting:** Status code 10 maps to ' Abnormal' (with a leading space) in the code. This may be intentional formatting or a typo.

