using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Controllers;

public class HomeController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ITransactionService transactionService,
        IExchangeRateService exchangeRateService,
        MoneyFexDbContext context,
        ILogger<HomeController> logger)
    {
        _transactionService = transactionService;
        _exchangeRateService = exchangeRateService;
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var viewModel = new HomeViewModel
            {
                DefaultSendingCountry = "GB",
                DefaultSendingCurrency = "GBP",
                DefaultReceivingCountry = "NG",
                DefaultReceivingCurrency = "NGN"
            };

            // Get sending countries
            var sendingCountries = await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.CountryName)
                .Select(c => new CountryViewModel
                {
                    CountryCode = c.CountryCode,
                    CountryName = c.CountryName,
                    Currency = c.Currency,
                    CurrencySymbol = c.CurrencySymbol
                })
                .ToListAsync();

            // Get receiving countries
            var receivingCountries = await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.CountryName)
                .Select(c => new CountryViewModel
                {
                    CountryCode = c.CountryCode,
                    CountryName = c.CountryName,
                    Currency = c.Currency,
                    CurrencySymbol = c.CurrencySymbol
                })
                .ToListAsync();

            viewModel.SendingCountries = sendingCountries;
            viewModel.ReceivingCountries = receivingCountries;

            ViewBag.SendingCountries = sendingCountries;
            ViewBag.ReceivingCountries = receivingCountries;
            ViewBag.DefaultSendingCountry = viewModel.DefaultSendingCountry;
            ViewBag.DefaultReceivingCountry = viewModel.DefaultReceivingCountry;
            ViewBag.DefaultReceivingCurrency = viewModel.DefaultReceivingCurrency;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page");
            return View(new HomeViewModel());
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetTransferSummary([FromBody] TransferSummaryRequest request)
    {
        try
        {
            // Convert transfer method to TransactionType enum
            var transferMethod = request.TransferMethod switch
            {
                1 => TransactionType.CashPickup,
                2 => TransactionType.KiiBank,
                3 => TransactionType.MobileWallet,
                4 => TransactionType.BankDeposit,
                _ => TransactionType.BankDeposit
            };

            // Calculate using exchange rate service
            var calculationResult = await _exchangeRateService.CalculateTransferSummaryAsync(
                request.SendingAmount,
                request.ReceivingAmount,
                request.SendingCurrency,
                request.ReceivingCurrency,
                request.SendingCountry,
                request.ReceivingCountry,
                transferMethod,
                request.IsReceivingAmount,
                false // TODO: Check if this is first transaction
            );

            var result = new TransferSummaryViewModel
            {
                SendingAmount = calculationResult.SendingAmount,
                ReceivingAmount = calculationResult.ReceivingAmount,
                Fee = calculationResult.Fee,
                ActualFee = calculationResult.ActualFee,
                ExchangeRate = calculationResult.ExchangeRate,
                TotalAmount = calculationResult.TotalAmount,
                SendingCurrency = calculationResult.SendingCurrency,
                ReceivingCurrency = calculationResult.ReceivingCurrency,
                SendingCurrencySymbol = calculationResult.SendingCurrencySymbol,
                IsIntroductoryRate = calculationResult.IsIntroductoryRate,
                IsIntroductoryFee = calculationResult.IsIntroductoryFee,
                IsValid = new ValidationResult
                {
                    Data = calculationResult.IsValid,
                    Message = calculationResult.ValidationMessage
                }
            };

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating transfer summary");
            return Json(new TransferSummaryViewModel
            {
                IsValid = new ValidationResult { Data = false, Message = "Error calculating transfer summary. Please try again." }
            });
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }
}

public class TransferSummaryRequest
{
    public decimal SendingAmount { get; set; }
    public decimal ReceivingAmount { get; set; }
    public string SendingCurrency { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    public string SendingCountry { get; set; } = string.Empty;
    public string ReceivingCountry { get; set; } = string.Empty;
    public bool IsReceivingAmount { get; set; }
    public int TransferMethod { get; set; }
    public int? SenderId { get; set; }
}

