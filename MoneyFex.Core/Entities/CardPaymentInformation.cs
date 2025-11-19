namespace MoneyFex.Core.Entities;

/// <summary>
/// Card payment information
/// Based on legacy CardTopUpCreditDebitInformation entity
/// Stores credit/debit card details for all transaction types
/// </summary>
public class CardPaymentInformation
{
    public int Id { get; set; }
    
    // Transaction references (legacy uses different IDs for different transaction types)
    public int? TransactionId { get; set; } // For unified transaction reference
    public int? CardTransactionId { get; set; } // Legacy: For card transactions
    public int? NonCardTransactionId { get; set; } // Legacy: For cash pickup transactions
    public int? TopUpSomeoneElseTransactionId { get; set; } // Legacy: For top-up transactions
    
    // Card information
    public string? NameOnCard { get; set; }
    public string? CardNumber { get; set; } // Masked card number
    public string? ExpiryDate { get; set; } // Format: MM/YY or MM/YYYY
    
    // Card flags
    public bool IsSavedCard { get; set; }
    public bool AutoRecharged { get; set; }
    
    // Transfer type: 2=Cash Pickup, 3=Mobile/KiiBank, 4=Bank Deposit
    public int TransferType { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Transaction? Transaction { get; set; }
}

