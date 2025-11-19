using MoneyFex.Core.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for payment method selection page
/// Based on legacy FAXER.PORTAL.Models.PaymentMethodViewModel
/// </summary>
public class PaymentMethodViewModel
{
    public const string BindProperty = "PaymentMethod,TotalAmount,SendingCurrencySymbol,SenderPaymentMode," +
        "EnableAutoPayment,AutopaymentFrequency,AutoPaymentAmount,PaymentDay," +
        "KiipayWalletBalance,HasKiiPayWallet,CardDetails,TransactionId,ReceiptNo";

    [Display(Name = "Payment Method")]
    [StringLength(200)]
    public string? PaymentMethod { get; set; }

    [Range(0.0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [StringLength(10)]
    public string SendingCurrencySymbol { get; set; } = string.Empty;

    // Using PaymentMode from Core.Entities.Enums (maps to legacy SenderPaymentMode)
    // Legacy: CreditDebitCard=0, SavedDebitCreditCard=1, KiiPayWallet=2, MoneyFexBankAccount=3, AutomatedBankPayout=6, DirectBankPayment=7
    // New: Card=0, BankAccount=1, MobileWallet=2, Cash=3
    public PaymentMode SenderPaymentMode { get; set; } = PaymentMode.Card;

    public bool EnableAutoPayment { get; set; }

    [Range(0, 4)]
    public AutoPaymentFrequency AutopaymentFrequency { get; set; } = AutoPaymentFrequency.None;

    [Range(0.0, double.MaxValue)]
    public decimal AutoPaymentAmount { get; set; }

    [StringLength(200)]
    public string? PaymentDay { get; set; }

    [Range(0.0, double.MaxValue)]
    public decimal KiipayWalletBalance { get; set; }

    public bool HasKiiPayWallet { get; set; }
    
    public bool HasEnableMoneyFexBankAccount { get; set; }
    
    public int TransactionSummaryId { get; set; }
    
    public bool IsEnableVolumePayment { get; set; }
    
    public List<SavedCardViewModel> CardDetails { get; set; } = new();
    
    // Transaction tracking properties
    public int TransactionId { get; set; }
    
    [StringLength(50)]
    public string ReceiptNo { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for saved debit/credit card details
/// </summary>
public class SenderSavedDebitCreditCardViewModel
{
    public int CardId { get; set; }
    public bool IsChecked { get; set; }
    public CreditDebitCardType CreditDebitCardType { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string? SecurityCode { get; set; }
}

/// <summary>
/// Alias for saved card view model (used by other controllers)
/// </summary>
public class SavedCardViewModel : SenderSavedDebitCreditCardViewModel
{
}
