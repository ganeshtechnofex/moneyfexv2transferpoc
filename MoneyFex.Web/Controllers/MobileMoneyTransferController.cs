using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Interfaces;
using MoneyFex.Core.Messages;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.Services;
using MoneyFex.Web.ViewModels;
using System.Text.Json;

namespace MoneyFex.Web.Controllers;

/// <summary>
/// Controller for mobile money transfer functionality
/// Replicates legacy /SenderMobileMoneyTransfer/Index endpoint
/// </summary>
public class MobileMoneyTransferController : Controller
{
    private readonly ILogger<MobileMoneyTransferController> _logger;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly MoneyFexDbContext _context;
    private readonly TransactionLimitService _transactionLimitService;
    private readonly ITransferQueueProducer _transferQueueProducer;

    public MobileMoneyTransferController(
        ILogger<MobileMoneyTransferController> logger,
        IExchangeRateService exchangeRateService,
        MoneyFexDbContext context,
        TransactionLimitService transactionLimitService,
        ITransferQueueProducer transferQueueProducer)
    {
        _logger = logger;
        _exchangeRateService = exchangeRateService;
        _context = context;
        _transactionLimitService = transactionLimitService;
        _transferQueueProducer = transferQueueProducer;
    }

    /// <summary>
    /// GET: MobileMoneyTransfer/Index
    /// Replicates legacy endpoint: /SenderMobileMoneyTransfer/Index?CountryCode=CM&WalletId=3037
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? countryCode = "", int walletId = 0)
    {
        try
        {
            // Get sender ID from session (for POC, use default)
            var senderId = GetSenderIdFromSession();
            
            // Get countries for dropdown
            var countries = await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.CountryName)
                .Select(c => new SelectListItem
                {
                    Value = c.CountryCode,
                    Text = c.CountryName,
                    Selected = c.CountryCode == countryCode
                })
                .ToListAsync();

            ViewBag.Countries = countries;

            // Get recently paid mobile numbers
            var recentlyPaidNumbers = await GetRecentPaidReceiversAsync(senderId, walletId, countryCode);
            ViewBag.RecentlyPaidNumbers = recentlyPaidNumbers;

            // Initialize view model
            var viewModel = new MobileMoneyTransferViewModel();

            // Get receiving country from query parameters
            var receivingCountryFromQuery = Request.Query["ReceivingCountry"].FirstOrDefault() ?? 
                (TempData["ReceivingCountry"] as string);
            
            // If country code is provided, set it and get phone code
            if (!string.IsNullOrEmpty(countryCode))
            {
                viewModel.CountryCode = countryCode;
                viewModel.ReceivingCountry = countryCode; // Set receiving country same as country code
            }
            else if (!string.IsNullOrEmpty(receivingCountryFromQuery))
            {
                viewModel.CountryCode = receivingCountryFromQuery;
                viewModel.ReceivingCountry = receivingCountryFromQuery;
            }
            else
            {
                // Default to receiving country from session or default
                var defaultCountry = await _context.Countries
                    .FirstOrDefaultAsync(c => c.CountryCode == "NG");
                if (defaultCountry != null)
                {
                    viewModel.CountryCode = defaultCountry.CountryCode;
                    viewModel.ReceivingCountry = defaultCountry.CountryCode;
                }
            }
            
            // Get phone code for the country
            if (!string.IsNullOrEmpty(viewModel.CountryCode))
            {
                viewModel.CountryPhoneCode = GetCountryPhoneCodeHelper(viewModel.CountryCode);
            }

            // Set wallets based on country
            await SetWalletsViewBagAsync(countryCode ?? viewModel.CountryCode, walletId);

            // Get transaction summary from query parameters or TempData (set in TransferMoneyNow)
            var sendingAmount = Request.Query["SendingAmount"].FirstOrDefault() != null 
                ? decimal.Parse(Request.Query["SendingAmount"].FirstOrDefault()!) 
                : (TempData["SendingAmount"] as decimal? ?? 0);
            var receivingAmount = Request.Query["ReceivingAmount"].FirstOrDefault() != null
                ? decimal.Parse(Request.Query["ReceivingAmount"].FirstOrDefault()!)
                : (TempData["ReceivingAmount"] as decimal? ?? 0);
            var sendingCurrency = Request.Query["SendingCurrency"].FirstOrDefault() ?? 
                (TempData["SendingCurrency"] as string ?? "GBP");
            var receivingCurrency = Request.Query["ReceivingCurrency"].FirstOrDefault() ?? 
                (TempData["ReceivingCurrency"] as string ?? "NGN");
            var sendingCountry = Request.Query["SendingCountry"].FirstOrDefault() ?? 
                (TempData["SendingCountry"] as string ?? "GB");
            var receivingCountry = Request.Query["ReceivingCountry"].FirstOrDefault() ?? 
                (TempData["ReceivingCountry"] as string ?? countryCode ?? "NG");
            
            // If amounts are 0, try to get from session
            if (sendingAmount == 0)
            {
                var sessionAmount = HttpContext.Session.GetString("SendingAmount");
                if (!string.IsNullOrEmpty(sessionAmount) && decimal.TryParse(sessionAmount, out var amount))
                {
                    sendingAmount = amount;
                }
            }

            if (sendingAmount > 0)
            {
                viewModel.SendingAmount = sendingAmount;
                viewModel.ReceivingAmount = receivingAmount;
                viewModel.SendingCurrency = sendingCurrency;
                viewModel.ReceivingCurrency = receivingCurrency;
                viewModel.SendingCountry = sendingCountry;
                viewModel.ReceivingCountry = receivingCountry;

                // Calculate exchange rate and fee if not already calculated
                try
                {
                    var calculation = await _exchangeRateService.CalculateTransferSummaryAsync(
                        sendingAmount, 0,
                        sendingCurrency, receivingCurrency,
                        sendingCountry, receivingCountry,
                        TransactionType.MobileWallet,
                        false, false);
                    
                    viewModel.ExchangeRate = calculation.ExchangeRate;
                    viewModel.Fee = calculation.Fee;
                    viewModel.TotalAmount = calculation.TotalAmount;
                    viewModel.ReceivingAmount = calculation.ReceivingAmount; // Use calculated receiving amount
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not calculate exchange rate, using provided values");
                    // Use default values if calculation fails
                    viewModel.ExchangeRate = receivingAmount > 0 && sendingAmount > 0 ? receivingAmount / sendingAmount : 0;
                    viewModel.Fee = 0;
                    viewModel.TotalAmount = sendingAmount;
                }

                // Set ViewBag for summary display
                ViewBag.ReceivingCountryCurrency = receivingCurrency;
                ViewBag.TransferMethod = "Mobile Wallet";
                ViewBag.SendingCountryCurrency = sendingCurrency;
                ViewBag.SendingAmount = sendingAmount;
                ViewBag.ReceivingCountry = receivingCountry.ToLower();
            }

            // Set wallet ID if provided
            if (walletId > 0)
            {
                viewModel.WalletId = walletId;
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading mobile money transfer form");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: MobileMoneyTransfer/Index
    /// Handles form submission and creates transaction
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(MobileMoneyTransferViewModel model)
    {
        try
        {
            // Get sender ID
            var senderId = GetSenderIdFromSession();

            // Get countries and wallets for dropdowns
            var countries = await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.CountryName)
                .Select(c => new SelectListItem
                {
                    Value = c.CountryCode,
                    Text = c.CountryName,
                    Selected = c.CountryCode == model.CountryCode
                })
                .ToListAsync();

            ViewBag.Countries = countries;
            await SetWalletsViewBagAsync(model.CountryCode, model.WalletId);

            var recentlyPaidNumbers = await GetRecentPaidReceiversAsync(senderId, model.WalletId, model.CountryCode);
            ViewBag.RecentlyPaidNumbers = recentlyPaidNumbers;

            // Set country phone code
            model.CountryPhoneCode = GetCountryPhoneCodeHelper(model.CountryCode);

            // Set ViewBag for summary
            if (model.SendingAmount > 0)
            {
                ViewBag.ReceivingCountryCurrency = model.ReceivingCurrency;
                ViewBag.TransferMethod = "Mobile Wallet";
                ViewBag.SendingCountryCurrency = model.SendingCurrency;
                ViewBag.SendingAmount = model.SendingAmount;
                ViewBag.ReceivingCountry = model.ReceivingCountry.ToLower();
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate mobile number format
            if (!string.IsNullOrEmpty(model.MobileNumber))
            {
                model.MobileNumber = model.MobileNumber.TrimStart('0');
                var countryMobileCode = GetCountryPhoneCodeHelper(model.CountryCode);
                if (model.MobileNumber.StartsWith(countryMobileCode))
                {
                    model.MobileNumber = model.MobileNumber.Substring(countryMobileCode.Length);
                }
            }

            // Validate receiver name (should be full name)
            if (!string.IsNullOrEmpty(model.ReceiverName))
            {
                var nameParts = model.ReceiverName.Trim().Split(' ');
                if (nameParts.Length < 2)
                {
                    ModelState.AddModelError("ReceiverName", "Enter recipient full name");
                    return View(model);
                }
            }

            // Special validation for Kenya (KE)
            if (model.CountryCode == "KE")
            {
                if (string.IsNullOrEmpty(model.ReceiverStreet))
                {
                    ModelState.AddModelError("ReceiverStreet", "Enter street address");
                    return View(model);
                }
                if (!model.IdentityCardId.HasValue || model.IdentityCardId.Value == 0)
                {
                    ModelState.AddModelError("IdentityCardId", "Select ID Card type");
                    return View(model);
                }
                if (string.IsNullOrEmpty(model.IdentityCardNumber))
                {
                    ModelState.AddModelError("IdentityCardNumber", "Enter ID Card number");
                    return View(model);
                }
            }

            // Get transaction summary - prioritize model values, fallback to query/TempData
            var sendingAmount = model.SendingAmount > 0 ? model.SendingAmount : 
                (Request.Query["SendingAmount"].FirstOrDefault() != null 
                    ? decimal.Parse(Request.Query["SendingAmount"].FirstOrDefault()!) 
                    : (TempData["SendingAmount"] as decimal? ?? 0));
            var receivingAmount = model.ReceivingAmount > 0 ? model.ReceivingAmount :
                (Request.Query["ReceivingAmount"].FirstOrDefault() != null
                    ? decimal.Parse(Request.Query["ReceivingAmount"].FirstOrDefault()!)
                    : (TempData["ReceivingAmount"] as decimal? ?? 0));
            var sendingCurrency = !string.IsNullOrEmpty(model.SendingCurrency) ? model.SendingCurrency :
                (Request.Query["SendingCurrency"].FirstOrDefault() ?? 
                (TempData["SendingCurrency"] as string ?? "GBP"));
            var receivingCurrency = !string.IsNullOrEmpty(model.ReceivingCurrency) ? model.ReceivingCurrency :
                (Request.Query["ReceivingCurrency"].FirstOrDefault() ?? 
                (TempData["ReceivingCurrency"] as string ?? "NGN"));
            var sendingCountry = !string.IsNullOrEmpty(model.SendingCountry) ? model.SendingCountry :
                (Request.Query["SendingCountry"].FirstOrDefault() ?? 
                (TempData["SendingCountry"] as string ?? "GB"));
            var receivingCountry = !string.IsNullOrEmpty(model.ReceivingCountry) ? model.ReceivingCountry :
                (Request.Query["ReceivingCountry"].FirstOrDefault() ?? 
                (TempData["ReceivingCountry"] as string ?? model.CountryCode ?? "NG"));
            
            // Ensure CountryCode is set from receiving country if not set
            if (string.IsNullOrEmpty(model.CountryCode))
            {
                model.CountryCode = receivingCountry;
            }

            // Recalculate if needed
            if (sendingAmount > 0)
            {
                var calculation = await _exchangeRateService.CalculateTransferSummaryAsync(
                    sendingAmount, 0,
                    sendingCurrency, receivingCurrency,
                    sendingCountry, receivingCountry,
                    TransactionType.MobileWallet,
                    false, false);

                sendingAmount = calculation.SendingAmount;
                receivingAmount = calculation.ReceivingAmount;
                var fee = calculation.Fee;
                var exchangeRate = calculation.ExchangeRate;
                var totalAmount = calculation.TotalAmount;

                // Generate receipt number
                var receiptNo = await GenerateUniqueReceiptNumberAsync();

                // Get or create sender
                var sender = await GetOrCreateSenderAsync(senderId, sendingCountry);

                // Create transaction
                var transaction = new Transaction
                {
                    ReceiptNo = receiptNo,
                    SenderId = sender.Id,
                    SendingCountryCode = sendingCountry,
                    ReceivingCountryCode = receivingCountry,
                    SendingCurrency = sendingCurrency,
                    ReceivingCurrency = receivingCurrency,
                    SendingAmount = sendingAmount,
                    ReceivingAmount = receivingAmount,
                    Fee = fee,
                    TotalAmount = totalAmount,
                    ExchangeRate = exchangeRate,
                    TransactionDate = DateTime.UtcNow,
                    Status = TransactionStatus.PaymentPending,
                    TransactionModule = TransactionModule.Sender,
                    SenderPaymentMode = PaymentMode.Card, // Will be updated after payment
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Get wallet operator
                var walletOperator = await _context.MobileWalletOperators
                    .FirstOrDefaultAsync(w => w.Id == model.WalletId);

                // Create mobile money transfer
                var mobileTransfer = new MobileMoneyTransfer
                {
                    TransactionId = transaction.Id,
                    WalletOperatorId = model.WalletId,
                    PaidToMobileNo = model.MobileNumber,
                    ReceiverName = model.ReceiverName,
                    ReceiverCity = model.ReceiverStreet,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MobileMoneyTransfers.Add(mobileTransfer);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Mobile money transfer transaction created. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}, WalletId: {WalletId}",
                    transaction.Id, receiptNo, model.WalletId);

                // Store transaction ID in session for summary page
                HttpContext.Session.SetInt32("MobileTransferTransactionId", transaction.Id);
                HttpContext.Session.SetString("MobileTransferReceiptNo", receiptNo);

                // Redirect to summary page (similar to legacy MobileSummaryAbroad)
                return RedirectToAction("MobileSummaryAbroad", new { transactionId = transaction.Id });
            }
            else
            {
                ModelState.AddModelError("", "Invalid transaction amount. Please start from the beginning.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mobile money transfer");
            ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
            
            // Reload dropdowns
            var countries = await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.CountryName)
                .Select(c => new SelectListItem
                {
                    Value = c.CountryCode,
                    Text = c.CountryName
                })
                .ToListAsync();
            ViewBag.Countries = countries;
            await SetWalletsViewBagAsync(model.CountryCode, model.WalletId);
            
            return View(model);
        }
    }

    /// <summary>
    /// GET: Get country phone code
    /// </summary>
    [HttpGet]
    public IActionResult GetCountryPhoneCode(string countryCode)
    {
        if (!string.IsNullOrEmpty(countryCode))
        {
            var phoneCode = GetCountryPhoneCodeHelper(countryCode);
            return Json(new { countryPhoneCode = phoneCode });
        }
        return Json(new { countryPhoneCode = "" });
    }

    /// <summary>
    /// GET: Get recently paid mobile number info
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRecentlyPaidNumberInfo(string mobileNumber)
    {
        try
        {
            var senderId = GetSenderIdFromSession();
            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.Transaction)
                .Where(m => m.PaidToMobileNo == mobileNumber && m.Transaction.SenderId == senderId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            if (mobileTransfer != null)
            {
                return Json(new
                {
                    mobileNumber = mobileTransfer.PaidToMobileNo,
                    receiverName = mobileTransfer.ReceiverName,
                    countryCode = mobileTransfer.Transaction.ReceivingCountryCode,
                    walletId = mobileTransfer.WalletOperatorId
                });
            }

            return Json(new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently paid number info");
            return Json(new { });
        }
    }

    /// <summary>
    /// GET: Check if FlutterWave API is used (requires email)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckifFlutterWaveApi(string countryCode = "", int walletId = 0)
    {
        // For POC, return false. In production, check API service configuration
        return Json(false);
    }

    /// <summary>
    /// GET: MobileSummaryAbroad - Summary page before payment
    /// Replicates legacy /SenderMobileMoneyTransfer/MobileSummaryAbroad
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MobileSummaryAbroad(int transactionId)
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

            // Get mobile money transfer details
            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.WalletOperator)
                .FirstOrDefaultAsync(m => m.TransactionId == transactionId);

            if (mobileTransfer == null)
            {
                _logger.LogWarning("Mobile money transfer not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);
            var receivingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.ReceivingCountryCode);

            // Build view model
            var viewModel = new MobileTransferSummaryViewModel
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
                ReceiverName = mobileTransfer.ReceiverName ?? "N/A",
                MobileNumber = mobileTransfer.PaidToMobileNo ?? "N/A",
                WalletName = mobileTransfer.WalletOperator?.Name ?? "N/A"
            };

            // Set ViewBag for view
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Mobile Wallet";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = mobileTransfer.ReceiverName ?? "N/A";
            
            // Extract first name from receiver name
            var fullName = mobileTransfer.ReceiverName ?? "";
            var firstName = "";
            if (!string.IsNullOrEmpty(fullName))
            {
                var names = fullName.Split(' ');
                firstName = names.Length > 0 ? names[0] : fullName;
            }
            ViewBag.ReceiverFirstName = firstName;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading mobile money transfer summary");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: MobileSummaryAbroad - Proceed to payment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MobileSummaryAbroad(MobileTransferSummaryViewModel model)
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

            // Get mobile money transfer details
            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.WalletOperator)
                .FirstOrDefaultAsync(m => m.TransactionId == model.TransactionId);

            if (mobileTransfer == null)
            {
                _logger.LogWarning("Mobile money transfer not found for transaction: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Mobile transfer details not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);

            // Redirect to InternationalPayment page (payment method selection)
            return RedirectToAction("InternationalPayment", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mobile money transfer summary");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// GET: InternationalPayment - Payment method selection page
    /// Replicates legacy /SenderMobileMoneyTransfer/InternationalPayment
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> InternationalPayment(int transactionId)
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

            // Get mobile money transfer details
            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.WalletOperator)
                .FirstOrDefaultAsync(m => m.TransactionId == transactionId);

            if (mobileTransfer == null)
            {
                _logger.LogWarning("Mobile money transfer not found for transaction: {TransactionId}", transactionId);
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
                HasEnableMoneyFexBankAccount = false, // TODO: Check if MoneyFex bank account is enabled
                CardDetails = new List<SavedCardViewModel>() // TODO: Load saved cards if any
            };

            // Set ViewBag for view
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Mobile Wallet";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = mobileTransfer.ReceiverName ?? "N/A";
            ViewBag.Fee = transaction.Fee;
            ViewBag.CreditDebitFee = 0.05m; // Static fee for POC
            ViewBag.ManualBankDepositFee = 0.79m; // Static fee for POC
            ViewBag.ErrorMessage = TempData["ErrorMessage"] as string ?? "";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading international payment page");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: InternationalPayment - Process payment method selection
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InternationalPayment(PaymentMethodViewModel model)
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

            // Get mobile money transfer details
            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.WalletOperator)
                .FirstOrDefaultAsync(m => m.TransactionId == model.TransactionId);

            if (mobileTransfer == null)
            {
                _logger.LogWarning("Mobile money transfer not found for transaction: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Mobile transfer details not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);
            var receivingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.ReceivingCountryCode);

            // Set ViewBag for view (in case of errors)
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Mobile Wallet";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = mobileTransfer.ReceiverName ?? "N/A";
            ViewBag.Fee = transaction.Fee;
            ViewBag.CreditDebitFee = 0.05m;
            ViewBag.ManualBankDepositFee = 0.79m;

            // Handle saved card selection
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

            // Get recipient ID for limit validation
            var recipientId = await GetRecipientIdAsync(mobileTransfer.WalletOperatorId, mobileTransfer.PaidToMobileNo);

            // Validate transaction limits
            var hasExceededReceiverLimit = await _transactionLimitService.HasExceededReceiverLimitAsync(
                transaction.SenderId,
                recipientId,
                transaction.SendingCountryCode,
                transaction.ReceivingCountryCode,
                TransactionType.MobileWallet);

            if (hasExceededReceiverLimit)
            {
                ModelState.AddModelError("TransactionError", "Recipient daily transaction limit exceeded");
                return View(model);
            }

            var hasExceededSenderLimit = await _transactionLimitService.HasExceededSenderTransactionLimitAsync(
                transaction.SenderId,
                transaction.SendingCountryCode,
                transaction.ReceivingCountryCode,
                TransactionType.MobileWallet);

            if (hasExceededSenderLimit)
            {
                ModelState.AddModelError("TransactionError", "Sender daily transaction limit exceeded");
                return View(model);
            }

            // Handle payment method selection
            switch (model.SenderPaymentMode)
            {
                case PaymentMode.Card:
                    // If saved card was selected, redirect to DebitCreditCardDetails with saved card flag
                    if (selectedCardId.HasValue && selectedCardId.Value > 0)
                    {
                        // Store card details in session/TempData for DebitCreditCardDetails
                        HttpContext.Session.SetInt32("SelectedCardId", selectedCardId.Value);
                        if (!string.IsNullOrEmpty(creditCardSecurityCode))
                        {
                            HttpContext.Session.SetString("CardSecurityCode", creditCardSecurityCode);
                        }
                        return RedirectToAction("DebitCreditCardDetails", new { IsFromSavedDebitCard = true, transactionId = transaction.Id });
                    }
                    // New card - redirect to DebitCreditCardDetails
                    return RedirectToAction("DebitCreditCardDetails", new { transactionId = transaction.Id });

                case PaymentMode.BankAccount:
                    // MoneyFex Bank Account
                    return RedirectToAction("MoneyFexBankDeposit", new { transactionId = transaction.Id });

                case PaymentMode.MobileWallet:
                    // KiiPay Wallet - complete transaction directly
                    // TODO: Implement wallet balance check and deduction
                    transaction.SenderPaymentMode = PaymentMode.MobileWallet;
                    transaction.Status = TransactionStatus.Paid; // For POC, mark as paid
                    await _context.SaveChangesAsync();
                    
                    // Queue transfer for background processing AFTER payment is confirmed
                    await QueueTransferForProcessingAsync(transaction, mobileTransfer);
                    
                    return RedirectToAction("AddMoneyToWalletSuccess", new { transactionId = transaction.Id });

                default:
                    ModelState.AddModelError("", "Please select a valid payment method.");
                    return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment method selection");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Queue transfer for background processing after payment is confirmed
    /// This should only be called when transaction status is Paid
    /// </summary>
    private async Task QueueTransferForProcessingAsync(Transaction transaction, MobileMoneyTransfer mobileTransfer)
    {
        try
        {
            // Only queue if payment is confirmed (status is Paid)
            if (transaction.Status != TransactionStatus.Paid)
            {
                _logger.LogWarning(
                    "Attempted to queue transfer before payment confirmation. TransactionId: {TransactionId}, Status: {Status}",
                    transaction.Id, transaction.Status);
                return;
            }

            // Check if transfer is already queued or processed
            if (transaction.Status == TransactionStatus.InProgress || 
                transaction.Status == TransactionStatus.Completed)
            {
                _logger.LogInformation(
                    "Transfer already queued or processed. TransactionId: {TransactionId}, Status: {Status}",
                    transaction.Id, transaction.Status);
                return;
            }

            var queueMessage = new TransferQueueMessage
            {
                TransactionId = transaction.Id,
                ReceiptNo = transaction.ReceiptNo,
                TransferType = TransferType.MobileMoneyTransfer,
                Payload = JsonSerializer.Serialize(new
                {
                    WalletId = mobileTransfer.WalletOperatorId,
                    MobileNumber = mobileTransfer.PaidToMobileNo,
                    ReceiverName = mobileTransfer.ReceiverName,
                    ReceiverCity = mobileTransfer.ReceiverCity
                }),
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            await _transferQueueProducer.EnqueueTransferAsync(queueMessage);

            // Update transaction status to InProgress
            transaction.Status = TransactionStatus.InProgress;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Transfer queued for processing after payment confirmation. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}",
                transaction.Id, transaction.ReceiptNo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to queue transfer after payment. TransactionId: {TransactionId}. Transfer will need to be processed manually.",
                transaction.Id);
            // Don't throw - payment is already confirmed, just logging failed
        }
    }

    private int GetSenderIdFromSession()
    {
        // For POC, get from session or use default
        // In production, get from authenticated user
        var senderIdStr = HttpContext.Session.GetString("SenderId");
        if (int.TryParse(senderIdStr, out var senderId))
        {
            return senderId;
        }

        // Return default sender ID for POC
        return 1;
    }

    private async Task<List<SelectListItem>> GetRecentPaidReceiversAsync(int senderId, int walletId, string? countryCode)
    {
        try
        {
            var query = _context.MobileMoneyTransfers
                .Include(m => m.Transaction)
                .Where(m => m.Transaction.SenderId == senderId);

            if (!string.IsNullOrEmpty(countryCode))
            {
                query = query.Where(m => m.Transaction.ReceivingCountryCode == countryCode);
            }

            if (walletId > 0)
            {
                query = query.Where(m => m.WalletOperatorId == walletId);
            }

            var recentTransfers = await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .Select(m => new
                {
                    Code = m.PaidToMobileNo,
                    Name = $"{m.ReceiverName} - {m.PaidToMobileNo}"
                })
                .Distinct()
                .ToListAsync();

            return recentTransfers
                .Select(r => new SelectListItem
                {
                    Value = r.Code,
                    Text = r.Name
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent paid receivers");
            return new List<SelectListItem>();
        }
    }

    private async Task SetWalletsViewBagAsync(string? countryCode, int selectedWalletId = 0)
    {
        try
        {
            if (string.IsNullOrEmpty(countryCode))
            {
                ViewBag.Wallets = new List<SelectListItem>();
                return;
            }

            var wallets = await _context.MobileWalletOperators
                .Where(w => w.CountryCode == countryCode && w.IsActive)
                .OrderBy(w => w.Name)
                .Select(w => new SelectListItem
                {
                    Value = w.Id.ToString(),
                    Text = w.Name,
                    Selected = w.Id == selectedWalletId
                })
                .ToListAsync();

            ViewBag.Wallets = wallets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting wallets ViewBag");
            ViewBag.Wallets = new List<SelectListItem>();
        }
    }

    private string GetCountryPhoneCodeHelper(string countryCode)
    {
        // Common phone codes mapping (can be moved to database)
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
            _ => "+234" // Default to Nigeria
        };
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
    /// Get recipient ID for a mobile transfer (for limit validation)
    /// </summary>
    private async Task<int?> GetRecipientIdAsync(int walletId, string mobileNumber)
    {
        try
        {
            // For POC, return null (recipient ID not critical for limit validation)
            // In production, this would look up or create a recipient record
            // and return the recipient ID
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipient ID");
            return null;
        }
    }

    /// <summary>
    /// GET: DebitCreditCardDetails - Card payment details page
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

            // Get mobile money transfer details
            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.WalletOperator)
                .FirstOrDefaultAsync(m => m.TransactionId == transactionId);

            if (mobileTransfer == null)
            {
                _logger.LogWarning("Mobile money transfer not found for transaction: {TransactionId}", transactionId);
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
                FaxingAmount = transaction.TotalAmount + 0.05m, // Add card fee
                FaxingCurrency = transaction.SendingCurrency,
                FaxingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                ReceiverName = mobileTransfer.ReceiverName ?? "N/A",
                CreditDebitCardFee = 0.05m,
                SaveCard = IsAddDebitCreditCard
            };

            // Get sender address (for POC, use default)
            var sender = await _context.Senders.FirstOrDefaultAsync(s => s.Id == transaction.SenderId);
            viewModel.AddressLineOne = sender?.Address1 ?? "123 Main Street"; // Default for POC
            
            // Get country name from Country entity
            viewModel.CountyName = sendingCountry?.CountryName ?? transaction.SendingCountryCode;

            // Set ViewBag for view
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Mobile Wallet";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = mobileTransfer.ReceiverName ?? "N/A";
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
    /// POST: DebitCreditCardDetails - Process card payment
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

            // Get mobile money transfer details
            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.WalletOperator)
                .FirstOrDefaultAsync(m => m.TransactionId == model.TransactionId);

            if (mobileTransfer == null)
            {
                _logger.LogWarning("Mobile money transfer not found for transaction: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Mobile transfer details not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Set ViewBag for view (in case of errors)
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Mobile Wallet";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = mobileTransfer.ReceiverName ?? "N/A";
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
                NonCardTransactionId = null, // Not applicable for mobile money transfer
                CardTransactionId = null, // Not applicable for mobile money transfer
                TopUpSomeoneElseTransactionId = null, // Not applicable
                NameOnCard = model.NameOnCard,
                CardNumber = MaskCardNumber(model.CardNumber), // Mask card number
                ExpiryDate = expiryDate,
                IsSavedCard = model.SaveCard,
                AutoRecharged = false,
                TransferType = 3, // 3 = Mobile/KiiBank transfer
                CreatedAt = DateTime.UtcNow
            };
            
            _context.CardPaymentInformations.Add(cardPayment);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Card payment processed successfully. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}",
                transaction.Id, transaction.ReceiptNo);

            // Queue transfer for background processing AFTER payment is confirmed
            await QueueTransferForProcessingAsync(transaction, mobileTransfer);

            // Redirect to success page
            return RedirectToAction("AddMoneyToWalletSuccess", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card payment");
            ModelState.AddModelError("", "An error occurred while processing your payment. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// GET: MoneyFexBankDeposit - Bank deposit instructions page
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

            // Get mobile money transfer details
            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.WalletOperator)
                .FirstOrDefaultAsync(m => m.TransactionId == transactionId);

            if (mobileTransfer == null)
            {
                _logger.LogWarning("Mobile money transfer not found for transaction: {TransactionId}", transactionId);
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
                Amount = transaction.TotalAmount,
                SendingCurrencyCode = transaction.SendingCurrency,
                SendingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                AccountNumber = "12345678", // Default account for POC
                ShortCode = "MFX", // Default short code
                LabelName = "Sort Code", // Default label
                PaymentReference = transaction.ReceiptNo // Use receipt number as payment reference
            };

            // Set ViewBag for view
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Mobile Wallet";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = mobileTransfer.ReceiverName ?? "N/A";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading MoneyFex bank deposit page");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: MoneyFexBankDeposit - Confirm bank deposit
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

            // Update transaction status to PaymentPending (will be updated when payment is confirmed)
            transaction.SenderPaymentMode = PaymentMode.BankAccount;
            transaction.Status = TransactionStatus.PaymentPending; // Awaiting bank deposit confirmation
            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Bank deposit confirmation submitted. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}",
                transaction.Id, transaction.ReceiptNo);

            // Note: Transfer will be queued when bank deposit is actually confirmed
            // (via webhook or manual confirmation process that updates status to Paid)

            // Redirect to success page (or pending confirmation page)
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
    /// GET: AddMoneyToWalletSuccess - Success page after payment
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

            // Get mobile money transfer details
            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.WalletOperator)
                .FirstOrDefaultAsync(m => m.TransactionId == transactionId);

            if (mobileTransfer == null)
            {
                _logger.LogWarning("Mobile money transfer not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);

            // Build view model
            var viewModel = new AddMoneyToWalletSuccessViewModel
            {
                Amount = transaction.SendingAmount,
                Currency = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                ReceiverName = mobileTransfer.ReceiverName ?? "N/A",
                ReceiptNo = transaction.ReceiptNo,
                TransactionId = transactionId
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading success page");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    private async Task<Sender> GetOrCreateSenderAsync(int senderId, string countryCode)
    {
        var sender = await _context.Senders.FirstOrDefaultAsync(s => s.Id == senderId);
        if (sender != null)
        {
            return sender;
        }

        // For POC, create default sender
        var country = await _context.Countries.FirstOrDefaultAsync(c => c.CountryCode == countryCode);
        sender = new Sender
        {
            FirstName = "Demo",
            LastName = "User",
            Email = "demo@moneyfex.com",
            CountryCode = countryCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Senders.Add(sender);
        await _context.SaveChangesAsync();
        return sender;
    }

    private string MaskCardNumber(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            return "****";
        
        return $"****{cardNumber.Substring(cardNumber.Length - 4)}";
    }

    private string DetectCardIssuer(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber))
            return "Unknown";
        
        var firstDigit = cardNumber[0];
        return firstDigit switch
        {
            '4' => "Visa",
            '5' => "Mastercard",
            '3' => "American Express",
            '6' => "Discover",
            _ => "Unknown"
        };
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

        return $"MFX{DateTime.UtcNow:yyyyMMddHHmmss}{random.Next(1000, 9999)}";
    }

    /// <summary>
    /// GET: MobileMoneyTransfer/Details
    /// Display mobile money transfer transaction details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int? transactionId, string? receiptNo)
    {
        try
        {
            MobileMoneyTransfer? mobileTransfer = null;

            if (transactionId.HasValue)
            {
                mobileTransfer = await _context.MobileMoneyTransfers
                    .Include(m => m.WalletOperator)
                    .Include(m => m.Transaction)
                        .ThenInclude(t => t.Sender)
                    .Include(m => m.Transaction)
                        .ThenInclude(t => t.ReceivingCountry)
                    .FirstOrDefaultAsync(m => m.TransactionId == transactionId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(receiptNo))
            {
                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.ReceiptNo == receiptNo);
                
                if (transaction != null)
                {
                    mobileTransfer = await _context.MobileMoneyTransfers
                        .Include(m => m.WalletOperator)
                        .Include(m => m.Transaction)
                            .ThenInclude(t => t.Sender)
                        .Include(m => m.Transaction)
                            .ThenInclude(t => t.ReceivingCountry)
                        .FirstOrDefaultAsync(m => m.TransactionId == transaction.Id);
                }
            }

            if (mobileTransfer == null)
            {
                return NotFound();
            }

            return View(mobileTransfer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading mobile money transfer details");
            return NotFound();
        }
    }

    #endregion
}

