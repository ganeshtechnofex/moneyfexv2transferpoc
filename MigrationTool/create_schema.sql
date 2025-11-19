-- MoneyFex Database Schema Creation Script
-- Run this script on your PostgreSQL database before running the migration
-- Database: moneyfex_db1 (or your target database name)

-- Create countries table
CREATE TABLE IF NOT EXISTS countries (
    "CountryCode" VARCHAR(3) NOT NULL PRIMARY KEY,
    "CountryName" VARCHAR(100) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL,
    "CurrencySymbol" VARCHAR(10) NOT NULL,
    "IsActive" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

-- Create recipients table
CREATE TABLE IF NOT EXISTS recipients (
    "Id" SERIAL PRIMARY KEY,
    "ReceiverName" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL
);

-- Create staff table
CREATE TABLE IF NOT EXISTS staff (
    "Id" SERIAL PRIMARY KEY,
    "FirstName" VARCHAR(100) NOT NULL,
    "MiddleName" VARCHAR(100),
    "LastName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "IsActive" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

-- Create banks table
CREATE TABLE IF NOT EXISTS banks (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Code" VARCHAR(50),
    "CountryCode" VARCHAR(3),
    "IsActive" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_banks_countries_CountryCode" FOREIGN KEY ("CountryCode") REFERENCES countries("CountryCode") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_banks_CountryCode" ON banks("CountryCode");

-- Create mobile_wallet_operators table
CREATE TABLE IF NOT EXISTS mobile_wallet_operators (
    "Id" SERIAL PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "CountryCode" VARCHAR(3),
    "MobileNetworkCode" TEXT,
    "PayoutProviderId" INTEGER,
    "IsActive" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_mobile_wallet_operators_countries_CountryCode" FOREIGN KEY ("CountryCode") REFERENCES countries("CountryCode") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_mobile_wallet_operators_CountryCode" ON mobile_wallet_operators("CountryCode");

-- Create receiver_details table
CREATE TABLE IF NOT EXISTS receiver_details (
    "Id" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(200) NOT NULL,
    "PhoneNumber" VARCHAR(50),
    "City" VARCHAR(100),
    "CountryCode" VARCHAR(3),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_receiver_details_countries_CountryCode" FOREIGN KEY ("CountryCode") REFERENCES countries("CountryCode") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_receiver_details_CountryCode" ON receiver_details("CountryCode");

-- Create senders table
CREATE TABLE IF NOT EXISTS senders (
    "Id" SERIAL PRIMARY KEY,
    "FirstName" VARCHAR(100) NOT NULL,
    "MiddleName" VARCHAR(100),
    "LastName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(255) NOT NULL,
    "PhoneNumber" VARCHAR(50),
    "AccountNo" VARCHAR(50) UNIQUE,
    "Address1" TEXT,
    "Address2" TEXT,
    "City" TEXT,
    "State" TEXT,
    "CountryCode" VARCHAR(3),
    "PostalCode" TEXT,
    "IsBusiness" BOOLEAN NOT NULL,
    "IsActive" BOOLEAN NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_senders_countries_CountryCode" FOREIGN KEY ("CountryCode") REFERENCES countries("CountryCode") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_senders_AccountNo" ON senders("AccountNo");
CREATE INDEX IF NOT EXISTS "IX_senders_CountryCode" ON senders("CountryCode");
CREATE INDEX IF NOT EXISTS "IX_senders_Email" ON senders("Email");

-- Create sender_logins table
CREATE TABLE IF NOT EXISTS sender_logins (
    "SenderId" INTEGER PRIMARY KEY,
    "IsActive" BOOLEAN NOT NULL,
    "LastLoginAt" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_sender_logins_senders_SenderId" FOREIGN KEY ("SenderId") REFERENCES senders("Id") ON DELETE CASCADE
);

-- Create transactions table
CREATE TABLE IF NOT EXISTS transactions (
    "Id" SERIAL PRIMARY KEY,
    "TransactionDate" TIMESTAMPTZ NOT NULL,
    "ReceiptNo" VARCHAR(50) NOT NULL UNIQUE,
    "SenderId" INTEGER NOT NULL,
    "SendingCountryCode" VARCHAR(3) NOT NULL,
    "ReceivingCountryCode" VARCHAR(3) NOT NULL,
    "SendingCurrency" TEXT NOT NULL,
    "ReceivingCurrency" TEXT NOT NULL,
    "SendingAmount" NUMERIC NOT NULL,
    "ReceivingAmount" NUMERIC NOT NULL,
    "Fee" NUMERIC NOT NULL,
    "TotalAmount" NUMERIC NOT NULL,
    "ExchangeRate" NUMERIC NOT NULL,
    "PaymentReference" TEXT,
    "SenderPaymentMode" INTEGER NOT NULL,
    "TransactionModule" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "ApiService" INTEGER,
    "TransferReference" VARCHAR(100),
    "RecipientId" INTEGER,
    "IsComplianceNeeded" BOOLEAN NOT NULL,
    "IsComplianceApproved" BOOLEAN NOT NULL,
    "ComplianceApprovedBy" INTEGER,
    "ComplianceApprovedAt" TIMESTAMPTZ,
    "PayingStaffId" INTEGER,
    "PayingStaffName" VARCHAR(200),
    "UpdatedByStaffId" INTEGER,
    "AgentCommission" NUMERIC(18,2),
    "ExtraFee" NUMERIC(18,2),
    "Margin" NUMERIC(18,2),
    "MFRate" NUMERIC(18,6),
    "TransferZeroSenderId" VARCHAR(100),
    "ReasonForTransfer" INTEGER,
    "CardProcessorApi" INTEGER,
    "IsFromMobile" BOOLEAN NOT NULL,
    "TransactionUpdateDate" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_transactions_countries_ReceivingCountryCode" FOREIGN KEY ("ReceivingCountryCode") REFERENCES countries("CountryCode") ON DELETE RESTRICT,
    CONSTRAINT "FK_transactions_countries_SendingCountryCode" FOREIGN KEY ("SendingCountryCode") REFERENCES countries("CountryCode") ON DELETE RESTRICT,
    CONSTRAINT "FK_transactions_senders_SenderId" FOREIGN KEY ("SenderId") REFERENCES senders("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_transactions_staff_ComplianceApprovedBy" FOREIGN KEY ("ComplianceApprovedBy") REFERENCES staff("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_transactions_staff_PayingStaffId" FOREIGN KEY ("PayingStaffId") REFERENCES staff("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_transactions_staff_UpdatedByStaffId" FOREIGN KEY ("UpdatedByStaffId") REFERENCES staff("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_transactions_ComplianceApprovedBy" ON transactions("ComplianceApprovedBy");
CREATE INDEX IF NOT EXISTS "IX_transactions_PayingStaffId" ON transactions("PayingStaffId");
CREATE INDEX IF NOT EXISTS "IX_transactions_PaymentReference" ON transactions("PaymentReference");
CREATE INDEX IF NOT EXISTS "IX_transactions_ReceiptNo" ON transactions("ReceiptNo");
CREATE INDEX IF NOT EXISTS "IX_transactions_ReceivingCountryCode" ON transactions("ReceivingCountryCode");
CREATE INDEX IF NOT EXISTS "IX_transactions_SenderId" ON transactions("SenderId");
CREATE INDEX IF NOT EXISTS "IX_transactions_SendingCountryCode" ON transactions("SendingCountryCode");
CREATE INDEX IF NOT EXISTS "IX_transactions_Status" ON transactions("Status");
CREATE INDEX IF NOT EXISTS "IX_transactions_TransactionDate" ON transactions("TransactionDate");
CREATE INDEX IF NOT EXISTS "IX_transactions_UpdatedByStaffId" ON transactions("UpdatedByStaffId");

-- Create bank_account_deposits table
CREATE TABLE IF NOT EXISTS bank_account_deposits (
    "TransactionId" INTEGER PRIMARY KEY,
    "BankId" INTEGER,
    "BankName" VARCHAR(200),
    "BankCode" VARCHAR(50),
    "ReceiverAccountNo" VARCHAR(100),
    "ReceiverName" VARCHAR(200),
    "ReceiverCity" VARCHAR(100),
    "ReceiverCountry" VARCHAR(3),
    "ReceiverMobileNo" VARCHAR(50),
    "RecipientId" INTEGER,
    "IsManualDeposit" BOOLEAN NOT NULL,
    "IsManualApprovalNeeded" BOOLEAN NOT NULL,
    "IsManuallyApproved" BOOLEAN NOT NULL,
    "IsEuropeTransfer" BOOLEAN NOT NULL,
    "IsTransactionDuplicated" BOOLEAN NOT NULL,
    "DuplicateTransactionReceiptNo" VARCHAR(50),
    "IsBusiness" BOOLEAN NOT NULL,
    "HasMadePaymentToBankAccount" BOOLEAN NOT NULL,
    "TransactionDescription" VARCHAR(500),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_bank_account_deposits_banks_BankId" FOREIGN KEY ("BankId") REFERENCES banks("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_bank_account_deposits_recipients_RecipientId" FOREIGN KEY ("RecipientId") REFERENCES recipients("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_bank_account_deposits_transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES transactions("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_bank_account_deposits_BankId" ON bank_account_deposits("BankId");
CREATE INDEX IF NOT EXISTS "IX_bank_account_deposits_RecipientId" ON bank_account_deposits("RecipientId");

-- Create mobile_money_transfers table
CREATE TABLE IF NOT EXISTS mobile_money_transfers (
    "TransactionId" INTEGER PRIMARY KEY,
    "WalletOperatorId" INTEGER NOT NULL,
    "PaidToMobileNo" VARCHAR(50) NOT NULL,
    "ReceiverName" VARCHAR(200),
    "ReceiverCity" VARCHAR(100),
    "RecipientId" INTEGER,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_mobile_money_transfers_mobile_wallet_operators_WalletOperatorId" FOREIGN KEY ("WalletOperatorId") REFERENCES mobile_wallet_operators("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_mobile_money_transfers_recipients_RecipientId" FOREIGN KEY ("RecipientId") REFERENCES recipients("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_mobile_money_transfers_transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES transactions("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_mobile_money_transfers_RecipientId" ON mobile_money_transfers("RecipientId");
CREATE INDEX IF NOT EXISTS "IX_mobile_money_transfers_WalletOperatorId" ON mobile_money_transfers("WalletOperatorId");

-- Create cash_pickups table
CREATE TABLE IF NOT EXISTS cash_pickups (
    "TransactionId" INTEGER PRIMARY KEY,
    "MFCN" VARCHAR(50) UNIQUE,
    "RecipientId" INTEGER,
    "NonCardReceiverId" INTEGER,
    "RecipientIdentityCardId" INTEGER,
    "RecipientIdentityCardNumber" VARCHAR(100),
    "IsApprovedByAdmin" BOOLEAN NOT NULL,
    "AgentStaffName" VARCHAR(200),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_cash_pickups_receiver_details_NonCardReceiverId" FOREIGN KEY ("NonCardReceiverId") REFERENCES receiver_details("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_cash_pickups_recipients_RecipientId" FOREIGN KEY ("RecipientId") REFERENCES recipients("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_cash_pickups_transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES transactions("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_cash_pickups_MFCN" ON cash_pickups("MFCN");
CREATE INDEX IF NOT EXISTS "IX_cash_pickups_NonCardReceiverId" ON cash_pickups("NonCardReceiverId");
CREATE INDEX IF NOT EXISTS "IX_cash_pickups_RecipientId" ON cash_pickups("RecipientId");

-- Create reinitialize_transactions table
CREATE TABLE IF NOT EXISTS reinitialize_transactions (
    "Id" SERIAL PRIMARY KEY,
    "ReceiptNo" VARCHAR(50) NOT NULL,
    "NewReceiptNo" VARCHAR(50) NOT NULL UNIQUE,
    "CreatedById" INTEGER,
    "CreatedByName" VARCHAR(200),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_reinitialize_transactions_staff_CreatedById" FOREIGN KEY ("CreatedById") REFERENCES staff("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_reinitialize_transactions_CreatedById" ON reinitialize_transactions("CreatedById");
CREATE INDEX IF NOT EXISTS "IX_reinitialize_transactions_NewReceiptNo" ON reinitialize_transactions("NewReceiptNo");
CREATE INDEX IF NOT EXISTS "IX_reinitialize_transactions_ReceiptNo" ON reinitialize_transactions("ReceiptNo");

-- Create card_payment_information table
CREATE TABLE IF NOT EXISTS card_payment_information (
    "Id" SERIAL PRIMARY KEY,
    "TransactionId" INTEGER,
    "CardTransactionId" INTEGER,
    "NonCardTransactionId" INTEGER,
    "TopUpSomeoneElseTransactionId" INTEGER,
    "NameOnCard" VARCHAR(200),
    "CardNumber" VARCHAR(50),
    "ExpiryDate" VARCHAR(10),
    "IsSavedCard" BOOLEAN NOT NULL,
    "AutoRecharged" BOOLEAN NOT NULL,
    "TransferType" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_card_payment_information_transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES transactions("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_card_payment_information_CardTransactionId" ON card_payment_information("CardTransactionId");
CREATE INDEX IF NOT EXISTS "IX_card_payment_information_NonCardTransactionId" ON card_payment_information("NonCardTransactionId");
CREATE INDEX IF NOT EXISTS "IX_card_payment_information_TopUpSomeoneElseTransactionId" ON card_payment_information("TopUpSomeoneElseTransactionId");
CREATE INDEX IF NOT EXISTS "IX_card_payment_information_TransactionId" ON card_payment_information("TransactionId");
CREATE INDEX IF NOT EXISTS "IX_card_payment_information_TransferType" ON card_payment_information("TransferType");

-- Create kiibank_transfers table (if needed)
CREATE TABLE IF NOT EXISTS kiibank_transfers (
    "TransactionId" INTEGER PRIMARY KEY,
    "AccountNo" VARCHAR(100),
    "ReceiverName" VARCHAR(200),
    "AccountOwnerName" VARCHAR(200),
    "AccountHolderPhoneNo" VARCHAR(50),
    "BankId" INTEGER,
    "BankBranchId" INTEGER,
    "BankBranchCode" VARCHAR(50),
    "TransactionReference" VARCHAR(100),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_kiibank_transfers_banks_BankId" FOREIGN KEY ("BankId") REFERENCES banks("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_kiibank_transfers_transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES transactions("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_kiibank_transfers_BankId" ON kiibank_transfers("BankId");

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'Database schema created successfully!';
END $$;

