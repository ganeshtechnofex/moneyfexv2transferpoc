# Master Database Tables Catalog - Transaction Statement Procedures

## Overview
This document provides a normalized, comprehensive catalog of all database tables and their properties used across transaction statement stored procedures. This consolidated view eliminates duplication and provides a single source of truth for table structures.

---

## Table of Contents
1. [Transaction Types Reference](#transaction-types-reference)
2. [Master Tables Catalog](#master-tables-catalog)
3. [Stored Procedure to Table Mapping](#stored-procedure-to-table-mapping)
4. [Property Usage Matrix](#property-usage-matrix)
5. [Join Relationships](#join-relationships)
6. [Status Code Reference](#status-code-reference)

---

## Transaction Types Reference

| TransferType | TransactionServiceType | Transaction Type | Primary Table |
|--------------|------------------------|------------------|---------------|
| 1 | 6 | Bank Deposit | BankAccountDeposit |
| 2 | 1 | Mobile Wallet | MobileMoneyTransfer |
| 3 | 5 | Cash Pickup | FaxingNonCardTransaction |
| 7 | 7 | KiiBank | KiiBankTransfer |

---

## Master Tables Catalog

### Core Transaction Tables

#### 1. BankAccountDeposit
**Purpose:** Stores bank account deposit transactions  
**Primary Key:** `Id`  
**Used By:** SP_GETTRANSACTIONSTATEMENTOFASENDER, SP_Get_BankTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Transaction identifier | All |
| TransactionDate | datetime | Date of the transaction | All |
| Status | int | Transaction status (0-13) | All |
| PaidFromModule | int | User type who performed transaction (0-5) | SP_Get_BankTransactionStatement |
| Fee | decimal | Transaction fee | All |
| ReceiptNo | nvarchar | Receipt number/transaction identifier | All |
| PaymentReference | nvarchar | Payment reference number | SP_Get_BankTransactionStatement |
| SenderId | int | Foreign key to FaxerInformation | All |
| SendingCountry | nvarchar | Country code for sending country | All |
| ReceivingCountry | nvarchar | Country code for receiving country | All |
| SendingCurrency | nvarchar | Currency code for sending country | All |
| ReceivingCurrency | nvarchar | Currency code for receiving country | All |
| ReceiverName | nvarchar | Name of the receiver | SP_Get_BankTransactionStatement |
| ReceiverCity | nvarchar | City of the receiver | SP_Get_BankTransactionStatement |
| ReceiverAccountNo | nvarchar | Account number of the receiver | SP_Get_BankTransactionStatement |
| SendingAmount | decimal | Amount sent | SP_Get_BankTransactionStatement |
| ReceivingAmount | decimal | Amount received | SP_Get_BankTransactionStatement |
| TotalAmount | decimal | Total transaction amount | All |
| ExchangeRate | decimal | Exchange rate applied | SP_Get_BankTransactionStatement |
| SenderPaymentMode | nvarchar | Payment mode used by sender | SP_Get_BankTransactionStatement |
| IsManualApproveNeeded | bit | Flag indicating if manual approval is needed | SP_Get_BankTransactionStatement |
| IsComplianceApproved | bit | Flag indicating if compliance approved | SP_Get_BankTransactionStatement |
| IsComplianceNeededForTrans | bit | Flag indicating if compliance needed | SP_Get_BankTransactionStatement |
| Apiservice | int | API service used (0-4) | SP_Get_BankTransactionStatement |
| TransferReference | nvarchar | Transfer reference number | SP_Get_BankTransactionStatement |
| IsTransactionDuplicated | bit | Flag indicating if transaction is duplicated | SP_Get_BankTransactionStatement |
| RecipientId | int | Recipient identifier | SP_Get_BankTransactionStatement |
| UpdateByStaffId | int | Staff ID who last updated the transaction | SP_Get_BankTransactionStatement |
| PayingStaffId | int | Staff ID who processed payment | SP_Get_BankTransactionStatement |
| BankId | int | Foreign key to Bank | SP_Get_BankTransactionStatement |
| BankCode | nvarchar | Bank code | SP_Get_BankTransactionStatement |
| BankName | nvarchar | Bank name | SP_Get_BankTransactionStatement |
| IsEuropeTransfer | bit | Flag indicating if it's a Europe transfer | SP_Get_BankTransactionStatement |
| IsManualDeposit | bit | Flag indicating if it's a manual deposit | SP_Get_BankTransactionStatement |

---

#### 2. MobileMoneyTransfer
**Purpose:** Stores mobile money transfer transactions  
**Primary Key:** `Id`  
**Used By:** SP_GETTRANSACTIONSTATEMENTOFASENDER, SP_Get_MobileTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Transaction identifier | All |
| TransactionDate | datetime | Date of the transaction | All |
| Status | int | Transaction status (0-10) | All |
| PaidFromModule | int | Module from which payment was made (0-5) | SP_Get_MobileTransactionStatement |
| Fee | decimal | Transaction fee | All |
| PaidToMobileNo | nvarchar | Mobile number of the recipient | SP_Get_MobileTransactionStatement |
| ReceiverName | nvarchar | Name of the receiver | All |
| ReceiverCity | nvarchar | City of the receiver | SP_Get_MobileTransactionStatement |
| ReceivingCurrency | nvarchar | Currency code for receiving country | All |
| SendingCurrency | nvarchar | Currency code for sending country | All |
| SenderPaymentMode | nvarchar | Payment mode used by sender | SP_Get_MobileTransactionStatement |
| PaymentReference | nvarchar | Payment reference number | SP_Get_MobileTransactionStatement |
| ReceiptNo | nvarchar | Receipt number/transaction identifier | All |
| SenderId | int | Foreign key to FaxerInformation | All |
| SendingCountry | nvarchar | Country code for sending country | All |
| ReceivingCountry | nvarchar | Country code for receiving country | All |
| WalletOperatorId | int | Foreign key to MobileWalletOperator | SP_Get_MobileTransactionStatement |
| IsComplianceApproved | bit | Compliance approval flag | SP_Get_MobileTransactionStatement |
| IsComplianceNeededForTrans | bit | Flag indicating if compliance is needed | SP_Get_MobileTransactionStatement |
| Apiservice | int | API service used (0-9) | SP_Get_MobileTransactionStatement |
| TransferReference | nvarchar | Transfer reference number | SP_Get_MobileTransactionStatement |
| RecipientId | int | Recipient identifier | SP_Get_MobileTransactionStatement |
| UpdateByStaffId | int | Foreign key to StaffInformation | SP_Get_MobileTransactionStatement |
| PayingStaffId | int | Staff ID who processed the payment | SP_Get_MobileTransactionStatement |
| TotalAmount | decimal | Total transaction amount | All |
| SendingAmount | decimal | Amount sent | SP_Get_MobileTransactionStatement |
| ReceivingAmount | decimal | Amount received | SP_Get_MobileTransactionStatement |
| ExchangeRate | decimal | Exchange rate applied | SP_Get_MobileTransactionStatement |

---

#### 3. FaxingNonCardTransaction
**Purpose:** Stores cash pickup/non-card transactions  
**Primary Key:** `Id`  
**Used By:** SP_GETTRANSACTIONSTATEMENTOFASENDER, SP_Get_CashTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Transaction identifier | All |
| TransactionDate | datetime | Date of the transaction | All |
| FaxingStatus | int | Transaction status (0-11) | All |
| OperatingUserType | int | Type of user who performed the transaction (0-5) | SP_Get_CashTransactionStatement |
| FaxingFee | decimal | Transaction fee | All |
| ReceiptNumber | nvarchar | Receipt number/transaction identifier | All |
| MFCN | nvarchar | MoneyFex Control Number | SP_Get_CashTransactionStatement |
| SenderId | int | Foreign key to FaxerInformation | All |
| SendingCountry | nvarchar | Country code for sending country | All |
| ReceivingCountry | nvarchar | Country code for receiving country | All |
| SendingCurrency | nvarchar | Currency code for sending country | All |
| ReceivingCurrency | nvarchar | Currency code for receiving country | All |
| FaxingAmount | decimal | Sending amount | SP_Get_CashTransactionStatement |
| ReceivingAmount | decimal | Amount received | SP_Get_CashTransactionStatement |
| TotalAmount | decimal | Total transaction amount | All |
| ExchangeRate | decimal | Exchange rate applied | SP_Get_CashTransactionStatement |
| SenderPaymentMode | nvarchar | Payment mode used by sender | SP_Get_CashTransactionStatement |
| PaymentReference | nvarchar | Payment reference number | SP_Get_CashTransactionStatement |
| IsComplianceApproved | bit | Flag indicating if compliance approved | SP_Get_CashTransactionStatement |
| IsComplianceNeededForTrans | bit | Flag indicating if compliance needed | SP_Get_CashTransactionStatement |
| PayingStaffId | int | Staff ID who processed payment | SP_Get_CashTransactionStatement |
| Apiservice | int | API service used (0-4) | SP_Get_CashTransactionStatement |
| TransferReference | nvarchar | Transfer reference number | SP_Get_CashTransactionStatement |
| NonCardRecieverId | int | Foreign key to ReceiversDetails | SP_Get_CashTransactionStatement |
| RecipientId | int | Foreign key to Recipients | SP_Get_CashTransactionStatement |
| UpdatedByStaffId | int | Staff ID who last updated the transaction | SP_Get_CashTransactionStatement |

---

#### 4. KiiBankTransfer
**Purpose:** Stores KiiBank transfer transactions  
**Primary Key:** `Id`  
**Used By:** SP_Get_KiiBankTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Transaction identifier | All |
| TransactionDate | datetime | Date of the transaction | All |
| Status | int | Transaction status (0-10) | All |
| PaidFromModule | int | Type of user who performed the transaction (0-5) | All |
| Fee | decimal | Transaction fee | All |
| ReceiptNo | nvarchar | Receipt number/transaction identifier | All |
| TransactionReference | nvarchar | Transaction reference number | All |
| PaymentReference | nvarchar | Payment reference number | All |
| SenderId | int | Foreign key to FaxerInformation | All |
| SendingCountry | nvarchar | Country code for sending country | All |
| ReceivingCountry | nvarchar | Country code for receiving country | All |
| SendingCurrency | nvarchar | Currency code for sending country | All |
| ReceivingCurrency | nvarchar | Currency code for receiving country | All |
| ReceiverName | nvarchar | Name of the receiver | All |
| AccountNo | nvarchar | Account number of the receiver | All |
| SendingAmount | decimal | Amount sent | All |
| ReceivingAmount | decimal | Amount received | All |
| TotalAmount | decimal | Total transaction amount | All |
| ExchangeRate | decimal | Exchange rate applied | All |
| SenderPaymentMode | nvarchar | Payment mode used by sender | All |
| IsComplianceApproved | bit | Flag indicating if compliance approved | All |
| IsComplianceNeededForTrans | bit | Flag indicating if compliance needed | All |
| RecipientId | int | Recipient identifier | All |
| UpdateByStaffId | int | Staff ID who last updated the transaction | All |
| PayingStaffId | int | Staff ID who processed payment | All |

---

### Reference Tables

#### 5. FaxerInformation
**Purpose:** Stores sender/customer information  
**Primary Key:** `Id`  
**Used By:** All procedures

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Sender identifier | All |
| FirstName | nvarchar | First name of sender | All (except SP_GETTRANSACTIONSTATEMENTOFASENDER) |
| MiddleName | nvarchar | Middle name of sender | All (except SP_GETTRANSACTIONSTATEMENTOFASENDER) |
| LastName | nvarchar | Last name of sender | All (except SP_GETTRANSACTIONSTATEMENTOFASENDER) |
| Email | nvarchar | Email address of sender | All (except SP_GETTRANSACTIONSTATEMENTOFASENDER) |
| AccountNo | nvarchar | Account number (MFCode) | All (except SP_GETTRANSACTIONSTATEMENTOFASENDER) |
| PhoneNumber | nvarchar | Phone number of sender | All (except SP_GETTRANSACTIONSTATEMENTOFASENDER) |
| IsBusiness | bit | Flag indicating if sender is a business | All (except SP_GETTRANSACTIONSTATEMENTOFASENDER) |
| Address1 | nvarchar | Billing address of the sender | SP_GETTRANSACTIONSTATEMENTOFASENDER only |

---

#### 6. Country
**Purpose:** Stores country information (used twice for sending and receiving countries)  
**Primary Key:** `CountryCode`  
**Used By:** All procedures

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| CountryCode | nvarchar | Country code identifier | All |
| CountryName | nvarchar | Name of the country | All (except SP_GETTRANSACTIONSTATEMENTOFASENDER) |
| Currency | nvarchar | Currency code | All |
| CurrencySymbol | nvarchar | Currency symbol | All (except SP_GETTRANSACTIONSTATEMENTOFASENDER) |

---

#### 7. StaffInformation
**Purpose:** Stores staff information for transaction updates  
**Primary Key:** `Id`  
**Used By:** SP_Get_MobileTransactionStatement, SP_Get_BankTransactionStatement, SP_Get_CashTransactionStatement, SP_Get_KiiBankTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Staff identifier | All |
| FirstName | nvarchar | First name of staff member | All |
| MiddleName | nvarchar | Middle name of staff member | All |
| LastName | nvarchar | Last name of staff member | All |

---

#### 8. FaxerLogin
**Purpose:** Stores sender login information  
**Primary Key:** `FaxerId`  
**Used By:** SP_Get_MobileTransactionStatement, SP_Get_BankTransactionStatement, SP_Get_CashTransactionStatement, SP_Get_KiiBankTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| FaxerId | int | Foreign key to FaxerInformation.Id | All |
| IsActive | bit | Flag indicating if sender account is active | All |

---

### Supporting Tables

#### 9. Bank
**Purpose:** Stores bank information for payout providers  
**Primary Key:** `Id`  
**Used By:** SP_Get_BankTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Bank identifier | All |
| Name | nvarchar | Name of the bank/payout provider | All |

---

#### 10. MobileWalletOperator
**Purpose:** Stores wallet operator/provider information  
**Primary Key:** `Id`  
**Used By:** SP_Get_MobileTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Wallet operator identifier | All |
| Name | nvarchar | Name of the wallet operator/payout provider | All |

---

#### 11. Recipients
**Purpose:** Stores recipient information for cash pickup transactions  
**Primary Key:** `Id`  
**Used By:** SP_Get_CashTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Recipient identifier | All |
| ReceiverName | nvarchar | Name of the receiver | All |

---

#### 12. ReceiversDetails
**Purpose:** Stores detailed receiver information for cash pickup transactions  
**Primary Key:** `Id`  
**Used By:** SP_GETTRANSACTIONSTATEMENTOFASENDER, SP_Get_CashTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| Id | int | Receiver identifier | All |
| PhoneNumber | nvarchar | Phone number of receiver | SP_Get_CashTransactionStatement |
| FullName | nvarchar | Full name of receiver | All |
| City | nvarchar | City of receiver | SP_Get_CashTransactionStatement |

---

### Auxiliary Agent Tables

#### 13. AuxAgentBankAccountDepositDetail
**Purpose:** Stores auxiliary agent bank account deposit details (overrides main transaction values)  
**Primary Key:** `BankAccountDepositId`  
**Used By:** SP_Get_BankTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| BankAccountDepositId | int | Foreign key to BankAccountDeposit.Id | All |
| Fee | decimal | Override fee amount | All |
| SendingAmount | decimal | Override sending amount | All |
| ReceivingAmount | decimal | Override receiving amount | All |
| TotalAmount | decimal | Override total amount | All |
| ExchangeRate | decimal | Override exchange rate | All |

**Note:** These values take precedence over the main transaction values when present.

---

#### 14. AuxAgentMobileMoneyTransferDetail
**Purpose:** Stores auxiliary agent mobile money transfer details (overrides main transaction values)  
**Primary Key:** `MobileMoneyTransferId`  
**Used By:** SP_Get_MobileTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| MobileMoneyTransferId | int | Foreign key to MobileMoneyTransfer.Id | All |
| Fee | decimal | Override fee amount | All |
| SendingAmount | decimal | Override sending amount | All |
| ReceivingAmount | decimal | Override receiving amount | All |
| TotalAmount | decimal | Override total amount | All |
| ExchangeRate | decimal | Override exchange rate | All |

**Note:** These values take precedence over the main transaction values when present.

---

#### 15. AuxAgentCashPickUpDetail
**Purpose:** Stores auxiliary agent cash pickup details (overrides main transaction values)  
**Primary Key:** `CashPickUpId`  
**Used By:** SP_Get_CashTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| CashPickUpId | int | Foreign key to FaxingNonCardTransaction.Id | All |
| Fee | decimal | Override fee amount | All |
| SendingAmount | decimal | Override sending amount | All |
| ReceivingAmount | decimal | Override receiving amount | All |
| TotalAmount | decimal | Override total amount | All |
| ExchangeRate | decimal | Override exchange rate | All |

**Note:** These values take precedence over the main transaction values when present.

---

### Payment & Card Information Tables

#### 16. CardTopUpCreditDebitInformation
**Purpose:** Stores credit/debit card information for card payments  
**Primary Key:** Composite (TransferType, CardTransactionId)  
**Used By:** SP_Get_MobileTransactionStatement, SP_Get_BankTransactionStatement, SP_Get_CashTransactionStatement, SP_Get_KiiBankTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures | TransferType Filter |
|----------|-----------|-------------|-------------------|---------------------|
| TransferType | int | Transfer type identifier | All | Varies by procedure |
| CardTransactionId | int | Foreign key to transaction ID | All | - |
| CardNumber | nvarchar | Card number (masked) | All | - |

**TransferType Usage:**
- SP_Get_MobileTransactionStatement: TransferType = 3
- SP_Get_BankTransactionStatement: TransferType = 4
- SP_Get_CashTransactionStatement: TransferType = 2
- SP_Get_KiiBankTransactionStatement: TransferType = 3

---

#### 17. SecureTradingApiResponseTransactionLog
**Purpose:** Stores Secure Trading API response transaction logs (card payment information)  
**Primary Key:** `orderreference`  
**Used By:** SP_GETTRANSACTIONSTATEMENTOFASENDER

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| orderreference | nvarchar | Order reference matching transaction receipt number | All |
| issuer | nvarchar | Card issuer name | All |
| maskedpan | nvarchar | Masked primary account number (last 4 digits extracted) | All |

---

### Transaction Management Tables

#### 18. ReinitializeTransaction
**Purpose:** Stores information about reinitialized transactions  
**Primary Key:** `NewReceiptNo`  
**Used By:** SP_Get_MobileTransactionStatement, SP_Get_BankTransactionStatement, SP_Get_CashTransactionStatement

**Properties:**
| Property | Data Type | Description | Used In Procedures |
|----------|-----------|-------------|-------------------|
| NewReceiptNo | nvarchar | New receipt number after reinitialization | All |
| ReceiptNo | nvarchar | Original receipt number | All |
| CreatedByName | nvarchar | Name of staff who created the reinitialization | All |
| Date | datetime | Date of reinitialization | All |

**Note:** NOT used in SP_Get_KiiBankTransactionStatement

---

## Stored Procedure to Table Mapping

| Stored Procedure | Transaction Types | Total Tables | Permanent Tables | Temporary Tables |
|-----------------|-------------------|--------------|------------------|------------------|
| SP_GETTRANSACTIONSTATEMENTOFASENDER | Bank, Mobile, Cash | 9 | 7 | 2 |
| SP_Get_BankTransactionStatement | Bank | 11 | 9 | 2 |
| SP_Get_MobileTransactionStatement | Mobile | 11 | 9 | 2 |
| SP_Get_CashTransactionStatement | Cash | 12 | 10 | 2 |
| SP_Get_KiiBankTransactionStatement | KiiBank | 8 | 6 | 2 |

### Detailed Mapping

#### SP_GETTRANSACTIONSTATEMENTOFASENDER
- BankAccountDeposit
- MobileMoneyTransfer
- FaxingNonCardTransaction
- SecureTradingApiResponseTransactionLog
- FaxerInformation
- Country
- ReceiversDetails
- ##bankDeposit (temp)
- #finalData (temp)

#### SP_Get_BankTransactionStatement
- BankAccountDeposit
- FaxerInformation
- Bank
- StaffInformation
- AuxAgentBankAccountDepositDetail
- FaxerLogin
- Country
- CardTopUpCreditDebitInformation
- ReinitializeTransaction
- ##BankDeposit (temp)
- #finalData (temp)

#### SP_Get_MobileTransactionStatement
- MobileMoneyTransfer
- MobileWalletOperator
- FaxerInformation
- StaffInformation
- AuxAgentMobileMoneyTransferDetail
- Country
- CardTopUpCreditDebitInformation
- FaxerLogin
- ReinitializeTransaction
- ##Mobile (temp)
- #finalData (temp)

#### SP_Get_CashTransactionStatement
- FaxingNonCardTransaction
- Recipients
- FaxerInformation
- StaffInformation
- AuxAgentCashPickUpDetail
- ReceiversDetails
- FaxerLogin
- Country
- CardTopUpCreditDebitInformation
- ReinitializeTransaction
- ##Cash (temp)
- #finalData (temp)

#### SP_Get_KiiBankTransactionStatement
- KiiBankTransfer
- FaxerInformation
- StaffInformation
- FaxerLogin
- Country
- CardTopUpCreditDebitInformation
- ##Mobile (temp)
- #finalData (temp)

---

## Property Usage Matrix

### Common Properties Across Transaction Tables

| Property | BankAccountDeposit | MobileMoneyTransfer | FaxingNonCardTransaction | KiiBankTransfer |
|----------|-------------------|---------------------|-------------------------|-----------------|
| Id | ✓ | ✓ | ✓ | ✓ |
| TransactionDate | ✓ | ✓ | ✓ | ✓ |
| Status/FaxingStatus | Status | Status | FaxingStatus | Status |
| Fee/FaxingFee | Fee | Fee | FaxingFee | Fee |
| ReceiptNo/ReceiptNumber | ReceiptNo | ReceiptNo | ReceiptNumber | ReceiptNo |
| SenderId | ✓ | ✓ | ✓ | ✓ |
| SendingCountry | ✓ | ✓ | ✓ | ✓ |
| ReceivingCountry | ✓ | ✓ | ✓ | ✓ |
| SendingCurrency | ✓ | ✓ | ✓ | ✓ |
| ReceivingCurrency | ✓ | ✓ | ✓ | ✓ |
| TotalAmount | ✓ | ✓ | ✓ | ✓ |
| SendingAmount | ✓ | ✓ | ✓ | ✓ |
| ReceivingAmount | ✓ | ✓ | ✓ | ✓ |
| ExchangeRate | ✓ | ✓ | ✓ | ✓ |
| PaymentReference | ✓ | ✓ | ✓ | ✓ |
| SenderPaymentMode | ✓ | ✓ | ✓ | ✓ |
| IsComplianceApproved | ✓ | ✓ | ✓ | ✓ |
| IsComplianceNeededForTrans | ✓ | ✓ | ✓ | ✓ |
| RecipientId | ✓ | ✓ | ✓ | ✓ |
| UpdateByStaffId/UpdatedByStaffId | UpdateByStaffId | UpdateByStaffId | UpdatedByStaffId | UpdateByStaffId |
| PayingStaffId | ✓ | ✓ | ✓ | ✓ |
| PaidFromModule/OperatingUserType | PaidFromModule | PaidFromModule | OperatingUserType | PaidFromModule |

---

## Join Relationships

### Standard Join Pattern
All procedures follow a similar join pattern:

```
[TransactionTable] (Primary)
├── INNER JOIN FaxerInformation (via SenderId)
│   └── LEFT JOIN FaxerLogin (via FaxerId)
├── INNER JOIN Country [sendingCountry] (via SendingCountry)
├── INNER JOIN Country [receivingCountry] (via ReceivingCountry)
├── LEFT JOIN StaffInformation (via UpdateByStaffId/UpdatedByStaffId)
├── LEFT JOIN [AuxAgentTable] (via TransactionId) [if applicable]
├── LEFT JOIN CardTopUpCreditDebitInformation (via Id, with TransferType filter)
└── LEFT JOIN ReinitializeTransaction (via ReceiptNo) [if applicable]
```

### Procedure-Specific Joins

**SP_Get_BankTransactionStatement:**
- LEFT JOIN Bank (via BankId)

**SP_Get_MobileTransactionStatement:**
- INNER JOIN MobileWalletOperator (via WalletOperatorId)

**SP_Get_CashTransactionStatement:**
- INNER JOIN Recipients (via RecipientId)
- LEFT JOIN ReceiversDetails (via NonCardRecieverId)

---

## Status Code Reference

### BankAccountDeposit.Status
| Code | Status Name |
|------|-------------|
| 0 | In Progress |
| 1 | In Progress |
| 2 | Cancelled |
| 3 | Paid |
| 4 | In progress |
| 5 | Failed |
| 6 | Payment Pending |
| 7 | In progress (ID Check) |
| 8 | In progress (MoneyFex Bank Deposit) |
| 9 | In progress |
| 10 | Abnormal /  Abnormal (with leading space) |
| 11 | Full Refund |
| 12 | Partial Refund |
| 13 | In Progress |

### MobileMoneyTransfer.Status
| Code | Status Name |
|------|-------------|
| 0 | Failed |
| 1 | In Progress |
| 2 | Paid |
| 3 | Cancelled |
| 4 | Payment Pending |
| 5 | In Progress (ID Check) |
| 6 | In progress |
| 7 | In progress |
| 8 | In progress |
| 9 | Refund |
| 10 | Refund |

### FaxingNonCardTransaction.FaxingStatus
| Code | Status Name |
|------|-------------|
| 0 | Not Received |
| 1 | Received |
| 2 | Cancelled |
| 3 | Refunded |
| 4 | In Progress |
| 5 | Completed |
| 6 | Payment Pending |
| 7 | In Progress (ID Check) |
| 8 | In progress |
| 9 | Refund |
| 10 | Refund |
| 11 | In Progress |

### KiiBankTransfer.Status
| Code | Status Name |
|------|-------------|
| 0 | Failed |
| 1 | In Progress |
| 2 | Paid |
| 3 | Cancelled |
| 4 | Payment Pending |
| 5 | In Progress (ID Check) |
| 6 | In progress |
| 7 | In progress |
| 8 | In progress / In progress  (with trailing space) |
| 9 | Refund |
| 10 | Refund |

### PaidFromModule / OperatingUserType
| Code | Value |
|------|-------|
| 0 | Sender |
| 1 | '' (empty) |
| 2 | '' (empty) |
| 3 | Agent |
| 4 | Admin Staff |
| 5 | '' (empty) or ' ' (space for Cash) |

### Apiservice (where applicable)
| Code | Service Name |
|------|--------------|
| 0 | VGG |
| 1 | TransferZero |
| 2 | EmergentApi |
| 3 | MTN |
| 4 | Zenith |
| 9 | Magma (Mobile only) |
| Default | Wari |

---

## Temporary Tables

All procedures use a similar pattern for temporary tables:

### Pattern
1. **Global Temporary Table (##TableName)**
   - Created via dynamic SQL
   - Contains filtered transaction data
   - Includes: Id, TransactionDate, TransferType, TransactionServiceType, StatusName, TransactionPerformedBy, HasFee, PayoutProviderName

2. **Local Temporary Table (#finalData)**
   - Created from global temp table
   - Contains paginated results
   - Ordered by TransactionDate DESC

### Temporary Table Names by Procedure
- SP_GETTRANSACTIONSTATEMENTOFASENDER: ##bankDeposit, #finalData
- SP_Get_BankTransactionStatement: ##BankDeposit, #finalData
- SP_Get_MobileTransactionStatement: ##Mobile, #finalData
- SP_Get_CashTransactionStatement: ##Cash, #finalData
- SP_Get_KiiBankTransactionStatement: ##Mobile, #finalData (note: legacy naming)

---

## Summary Statistics

### Total Unique Tables: 18
- **Core Transaction Tables:** 4
- **Reference Tables:** 4
- **Supporting Tables:** 2
- **Auxiliary Agent Tables:** 3
- **Payment & Card Information Tables:** 2
- **Transaction Management Tables:** 1
- **Legacy/Alternative Tables:** 2 (SecureTradingApiResponseTransactionLog, Recipients)

### Table Usage Frequency
- **Used in 5 procedures:** FaxerInformation, Country
- **Used in 4 procedures:** StaffInformation, FaxerLogin, CardTopUpCreditDebitInformation
- **Used in 3 procedures:** ReinitializeTransaction
- **Used in 2 procedures:** BankAccountDeposit, MobileMoneyTransfer, FaxingNonCardTransaction, ReceiversDetails
- **Used in 1 procedure:** All others

---

## Notes

1. **Normalization:** This catalog consolidates all table definitions to eliminate duplication across procedure-specific documentation.

2. **Property Variations:** Some properties have different names across tables but serve the same purpose (e.g., `Status` vs `FaxingStatus`, `ReceiptNo` vs `ReceiptNumber`).

3. **Auxiliary Agent Pattern:** Bank, Mobile, and Cash transactions support auxiliary agent overrides through dedicated detail tables. KiiBank does not.

4. **Reinitialization:** Only Bank, Mobile, and Cash transactions support reinitialization tracking. KiiBank does not.

5. **Card Payment Information:** Different procedures use different TransferType filters when joining CardTopUpCreditDebitInformation.

6. **Temporary Table Naming:** SP_Get_KiiBankTransactionStatement uses `##Mobile` despite handling KiiBank transactions - this appears to be legacy naming.

