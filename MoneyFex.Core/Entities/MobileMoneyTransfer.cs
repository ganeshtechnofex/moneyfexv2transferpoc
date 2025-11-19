namespace MoneyFex.Core.Entities;

/// <summary>
/// Mobile money transfer transaction details
/// Based on legacy MobileMoneyTransfer entity
/// Optimized: Common properties moved to Transaction base entity
/// </summary>
public class MobileMoneyTransfer
{
    public int TransactionId { get; set; }
    
    // Mobile wallet information
    public int WalletOperatorId { get; set; }
    public string PaidToMobileNo { get; set; } = string.Empty;
    
    // Receiver information (specific to mobile transfers)
    public string? ReceiverName { get; set; }
    public string? ReceiverCity { get; set; }
    public int? RecipientId { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
    public MobileWalletOperator WalletOperator { get; set; } = null!;
    public Recipient? Recipient { get; set; }
}

