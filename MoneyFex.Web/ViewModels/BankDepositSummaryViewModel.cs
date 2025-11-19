using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for bank deposit summary page
/// Based on legacy SenderTransferSummaryVm
/// </summary>
public class BankDepositSummaryViewModel
{
    public int Id { get; set; }
    
    [StringLength(200)]
    public string SendingCurrencyCode { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string SendingCurrencySymbol { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string ReceivingCurrencyCode { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string ReceivingCurrencySymbol { get; set; } = string.Empty;
    
    [Range(0.0, double.MaxValue)]
    public decimal Amount { get; set; }
    
    // Additional amount fields
    [Range(0.0, double.MaxValue)]
    public decimal SendingAmount { get; set; }
    
    [Range(0.0, double.MaxValue)]
    public decimal ReceivingAmount { get; set; }
    
    [Range(0.0, double.MaxValue)]
    public decimal Fee { get; set; }
    
    [Range(0.0, double.MaxValue)]
    public decimal TotalAmount { get; set; }
    
    [Range(0.0, double.MaxValue)]
    public decimal ExchangeRate { get; set; }
    
    [Range(0.0, double.MaxValue)]
    public decimal PaidAmount { get; set; }
    
    [StringLength(200)]
    public string ReceiverName { get; set; } = string.Empty;
    
    [Range(0.0, double.MaxValue)]
    public decimal ReceivedAmount { get; set; }
    
    [StringLength(200)]
    public string? PaymentReference { get; set; }
    
    // Country and currency codes
    public string SendingCountry { get; set; } = string.Empty;
    public string ReceivingCountry { get; set; } = string.Empty;
    public string SendingCurrency { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    
    // Additional fields for transaction tracking
    public int TransactionId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public string BankAccountNo { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
}

