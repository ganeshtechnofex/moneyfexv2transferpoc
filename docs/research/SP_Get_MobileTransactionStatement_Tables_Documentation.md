# Stored Procedure: SP_Get_MobileTransactionStatement - Tables Documentation

## Overview
This document provides a comprehensive list of all tables and their properties used in the stored procedure `SP_Get_MobileTransactionStatement`.

---

## Permanent Tables

### 1. MobileMoneyTransfer
**Purpose:** Main transaction table for mobile money transfers

**Join Type:** Primary table (INNER JOIN)

**Properties Used:**
- `Id` - Transaction identifier
- `TransactionDate` - Date of the transaction
- `Status` - Transaction status (0-10)
- `PaidFromModule` - Module from which payment was made (0-5)
- `Fee` - Transaction fee
- `PaidToMobileNo` - Mobile number of the recipient
- `ReceiverName` - Name of the receiver
- `ReceiverCity` - City of the receiver
- `ReceivingCurrency` - Currency code for receiving country
- `SendingCurrency` - Currency code for sending country
- `SenderPaymentMode` - Payment mode used by sender
- `PaymentReference` - Payment reference number
- `ReceiptNo` - Receipt number/transaction identifier
- `SenderId` - Foreign key to FaxerInformation
- `SendingCountry` - Country code for sending country
- `ReceivingCountry` - Country code for receiving country
- `WalletOperatorId` - Foreign key to MobileWalletOperator
- `IsComplianceApproved` - Compliance approval flag
- `IsComplianceNeededForTrans` - Flag indicating if compliance is needed
- `Apiservice` - API service used (0-9)
- `TransferReference` - Transfer reference number
- `RecipientId` - Recipient identifier
- `UpdateByStaffId` - Foreign key to StaffInformation
- `PayingStaffId` - Staff ID who processed the payment
- `TotalAmount` - Total transaction amount
- `SendingAmount` - Amount sent
- `ReceivingAmount` - Amount received
- `ExchangeRate` - Exchange rate applied

---

### 2. MobileWalletOperator
**Purpose:** Stores wallet operator/provider information

**Join Type:** INNER JOIN (as `walletInfo`)

**Join Condition:** `mobileMoneyTransfer.WalletOperatorId = walletInfo.Id`

**Properties Used:**
- `Id` - Wallet operator identifier
- `Name` - Name of the wallet operator/payout provider

---

### 3. FaxerInformation
**Purpose:** Stores sender/customer information

**Join Type:** INNER JOIN (as `senderInfo`)

**Join Condition:** `mobileMoneyTransfer.SenderId = senderInfo.Id`

**Properties Used:**
- `Id` - Sender identifier
- `FirstName` - Sender's first name
- `MiddleName` - Sender's middle name
- `LastName` - Sender's last name
- `Email` - Sender's email address
- `PhoneNumber` - Sender's phone number
- `AccountNo` - Sender's account number (MFCode)
- `IsBusiness` - Flag indicating if sender is a business

---

### 4. StaffInformation
**Purpose:** Stores staff member information

**Join Type:** LEFT JOIN (as `staffInfo`)

**Join Condition:** `staffInfo.Id = mobileMoneyTransfer.UpdateByStaffId`

**Properties Used:**
- `Id` - Staff identifier
- `FirstName` - Staff's first name
- `MiddleName` - Staff's middle name
- `LastName` - Staff's last name

---

### 5. AuxAgentMobileMoneyTransferDetail
**Purpose:** Stores auxiliary agent transaction details (overrides main transaction values)

**Join Type:** LEFT JOIN (as `auxagentMobileTrans`)

**Join Condition:** `mobileMoneyTransfer.Id = auxagentMobileTrans.MobileMoneyTransferId`

**Properties Used:**
- `MobileMoneyTransferId` - Foreign key to MobileMoneyTransfer
- `Fee` - Override fee amount
- `SendingAmount` - Override sending amount
- `ReceivingAmount` - Override receiving amount
- `TotalAmount` - Override total amount
- `ExchangeRate` - Override exchange rate

---

### 6. Country
**Purpose:** Stores country information (used twice for sending and receiving countries)

**Join Type:** INNER JOIN (as `sendingCountry` and `receivingCountry`)

**Join Conditions:**
- `mobileMoneyTransfer.SendingCountry = sendingCountry.CountryCode`
- `mobileMoneyTransfer.ReceivingCountry = receivingCountry.CountryCode`

**Properties Used:**
- `CountryCode` - Country code identifier
- `CountryName` - Name of the country
- `Currency` - Currency code
- `CurrencySymbol` - Currency symbol

---

### 7. CardTopUpCreditDebitInformation
**Purpose:** Stores credit/debit card information for transactions

**Join Type:** LEFT JOIN (as `creditDebitCardInfo`)

**Join Condition:** 
- `creditDebitCardInfo.TransferType = 3`
- `mobileMoneyTransfer.Id = creditDebitCardInfo.CardTransactionId`

**Properties Used:**
- `TransferType` - Type of transfer (filtered to 3)
- `CardTransactionId` - Foreign key to transaction
- `CardNumber` - Card number used for payment

---

### 8. FaxerLogin
**Purpose:** Stores login/authentication information for senders

**Join Type:** LEFT JOIN (as `senderLogin`)

**Join Condition:** `senderInfo.Id = senderLogin.FaxerId`

**Properties Used:**
- `FaxerId` - Foreign key to FaxerInformation
- `IsActive` - Flag indicating if sender account is active

---

### 9. ReinitializeTransaction
**Purpose:** Stores information about reinitialized transactions

**Join Type:** LEFT JOIN (as `ReInTrans`)

**Join Condition:** `mobileMoneyTransfer.ReceiptNo = ReInTrans.NewReceiptNo`

**Properties Used:**
- `NewReceiptNo` - New receipt number after reinitialization
- `ReceiptNo` - Original receipt number
- `CreatedByName` - Name of staff who created the reinitialization
- `Date` - Date of reinitialization

---

## Temporary Tables

### 1. ##Mobile (Global Temporary Table)
**Purpose:** Intermediate storage for filtered mobile transactions

**Created By:** Dynamic SQL execution

**Structure:**
- `Id` - Transaction ID
- `TransactionDate` - Transaction date
- `TransferType` - Always set to 2
- `TransactionServiceType` - Always set to 1
- `StatusName` - Human-readable status name
- `TransactionPerformedBy` - Who performed the transaction
- `HasFee` - Boolean flag indicating if transaction has fee
- `PayoutProviderName` - Name of the payout provider

**Lifecycle:** Created and dropped within the procedure execution

---

### 2. #finalData (Local Temporary Table)
**Purpose:** Final filtered and paginated data before final result set

**Created By:** `SELECT INTO` statement

**Structure:**
- Inherits all columns from `##Mobile` table
- Contains paginated results based on `@PageNum` and `@PageSize` parameters

**Lifecycle:** Created and dropped within the procedure execution

---

## Table Relationships Summary

```
MobileMoneyTransfer (Primary)
├── INNER JOIN MobileWalletOperator (via WalletOperatorId)
├── INNER JOIN FaxerInformation (via SenderId)
│   └── LEFT JOIN FaxerLogin (via FaxerId)
├── INNER JOIN Country [sendingCountry] (via SendingCountry)
├── INNER JOIN Country [receivingCountry] (via ReceivingCountry)
├── LEFT JOIN StaffInformation (via UpdateByStaffId)
├── LEFT JOIN AuxAgentMobileMoneyTransferDetail (via Id)
├── LEFT JOIN CardTopUpCreditDebitInformation (via Id, TransferType=3)
└── LEFT JOIN ReinitializeTransaction (via ReceiptNo)
```

---

## Status Code Mappings

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

### PaidFromModule Values:
- `0` = 'Sender'
- `1` = '' (empty)
- `2` = '' (empty)
- `3` = 'Agent'
- `4` = 'Admin Staff'
- `5` = '' (empty)
- `Default` = '' (empty)

### ApiService Values:
- `0` = 'VGG'
- `1` = 'TransferZero'
- `2` = 'EmergentApi'
- `3` = 'MTN'
- `4` = 'Zenith'
- `9` = 'Magma'
- `Default` = 'Wari'

---

## Total Count

**Permanent Tables:** 9
**Temporary Tables:** 2
**Total Tables:** 11

---

## Notes

1. The procedure uses dynamic SQL to build the query based on input parameters
2. Temporary tables are used for filtering and pagination
3. Multiple LEFT JOINs allow for optional related data
4. The Country table is joined twice to get both sending and receiving country information
5. AuxAgentMobileMoneyTransferDetail provides override values when present
6. The procedure supports extensive filtering through various parameters

