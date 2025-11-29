using MoneyFex.Core.Entities;

namespace MoneyFex.Web.ViewModels;

public class HomeViewModel
{
    public List<CountryViewModel> SendingCountries { get; set; } = new();
    public List<CountryViewModel> ReceivingCountries { get; set; } = new();
    public string DefaultSendingCountry { get; set; } = "GB";
    public string DefaultSendingCurrency { get; set; } = "GBP";
    public string DefaultReceivingCountry { get; set; } = "NG";
    public string DefaultReceivingCurrency { get; set; } = "NGN";
    public List<FeedbackViewModel> Feedbacks { get; set; } = new();
}

public class CountryViewModel
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
}

public class FeedbackViewModel
{
    public string FeedBack { get; set; } = string.Empty;
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerTypeName { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class TransferSummaryViewModel
{
    // Existing properties (used by HomeController)
    public decimal SendingAmount { get; set; }
    public decimal ReceivingAmount { get; set; }
    public decimal Fee { get; set; }
    public decimal ActualFee { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string SendingCurrency { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    public string SendingCurrencySymbol { get; set; } = string.Empty;
    public bool IsIntroductoryRate { get; set; }
    public bool IsIntroductoryFee { get; set; }
    public ValidationResult IsValid { get; set; } = new();
    
    // Additional properties for SenderTransaction Summary page
    public string TransferMethod { get; set; } = string.Empty;
    public string ReceiverFullName { get; set; } = string.Empty;
    public string ReceiverFirstName { get; set; } = string.Empty;
    public string SendingCountry { get; set; } = string.Empty;
    public string SendingCurrencyCode { get; set; } = string.Empty;
    public string ReceivingCurrencyCode { get; set; } = string.Empty;
    public string ReceivingCountry { get; set; } = string.Empty;
    public string ReceivingCurrencySymbol { get; set; } = string.Empty;
    public decimal TotalSendingAmount { get; set; }
    public string ReceivingCountryFlag { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class ValidationResult
{
    public bool Data { get; set; }
    public string Message { get; set; } = string.Empty;
}

