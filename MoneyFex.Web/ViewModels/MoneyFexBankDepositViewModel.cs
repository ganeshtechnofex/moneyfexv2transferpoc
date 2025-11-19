using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for MoneyFex bank deposit page
/// Based on legacy SenderMoneyFexBankDepositVM
/// </summary>
public class MoneyFexBankDepositViewModel
{
    public int TransactionId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;

    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Display(Name = "Sending Currency Code")]
    public string SendingCurrencyCode { get; set; } = string.Empty;

    [Display(Name = "Sending Currency Symbol")]
    public string SendingCurrencySymbol { get; set; } = string.Empty;

    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Display(Name = "Sort Code / Short Code")]
    public string ShortCode { get; set; } = string.Empty;

    [Display(Name = "Label Name")]
    public string LabelName { get; set; } = "Sort Code";

    [Display(Name = "Payment Reference")]
    public string PaymentReference { get; set; } = string.Empty;
}

