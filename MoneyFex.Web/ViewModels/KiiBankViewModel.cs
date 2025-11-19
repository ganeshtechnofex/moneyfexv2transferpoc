using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

public class KiiBankViewModel
{
    [Required(ErrorMessage = "Account number is required")]
    [Display(Name = "Account Number")]
    [StringLength(50)]
    public string AccountNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Receiver name is required")]
    [Display(Name = "Receiver Name")]
    [StringLength(100)]
    public string ReceiverName { get; set; } = string.Empty;

    [Display(Name = "Transaction Reference")]
    [StringLength(50)]
    public string? TransactionReference { get; set; }

    // Transaction details
    public decimal SendingAmount { get; set; }
    public decimal ReceivingAmount { get; set; }
    public string SendingCurrency { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    public string SendingCountry { get; set; } = string.Empty;
    public string ReceivingCountry { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal Fee { get; set; }
    public decimal TotalAmount { get; set; }
}

