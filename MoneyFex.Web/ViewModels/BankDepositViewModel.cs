using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

public class BankDepositViewModel
{
    [Required(ErrorMessage = "Bank is required")]
    [Display(Name = "Bank")]
    public int BankId { get; set; }

    [Required(ErrorMessage = "Account number is required")]
    [Display(Name = "Account Number")]
    [StringLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    // Alias for AccountNumber for backward compatibility
    public string ReceiverAccountNo 
    { 
        get => AccountNumber; 
        set => AccountNumber = value; 
    }

    [Required(ErrorMessage = "Account owner name is required")]
    [Display(Name = "Account Owner Name")]
    [StringLength(100)]
    public string AccountOwnerName { get; set; } = string.Empty;

    // Alias for AccountOwnerName for backward compatibility
    public string ReceiverName 
    { 
        get => AccountOwnerName; 
        set => AccountOwnerName = value; 
    }

    [Display(Name = "Branch Code")]
    [StringLength(20)]
    public string? BranchCode { get; set; }

    [Required(ErrorMessage = "Mobile number is required")]
    [Display(Name = "Mobile Number")]
    [StringLength(20)]
    public string MobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required")]
    [Display(Name = "Country")]
    [StringLength(3)]
    public string CountryCode { get; set; } = string.Empty;

    [Display(Name = "Receiver City")]
    [StringLength(100)]
    public string? ReceiverCity { get; set; }

    [Display(Name = "Receiver Address")]
    [StringLength(200)]
    public string? ReceiverAddress { get; set; }

    [Display(Name = "Receiver Email")]
    [EmailAddress]
    [StringLength(100)]
    public string? ReceiverEmail { get; set; }

    [Display(Name = "Receiver Postal Code")]
    [StringLength(20)]
    public string? ReceiverPostalCode { get; set; }

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

