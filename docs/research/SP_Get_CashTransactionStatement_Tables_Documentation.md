# Stored Procedure: SP_Get_CashTransactionStatement - Tables Documentation

## Overview
This document provides a comprehensive list of all tables and their properties used in the stored procedure `SP_Get_CashTransactionStatement`. This procedure retrieves cash pickup transaction statements with extensive filtering capabilities.

---

## Transaction Service Type

### TransactionServiceType Value:
- **5** = Cash Pickup Transaction

### TransferType Value:
- **3** = Cash Pickup

---

## Permanent Tables

### 1. FaxingNonCardTransaction (cashPickup)
**Purpose:** Primary table storing cash pickup/non-card transactions

**Service Type:** Cash Pickup (TransferType = 3, TransactionServiceType = 5)

**Join Type:** Primary table in main query

**Properties Used:**
- `Id` - Transaction identifier
- `TransactionDate` - Date of the transaction
- `FaxingStatus` - Transaction status (0-11)
- `OperatingUserType` - Type of user who performed the transaction (0-5)
- `FaxingFee` - Transaction fee
- `ReceiptNumber` - Receipt number/transaction identifier
- `MFCN` - MoneyFex Control Number
- `SenderId` - Foreign key to FaxerInformation
- `SendingCountry` - Country code for sending country
- `ReceivingCountry` - Country code for receiving country
- `SendingCurrency` - Currency code for sending country
- `ReceivingCurrency` - Currency code for receiving country
- `FaxingAmount` - Sending amount
- `ReceivingAmount` - Amount received
- `TotalAmount` - Total transaction amount
- `ExchangeRate` - Exchange rate applied
- `SenderPaymentMode` - Payment mode used by sender
- `PaymentReference` - Payment reference number
- `IsComplianceApproved` - Flag indicating if compliance approved
- `IsComplianceNeededForTrans` - Flag indicating if compliance needed
- `PayingStaffId` - Staff ID who processed payment
- `Apiservice` - API service used (0-4)
- `TransferReference` - Transfer reference number
- `NonCardRecieverId` - Foreign key to ReceiversDetails
- `RecipientId` - Foreign key to Recipients
- `UpdatedByStaffId` - Staff ID who last updated the transaction

**Join Conditions:**
- `cashPickup.RecipientId = recipent.Id` (INNER JOIN)
- `cashPickup.SenderId = senderInfo.Id` (INNER JOIN)
- `cashPickup.UpdatedByStaffId = staffInfo.Id` (LEFT JOIN)
- `cashPickup.NonCardRecieverId = receiverDetails.Id` (LEFT JOIN)
- `cashPickup.SendingCountry = sendingCountry.CountryCode` (INNER JOIN)
- `cashPickup.ReceivingCountry = receivingCountry.CountryCode` (INNER JOIN)
- `cashPickup.Id = creditDebitCardInfo.CardTransactionId` (LEFT JOIN, with creditDebitCardInfo.TransferType = 2)
- `cashPickup.ReceiptNumber = ReInTrans.NewReceiptNo` (LEFT JOIN)

---

### 2. Recipients (recipent)
**Purpose:** Stores recipient information for cash pickup transactions

**Service Type:** Cash Pickup

**Join Type:** INNER JOIN

**Join Condition:** `cashPickup.RecipientId = recipent.Id`

**Properties Used:**
- `Id` - Recipient identifier
- `ReceiverName` - Name of the receiver (used in WHERE clause for filtering)

---

### 3. FaxerInformation (senderInfo)
**Purpose:** Stores sender/customer information

**Service Type:** Cash Pickup

**Join Type:** INNER JOIN

**Join Condition:** `cashPickup.SenderId = senderInfo.Id`

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

### 4. StaffInformation (staffInfo)
**Purpose:** Stores staff information for transaction updates

**Service Type:** Cash Pickup

**Join Type:** LEFT JOIN

**Join Condition:** `cashPickup.UpdatedByStaffId = staffInfo.Id`

**Properties Used:**
- `Id` - Staff identifier
- `FirstName` - First name of staff member
- `MiddleName` - Middle name of staff member
- `LastName` - Last name of staff member

---

### 5. AuxAgentCashPickUpDetail (auxcashtransfer)
**Purpose:** Stores auxiliary agent cash pickup details (overrides main transaction values)

**Service Type:** Cash Pickup

**Join Type:** LEFT JOIN

**Join Condition:** `auxcashtransfer.CashPickUpId = cashPickup.Id`

**Properties Used:**
- `CashPickUpId` - Foreign key to FaxingNonCardTransaction.Id
- `Fee` - Fee amount (overrides cashPickup.FaxingFee)
- `SendingAmount` - Sending amount (overrides cashPickup.FaxingAmount)
- `ReceivingAmount` - Receiving amount (overrides cashPickup.ReceivingAmount)
- `TotalAmount` - Total amount (overrides cashPickup.TotalAmount)
- `ExchangeRate` - Exchange rate (overrides cashPickup.ExchangeRate)

---

### 6. ReceiversDetails (receiverDetails)
**Purpose:** Stores detailed receiver information for cash pickup transactions

**Service Type:** Cash Pickup

**Join Type:** LEFT JOIN

**Join Condition:** `cashPickup.NonCardRecieverId = receiverDetails.Id`

**Properties Used:**
- `Id` - Receiver identifier
- `PhoneNumber` - Phone number of receiver (used as AccountNumber and SenderTelephoneNo)
- `FullName` - Full name of receiver
- `City` - City of receiver

---

### 7. FaxerLogin (senderLogin)
**Purpose:** Stores sender login information

**Service Type:** Cash Pickup

**Join Type:** LEFT JOIN

**Join Condition:** `senderInfo.Id = senderLogin.FaxerId`

**Properties Used:**
- `FaxerId` - Foreign key to FaxerInformation.Id
- `IsActive` - Flag indicating if sender account is active

---

### 8. Country (sendingCountry, receivingCountry)
**Purpose:** Stores country information (used twice for sending and receiving countries)

**Service Type:** Cash Pickup

**Join Type:** INNER JOIN (as `sendingCountry` and `receivingCountry`)

**Join Conditions:**
- `cashPickup.SendingCountry = sendingCountry.CountryCode`
- `cashPickup.ReceivingCountry = receivingCountry.CountryCode`

**Properties Used:**
- `CountryCode` - Country code identifier
- `CountryName` - Name of the country
- `Currency` - Currency code
- `CurrencySymbol` - Currency symbol

---

### 9. CardTopUpCreditDebitInformation (creditDebitCardInfo)
**Purpose:** Stores credit/debit card information for card payments

**Service Type:** Cash Pickup

**Join Type:** LEFT JOIN

**Join Conditions:**
- `creditDebitCardInfo.TransferType = 2`
- `cashPickup.Id = creditDebitCardInfo.CardTransactionId`

**Properties Used:**
- `TransferType` - Transfer type (must be 2)
- `CardTransactionId` - Foreign key to transaction ID
- `CardNumber` - Card number (masked)

---

### 10. ReinitializeTransaction (ReInTrans)
**Purpose:** Stores information about reinitialized transactions

**Service Type:** Cash Pickup

**Join Type:** LEFT JOIN

**Join Condition:** `cashPickup.ReceiptNumber = ReInTrans.NewReceiptNo`

**Properties Used:**
- `NewReceiptNo` - New receipt number after reinitialization
- `ReceiptNo` - Original receipt number
- `CreatedByName` - Name of staff who created the reinitialization
- `Date` - Date of reinitialization

---

## Temporary Tables

### 1. ##Cash (Global Temporary Table)
**Purpose:** Intermediate storage for filtered cash pickup transactions

**Created By:** Dynamic SQL execution (`SELECT INTO ##Cash`)

**Structure:**
- `Id` - Transaction ID
- `TransactionDate` - Transaction date
- `TransferType` - Transaction type (always 3 for Cash Pickup)
- `TransactionServiceType` - Service type (always 5)
- `StatusName` - Human-readable status name
- `TransactionPerformedBy` - Who performed the transaction
- `HasFee` - Flag indicating if transaction has fee
- `PayoutProviderName` - Payout provider name (empty string)

**Lifecycle:** Created and dropped within the procedure execution

**Data Source:** FaxingNonCardTransaction (with joins to Recipients and FaxerInformation)

**Filtering Applied:**
- Search string (MFCN or ReceiptNumber)
- SenderId
- StaffId (UpdatedByStaffId)
- SendingCountry
- ReceivingCountry
- SenderName (from FaxerInformation)
- ReceiverName (from Recipients)
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
- PayoutProvider (PayoutProviderName)

---

### 2. #finalData (Local Temporary Table)
**Purpose:** Final filtered and paginated data before final result set

**Created By:** `SELECT INTO #finalData` statement

**Structure:**
- Inherits all columns from `##Cash` table
- Contains paginated results based on `@PageNum` and `@PageSize` parameters
- Ordered by `TransactionDate DESC`

**Lifecycle:** Created and dropped within the procedure execution

**Join Condition:** `finalData.TransferType = 3 AND finalData.Id = cashPickup.Id`

---

## Status Code Mappings

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

### OperatingUserType Values:
- `0` = 'Sender'
- `1` = '' (empty)
- `2` = '' (empty)
- `3` = 'Agent'
- `4` = 'Admin Staff'
- `5` = ' ' (space)
- `Default` = ' ' (space)

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
- `AccountNumber` - Receiver's phone number (from ReceiversDetails.PhoneNumber)
- `SenderTelephoneNo` - Receiver's phone number (from ReceiversDetails.PhoneNumber)
- `ReceiverName` - Full name of receiver (from ReceiversDetails.FullName, default: 'No Name')
- `ReceiverCity` - City of receiver (from ReceiversDetails.City)
- `ReceiverCountry` - Name of receiving country (from receivingCountry.CountryName)
- `IsManualApprovalNeeded` - Always 0 (BIT)
- `HasFee` - Flag indicating if transaction has fee (1 if FaxingFee > 0, else 0)
- `Status` - Transaction status code (from FaxingStatus)
- `statusOfMobileWallet` - Always 0
- `StatusofMobileTransfer` - Always 0
- `StatusOfBankDepoist` - Always 2
- `StatusName` - Human-readable status name
- `TransactionType` - Always 'Cash Pickup'
- `ReceivingCurrencySymbol` - Currency symbol for receiving country
- `ReceivingCurrrency` - Receiving currency code (from cashPickup or receivingCountry)
- `SendingCurrency` - Sending currency code (from cashPickup or sendingCountry)
- `SendingCurrencySymbol` - Currency symbol for sending country
- `Fee` - Transaction fee (from auxcashtransfer.Fee or cashPickup.FaxingFee)
- `GrossAmount` - Gross amount (from auxcashtransfer.SendingAmount or cashPickup.FaxingAmount)
- `ReceivingAmount` - Receiving amount (from auxcashtransfer.ReceivingAmount or cashPickup.ReceivingAmount)
- `TotalAmount` - Total amount (from auxcashtransfer.TotalAmount or cashPickup.TotalAmount)
- `ExchangeRate` - Exchange rate (from auxcashtransfer.ExchangeRate or cashPickup.ExchangeRate)
- `SenderPaymentMode` - Payment mode used by sender
- `CardNumber` - Card number (from creditDebitCardInfo.CardNumber, default: '')
- `Date` - Formatted transaction date (convert to varchar, format 106)
- `TransactionServiceType` - Always 5
- `TransactionDate` - Transaction date/time
- `Reference` - Payment reference (from PaymentReference, default: '')
- `PaymentReference` - Payment reference (from PaymentReference, default: '')
- `BankCode` - Always empty string
- `WalletName` - Always empty string
- `BankName` - Always empty string
- `TransactionIdentifier` - Receipt number (from ReceiptNumber, default: '')
- `FaxerAccountNo` - Sender account number (from senderInfo.AccountNo)
- `FaxerCountry` - Sending country name (from sendingCountry.CountryName)
- `SenderName` - Full name of sender (concatenated from FirstName, MiddleName, LastName)
- `IsManualBankDeposit` - Always 0 (BIT)
- `senderId` - Sender ID
- `IsRetryAbleCountry` - Always 0 (BIT)
- `IsBusiness` - Flag indicating if sender is a business (from senderInfo.IsBusiness)
- `IsAbnormalTransaction` - Always 0 (BIT)
- `IsEuropeTransfer` - Always 0 (BIT)
- `IsAwaitForApproval` - Compliance approval flag (calculated based on IsComplianceApproved and FaxingStatus)
- `PaidFromModule` - Operating user type (from OperatingUserType)
- `AgentStaffId` - Staff ID who processed payment (from PayingStaffId)
- `ApiService` - API service name (mapped from Apiservice)
- `TransferReference` - Transfer reference (from TransferReference, default: '')
- `IsDuplicatedTransaction` - Always 0 (BIT)
- `DuplicatedTransactionReceiptNo` - Receipt number (from ReceiptNumber)
- `ReInitializedReceiptNo` - Original receipt number from reinitialization (from ReInTrans.ReceiptNo, default: '')
- `IsReInitializedTransaction` - Flag indicating if transaction was reinitialized (1 if ReInTrans.ReceiptNo IS NOT NULL, else 0)
- `ReInitializeStaffName` - Name of staff who reinitialized (from ReInTrans.CreatedByName, default: '')
- `ReInitializedDateTime` - Date/time of reinitialization (from ReInTrans.Date, formatted, default: '')
- `RecipientId` - Recipient ID
- `SenderCountryCode` - Sending country code
- `SenderEmail` - Sender email (from senderInfo.Email, default: '')
- `ReceivingCountryCode` - Receiving country code
- `TransactionPerformedBy` - Who performed the transaction (mapped from OperatingUserType)
- `PayoutProviderName` - Always empty string
- `TransactionUpdatedById` - Staff ID who updated transaction (from UpdatedByStaffId)
- `TransactionUpdatedByName` - Name of staff who updated transaction (concatenated from staffInfo FirstName, MiddleName, LastName)
- `PayoutType` - Always 'Manual'
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
- `@searchString` - Search by MFCN or ReceiptNumber (default: '')
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
- `@PayoutProvider` - Filter by payout provider name (default: '')
- `@StaffId` - Filter by staff ID who updated transaction (default: 0)

---

## Query Execution Flow

1. **Dynamic Query Building:**
   - Builds base query for FaxingNonCardTransaction with joins to Recipients and FaxerInformation
   - Applies filters based on input parameters
   - Creates temporary table `##Cash` with filtered results

2. **Additional Filtering:**
   - Applies additional filters on `##Cash` table:
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
   - Joins `#finalData` with FaxingNonCardTransaction and all related tables
   - Retrieves comprehensive transaction details with all related information
   - Results ordered by `TransactionDate DESC`

5. **Cleanup:**
   - Drops temporary tables `##Cash` and `#finalData`

---

## Table Relationships Summary

### Cash Pickup Transaction Flow
```
FaxingNonCardTransaction (Primary)
├── INNER JOIN Recipients (via RecipientId)
├── INNER JOIN FaxerInformation (via SenderId)
├── LEFT JOIN StaffInformation (via UpdatedByStaffId)
├── LEFT JOIN AuxAgentCashPickUpDetail (via CashPickUpId)
├── LEFT JOIN ReceiversDetails (via NonCardRecieverId)
├── LEFT JOIN FaxerLogin (via FaxerId from FaxerInformation)
├── INNER JOIN Country [sendingCountry] (via SendingCountry)
├── INNER JOIN Country [receivingCountry] (via ReceivingCountry)
├── LEFT JOIN CardTopUpCreditDebitInformation (via Id, with TransferType = 2)
└── LEFT JOIN ReinitializeTransaction (via ReceiptNumber = NewReceiptNo)
```

---

## Total Count

**Permanent Tables:** 10
- FaxingNonCardTransaction
- Recipients
- FaxerInformation
- StaffInformation
- AuxAgentCashPickUpDetail
- ReceiversDetails
- FaxerLogin
- Country (used twice but counted as one table)
- CardTopUpCreditDebitInformation
- ReinitializeTransaction

**Temporary Tables:** 2
- ##Cash (global temp table)
- #finalData (local temp table)

**Total Tables:** 12

---

## Special Notes

1. **Dynamic SQL:** The procedure uses dynamic SQL to build the initial query based on input parameters, which allows for flexible filtering.

2. **Auxiliary Agent Overrides:** The `AuxAgentCashPickUpDetail` table provides override values for Fee, SendingAmount, ReceivingAmount, TotalAmount, and ExchangeRate. These values take precedence over the main transaction values when present.

3. **Reinitialization Tracking:** The procedure tracks reinitialized transactions through the `ReinitializeTransaction` table, providing information about the original receipt number and staff who performed the reinitialization.

4. **Compliance Approval Logic:** The `IsAwaitForApproval` flag is calculated as:
   - `0` if `IsComplianceApproved = 1` OR `FaxingStatus = 2` (Cancelled)
   - Otherwise, uses `IsComplianceNeededForTrans` value

5. **Card Payment Information:** Card details are retrieved from `CardTopUpCreditDebitInformation` only when `TransferType = 2`, which may represent a specific payment method type.

6. **Pagination:** Pagination is applied after initial filtering but before the final detailed join, optimizing performance by limiting the number of records that need to be joined with all related tables.

7. **Status Mapping:** The procedure maps numeric status codes to human-readable status names in multiple places (both in the temp table creation and final result set).

8. **Operating User Type:** The `OperatingUserType` field determines who performed the transaction (Sender, Agent, Admin Staff, etc.) and is used both for filtering and display.

9. **Unused Parameters:** Some parameters like `@IsBusiness` and `@IsRegisteredByAuxAgent` are defined but not currently used in the filtering logic.

10. **Date Format:** Date filtering uses format 111 (YYYY/MM/DD) for comparison, and the result set uses format 106 (DD Mon YYYY) for display.

