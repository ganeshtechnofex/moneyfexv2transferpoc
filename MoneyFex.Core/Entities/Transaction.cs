using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Core.Entities;

/// <summary>
/// Base transaction entity containing common fields for all transaction types
/// </summary>
public class Transaction
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    
    // Sender information
    public int SenderId { get; set; }
    
    // Country and currency information
    public string SendingCountryCode { get; set; } = string.Empty;
    public string ReceivingCountryCode { get; set; } = string.Empty;
    public string SendingCurrency { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    
    // Amount information
    public decimal SendingAmount { get; set; }
    public decimal ReceivingAmount { get; set; }
    public decimal Fee { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    
    // Payment information
    public string? PaymentReference { get; set; }
    public PaymentMode SenderPaymentMode { get; set; }
    
    // Transaction metadata
    public TransactionModule TransactionModule { get; set; }
    public TransactionStatus Status { get; set; }
    public ApiService? ApiService { get; set; }
    public string? TransferReference { get; set; }
    public int? RecipientId { get; set; }
    
    // Compliance information
    public bool IsComplianceNeeded { get; set; }
    public bool IsComplianceApproved { get; set; }
    public int? ComplianceApprovedBy { get; set; }
    public DateTime? ComplianceApprovedAt { get; set; }
    
    // Staff information
    public int? PayingStaffId { get; set; }
    public string? PayingStaffName { get; set; }
    public int? UpdatedByStaffId { get; set; }
    
    // Financial metadata (common across all transaction types)
    public decimal? AgentCommission { get; set; }
    public decimal? ExtraFee { get; set; }
    public decimal? Margin { get; set; }
    public decimal? MFRate { get; set; }
    
    // Additional metadata
    public string? TransferZeroSenderId { get; set; }
    public ReasonForTransfer? ReasonForTransfer { get; set; }
    public CardProcessorApi? CardProcessorApi { get; set; }
    public bool IsFromMobile { get; set; }
    public DateTime? TransactionUpdateDate { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Sender Sender { get; set; } = null!;
    public Country? SendingCountry { get; set; }
    public Country? ReceivingCountry { get; set; }
    public Staff? PayingStaff { get; set; }
    public Staff? UpdatedByStaff { get; set; }
    public Staff? ComplianceApprovedByStaff { get; set; }
}

