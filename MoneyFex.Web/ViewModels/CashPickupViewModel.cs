using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

public class CashPickupViewModel
{
    [Required(ErrorMessage = "Receiver first name is required")]
    [Display(Name = "First Name")]
    [StringLength(50)]
    public string ReceiverFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Receiver last name is required")]
    [Display(Name = "Last Name")]
    [StringLength(50)]
    public string ReceiverLastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required")]
    [Display(Name = "Mobile Number")]
    [StringLength(20)]
    [Phone]
    public string MobileNumber { get; set; } = string.Empty;

    [Display(Name = "Pickup Location")]
    [StringLength(200)]
    public string? PickupLocation { get; set; }

    [Display(Name = "Receiver City")]
    [StringLength(100)]
    public string? ReceiverCity { get; set; }

    [Required(ErrorMessage = "MFCN is required")]
    [Display(Name = "MFCN")]
    [StringLength(50)]
    public string MFCN { get; set; } = string.Empty;

    // Computed property for full receiver name (read-only)
    [Display(Name = "Receiver Name")]
    public string ReceiverName 
    { 
        get => $"{ReceiverFirstName} {ReceiverLastName}".Trim(); 
        set 
        {
            // When setting, split into first and last name
            if (!string.IsNullOrWhiteSpace(value))
            {
                var parts = value.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                ReceiverFirstName = parts.Length > 0 ? parts[0] : string.Empty;
                ReceiverLastName = parts.Length > 1 ? parts[1] : string.Empty;
            }
        }
    }

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

