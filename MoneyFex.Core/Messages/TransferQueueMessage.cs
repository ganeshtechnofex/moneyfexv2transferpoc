namespace MoneyFex.Core.Messages;

/// <summary>
/// Message DTO for queuing money transfer processing via Kafka
/// This is NOT a database entity - it's a transient message object
/// The actual transaction data is stored in Transaction and MobileMoneyTransfer entities
/// </summary>
public class TransferQueueMessage
{
    public int TransactionId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public TransferType TransferType { get; set; }
    public string Payload { get; set; } = string.Empty; // JSON for transfer-specific data
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// Types of money transfers
/// </summary>
public enum TransferType
{
    MobileMoneyTransfer,
    BankAccountDeposit,
    CashPickup,
    KiiBankTransfer
}

