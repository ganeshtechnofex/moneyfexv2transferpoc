using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

public class MobileTransferViewModel
{
    [Required(ErrorMessage = "Wallet operator is required")]
    [Display(Name = "Wallet Operator")]
    public int WalletOperatorId { get; set; }

    [Required(ErrorMessage = "Mobile number is required")]
    [Display(Name = "Mobile Number")]
    [StringLength(20)]
    [Phone]
    public string MobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Receiver name is required")]
    [Display(Name = "Receiver Name")]
    [StringLength(100)]
    public string ReceiverName { get; set; } = string.Empty;

    [Display(Name = "Receiver City")]
    [StringLength(100)]
    public string? ReceiverCity { get; set; }

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

