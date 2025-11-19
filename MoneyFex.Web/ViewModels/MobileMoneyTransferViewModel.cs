using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for mobile money transfer form
/// Based on legacy SenderMobileMoneyTransferVM
/// </summary>
public class MobileMoneyTransferViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Country is required")]
    [Display(Name = "Country")]
    public string CountryCode { get; set; } = string.Empty;

    [Display(Name = "Country Phone Code")]
    public string CountryPhoneCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select Mobile Wallet")]
    [Display(Name = "Mobile Wallet")]
    public int WalletId { get; set; }

    [Required(ErrorMessage = "Enter Mobile Number")]
    [Display(Name = "Mobile Number")]
    [StringLength(20)]
    public string MobileNumber { get; set; } = string.Empty;

    [Display(Name = "Recently Paid Mobile Number")]
    public string? RecentlyPaidMobile { get; set; }

    [Required(ErrorMessage = "Enter Receiver Name")]
    [Display(Name = "Receiver Name")]
    [StringLength(100)]
    public string ReceiverName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Receiver Email")]
    [StringLength(100)]
    public string? ReceiverEmail { get; set; }

    [Display(Name = "Receiver Street/Address")]
    [StringLength(200)]
    public string? ReceiverStreet { get; set; }

    [Display(Name = "Identity Card Type")]
    public int? IdentityCardId { get; set; }

    [Display(Name = "Identity Card Number")]
    [StringLength(50)]
    public string? IdentityCardNumber { get; set; }

    [Display(Name = "Reason for Transfer")]
    public string? ReasonForTransfer { get; set; }

    // Transaction summary fields (from previous step)
    public decimal SendingAmount { get; set; }
    public decimal ReceivingAmount { get; set; }
    public string SendingCurrency { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    public string SendingCountry { get; set; } = string.Empty;
    public string ReceivingCountry { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal Fee { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Sender ID for tracking
    public int? SenderId { get; set; }
}

