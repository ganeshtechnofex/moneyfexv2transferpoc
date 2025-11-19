namespace MoneyFex.Core.Entities.Enums;

/// <summary>
/// Transaction type enumeration
/// Maps to TransferType in the original system
/// </summary>
public enum TransactionType
{
    BankDeposit = 1,
    MobileWallet = 2,
    CashPickup = 3,
    KiiBank = 7
}

/// <summary>
/// Transaction service type enumeration
/// Maps to TransactionServiceType in the original system
/// </summary>
public enum TransactionServiceType
{
    MobileWallet = 1,
    CashPickup = 5,
    BankDeposit = 6,
    KiiBank = 7
}

