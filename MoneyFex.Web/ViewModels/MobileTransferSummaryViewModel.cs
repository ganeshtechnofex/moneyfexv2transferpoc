using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for mobile money transfer summary page
/// Based on legacy SenderTransferSummaryVm
/// </summary>
public class MobileTransferSummaryViewModel
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
    
    [Range(0.0, double.MaxValue)]
    public decimal Fee { get; set; }
    
    [Range(0.0, double.MaxValue)]
    public decimal PaidAmount { get; set; }
    
    [StringLength(200)]
    public string ReceiverName { get; set; } = string.Empty;
    
    [Range(0.0, double.MaxValue)]
    public decimal ReceivedAmount { get; set; }
    
    [StringLength(200)]
    public string? PaymentReference { get; set; }
    
    // Additional fields for transaction tracking
    public int TransactionId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string WalletName { get; set; } = string.Empty;
}

