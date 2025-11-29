using System.ComponentModel.DataAnnotations;
using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for sender cash pickup form
/// Based on legacy SenderCashPickUpVM
/// </summary>
public class SenderCashPickUpViewModel
{
    public const string BindProperty = "Id,RecentReceiverId,FullName,CountryCode,MobileNumber,EmailAddress,Reason,IdenityCardId,IdentityCardNumber";

    [Range(0, int.MaxValue)]
    public int Id { get; set; }

    public int? RecentReceiverId { get; set; }

    [MaxLength(200)]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Select Country")]
    public string CountryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter Mobile Number")]
    public string MobileNumber { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int IdenityCardId { get; set; }

    public string? IdentityCardNumber { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? EmailAddress { get; set; }

    [Required(ErrorMessage = "Select Reason for Transfer")]
    public ReasonForTransfer? Reason { get; set; }

    public int TransactionSummaryId { get; set; }
    public string? SendingCurrency { get; set; }
    public string? ReceivingCurrency { get; set; }
    
    // Additional fields for view display
    public decimal SendingAmount { get; set; }
    public decimal ReceivingAmount { get; set; }
    public string SendingCountry { get; set; } = string.Empty;
    public string ReceivingCountry { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal Fee { get; set; }
    public decimal TotalAmount { get; set; }
    public int SenderId { get; set; }
    public string CountryPhoneCode { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

