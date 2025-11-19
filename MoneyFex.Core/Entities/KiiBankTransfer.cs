namespace MoneyFex.Core.Entities;

/// <summary>
/// KiiBank transfer transaction details
/// Based on legacy KiiBank transfer entities
/// Optimized: Common properties moved to Transaction base entity
/// </summary>
public class KiiBankTransfer
{
    public int TransactionId { get; set; }
    
    // KiiBank specific information
    public string? AccountNo { get; set; }
    public string? ReceiverName { get; set; }
    public string? AccountOwnerName { get; set; }
    public string? AccountHolderPhoneNo { get; set; }
    public int? BankId { get; set; }
    public int? BankBranchId { get; set; }
    public string? BankBranchCode { get; set; }
    public string? TransactionReference { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
    public Bank? Bank { get; set; }
}

