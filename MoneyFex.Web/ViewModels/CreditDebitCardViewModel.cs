using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for credit/debit card payment details
/// Based on legacy CreditDebitCardViewModel
/// </summary>
public class CreditDebitCardViewModel
{
    public const string BindProperty = "FaxingAmount,NameOnCard,CardNumber,EndMM,EndYY,SecurityCode," +
        "AddressLineOne,AddressLineTwo,CityName,ZipCode,SaveCard,CountyName,FaxingCurrency," +
        "FaxingCurrencySymbol,Confirm,StripeTokenID,CreditDebitCardType,ThreeDEnrolled," +
        "CreditDebitCardFee,ReceiverName,TransactionId,ReceiptNo";
    [Display(Name = "Faxing Amount Including Fee")]
    [Required(ErrorMessage = "Enter Faxing Amount")]
    public decimal FaxingAmount { get; set; }

    [Display(Name = "Name on Card")]
    [Required(ErrorMessage = "Enter Name On Card")]
    public string NameOnCard { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    [Display(Name = "Card Number")]
    [Required(ErrorMessage = "Enter Card Number")]
    public string CardNumber { get; set; } = string.Empty;

    [Display(Name = "MM")]
    [Required(ErrorMessage = "Enter Month")]
    [StringLength(2)]
    public string EndMM { get; set; } = string.Empty;

    [Display(Name = "YY")]
    [Required(ErrorMessage = "Enter Year")]
    [StringLength(2)]
    public string EndYY { get; set; } = string.Empty;

    [Display(Name = "Security Code")]
    [Required(ErrorMessage = "Enter Security Code")]
    [StringLength(4)]
    public string SecurityCode { get; set; } = string.Empty;

    [Display(Name = "Address line 1")]
    [Required(ErrorMessage = "Enter Address")]
    public string AddressLineOne { get; set; } = string.Empty;

    [Display(Name = "Address Line 2(optional)")]
    public string? AddressLineTwo { get; set; }

    [Display(Name = "City")]
    [Required(ErrorMessage = "Enter City")]
    public string CityName { get; set; } = string.Empty;

    [Display(Name = "Post/Zip Code")]
    [Required(ErrorMessage = "Enter Zip Code")]
    public string ZipCode { get; set; } = string.Empty;

    [Display(Name = "Save this Credit/Debit card for future use")]
    public bool SaveCard { get; set; }

    [Display(Name = "Country")]
    [Required(ErrorMessage = "Enter Country")]
    public string CountyName { get; set; } = string.Empty;

    [StringLength(200)]
    public string FaxingCurrency { get; set; } = string.Empty;

    [StringLength(200)]
    public string FaxingCurrencySymbol { get; set; } = string.Empty;

    public bool Confirm { get; set; }

    [StringLength(200)]
    public string? StripeTokenID { get; set; }

    public CreditDebitCardType CreditDebitCardType { get; set; }

    public bool ThreeDEnrolled { get; set; }

    public decimal CreditDebitCardFee { get; set; } = 0.05m; // Default fee for POC

    public string? CardUsageMsg { get; set; }
    public bool IsCardUsageMsg { get; set; }
    public string? ErrorMsg { get; set; }
    public int TransactionSummaryId { get; set; }
    
    // Transaction tracking
    public int TransactionId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
}

/// <summary>
/// Credit/Debit card type enumeration
/// </summary>
public enum CreditDebitCardType
{
    [Display(Name = "Visa")]
    VisaCard,
    [Display(Name = "Mastercard")]
    MasterCard
}

