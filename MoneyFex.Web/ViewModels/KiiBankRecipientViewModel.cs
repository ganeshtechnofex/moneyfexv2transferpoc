using System.ComponentModel.DataAnnotations;
using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for KiiBank recipient account details
/// Based on legacy KiiBankRecipientViewModel
/// </summary>
public class KiiBankRecipientViewModel
{
    public int Id { get; set; }
    
    public int TransactionSummaryId { get; set; }
    
    [Required(ErrorMessage = "Account number is required")]
    [Display(Name = "Account Number")]
    [StringLength(50)]
    public string AccountNumber { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? MobileNumber { get; set; }
    
    [Required(ErrorMessage = "Account name is required")]
    [Display(Name = "Account Name")]
    [StringLength(100)]
    public string AccountName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please select a reason for transfer")]
    [Display(Name = "Reason for Transfer")]
    public ReasonForTransfer ReasonForTransfer { get; set; } = ReasonForTransfer.Non;
}

