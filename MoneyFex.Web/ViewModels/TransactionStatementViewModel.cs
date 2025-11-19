namespace MoneyFex.Web.ViewModels;

public class TransactionStatementViewModel
{
    public int TransactionId { get; set; }
    public string identifier { get; set; } = string.Empty; // Receipt number
    public string TransferMethod { get; set; } = string.Empty;
    public int TransactionServiceType { get; set; }
    
    // Countries
    public string SendingCountry { get; set; } = string.Empty;
    public string ReceivingCountry { get; set; } = string.Empty;
    
    // Currencies
    public string SendingCurrency { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    
    // Sender information
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPhoneNumber { get; set; } = string.Empty;
    public string SenderMFAccountNo { get; set; } = string.Empty;
    
    // Receiver information
    public int? RecipentId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceivingAccountNo { get; set; } = string.Empty;
    
    // Amount information
    public string Amount { get; set; } = string.Empty; // Formatted with currency symbol
    public string Fee { get; set; } = string.Empty; // Formatted with currency symbol
    public decimal ReceivingAmount { get; set; }
    public decimal GrossAmount { get; set; } // Total amount including fee
    
    // Transaction details
    public string DateTime { get; set; } = string.Empty; // Formatted date
    public string TransactionTime { get; set; } = string.Empty; // Formatted time
    public DateTime TransactionDate { get; set; }
    public string Reference { get; set; } = string.Empty; // Payment reference
    
    // Status and responsible person
    public string TransactionStatusForAdmin { get; set; } = string.Empty;
    public string TransactionPerformedBy { get; set; } = string.Empty; // sender, agent, admin
    public string PayoutType { get; set; } = string.Empty;
    public string? UpdatedByStaffName { get; set; }
    public int? AgentStaffId { get; set; }
    
    // Payout provider
    public string PayoutProviderName { get; set; } = string.Empty;
    
    // Notes
    public int NoteCount { get; set; }
    
    // Flags for actions
    public bool IsManualConfimationNeed { get; set; }
    public bool IsTransactionCancelAble { get; set; }
    public bool IsAwaitForApproval { get; set; }
    public bool IsReInitializedTransaction { get; set; }
    public string? ReInitializedReceiptNo { get; set; }
    
    // For pagination
    public int TotalCount { get; set; }
}

