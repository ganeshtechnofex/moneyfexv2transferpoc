using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.Services;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Controllers;

public class TransferMoneyNowController : Controller
{
    private readonly ILogger<TransferMoneyNowController> _logger;
    private readonly TransferMoneyNowService _transferMoneyNowService;
    private readonly MoneyFexDbContext _context;
    private readonly IExchangeRateService _exchangeRateService;
    private const string CUSTOMER_SESSION_KEY = "CustomerLoggedIn";
    private const string CUSTOMER_SENDER_ID_KEY = "CustomerSenderId";

    public TransferMoneyNowController(
        ILogger<TransferMoneyNowController> logger,
        TransferMoneyNowService transferMoneyNowService,
        MoneyFexDbContext context,
        IExchangeRateService exchangeRateService)
    {
        _logger = logger;
        _transferMoneyNowService = transferMoneyNowService;
        _context = context;
        _exchangeRateService = exchangeRateService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool isFormHomePage = false)
    {
        // Check if customer is logged in
        if (!IsCustomerLoggedIn())
        {
            return RedirectToAction("Index", "CustomerLogin");
        }

        // Get or create sender for the logged-in customer
        var senderId = await GetOrCreateSenderForCustomerAsync();
        if (senderId == 0)
        {
            _logger.LogError("Failed to get or create sender for customer");
            return RedirectToAction("Index", "CustomerLogin");
        }

        // Get view model data
        var viewModel = await _transferMoneyNowService.GetRecentTransferAndRecipientsAsync(senderId);

        // Get sender info for default values
        var sender = await _context.Senders
            .Include(s => s.Country)
            .FirstOrDefaultAsync(s => s.Id == senderId);

        if (sender == null)
        {
            return RedirectToAction("Index", "CustomerLogin");
        }

        // Set default countries and currencies
        var defaultReceivingCountry = "NG"; // Nigeria
        var defaultReceivingCurrency = "NGN";
        var receivingCountry = await _context.Countries
            .FirstOrDefaultAsync(c => c.CountryCode == defaultReceivingCountry);
        
        if (receivingCountry != null)
        {
            defaultReceivingCurrency = receivingCountry.Currency;
        }

        ViewBag.SendingCountry = sender.CountryCode ?? "GB";
        ViewBag.ReceivingCountry = defaultReceivingCountry;
        ViewBag.SendingCurrency = sender.Country?.Currency ?? "GBP";
        ViewBag.ReceivingCurrency = defaultReceivingCurrency;
        ViewBag.DefaultReceivingCurrency = defaultReceivingCurrency;
        ViewBag.SendingAmount = 3; // Default amount
        ViewBag.SenderId = senderId;

        // Get receiving countries for dropdown
        var receivingCountries = await _context.Countries
            .Where(c => c.IsActive)
            .OrderBy(c => c.CountryName)
            .Select(c => new
            {
                CountryCode = c.CountryCode,
                CountryName = c.CountryName,
                Currency = c.Currency
            })
            .ToListAsync();

        ViewBag.ReceivingCountries = receivingCountries;
        ViewBag.CountryNameWithCur = defaultReceivingCurrency + " &nbsp;&nbsp; " + 
            (receivingCountries.FirstOrDefault(c => c.CountryCode == defaultReceivingCountry)?.CountryName ?? "Nigeria");

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> GetTransferSummary([FromBody] TransferSummaryRequest? request)
    {
        try
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            // Validate request
            if (request == null)
            {
                _logger.LogWarning("GetTransferSummary called with null request");
                return Json(new
                {
                    success = false,
                    IsValid = new { Data = false, Message = "Invalid request data. Please try again." }
                });
            }

            // Map TransferMethod to TransactionType
            // UI values: 1=CashPickup, 2=KiiBank, 3=MobileWallet, 4=BankAccount
            // Enum values: BankDeposit=1, MobileWallet=2, CashPickup=3, KiiBank=7
            var transferMethod = request.TransferMethod switch
            {
                1 => TransactionType.CashPickup,      // UI: CashPickup -> Enum: CashPickup (3)
                2 => TransactionType.KiiBank,         // UI: KiiBank -> Enum: KiiBank (7)
                3 => TransactionType.MobileWallet,     // UI: MobileWallet -> Enum: MobileWallet (2)
                4 => TransactionType.BankDeposit,     // UI: BankAccount -> Enum: BankDeposit (1)
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

            // Return in camelCase format to match home page pattern
            var result = new
            {
                success = true,
                sendingAmount = calculationResult.SendingAmount,
                receivingAmount = calculationResult.ReceivingAmount,
                fee = calculationResult.Fee,
                actualFee = calculationResult.ActualFee,
                exchangeRate = calculationResult.ExchangeRate,
                totalAmount = calculationResult.TotalAmount,
                sendingCurrency = calculationResult.SendingCurrency,
                receivingCurrency = calculationResult.ReceivingCurrency,
                sendingCurrencySymbol = calculationResult.SendingCurrencySymbol,
                receivingCurrencySymbol = calculationResult.ReceivingCurrencySymbol,
                isIntroductoryRate = calculationResult.IsIntroductoryRate,
                isIntroductoryFee = calculationResult.IsIntroductoryFee,
                isValid = new
                {
                    data = calculationResult.IsValid,
                    message = calculationResult.ValidationMessage
                }
            };

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating transfer summary");
            return Json(new
            {
                success = false,
                IsValid = new { Data = false, Message = "Error calculating transfer summary. Please try again." }
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ValidateTransfer([FromBody] TransferSummaryRequest? request)
    {
        try
        {
            if (!IsCustomerLoggedIn())
            {
                return Json(new { Status = 0, Message = "Not authenticated" });
            }

            // Validate request
            if (request == null)
            {
                _logger.LogWarning("ValidateTransfer called with null request");
                return Json(new { Status = 0, Message = "Invalid request data. Please try again." });
            }

            // Basic validation
            if (request.SendingAmount <= 0)
            {
                return Json(new { Status = 0, Message = "Please enter a valid sending amount" });
            }

            if (request.SendingAmount > 50000)
            {
                return Json(new { Status = 0, Message = "Please enter send amount less than or equal to 50,000" });
            }

            // Map TransferMethod to route
            // UI values: 1=CashPickup, 2=KiiBank, 3=MobileWallet, 4=BankAccount
            var transferMethod = request.TransferMethod switch
            {
                1 => "CashPickup",
                2 => "KiiBank",
                3 => "MobileWallet",
                4 => "BankDeposit",
                _ => "BankDeposit"
            };

            return Json(new
            {
                Status = 1,
                Message = "Valid",
                TransferMethod = transferMethod
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating transfer");
            return Json(new { Status = 0, Message = "Error validating transfer. Please try again." });
        }
    }

    private bool IsCustomerLoggedIn()
    {
        return HttpContext.Session.GetString(CUSTOMER_SESSION_KEY) == "true";
    }

    private async Task<int> GetOrCreateSenderForCustomerAsync()
    {
        // Check if sender ID is already in session
        var senderIdStr = HttpContext.Session.GetString(CUSTOMER_SENDER_ID_KEY);
        if (!string.IsNullOrEmpty(senderIdStr) && int.TryParse(senderIdStr, out var existingSenderId))
        {
            var existingSender = await _context.Senders.FindAsync(existingSenderId);
            if (existingSender != null)
            {
                return existingSenderId;
            }
        }

        // Get customer username from session
        var username = HttpContext.Session.GetString("CustomerUsername") ?? "customer";

        // Try to find existing sender by email (using username as email for POC)
        var sender = await _context.Senders
            .FirstOrDefaultAsync(s => s.Email == $"{username}@moneyfex.com");

        if (sender != null)
        {
            HttpContext.Session.SetString(CUSTOMER_SENDER_ID_KEY, sender.Id.ToString());
            return sender.Id;
        }

        // Create new sender for customer
        var defaultCountry = await _context.Countries.FirstOrDefaultAsync(c => c.CountryCode == "GB");
        
        sender = new MoneyFex.Core.Entities.Sender
        {
            FirstName = "Customer",
            LastName = "User",
            Email = $"{username}@moneyfex.com",
            PhoneNumber = "+44123456789",
            CountryCode = "GB",
            IsActive = true,
            IsBusiness = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Senders.Add(sender);
        await _context.SaveChangesAsync();

        HttpContext.Session.SetString(CUSTOMER_SENDER_ID_KEY, sender.Id.ToString());
        return sender.Id;
    }
}

