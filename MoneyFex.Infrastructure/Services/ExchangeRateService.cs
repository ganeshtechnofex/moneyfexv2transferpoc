using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Infrastructure.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<ExchangeRateService> _logger;

    // Default exchange rates (fallback if database doesn't have rates)
    private readonly Dictionary<string, decimal> _defaultRates = new()
    {
        { "GBP_NGN", 850.0m },
        { "GBP_GHS", 12.5m },
        { "EUR_NGN", 950.0m },
        { "USD_NGN", 800.0m },
        { "GBP_KES", 150.0m },
        { "GBP_MAD", 12.0m }
    };

    // Default fee percentages by transfer method
    private readonly Dictionary<TransactionType, decimal> _feePercentages = new()
    {
        { TransactionType.BankDeposit, 0.02m },      // 2%
        { TransactionType.MobileWallet, 0.025m },   // 2.5%
        { TransactionType.CashPickup, 0.03m },       // 3%
        { TransactionType.KiiBank, 0.015m }         // 1.5%
    };

    public ExchangeRateService(MoneyFexDbContext context, ILogger<ExchangeRateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<decimal> GetExchangeRateAsync(
        string sendingCountry, 
        string receivingCountry, 
        TransactionType transferMethod,
        decimal amount)
    {
        try
        {
            // Get currencies from countries
            var sendingCountryData = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == sendingCountry);
            var receivingCountryData = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == receivingCountry);

            if (sendingCountryData == null || receivingCountryData == null)
            {
                return GetDefaultRate(sendingCountryData?.Currency ?? "GBP", receivingCountryData?.Currency ?? "NGN");
            }

            // TODO: Query TransferExchangeRateHistory table when it's created
            // For now, use default rates
            var rateKey = $"{sendingCountryData.Currency}_{receivingCountryData.Currency}";
            return GetDefaultRate(sendingCountryData.Currency, receivingCountryData.Currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rate");
            return GetDefaultRate("GBP", "NGN");
        }
    }

    public async Task<decimal> GetFeeAsync(
        string sendingCountry, 
        string receivingCountry, 
        TransactionType transferMethod,
        decimal amount,
        bool isFirstTransaction = false)
    {
        try
        {
            if (isFirstTransaction)
            {
                return 0; // No fee for first transaction
            }

            if (!_feePercentages.TryGetValue(transferMethod, out var feePercentage))
            {
                feePercentage = 0.02m; // Default 2%
            }

            var fee = amount * feePercentage;
            
            // Minimum fee
            var minFee = 1.0m;
            if (fee < minFee)
            {
                fee = minFee;
            }

            // Maximum fee
            var maxFee = amount * 0.05m; // Max 5%
            if (fee > maxFee)
            {
                fee = maxFee;
            }

            return Math.Round(fee, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating fee");
            return amount * 0.02m; // Default 2%
        }
    }

    public async Task<ExchangeRateCalculationResult> CalculateTransferSummaryAsync(
        decimal sendingAmount,
        decimal receivingAmount,
        string sendingCurrency,
        string receivingCurrency,
        string sendingCountry,
        string receivingCountry,
        TransactionType transferMethod,
        bool isReceivingAmount,
        bool isFirstTransaction = false)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(sendingCountry) || string.IsNullOrWhiteSpace(receivingCountry))
            {
                _logger.LogWarning("CalculateTransferSummaryAsync called with empty country codes. Sending: {SendingCountry}, Receiving: {ReceivingCountry}", 
                    sendingCountry, receivingCountry);
                return new ExchangeRateCalculationResult
                {
                    IsValid = false,
                    ValidationMessage = "Please select both sending and receiving countries."
                };
            }

            var result = new ExchangeRateCalculationResult
            {
                SendingCurrency = sendingCurrency,
                ReceivingCurrency = receivingCurrency,
                IsIntroductoryFee = isFirstTransaction,
                IsIntroductoryRate = isFirstTransaction
            };

            // Get currency symbols using country codes (not currency, as multiple countries can have same currency)
            var sendingCountryData = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == sendingCountry);
            result.SendingCurrencySymbol = sendingCountryData?.CurrencySymbol ?? "£";
            
            var receivingCountryData = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == receivingCountry);
            result.ReceivingCurrencySymbol = receivingCountryData?.CurrencySymbol ?? "$";

            // Get exchange rate (use a default amount of 1 if both amounts are 0 for initial display)
            var amountForRate = sendingAmount > 0 ? sendingAmount : (receivingAmount > 0 ? receivingAmount : 1.0m);
            var exchangeRate = await GetExchangeRateAsync(sendingCountry, receivingCountry, transferMethod, amountForRate);
            result.ExchangeRate = exchangeRate;

            // Calculate amounts
            if (isReceivingAmount && receivingAmount > 0)
            {
                // Calculate sending amount from receiving amount
                // Formula: ReceivingAmount = (SendingAmount - Fee) * ExchangeRate
                // Rearranged: SendingAmount = (ReceivingAmount / ExchangeRate) + Fee
                // But Fee depends on SendingAmount, so we need to iterate
                
                // Initial estimate without fee
                var estimatedSending = receivingAmount / exchangeRate;
                var fee = await GetFeeAsync(sendingCountry, receivingCountry, transferMethod, estimatedSending, isFirstTransaction);
                
                // Refine calculation: SendingAmount = (ReceivingAmount / ExchangeRate) + Fee
                // Iterate a few times for accuracy
                for (int i = 0; i < 3; i++)
                {
                    estimatedSending = (receivingAmount / exchangeRate) + fee;
                    fee = await GetFeeAsync(sendingCountry, receivingCountry, transferMethod, estimatedSending, isFirstTransaction);
                }
                
                result.SendingAmount = estimatedSending;
                result.ReceivingAmount = receivingAmount;
                result.Fee = fee;
                result.ActualFee = fee;
                result.TotalAmount = result.SendingAmount;
            }
            else if (sendingAmount > 0)
            {
                // Calculate receiving amount from sending amount using exchange rate API
                result.SendingAmount = sendingAmount;
                var fee = await GetFeeAsync(sendingCountry, receivingCountry, transferMethod, sendingAmount, isFirstTransaction);
                result.Fee = fee;
                result.ActualFee = fee;
                result.TotalAmount = sendingAmount;
                
                // Receiving amount = (Sending amount - Fee) * Exchange Rate
                // This uses the exchange rate fetched from the API
                var amountAfterFee = sendingAmount - fee;
                result.ReceivingAmount = amountAfterFee * exchangeRate;
                
                // Round to 2 decimal places
                result.ReceivingAmount = Math.Round(result.ReceivingAmount, 2);
            }
            else
            {
                // Initial state: both amounts are 0, but we still want to show the exchange rate
                result.SendingAmount = 0;
                result.ReceivingAmount = 0;
                result.Fee = 0;
                result.ActualFee = 0;
                result.TotalAmount = 0;
                // Exchange rate is already set above, so it will be displayed
            }

            // Validation - allow empty state (both amounts 0) to be valid
            if (result.SendingAmount == 0 && result.ReceivingAmount == 0)
            {
                result.IsValid = true;
                result.ValidationMessage = string.Empty;
            }
            else
            {
                result.IsValid = ValidateAmount(result.SendingAmount, result.ReceivingAmount, sendingCurrency);
                if (!result.IsValid)
                {
                    result.ValidationMessage = GetValidationMessage(result.SendingAmount, sendingCurrency);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating transfer summary");
            return new ExchangeRateCalculationResult
            {
                IsValid = false,
                ValidationMessage = "Error calculating transfer summary. Please try again."
            };
        }
    }

    private decimal GetDefaultRate(string sendingCurrency, string receivingCurrency)
    {
        var rateKey = $"{sendingCurrency}_{receivingCurrency}";
        if (_defaultRates.TryGetValue(rateKey, out var rate))
        {
            return rate;
        }

        // Default fallback rates
        return sendingCurrency switch
        {
            "GBP" when receivingCurrency == "NGN" => 850.0m,
            "GBP" when receivingCurrency == "GHS" => 12.5m,
            "EUR" when receivingCurrency == "NGN" => 950.0m,
            "USD" when receivingCurrency == "NGN" => 800.0m,
            _ => 1.0m
        };
    }

    private bool ValidateAmount(decimal sendingAmount, decimal receivingAmount, string currency)
    {
        // Allow calculation even if amount is 0 (for initial display)
        // Only validate if we have a meaningful amount
        if (sendingAmount > 0 && sendingAmount < 1)
        {
            return false;
        }
        
        if (sendingAmount <= 0 && receivingAmount <= 0)
        {
            return true; // Allow empty state
        }

        // Maximum amount validation
        var maxAmount = currency switch
        {
            "GBP" => 50000m,
            "USD" => 60000m,
            "EUR" => 55000m,
            _ => 50000m
        };

        return sendingAmount <= maxAmount;
    }

    private string GetValidationMessage(decimal sendingAmount, string currency)
    {
        if (sendingAmount <= 0)
        {
            return "Please enter an amount greater than 0";
        }

        var maxAmount = currency switch
        {
            "GBP" => 50000m,
            "USD" => 60000m,
            "EUR" => 55000m,
            _ => 50000m
        };

        if (sendingAmount > maxAmount)
        {
            return $"Please enter send amount less than or equal to {currency} {maxAmount:N0}";
        }

        return string.Empty;
    }
}

