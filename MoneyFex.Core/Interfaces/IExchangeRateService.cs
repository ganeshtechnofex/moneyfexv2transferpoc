using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Core.Interfaces;

public interface IExchangeRateService
{
    Task<decimal> GetExchangeRateAsync(
        string sendingCountry, 
        string receivingCountry, 
        TransactionType transferMethod,
        decimal amount);
    
    Task<decimal> GetFeeAsync(
        string sendingCountry, 
        string receivingCountry, 
        TransactionType transferMethod,
        decimal amount,
        bool isFirstTransaction = false);
    
    Task<ExchangeRateCalculationResult> CalculateTransferSummaryAsync(
        decimal sendingAmount,
        decimal receivingAmount,
        string sendingCurrency,
        string receivingCurrency,
        string sendingCountry,
        string receivingCountry,
        TransactionType transferMethod,
        bool isReceivingAmount,
        bool isFirstTransaction = false);
}

public class ExchangeRateCalculationResult
{
    public decimal SendingAmount { get; set; }
    public decimal ReceivingAmount { get; set; }
    public decimal Fee { get; set; }
    public decimal ActualFee { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string SendingCurrency { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    public string SendingCurrencySymbol { get; set; } = string.Empty;
    public string ReceivingCurrencySymbol { get; set; } = string.Empty;
    public bool IsIntroductoryRate { get; set; }
    public bool IsIntroductoryFee { get; set; }
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
}

