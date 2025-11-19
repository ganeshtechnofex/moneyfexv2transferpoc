-- MoneyFex Modular Monolithic Architecture
-- PostgreSQL Database Schema
-- Created for Transaction Management System

-- ============================================
-- Core Reference Tables
-- ============================================

-- Countries Table
CREATE TABLE IF NOT EXISTS countries (
    country_code VARCHAR(3) PRIMARY KEY,
    country_name VARCHAR(100) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    currency_symbol VARCHAR(10) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_countries_currency ON countries(currency);
CREATE INDEX idx_countries_active ON countries(is_active);

-- Banks Table
CREATE TABLE IF NOT EXISTS banks (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50),
    country_code VARCHAR(3) REFERENCES countries(country_code),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_banks_country ON banks(country_code);
CREATE INDEX idx_banks_active ON banks(is_active);

-- Mobile Wallet Operators Table
CREATE TABLE IF NOT EXISTS mobile_wallet_operators (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    country_code VARCHAR(3) REFERENCES countries(country_code),
    mobile_network_code VARCHAR(50),
    payout_provider_id INTEGER,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_wallet_operators_country ON mobile_wallet_operators(country_code);
CREATE INDEX idx_wallet_operators_active ON mobile_wallet_operators(is_active);

-- Senders (FaxerInformation) Table
CREATE TABLE IF NOT EXISTS senders (
    id SERIAL PRIMARY KEY,
    first_name VARCHAR(100),
    middle_name VARCHAR(100),
    last_name VARCHAR(100),
    email VARCHAR(255) NOT NULL,
    phone_number VARCHAR(50),
    account_no VARCHAR(50) UNIQUE,
    address1 VARCHAR(500),
    address2 VARCHAR(500),
    city VARCHAR(100),
    state VARCHAR(100),
    country_code VARCHAR(3) REFERENCES countries(country_code),
    postal_code VARCHAR(20),
    is_business BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_senders_email ON senders(email);
CREATE INDEX idx_senders_account_no ON senders(account_no);
CREATE INDEX idx_senders_phone ON senders(phone_number);
CREATE INDEX idx_senders_country ON senders(country_code);

-- Sender Login Table
CREATE TABLE IF NOT EXISTS sender_logins (
    sender_id INTEGER PRIMARY KEY REFERENCES senders(id) ON DELETE CASCADE,
    is_active BOOLEAN DEFAULT TRUE,
    last_login_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Staff Information Table
CREATE TABLE IF NOT EXISTS staff (
    id SERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_staff_email ON staff(email);
CREATE INDEX idx_staff_active ON staff(is_active);

-- Recipients Table (for Cash Pickup)
CREATE TABLE IF NOT EXISTS recipients (
    id SERIAL PRIMARY KEY,
    receiver_name VARCHAR(200) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Receiver Details Table (for Cash Pickup)
CREATE TABLE IF NOT EXISTS receiver_details (
    id SERIAL PRIMARY KEY,
    full_name VARCHAR(200) NOT NULL,
    phone_number VARCHAR(50),
    city VARCHAR(100),
    country_code VARCHAR(3) REFERENCES countries(country_code),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_receiver_details_country ON receiver_details(country_code);

-- ============================================
-- Transaction Tables
-- ============================================

-- Transaction Status Enum Type
CREATE TYPE transaction_status AS ENUM (
    'InProgress', 'Paid', 'Cancelled', 'Failed', 
    'PaymentPending', 'IdCheckInProgress', 'Refund', 
    'FullRefund', 'PartialRefund', 'Abnormal', 
    'NotReceived', 'Received', 'Completed', 'Held', 'Paused'
);

-- Transaction Module Type
CREATE TYPE transaction_module AS ENUM (
    'Sender', 'Agent', 'AdminStaff', 'CardUser', 
    'BusinessMerchant', 'KiiPayBusiness', 'KiiPayPersonal'
);

-- API Service Type
CREATE TYPE api_service AS ENUM (
    'VGG', 'TransferZero', 'EmergentApi', 'MTN', 
    'Zenith', 'Magma', 'Wari'
);

-- Payment Mode Type
CREATE TYPE payment_mode AS ENUM (
    'Card', 'BankAccount', 'MobileWallet', 'Cash'
);

-- Base Transaction Table (Common fields for all transaction types)
CREATE TABLE IF NOT EXISTS transactions (
    id SERIAL PRIMARY KEY,
    transaction_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    receipt_no VARCHAR(50) UNIQUE NOT NULL,
    sender_id INTEGER NOT NULL REFERENCES senders(id),
    sending_country_code VARCHAR(3) NOT NULL REFERENCES countries(country_code),
    receiving_country_code VARCHAR(3) NOT NULL REFERENCES countries(country_code),
    sending_currency VARCHAR(3) NOT NULL,
    receiving_currency VARCHAR(3) NOT NULL,
    sending_amount DECIMAL(18, 2) NOT NULL,
    receiving_amount DECIMAL(18, 2) NOT NULL,
    fee DECIMAL(18, 2) NOT NULL DEFAULT 0,
    total_amount DECIMAL(18, 2) NOT NULL,
    exchange_rate DECIMAL(18, 6) NOT NULL,
    payment_reference VARCHAR(100),
    sender_payment_mode payment_mode NOT NULL,
    transaction_module transaction_module NOT NULL,
    status transaction_status NOT NULL DEFAULT 'InProgress',
    api_service api_service,
    transfer_reference VARCHAR(100),
    recipient_id INTEGER,
    is_compliance_needed BOOLEAN DEFAULT FALSE,
    is_compliance_approved BOOLEAN DEFAULT FALSE,
    compliance_approved_by INTEGER REFERENCES staff(id),
    compliance_approved_at TIMESTAMP,
    paying_staff_id INTEGER REFERENCES staff(id),
    updated_by_staff_id INTEGER REFERENCES staff(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_transactions_sender ON transactions(sender_id);
CREATE INDEX idx_transactions_receipt ON transactions(receipt_no);
CREATE INDEX idx_transactions_date ON transactions(transaction_date);
CREATE INDEX idx_transactions_status ON transactions(status);
CREATE INDEX idx_transactions_sending_country ON transactions(sending_country_code);
CREATE INDEX idx_transactions_receiving_country ON transactions(receiving_country_code);
CREATE INDEX idx_transactions_payment_ref ON transactions(payment_reference);

-- Bank Account Deposit Transactions
CREATE TABLE IF NOT EXISTS bank_account_deposits (
    transaction_id INTEGER PRIMARY KEY REFERENCES transactions(id) ON DELETE CASCADE,
    bank_id INTEGER REFERENCES banks(id),
    bank_name VARCHAR(200),
    bank_code VARCHAR(50),
    receiver_account_no VARCHAR(100),
    receiver_name VARCHAR(200),
    receiver_city VARCHAR(100),
    is_manual_deposit BOOLEAN DEFAULT FALSE,
    is_manual_approval_needed BOOLEAN DEFAULT FALSE,
    is_manually_approved BOOLEAN DEFAULT FALSE,
    is_europe_transfer BOOLEAN DEFAULT FALSE,
    is_transaction_duplicated BOOLEAN DEFAULT FALSE,
    duplicate_transaction_receipt_no VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_bank_deposits_bank ON bank_account_deposits(bank_id);
CREATE INDEX idx_bank_deposits_receiver_account ON bank_account_deposits(receiver_account_no);

-- Mobile Money Transfer Transactions
CREATE TABLE IF NOT EXISTS mobile_money_transfers (
    transaction_id INTEGER PRIMARY KEY REFERENCES transactions(id) ON DELETE CASCADE,
    wallet_operator_id INTEGER NOT NULL REFERENCES mobile_wallet_operators(id),
    paid_to_mobile_no VARCHAR(50) NOT NULL,
    receiver_name VARCHAR(200),
    receiver_city VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_mobile_transfers_wallet_operator ON mobile_money_transfers(wallet_operator_id);
CREATE INDEX idx_mobile_transfers_mobile_no ON mobile_money_transfers(paid_to_mobile_no);

-- Cash Pickup Transactions
CREATE TABLE IF NOT EXISTS cash_pickups (
    transaction_id INTEGER PRIMARY KEY REFERENCES transactions(id) ON DELETE CASCADE,
    mfcn VARCHAR(50) UNIQUE,
    recipient_id INTEGER REFERENCES recipients(id),
    non_card_receiver_id INTEGER REFERENCES receiver_details(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_cash_pickups_mfcn ON cash_pickups(mfcn);
CREATE INDEX idx_cash_pickups_recipient ON cash_pickups(recipient_id);
CREATE INDEX idx_cash_pickups_receiver ON cash_pickups(non_card_receiver_id);

-- KiiBank Transfer Transactions
CREATE TABLE IF NOT EXISTS kiibank_transfers (
    transaction_id INTEGER PRIMARY KEY REFERENCES transactions(id) ON DELETE CASCADE,
    account_no VARCHAR(100),
    receiver_name VARCHAR(200),
    transaction_reference VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_kiibank_transfers_account ON kiibank_transfers(account_no);
CREATE INDEX idx_kiibank_transfers_ref ON kiibank_transfers(transaction_reference);

-- ============================================
-- Auxiliary Agent Override Tables
-- ============================================

-- Auxiliary Agent Bank Deposit Details
CREATE TABLE IF NOT EXISTS aux_agent_bank_deposit_details (
    id SERIAL PRIMARY KEY,
    transaction_id INTEGER NOT NULL REFERENCES bank_account_deposits(transaction_id) ON DELETE CASCADE,
    fee DECIMAL(18, 2),
    sending_amount DECIMAL(18, 2),
    receiving_amount DECIMAL(18, 2),
    total_amount DECIMAL(18, 2),
    exchange_rate DECIMAL(18, 6),
    account_balance DECIMAL(18, 2),
    debited_balance DECIMAL(18, 2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(transaction_id)
);

-- Auxiliary Agent Mobile Money Transfer Details
CREATE TABLE IF NOT EXISTS aux_agent_mobile_transfer_details (
    id SERIAL PRIMARY KEY,
    transaction_id INTEGER NOT NULL REFERENCES mobile_money_transfers(transaction_id) ON DELETE CASCADE,
    fee DECIMAL(18, 2),
    sending_amount DECIMAL(18, 2),
    receiving_amount DECIMAL(18, 2),
    total_amount DECIMAL(18, 2),
    exchange_rate DECIMAL(18, 6),
    account_balance DECIMAL(18, 2),
    debited_balance DECIMAL(18, 2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(transaction_id)
);

-- Auxiliary Agent Cash Pickup Details
CREATE TABLE IF NOT EXISTS aux_agent_cash_pickup_details (
    id SERIAL PRIMARY KEY,
    transaction_id INTEGER NOT NULL REFERENCES cash_pickups(transaction_id) ON DELETE CASCADE,
    fee DECIMAL(18, 2),
    sending_amount DECIMAL(18, 2),
    receiving_amount DECIMAL(18, 2),
    total_amount DECIMAL(18, 2),
    exchange_rate DECIMAL(18, 6),
    account_balance DECIMAL(18, 2),
    debited_balance DECIMAL(18, 2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(transaction_id)
);

-- ============================================
-- Payment Information Tables
-- ============================================

-- Card Payment Information
CREATE TABLE IF NOT EXISTS card_payment_information (
    id SERIAL PRIMARY KEY,
    transaction_id INTEGER NOT NULL REFERENCES transactions(id) ON DELETE CASCADE,
    transfer_type INTEGER NOT NULL, -- 2=Cash, 3=Mobile/KiiBank, 4=Bank
    card_number VARCHAR(50), -- Masked card number
    card_issuer VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_card_payments_transaction ON card_payment_information(transaction_id);
CREATE INDEX idx_card_payments_transfer_type ON card_payment_information(transfer_type);

-- ============================================
-- Transaction Management Tables
-- ============================================

-- Reinitialize Transaction Table
CREATE TABLE IF NOT EXISTS reinitialize_transactions (
    id SERIAL PRIMARY KEY,
    receipt_no VARCHAR(50) NOT NULL,
    new_receipt_no VARCHAR(50) NOT NULL UNIQUE,
    created_by_id INTEGER REFERENCES staff(id),
    created_by_name VARCHAR(200),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_reinit_receipt ON reinitialize_transactions(receipt_no);
CREATE INDEX idx_reinit_new_receipt ON reinitialize_transactions(new_receipt_no);

-- ============================================
-- Functions and Triggers
-- ============================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply triggers to all tables with updated_at
CREATE TRIGGER update_countries_updated_at BEFORE UPDATE ON countries
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_banks_updated_at BEFORE UPDATE ON banks
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_mobile_wallet_operators_updated_at BEFORE UPDATE ON mobile_wallet_operators
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_senders_updated_at BEFORE UPDATE ON senders
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_sender_logins_updated_at BEFORE UPDATE ON sender_logins
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_staff_updated_at BEFORE UPDATE ON staff
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_receiver_details_updated_at BEFORE UPDATE ON receiver_details
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_transactions_updated_at BEFORE UPDATE ON transactions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_bank_deposits_updated_at BEFORE UPDATE ON bank_account_deposits
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_mobile_transfers_updated_at BEFORE UPDATE ON mobile_money_transfers
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_cash_pickups_updated_at BEFORE UPDATE ON cash_pickups
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_kiibank_transfers_updated_at BEFORE UPDATE ON kiibank_transfers
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- Comments
-- ============================================

COMMENT ON TABLE transactions IS 'Base transaction table containing common fields for all transaction types';
COMMENT ON TABLE bank_account_deposits IS 'Bank account deposit specific transaction details';
COMMENT ON TABLE mobile_money_transfers IS 'Mobile money transfer specific transaction details';
COMMENT ON TABLE cash_pickups IS 'Cash pickup specific transaction details';
COMMENT ON TABLE kiibank_transfers IS 'KiiBank transfer specific transaction details';
COMMENT ON TABLE aux_agent_bank_deposit_details IS 'Auxiliary agent override values for bank deposits';
COMMENT ON TABLE aux_agent_mobile_transfer_details IS 'Auxiliary agent override values for mobile transfers';
COMMENT ON TABLE aux_agent_cash_pickup_details IS 'Auxiliary agent override values for cash pickups';
COMMENT ON TABLE reinitialize_transactions IS 'Tracks reinitialized transactions';

