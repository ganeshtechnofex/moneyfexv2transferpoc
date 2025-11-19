namespace MoneyFex.Core.Entities;

/// <summary>
/// Cash pickup transaction details
/// Based on legacy FaxingNonCardTransaction entity
/// Optimized: Common properties moved to Transaction base entity
/// </summary>
public class CashPickup
{
    public int TransactionId { get; set; }
    
    // Cash pickup specific information
    public string? MFCN { get; set; }
    public int? RecipientId { get; set; }
    public int? NonCardReceiverId { get; set; }
    public int? RecipientIdentityCardId { get; set; }
    public string? RecipientIdentityCardNumber { get; set; }
    public bool IsApprovedByAdmin { get; set; }
    public string? AgentStaffName { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
    public Recipient? Recipient { get; set; }
    public ReceiverDetail? NonCardReceiver { get; set; }
}

