namespace MoneyFex.Core.Entities;

/// <summary>
/// Bank account deposit transaction details
/// Based on legacy BankAccountDeposit entity
/// Optimized: Common properties moved to Transaction base entity
/// </summary>
public class BankAccountDeposit
{
    public int TransactionId { get; set; }
    
    // Bank information
    public int? BankId { get; set; }
    public string? BankName { get; set; }
    public string? BankCode { get; set; }
    
    // Receiver information (specific to bank deposits)
    public string? ReceiverAccountNo { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverCity { get; set; }
    public string? ReceiverCountry { get; set; }
    public string? ReceiverMobileNo { get; set; }
    public int? RecipientId { get; set; }
    
    // Bank deposit specific flags
    public bool IsManualDeposit { get; set; }
    public bool IsManualApprovalNeeded { get; set; }
    public bool IsManuallyApproved { get; set; }
    public bool IsEuropeTransfer { get; set; }
    public bool IsTransactionDuplicated { get; set; }
    public string? DuplicateTransactionReceiptNo { get; set; }
    public bool IsBusiness { get; set; }
    public bool HasMadePaymentToBankAccount { get; set; }
    
    // Bank deposit specific metadata
    public string? TransactionDescription { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
    public Bank? Bank { get; set; }
    public Recipient? Recipient { get; set; }
}

