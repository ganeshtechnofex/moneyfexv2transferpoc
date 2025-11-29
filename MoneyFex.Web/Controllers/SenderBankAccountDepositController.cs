using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.Services;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Controllers;

/// <summary>
/// Controller for sender bank account deposit functionality
/// Replicates legacy /SenderBankAccountDeposit/Index endpoint
/// </summary>
public class SenderBankAccountDepositController : Controller
{
    private readonly ILogger<SenderBankAccountDepositController> _logger;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly MoneyFexDbContext _context;
    private readonly TransactionLimitService _transactionLimitService;
    private readonly ITransactionIdempotencyService _idempotencyService;

    public SenderBankAccountDepositController(
        ILogger<SenderBankAccountDepositController> logger,
        IExchangeRateService exchangeRateService,
        MoneyFexDbContext context,
        TransactionLimitService transactionLimitService,
        ITransactionIdempotencyService idempotencyService)
    {
        _logger = logger;
        _exchangeRateService = exchangeRateService;
        _context = context;
        _transactionLimitService = transactionLimitService;
        _idempotencyService = idempotencyService;
    }

    /// <summary>
    /// GET: SenderBankAccountDeposit/Index
    /// Replicates legacy endpoint: /SenderBankAccountDeposit/Index?RecentAcccountNo=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? recentAccountNo = "")
    {
        try
        {
            // Get sender ID from session
            var senderId = GetSenderIdFromSession();
            
            // Get transaction summary from query parameters or TempData
            var sendingAmount = GetQueryParamAsDecimal("SendingAmount") ?? 
                GetQueryParamAsDecimal("amount") ?? 3m;
            var receivingAmount = GetQueryParamAsDecimal("ReceivingAmount") ?? 0m;
            var sendingCurrency = Request.Query["SendingCurrency"].FirstOrDefault() ?? 
                TempData["SendingCurrency"]?.ToString() ?? "GBP";
            var receivingCurrency = Request.Query["ReceivingCurrency"].FirstOrDefault() ?? 
                TempData["ReceivingCurrency"]?.ToString() ?? "NGN";
            var sendingCountry = Request.Query["SendingCountry"].FirstOrDefault() ?? 
                TempData["SendingCountry"]?.ToString() ?? "GB";
            var receivingCountry = Request.Query["ReceivingCountry"].FirstOrDefault() ?? 
                TempData["ReceivingCountry"]?.ToString() ?? "NG";

            // Validate receiving country code
            if (string.IsNullOrWhiteSpace(receivingCountry))
            {
                receivingCountry = "NG";
            }

            // Initialize view model
            var viewModel = new SenderBankAccountDepositViewModel
            {
                CountryCode = receivingCountry,
                RecentAccountNumber = recentAccountNo,
                SendingAmount = sendingAmount,
                ReceivingAmount = receivingAmount,
                SendingCurrency = sendingCurrency,
                ReceivingCurrency = receivingCurrency,
                SendingCountry = sendingCountry,
                ReceivingCountry = receivingCountry,
                SenderId = senderId
            };

            // Get phone code for the country
            viewModel.CountryPhoneCode = GetCountryPhoneCodeHelper(receivingCountry);

            // Check if it's Europe, South Africa, or West Africa transfer
            viewModel.IsEuropeTransfer = IsEuropeTransfer(receivingCountry);
            viewModel.IsSouthAfricaTransfer = receivingCountry == "ZA";
            viewModel.IsWestAfricaTransfer = IsWestAfricaTransfer(receivingCountry);

            // Get receiving country currency
            try
            {
                var receivingCountryData = await _context.Countries
                    .FirstOrDefaultAsync(c => c.CountryCode == receivingCountry);
                if (receivingCountryData != null)
                {
                    viewModel.ReceivingCurrency = receivingCountryData.Currency;
                }
                else
                {
                    // Fallback to NGN if country not found
                    viewModel.ReceivingCurrency = receivingCurrency;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting receiving country data for: {CountryCode}", receivingCountry);
                viewModel.ReceivingCurrency = receivingCurrency;
            }

            // Set ViewBag for summary display
            ViewBag.ReceivingCountryCurrency = viewModel.ReceivingCurrency;
            ViewBag.TransferMethod = "Bank Deposit";
            ViewBag.SendingCountryCurrency = sendingCurrency;
            ViewBag.SendingAmount = sendingAmount;
            ViewBag.ReceivingCountry = receivingCountry.ToLower();

            // Set ViewBags for dropdowns (handle errors gracefully)
            try
            {
                await SetBanksViewBagAsync(receivingCountry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting banks ViewBag for country: {CountryCode}", receivingCountry);
                ViewBag.BankNames = new List<SelectListItem>();
            }

            try
            {
                await SetRecentAccountNumbersViewBagAsync(senderId, receivingCountry, recentAccountNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting recent account numbers ViewBag");
                ViewBag.RecentAccountNumbers = new List<SelectListItem>();
            }

            try
            {
                await SetBranchViewBagAsync(0, null, receivingCountry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting branches ViewBag");
                ViewBag.Branches = new List<SelectListItem>();
            }

            try
            {
                SetIdCardTypesViewBag();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting ID card types ViewBag");
                ViewBag.IdCardTypes = new List<SelectListItem>();
            }

            // Populate recent account info if provided
            if (!string.IsNullOrEmpty(recentAccountNo) && senderId > 0)
            {
                await PopulateRecentAccountInfoAsync(viewModel, recentAccountNo, receivingCountry);
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bank account deposit form. Exception: {ExceptionMessage}, StackTrace: {StackTrace}", 
                ex.Message, ex.StackTrace);
            
            // Return a more helpful error page or redirect with error message
            TempData["Error"] = "An error occurred while loading the form. Please try again.";
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: SenderBankAccountDeposit/Index
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SenderBankAccountDepositViewModel model)
    {
        try
        {
            var senderId = GetSenderIdFromSession();
            model.SenderId = senderId;

            var normalizedIdempotencyKey = _idempotencyService.NormalizeKey(model.IdempotencyKey);
            if (!string.IsNullOrEmpty(normalizedIdempotencyKey))
            {
                var existingTransaction = await _idempotencyService.FindExistingAsync(senderId, normalizedIdempotencyKey);
                if (existingTransaction != null)
                {
                    HttpContext.Session.SetInt32("BankDepositTransactionId", existingTransaction.Id);
                    HttpContext.Session.SetString("BankDepositReceiptNo", existingTransaction.ReceiptNo);
                    return RedirectToAction("BankDepositAbroadSummary", new { transactionId = existingTransaction.Id });
                }
            }

            // Get sender with country information
            var sender = await _context.Senders
                .Include(s => s.Country)
                .FirstOrDefaultAsync(s => s.Id == senderId);
            
            if (sender != null)
            {
                model.SenderCountry = sender.CountryCode;
            }

            // Set ViewBags for dropdowns
            await SetBanksViewBagAsync(model.CountryCode);
            await SetRecentAccountNumbersViewBagAsync(senderId, model.CountryCode, model.RecentAccountNumber);
            await SetBranchViewBagAsync(model.BankId, model.BranchCode, model.CountryCode);
            SetIdCardTypesViewBag();

            // Get receiving country currency
            var receivingCountryData = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == model.CountryCode);
            if (receivingCountryData != null)
            {
                model.ReceivingCurrency = receivingCountryData.Currency;
            }

            // Set ViewBag for summary display
            ViewBag.ReceivingCountryCurrency = model.ReceivingCurrency;
            ViewBag.TransferMethod = "Bank Deposit";
            ViewBag.SendingCountryCurrency = model.SendingCurrency;
            ViewBag.SendingAmount = model.SendingAmount;
            ViewBag.ReceivingCountry = model.CountryCode.ToLower();

            // Validate model
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Additional validations based on country
            if (model.CountryCode == "KE" && model.BankId == 0)
            {
                ModelState.AddModelError("BankId", "Select Bank Name");
                return View(model);
            }

            if (model.CountryCode == "GH" && string.IsNullOrEmpty(model.BranchCode))
            {
                ModelState.AddModelError("BranchCode", "Select Branch");
                return View(model);
            }

            // Calculate exchange rate and fee if not already set
            if (model.Fee == 0 || model.ExchangeRate == 0 || model.ReceivingAmount == 0)
            {
                var calculation = await _exchangeRateService.CalculateTransferSummaryAsync(
                    model.SendingAmount,
                    0,
                    model.SendingCurrency,
                    model.ReceivingCurrency,
                    model.SendingCountry,
                    model.CountryCode,
                    TransactionType.BankDeposit,
                    false,
                    false);

                model.ExchangeRate = calculation.ExchangeRate;
                model.Fee = calculation.Fee;
                model.ReceivingAmount = calculation.ReceivingAmount;
                model.TotalAmount = calculation.TotalAmount;
            }

            // Generate receipt number
            var receiptNo = await GenerateUniqueReceiptNumberAsync();

            // Get sender country
            var senderCountry = model.SenderCountry ?? model.SendingCountry;
            if (sender != null && string.IsNullOrEmpty(senderCountry))
            {
                senderCountry = sender.CountryCode ?? "GB";
            }

            // Create transaction
            var idempotencyKeyToUse = normalizedIdempotencyKey ?? _idempotencyService.GenerateKey();
            model.IdempotencyKey = idempotencyKeyToUse;

            var transaction = new Transaction
            {
                ReceiptNo = receiptNo,
                SenderId = senderId,
                SendingCountryCode = senderCountry,
                ReceivingCountryCode = model.CountryCode,
                SendingCurrency = model.SendingCurrency,
                ReceivingCurrency = model.ReceivingCurrency,
                SendingAmount = model.SendingAmount,
                ReceivingAmount = model.ReceivingAmount,
                Fee = model.Fee,
                TotalAmount = model.TotalAmount,
                ExchangeRate = model.ExchangeRate,
                TransactionDate = DateTime.UtcNow,
                Status = TransactionStatus.PaymentPending,
                TransactionModule = TransactionModule.Sender,
                SenderPaymentMode = PaymentMode.Card, // Will be updated after payment
                IdempotencyKey = idempotencyKeyToUse,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Get bank information
            var bank = model.BankId > 0 
                ? await _context.Banks.FirstOrDefaultAsync(b => b.Id == model.BankId)
                : null;

            // Create bank account deposit
            var bankDeposit = new BankAccountDeposit
            {
                TransactionId = transaction.Id,
                BankId = model.BankId > 0 ? model.BankId : null,
                BankName = bank?.Name ?? model.BankName,
                BankCode = bank?.Code ?? model.BranchCode,
                ReceiverAccountNo = model.AccountNumber,
                ReceiverName = model.AccountOwnerName,
                ReceiverCity = model.ReceiverCity,
                ReceiverCountry = model.CountryCode,
                ReceiverMobileNo = model.MobileNumber,
                IsManualDeposit = false, // Can be determined based on country rules
                IsEuropeTransfer = model.IsEuropeTransfer,
                IsBusiness = model.IsBusiness,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BankAccountDeposits.Add(bankDeposit);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bank deposit transaction created. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}",
                transaction.Id, receiptNo);

            // Store transaction ID in session for summary page
            HttpContext.Session.SetInt32("BankDepositTransactionId", transaction.Id);
            HttpContext.Session.SetString("BankDepositReceiptNo", receiptNo);

            // Redirect to summary page
            return RedirectToAction("BankDepositAbroadSummary", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bank account deposit form. Exception: {ExceptionMessage}, StackTrace: {StackTrace}", 
                ex.Message, ex.StackTrace);
            
            ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
            
            // Reload ViewBags on error
            var senderId = GetSenderIdFromSession();
            try
            {
                await SetBanksViewBagAsync(model.CountryCode ?? "NG");
                await SetRecentAccountNumbersViewBagAsync(senderId, model.CountryCode ?? "NG", model.RecentAccountNumber);
                await SetBranchViewBagAsync(model.BankId, model.BranchCode, model.CountryCode ?? "NG");
                SetIdCardTypesViewBag();
                
                // Get receiving country currency
                var receivingCountryData = await _context.Countries
                    .FirstOrDefaultAsync(c => c.CountryCode == (model.CountryCode ?? "NG"));
                if (receivingCountryData != null)
                {
                    model.ReceivingCurrency = receivingCountryData.Currency;
                }
                
                ViewBag.ReceivingCountryCurrency = model.ReceivingCurrency;
                ViewBag.TransferMethod = "Bank Deposit";
                ViewBag.SendingCountryCurrency = model.SendingCurrency ?? "GBP";
                ViewBag.SendingAmount = model.SendingAmount;
                ViewBag.ReceivingCountry = (model.CountryCode ?? "NG").ToLower();
            }
            catch (Exception viewBagEx)
            {
                _logger.LogError(viewBagEx, "Error reloading ViewBags after error");
            }
            
            return View(model);
        }
    }

    /// <summary>
    /// GET: Get bank code and branches
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBankCode(int bankId)
    {
        try
        {
            var bank = await _context.Banks
                .FirstOrDefaultAsync(b => b.Id == bankId);

            if (bank == null)
            {
                return Json(new { branchCode = "", branches = new List<object>() });
            }

            // Get branches for the bank (if applicable)
            var branches = new List<object>(); // TODO: Implement branches if needed

            return Json(new
            {
                branchCode = bank.Code ?? "",
                branches = branches
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank code");
            return Json(new { branchCode = "", branches = new List<object>() });
        }
    }

    /// <summary>
    /// GET: Get account information from recent account number
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAccountInformation(string accountNo, string countryCode)
    {
        try
        {
            var senderId = GetSenderIdFromSession();
            
            // Find recent bank account deposit with this account number
            var recentDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Transaction)
                .Where(bd => bd.Transaction.SenderId == senderId && 
                            bd.ReceiverAccountNo == accountNo &&
                            bd.Transaction.ReceivingCountryCode == countryCode)
                .OrderByDescending(bd => bd.CreatedAt)
                .FirstOrDefaultAsync();

            if (recentDeposit != null)
            {
                return Json(new
                {
                    accountOwnerName = recentDeposit.ReceiverName ?? "",
                    bankId = recentDeposit.BankId ?? 0,
                    branchCode = recentDeposit.BankCode ?? "",
                    mobileNumber = "", // Not stored in BankAccountDeposit
                    countryPhoneCode = GetCountryPhoneCodeHelper(countryCode)
                });
            }

            return Json(new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account information");
            return Json(new { });
        }
    }

    /// <summary>
    /// GET: Check if FlutterWave API is used (requires email)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckifFlutterWaveApi(string countryCode, int bankId)
    {
        try
        {
            // For now, return false (can be enhanced based on bank API configuration)
            var isFlutterwave = false;
            var isSouthAfricanTransfer = countryCode == "ZA";

            return Json(new
            {
                isFlutterwaveApi = isFlutterwave,
                isSouthAfricanTransfer = isSouthAfricanTransfer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FlutterWave API");
            return Json(new { isFlutterwaveApi = false, isSouthAfricanTransfer = false });
        }
    }

    // Helper methods

    private int GetSenderIdFromSession()
    {
        // Try multiple session keys
        var senderIdStr = HttpContext.Session.GetString("CustomerSenderId") ?? 
                         HttpContext.Session.GetString("SenderId") ?? 
                         HttpContext.Session.GetString("CustomerSenderId");
        
        if (!string.IsNullOrEmpty(senderIdStr) && int.TryParse(senderIdStr, out var senderId) && senderId > 0)
        {
            return senderId;
        }
        
        // For POC, return default sender ID (1) if no session found
        // In production, this should redirect to login
        return 1;
    }

    private decimal? GetQueryParamAsDecimal(string paramName)
    {
        var value = Request.Query[paramName].FirstOrDefault();
        if (decimal.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }

    private async Task SetBanksViewBagAsync(string countryCode)
    {
        try
        {
            var banks = await _context.Banks
                .Where(b => b.CountryCode == countryCode && b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name,
                    Selected = false
                })
                .ToListAsync();

            ViewBag.BankNames = banks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting banks ViewBag");
            ViewBag.BankNames = new List<SelectListItem>();
        }
    }

    private async Task SetRecentAccountNumbersViewBagAsync(int senderId, string countryCode, string? recentAccountNo)
    {
        try
        {
            // Skip query if senderId is invalid (0)
            if (senderId <= 0)
            {
                ViewBag.RecentAccountNumbers = new List<SelectListItem>();
                return;
            }

            var recentAccountNumbers = await _context.BankAccountDeposits
                .Include(bd => bd.Transaction)
                .Where(bd => bd.Transaction != null && 
                            bd.Transaction.SenderId == senderId && 
                            bd.Transaction.ReceivingCountryCode == countryCode)
                .OrderByDescending(bd => bd.CreatedAt)
                .Take(10)
                .Select(bd => new
                {
                    Code = bd.ReceiverAccountNo ?? "",
                    Name = (bd.ReceiverAccountNo ?? "") + " - " + (bd.ReceiverName ?? "")
                })
                .Distinct()
                .ToListAsync();

            var selectList = recentAccountNumbers
                .Select(ra => new SelectListItem
                {
                    Value = ra.Code,
                    Text = ra.Name,
                    Selected = ra.Code == recentAccountNo
                })
                .ToList();

            ViewBag.RecentAccountNumbers = selectList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting recent account numbers ViewBag");
            ViewBag.RecentAccountNumbers = new List<SelectListItem>();
        }
    }

    private async Task SetBranchViewBagAsync(int bankId, string? branchCode, string countryCode)
    {
        try
        {
            // For now, return empty list (branches can be implemented later if needed)
            ViewBag.Branches = new List<SelectListItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting branches ViewBag");
            ViewBag.Branches = new List<SelectListItem>();
        }
    }

    private void SetIdCardTypesViewBag()
    {
        // Common ID card types (can be moved to database)
        var idCardTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "National ID" },
            new SelectListItem { Value = "2", Text = "Passport" },
            new SelectListItem { Value = "3", Text = "Driver's License" }
        };

        ViewBag.IdCardTypes = idCardTypes;
    }

    private async Task PopulateRecentAccountInfoAsync(SenderBankAccountDepositViewModel viewModel, string accountNo, string countryCode)
    {
        try
        {
            var senderId = GetSenderIdFromSession();
            
            // Skip query if senderId is invalid (0)
            if (senderId <= 0)
            {
                return;
            }

            var recentDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Transaction)
                .Where(bd => bd.Transaction != null && 
                            bd.Transaction.SenderId == senderId && 
                            bd.ReceiverAccountNo == accountNo &&
                            bd.Transaction.ReceivingCountryCode == countryCode)
                .OrderByDescending(bd => bd.CreatedAt)
                .FirstOrDefaultAsync();

            if (recentDeposit != null)
            {
                viewModel.AccountOwnerName = recentDeposit.ReceiverName ?? "";
                viewModel.BankId = recentDeposit.BankId ?? 0;
                viewModel.AccountNumber = recentDeposit.ReceiverAccountNo ?? "";
                viewModel.BranchCode = recentDeposit.BankCode;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating recent account info");
        }
    }

    private string GetCountryPhoneCodeHelper(string countryCode)
    {
        return countryCode switch
        {
            "NG" => "+234",
            "KE" => "+254",
            "GH" => "+233",
            "CM" => "+237",
            "UG" => "+256",
            "TZ" => "+255",
            "ZA" => "+27",
            "GB" => "+44",
            "US" => "+1",
            _ => "+234"
        };
    }

    private bool IsEuropeTransfer(string countryCode)
    {
        // European countries (simplified list)
        var europeCountries = new[] { "FR", "DE", "IT", "ES", "NL", "BE", "AT", "CH", "SE", "NO", "DK", "FI", "PL", "IE", "PT", "GR" };
        return europeCountries.Contains(countryCode);
    }

    private bool IsWestAfricaTransfer(string countryCode)
    {
        // West African countries
        var westAfricaCountries = new[] { "NG", "GH", "SN", "CI", "TG", "BJ", "ML", "NE", "BF", "GW", "GN", "SL", "LR", "MR", "GM" };
        return westAfricaCountries.Contains(countryCode);
    }

    private async Task<string> GenerateUniqueReceiptNumberAsync()
    {
        string receiptNo;
        var random = new Random();
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            receiptNo = $"MFX{DateTime.UtcNow:yyyyMMdd}{random.Next(100000, 999999)}";
            var exists = await _context.Transactions.AnyAsync(t => t.ReceiptNo == receiptNo);
            if (!exists)
            {
                return receiptNo;
            }
            attempts++;
        } while (attempts < maxAttempts);

        // Fallback with timestamp if all attempts fail
        return $"MFX{DateTime.UtcNow:yyyyMMddHHmmss}{random.Next(1000, 9999)}";
    }

    private string GetCurrencySymbol(string currencyCode)
    {
        // Common currency symbols mapping
        return currencyCode switch
        {
            "GBP" => "£",
            "USD" => "$",
            "EUR" => "€",
            "NGN" => "₦",
            "KES" => "KSh",
            "GHS" => "GH₵",
            "XAF" => "FCFA",
            "UGX" => "USh",
            "TZS" => "TSh",
            "ZAR" => "R",
            _ => currencyCode
        };
    }

    /// <summary>
    /// GET: BankDepositAbroadSummary - Summary page before payment
    /// Replicates legacy /SenderBankAccountDeposit/BankDepositAbroadSummary
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> BankDepositAbroadSummary(int transactionId)
    {
        try
        {
            // Get transaction from database
            var transaction = await _context.Transactions
                .Include(t => t.Sender)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get bank account deposit details
            var bankDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Bank)
                .FirstOrDefaultAsync(bd => bd.TransactionId == transactionId);

            if (bankDeposit == null)
            {
                _logger.LogWarning("Bank deposit not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);
            var receivingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.ReceivingCountryCode);

            // Build view model
            var viewModel = new BankDepositSummaryViewModel
            {
                Id = transactionId,
                TransactionId = transactionId,
                ReceiptNo = transaction.ReceiptNo,
                Amount = transaction.SendingAmount,
                Fee = transaction.Fee,
                PaidAmount = transaction.TotalAmount,
                ReceivedAmount = transaction.ReceivingAmount,
                SendingCurrencyCode = transaction.SendingCurrency,
                ReceivingCurrencyCode = transaction.ReceivingCurrency,
                SendingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                ReceivingCurrencySymbol = receivingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.ReceivingCurrency),
                ReceiverName = bankDeposit.ReceiverName ?? "N/A",
                BankAccountNo = bankDeposit.ReceiverAccountNo ?? "N/A",
                BankName = bankDeposit.BankName ?? bankDeposit.Bank?.Name ?? "N/A",
                BankCode = bankDeposit.BankCode ?? bankDeposit.Bank?.Code ?? "N/A"
            };

            // Set ViewBag for view
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Bank Deposit";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = bankDeposit.ReceiverName ?? "N/A";
            
            // Extract first name from receiver name
            var fullName = bankDeposit.ReceiverName ?? "";
            var firstName = "";
            if (!string.IsNullOrEmpty(fullName))
            {
                var names = fullName.Split(' ');
                firstName = names.Length > 0 ? names[0] : fullName;
            }
            viewModel.ReceiverName = firstName;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bank deposit summary");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: BankDepositAbroadSummary - Proceed to payment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankDepositAbroadSummary(BankDepositSummaryViewModel model)
    {
        try
        {
            // Get transaction from database
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == model.TransactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Transaction not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get bank account deposit details
            var bankDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Bank)
                .FirstOrDefaultAsync(bd => bd.TransactionId == model.TransactionId);

            if (bankDeposit == null)
            {
                _logger.LogWarning("Bank deposit not found for transaction: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Bank deposit details not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Redirect to InternationalPayNow (legacy pattern)
            return RedirectToAction("InternationalPayNow", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bank deposit summary");
            ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
            
            // Reload the summary view on error
            return await BankDepositAbroadSummary(model.TransactionId);
        }
    }

    /// <summary>
    /// GET: InternationalPayNow - Payment method selection page for bank deposit
    /// Replicates legacy /SenderBankAccountDeposit/InternationalPayNow
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> InternationalPayNow(int transactionId)
    {
        try
        {
            // Get transaction from database
            var transaction = await _context.Transactions
                .Include(t => t.Sender)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get bank account deposit details
            var bankDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Bank)
                .FirstOrDefaultAsync(bd => bd.TransactionId == transactionId);

            if (bankDeposit == null)
            {
                _logger.LogWarning("Bank deposit not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);
            var receivingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.ReceivingCountryCode);

            // Build view model
            var viewModel = new PaymentMethodViewModel
            {
                TransactionId = transactionId,
                ReceiptNo = transaction.ReceiptNo,
                TotalAmount = transaction.TotalAmount,
                SendingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                SenderPaymentMode = PaymentMode.Card, // Default to Card
                HasKiiPayWallet = false, // TODO: Check if sender has KiiPay wallet
                HasEnableMoneyFexBankAccount = false, // TODO: Check if MoneyFex bank account is enabled for sender's country
                CardDetails = new List<SavedCardViewModel>() // TODO: Load saved cards if any
            };

            // Set ViewBag for view
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Bank Deposit";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = bankDeposit.ReceiverName ?? "N/A";
            ViewBag.Fee = transaction.Fee;
            ViewBag.CreditDebitFee = 0.05m; // Static fee for POC
            ViewBag.ManualBankDepositFee = 0.79m; // Static fee for POC
            ViewBag.ErrorMessage = TempData["ErrorMessage"] as string ?? "";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading InternationalPayNow page");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: InternationalPayNow - Process payment method selection for bank deposit
    /// Replicates legacy /SenderBankAccountDeposit/InternationalPayNow POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InternationalPayNow(PaymentMethodViewModel model)
    {
        try
        {
            // Get transaction from database
            var transaction = await _context.Transactions
                .Include(t => t.Sender)
                .FirstOrDefaultAsync(t => t.Id == model.TransactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Transaction not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get bank account deposit details
            var bankDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Bank)
                .FirstOrDefaultAsync(bd => bd.TransactionId == model.TransactionId);

            if (bankDeposit == null)
            {
                _logger.LogWarning("Bank deposit not found for transaction: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Bank deposit details not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);
            var receivingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.ReceivingCountryCode);

            // Set ViewBag for view (in case of errors)
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Bank Deposit";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = bankDeposit.ReceiverName ?? "N/A";
            ViewBag.Fee = transaction.Fee;
            ViewBag.CreditDebitFee = 0.05m;
            ViewBag.ManualBankDepositFee = 0.79m;
            ViewBag.ErrorMessage = "";

            // Handle saved card selection (legacy pattern)
            int? selectedCardId = null;
            string? cardNumber = null;
            string? creditCardSecurityCode = null;
            
            if (model.CardDetails != null && model.CardDetails.Any())
            {
                var selectedCard = model.CardDetails.FirstOrDefault(c => c.IsChecked);
                if (selectedCard != null)
                {
                    selectedCardId = selectedCard.CardId;
                    cardNumber = selectedCard.CardNumber;
                    creditCardSecurityCode = selectedCard.SecurityCode;
                    model.SenderPaymentMode = PaymentMode.Card; // Saved card is also Card payment
                }
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                model.SendingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency);
                // Reload card details on error
                if (model.CardDetails == null || !model.CardDetails.Any())
                {
                    model.CardDetails = new List<SavedCardViewModel>(); // TODO: Load saved cards
                }
                return View(model);
            }

            // Get recipient ID for limit validation (if exists)
            var recipientId = bankDeposit.RecipientId ?? 0;

            // Validate transaction limits (legacy pattern)
            var hasExceededReceiverLimit = await _transactionLimitService.HasExceededReceiverLimitAsync(
                transaction.SenderId,
                recipientId,
                transaction.SendingCountryCode,
                transaction.ReceivingCountryCode,
                TransactionType.BankDeposit);

            if (hasExceededReceiverLimit)
            {
                ModelState.AddModelError("TransactionError", "Recipient daily transaction limit exceeded");
                model.SendingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency);
                if (model.CardDetails == null || !model.CardDetails.Any())
                {
                    model.CardDetails = new List<SavedCardViewModel>(); // TODO: Load saved cards
                }
                return View(model);
            }

            var hasExceededSenderLimit = await _transactionLimitService.HasExceededSenderTransactionLimitAsync(
                transaction.SenderId,
                transaction.SendingCountryCode,
                transaction.ReceivingCountryCode,
                TransactionType.BankDeposit);

            if (hasExceededSenderLimit)
            {
                ModelState.AddModelError("TransactionError", "Sender daily transaction limit exceeded");
                model.SendingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency);
                if (model.CardDetails == null || !model.CardDetails.Any())
                {
                    model.CardDetails = new List<SavedCardViewModel>(); // TODO: Load saved cards
                }
                return View(model);
            }

            // Update transaction with payment mode
            transaction.SenderPaymentMode = model.SenderPaymentMode;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment method selected for bank deposit. TransactionId: {TransactionId}, PaymentMode: {PaymentMode}, SelectedCardId: {SelectedCardId}",
                transaction.Id, model.SenderPaymentMode, selectedCardId);

            // Redirect based on payment mode (legacy pattern lines 625-646)
            switch (model.SenderPaymentMode)
            {
                case PaymentMode.Card:
                    // If saved card was selected, redirect with IsFromSavedDebitCard flag
                    if (selectedCardId.HasValue && selectedCardId.Value > 0)
                    {
                        // Store card details in session for DebitCreditCardDetails
                        HttpContext.Session.SetInt32("SelectedCardId", selectedCardId.Value);
                        if (!string.IsNullOrEmpty(creditCardSecurityCode))
                        {
                            HttpContext.Session.SetString("CardSecurityCode", creditCardSecurityCode);
                        }
                        // TODO: Validate card and save card details if needed (legacy lines 594-623)
                        return RedirectToAction("DebitCreditCardDetails", new 
                        { 
                            transactionId = transaction.Id,
                            IsFromSavedDebitCard = true 
                        });
                    }
                    // New card - redirect to DebitCreditCardDetails
                    return RedirectToAction("DebitCreditCardDetails", new { transactionId = transaction.Id });

                case PaymentMode.BankAccount:
                    // MoneyFex Bank Account
                    return RedirectToAction("MoneyFexBankDeposit", new { transactionId = transaction.Id });

                case PaymentMode.MobileWallet:
                    // KiiPay Wallet - check balance and complete transaction (legacy lines 629-637)
                    // TODO: Implement wallet balance check
                    // var hasEnoughBal = senderCommonFunc.SenderHasEnoughWalletBaltoTransfer(transaction.TotalAmount, senderWalletId);
                    // if (hasEnoughBal == false)
                    // {
                    //     ModelState.AddModelError("TransactionError", "Your wallet doesn't have enough balance!");
                    //     model.CardDetails = new List<SavedCardViewModel>(); // TODO: Load saved cards
                    //     return View(model);
                    // }
                    
                    // For POC, complete transaction directly
                    transaction.SenderPaymentMode = PaymentMode.MobileWallet;
                    transaction.Status = TransactionStatus.Paid;
                    await _context.SaveChangesAsync();
                    return RedirectToAction("AddMoneyToWalletSuccess", new { transactionId = transaction.Id });

                // TODO: Handle AutomatedBankPayout if implemented
                // case PaymentMode.AutomatedBankPayout:
                //     return RedirectToAction("Index", "AutomatedBankPayout");

                default:
                    ModelState.AddModelError("", "Please select a valid payment method.");
                    model.SendingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency);
                    if (model.CardDetails == null || !model.CardDetails.Any())
                    {
                        model.CardDetails = new List<SavedCardViewModel>(); // TODO: Load saved cards
                    }
                    return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing InternationalPayNow");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            TempData["ErrorMessage"] = "An error occurred while processing your payment method selection. Please try again.";
            return RedirectToAction("InternationalPayNow", new { transactionId = model.TransactionId });
        }
    }

    /// <summary>
    /// GET: DebitCreditCardDetails - Card payment details page for bank deposit
    /// Replicates legacy /SenderBankAccountDeposit/DebitCreditCardDetails
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DebitCreditCardDetails(int transactionId, bool IsAddDebitCreditCard = false, bool IsFromSavedDebitCard = false)
    {
        try
        {
            // Get transaction from database
            var transaction = await _context.Transactions
                .Include(t => t.Sender)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get bank account deposit details
            var bankDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Bank)
                .FirstOrDefaultAsync(bd => bd.TransactionId == transactionId);

            if (bankDeposit == null)
            {
                _logger.LogWarning("Bank deposit not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);

            // Build view model
            var viewModel = new CreditDebitCardViewModel
            {
                TransactionId = transactionId,
                ReceiptNo = transaction.ReceiptNo,
                FaxingAmount = transaction.TotalAmount + 0.05m, // Add card fee (legacy pattern)
                FaxingCurrency = transaction.SendingCurrency,
                FaxingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                ReceiverName = bankDeposit.ReceiverName ?? "N/A",
                CreditDebitCardFee = 0.05m, // Static fee for POC (legacy: 0.80)
                SaveCard = IsAddDebitCreditCard
            };

            // Get sender address (for POC, use default)
            var sender = await _context.Senders.FirstOrDefaultAsync(s => s.Id == transaction.SenderId);
            viewModel.AddressLineOne = sender?.Address1 ?? "123 Main Street"; // Default for POC
            
            // Get country name from Country entity
            viewModel.CountyName = sendingCountry?.CountryName ?? transaction.SendingCountryCode;

            // Set ViewBag for view
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Bank Deposit";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = bankDeposit.ReceiverName ?? "N/A";
            ViewBag.Fee = transaction.Fee;
            ViewBag.IsFromSavedDebitCard = IsFromSavedDebitCard;
            ViewBag.HasOneSavedCard = false; // TODO: Check if sender has saved cards
            ViewBag.CardErrorMessage = TempData["CardErrorMessage"] as string ?? "";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading debit credit card details page");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: DebitCreditCardDetails - Process card payment for bank deposit
    /// Replicates legacy /SenderBankAccountDeposit/DebitCreditCardDetails POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DebitCreditCardDetails(CreditDebitCardViewModel model)
    {
        try
        {
            // Get transaction from database
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == model.TransactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Transaction not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get bank account deposit details
            var bankDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Bank)
                .FirstOrDefaultAsync(bd => bd.TransactionId == model.TransactionId);

            if (bankDeposit == null)
            {
                _logger.LogWarning("Bank deposit not found for transaction: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Bank deposit details not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Set ViewBag for view (in case of errors)
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Bank Deposit";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = bankDeposit.ReceiverName ?? "N/A";
            ViewBag.Fee = transaction.Fee;
            ViewBag.IsFromSavedDebitCard = false;
            ViewBag.HasOneSavedCard = false;

            // Get country for CountyName if not provided
            if (string.IsNullOrEmpty(model.CountyName))
            {
                var sendingCountry = await _context.Countries
                    .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);
                model.CountyName = sendingCountry?.CountryName ?? transaction.SendingCountryCode;
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate card number format
            if (!string.IsNullOrEmpty(model.CardNumber))
            {
                var number = model.CardNumber.Split(' ');
                model.CardNumber = string.Join("", number);
            }

            // Basic card validation (for POC)
            if (string.IsNullOrEmpty(model.CardNumber) || model.CardNumber.Length < 13)
            {
                ModelState.AddModelError("CardNumber", "Invalid card number");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.SecurityCode) || model.SecurityCode.Length < 3)
            {
                ModelState.AddModelError("SecurityCode", "Invalid security code");
                return View(model);
            }

            // Validate expiry date
            if (!int.TryParse(model.EndMM, out var month) || month < 1 || month > 12)
            {
                ModelState.AddModelError("EndMM", "Invalid month");
                return View(model);
            }

            if (!int.TryParse(model.EndYY, out var year) || year < 0 || year > 99)
            {
                ModelState.AddModelError("EndYY", "Invalid year");
                return View(model);
            }

            // Check if card is expired
            var currentYear = DateTime.UtcNow.Year % 100;
            var currentMonth = DateTime.UtcNow.Month;
            if (year < currentYear || (year == currentYear && month < currentMonth))
            {
                ModelState.AddModelError("", "Card has expired");
                return View(model);
            }

            // For POC, simulate payment processing
            // In production, this would integrate with payment gateway (Stripe, WorldPay, etc.)
            transaction.SenderPaymentMode = PaymentMode.Card;
            transaction.Status = TransactionStatus.Paid; // For POC, mark as paid immediately
            transaction.UpdatedAt = DateTime.UtcNow;
            
            // Save card payment information
            var expiryDate = $"{model.EndMM}/{model.EndYY}";
            var cardPayment = new CardPaymentInformation
            {
                TransactionId = transaction.Id,
                NonCardTransactionId = null,
                CardTransactionId = null,
                TopUpSomeoneElseTransactionId = null,
                NameOnCard = model.NameOnCard,
                CardNumber = MaskCardNumber(model.CardNumber), // Mask card number
                ExpiryDate = expiryDate,
                IsSavedCard = model.SaveCard,
                AutoRecharged = false,
                TransferType = 4, // 4 = Bank Deposit transfer
                CreatedAt = DateTime.UtcNow
            };
            
            _context.CardPaymentInformations.Add(cardPayment);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Card payment processed successfully for bank deposit. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}",
                transaction.Id, transaction.ReceiptNo);

            // Redirect to success page (legacy pattern)
            return RedirectToAction("AddMoneyToWalletSuccess", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card payment for bank deposit");
            ModelState.AddModelError("", "An error occurred while processing your payment. Please try again.");
            return View(model);
        }
    }

    private string MaskCardNumber(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            return "****";
        
        return $"****{cardNumber.Substring(cardNumber.Length - 4)}";
    }

    /// <summary>
    /// GET: MoneyFexBankDeposit - Bank deposit instructions page for bank deposit
    /// Replicates legacy /SenderBankAccountDeposit/MoneyFexBankDeposit
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MoneyFexBankDeposit(int transactionId)
    {
        try
        {
            // Get transaction from database
            var transaction = await _context.Transactions
                .Include(t => t.Sender)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get bank account deposit details
            var bankDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Bank)
                .FirstOrDefaultAsync(bd => bd.TransactionId == transactionId);

            if (bankDeposit == null)
            {
                _logger.LogWarning("Bank deposit not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);

            // Build view model (simplified for POC)
            var viewModel = new MoneyFexBankDepositViewModel
            {
                TransactionId = transactionId,
                ReceiptNo = transaction.ReceiptNo,
                Amount = transaction.TotalAmount + 0.79m, // Add bank deposit fee (legacy pattern)
                SendingCurrencyCode = transaction.SendingCurrency,
                SendingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                AccountNumber = "12345678", // Default account for POC
                ShortCode = "MFX", // Default short code
                LabelName = "Sort Code", // Default label
                PaymentReference = transaction.ReceiptNo // Use receipt number as payment reference
            };

            // Set ViewBag for view
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Bank Deposit";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = bankDeposit.ReceiverName ?? "N/A";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading MoneyFex bank deposit page");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: MoneyFexBankDeposit - Confirm bank deposit for bank deposit
    /// Replicates legacy /SenderBankAccountDeposit/MoneyFexBankDeposit POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoneyFexBankDeposit(MoneyFexBankDepositViewModel model)
    {
        try
        {
            // Get transaction from database
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == model.TransactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Transaction not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get bank account deposit details
            var bankDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Bank)
                .FirstOrDefaultAsync(bd => bd.TransactionId == model.TransactionId);

            if (bankDeposit == null)
            {
                _logger.LogWarning("Bank deposit not found for transaction: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Bank deposit details not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Update transaction status to PaymentPending (will be updated when payment is confirmed)
            transaction.SenderPaymentMode = PaymentMode.BankAccount;
            transaction.Status = TransactionStatus.PaymentPending; // Awaiting bank deposit confirmation
            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Update bank deposit to indicate payment has been made
            bankDeposit.HasMadePaymentToBankAccount = true;
            bankDeposit.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Bank deposit confirmation submitted for bank deposit. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}",
                transaction.Id, transaction.ReceiptNo);

            // Redirect to success page
            return RedirectToAction("AddMoneyToWalletSuccess", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bank deposit confirmation");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// GET: AddMoneyToWalletSuccess - Success page after payment for bank deposit
    /// Replicates legacy /SenderBankAccountDeposit/AddMoneyToWalletSuccess
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AddMoneyToWalletSuccess(int transactionId)
    {
        try
        {
            // Get transaction from database
            var transaction = await _context.Transactions
                .Include(t => t.Sender)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get bank account deposit details
            var bankDeposit = await _context.BankAccountDeposits
                .Include(bd => bd.Bank)
                .FirstOrDefaultAsync(bd => bd.TransactionId == transactionId);

            if (bankDeposit == null)
            {
                _logger.LogWarning("Bank deposit not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);

            // Build view model (legacy pattern)
            var viewModel = new AddMoneyToWalletSuccessViewModel
            {
                Amount = transaction.SendingAmount,
                Currency = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                ReceiverName = bankDeposit.ReceiverName ?? "N/A",
                ReceiptNo = transaction.ReceiptNo,
                TransactionId = transactionId
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading success page for bank deposit");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// GET: SenderBankAccountDeposit/Details
    /// Display bank account deposit transaction details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int? transactionId, string? receiptNo)
    {
        try
        {
            BankAccountDeposit? bankDeposit = null;

            if (transactionId.HasValue)
            {
                bankDeposit = await _context.BankAccountDeposits
                    .Include(b => b.Bank)
                    .Include(b => b.Transaction)
                        .ThenInclude(t => t.Sender)
                    .Include(b => b.Transaction)
                        .ThenInclude(t => t.ReceivingCountry)
                    .FirstOrDefaultAsync(b => b.TransactionId == transactionId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(receiptNo))
            {
                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.ReceiptNo == receiptNo);
                
                if (transaction != null)
                {
                    bankDeposit = await _context.BankAccountDeposits
                        .Include(b => b.Bank)
                        .Include(b => b.Transaction)
                            .ThenInclude(t => t.Sender)
                        .Include(b => b.Transaction)
                            .ThenInclude(t => t.ReceivingCountry)
                        .FirstOrDefaultAsync(b => b.TransactionId == transaction.Id);
                }
            }

            if (bankDeposit == null)
            {
                return NotFound();
            }

            return View(bankDeposit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading bank deposit details");
            return NotFound();
        }
    }
}

