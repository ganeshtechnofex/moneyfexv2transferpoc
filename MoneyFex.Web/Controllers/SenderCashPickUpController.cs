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
/// Controller for sender cash pickup functionality
/// Replicates legacy /SenderCashPickUp/Index endpoint
/// </summary>
public class SenderCashPickUpController(
    ILogger<SenderCashPickUpController> logger,
    IExchangeRateService exchangeRateService,
    MoneyFexDbContext context,
    TransactionLimitService transactionLimitService) : Controller
{
    private readonly ILogger<SenderCashPickUpController> _logger = logger;
    private readonly IExchangeRateService _exchangeRateService = exchangeRateService;
    private readonly MoneyFexDbContext _context = context;
    private readonly TransactionLimitService _transactionLimitService = transactionLimitService;

    /// <summary>
    /// GET: SenderCashPickUp/Index
    /// Replicates legacy endpoint: /SenderCashPickUp/Index
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            // Get sender ID from session
            var senderId = GetSenderIdFromSession();

            // Get transaction summary from query parameters
            var sendingAmount = GetQueryParamAsDecimal("SendingAmount") ??
                GetQueryParamAsDecimal("amount") ?? 3m;
            var receivingAmount = GetQueryParamAsDecimal("ReceivingAmount") ?? 0m;
            var sendingCurrency = Request.Query["SendingCurrency"].FirstOrDefault() ?? "GBP";
            var receivingCurrency = Request.Query["ReceivingCurrency"].FirstOrDefault() ?? "NGN";
            var sendingCountry = Request.Query["SendingCountry"].FirstOrDefault() ?? "GB";
            var receivingCountry = Request.Query["ReceivingCountry"].FirstOrDefault() ?? "NG";

            // Validate receiving country code
            if (string.IsNullOrWhiteSpace(receivingCountry))
            {
                receivingCountry = "NG";
            }

            // Get country code (default to receiving country if not provided)
            var countryCode = Request.Query["CountryCode"].FirstOrDefault() ?? receivingCountry;
            
            // Ensure countryCode is never null or empty
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                countryCode = receivingCountry;
            }

            // Initialize view model
            var viewModel = new SenderCashPickUpViewModel
            {
                CountryCode = countryCode ?? string.Empty,
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
            ViewBag.TransferMethod = "Cash PickUp";
            ViewBag.SendingCountryCurrency = sendingCurrency;
            ViewBag.SendingAmount = sendingAmount;
            ViewBag.ReceivingCountry = receivingCountry.ToLower();
            ViewBag.CountryPhoneCode = viewModel.CountryPhoneCode;

            // Set ViewBags for dropdowns (handle errors gracefully)
            try
            {
                await SetCountriesViewBagAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting countries ViewBag");
                ViewBag.Countries = new List<SelectListItem>();
            }

            try
            {
                await SetRecentReceiversViewBagAsync(senderId, receivingCountry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting recent receivers ViewBag");
                ViewBag.RecentReceivers = new List<SelectListItem>();
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

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cash pickup form");
            TempData["Error"] = "An error occurred while loading the form. Please try again.";
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: SenderCashPickUp/Index
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SenderCashPickUpViewModel model)
    {
        try
        {
            var senderId = GetSenderIdFromSession();
            model.SenderId = senderId;

            // Ensure CountryCode is never null or empty
            if (string.IsNullOrWhiteSpace(model.CountryCode))
            {
                // Get from query string or default to receiving country
                var countryCode = Request.Query["CountryCode"].FirstOrDefault() 
                    ?? Request.Query["ReceivingCountry"].FirstOrDefault() 
                    ?? "NG";
                model.CountryCode = countryCode;
            }

            // Get country code for query (ensured to be non-null)
            var countryCodeForQuery = model.CountryCode ?? "NG";

            // Get sender with country information
            var sender = await _context.Senders
                .Include(s => s.Country)
                .FirstOrDefaultAsync(s => s.Id == senderId);

            if (sender != null)
            {
                model.SendingCountry = sender.CountryCode ?? "GB";
            }

            // Set ViewBags for dropdowns
            await SetCountriesViewBagAsync();
            await SetRecentReceiversViewBagAsync(senderId, countryCodeForQuery);
            SetIdCardTypesViewBag();

            // Get receiving country currency
            var receivingCountryData = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == countryCodeForQuery);
            if (receivingCountryData != null)
            {
                model.ReceivingCurrency = receivingCountryData.Currency;
            }

            // Set ViewBag for summary display
            ViewBag.ReceivingCountryCurrency = model.ReceivingCurrency;
            ViewBag.TransferMethod = "Cash PickUp";
            ViewBag.SendingCountryCurrency = model.SendingCurrency;
            ViewBag.SendingAmount = model.SendingAmount;
            ViewBag.ReceivingCountry = countryCodeForQuery.ToLower();
            ViewBag.CountryPhoneCode = GetCountryPhoneCodeHelper(countryCodeForQuery);

            // Validate model
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Additional validations
            if (!string.IsNullOrEmpty(model.MobileNumber))
            {
                model.MobileNumber = model.MobileNumber.TrimStart('0');
                var countryMobileCode = GetCountryPhoneCodeHelper(model.CountryCode ?? countryCodeForQuery);
                if (!string.IsNullOrEmpty(countryMobileCode) && 
                    model.MobileNumber.StartsWith(countryMobileCode))
                {
                    model.MobileNumber = model.MobileNumber.Substring(countryMobileCode.Length);
                }
            }

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError("FullName", "Enter recipient full name");
                return View(model);
            }

            if (model.Reason == null || model.Reason == ReasonForTransfer.Non)
            {
                ModelState.AddModelError("Reason", "Select Reason for Transfer");
                return View(model);
            }

            // Special validation for Morocco (MA)
            if (model.CountryCode == "MA")
            {
                if (model.IdenityCardId <= 0)
                {
                    ModelState.AddModelError("IdenityCardId", "Select Id card type");
                    return View(model);
                }
                if (string.IsNullOrWhiteSpace(model.IdentityCardNumber))
                {
                    ModelState.AddModelError("IdentityCardNumber", "Enter card number");
                    return View(model);
                }
            }

            // Get recipient ID for limit validation
            var recipientId = await GetRecipientIdAsync(model.MobileNumber, model.CountryCode);

            // Validate transaction limits
            var hasExceededReceiverLimit = await _transactionLimitService.HasExceededReceiverLimitAsync(
                senderId,
                recipientId,
                model.SendingCountry,
                model.CountryCode,
                TransactionType.CashPickup);

            if (hasExceededReceiverLimit)
            {
                ModelState.AddModelError("", "Transaction for Recipient limit exceeded");
                return View(model);
            }

            var hasExceededSenderLimit = await _transactionLimitService.HasExceededSenderTransactionLimitAsync(
                senderId,
                model.SendingCountry,
                model.CountryCode,
                TransactionType.CashPickup);

            if (hasExceededSenderLimit)
            {
                ModelState.AddModelError("", "Sender daily transaction limit exceeded");
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
                    TransactionType.CashPickup,
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
            var senderCountry = model.SendingCountry;
            if (sender != null && string.IsNullOrEmpty(senderCountry))
            {
                senderCountry = sender.CountryCode ?? "GB";
            }

            // Create transaction
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
                ReasonForTransfer = model.Reason,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Get or create receiver detail
            ReceiverDetail? receiverDetail = null;
            Recipient? recipient = null;

            if (model.RecentReceiverId.HasValue && model.RecentReceiverId.Value > 0)
            {
                // Use existing recipient
                recipient = await _context.Recipients
                    .FirstOrDefaultAsync(r => r.Id == model.RecentReceiverId.Value);
            }

            if (recipient == null)
            {
                // Create new receiver detail for non-card receiver
                receiverDetail = new ReceiverDetail
                {
                    FullName = model.FullName ?? string.Empty,
                    PhoneNumber = model.MobileNumber,
                    CountryCode = model.CountryCode,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ReceiverDetails.Add(receiverDetail);
                await _context.SaveChangesAsync();
            }

            // Create cash pickup
            var cashPickup = new CashPickup
            {
                TransactionId = transaction.Id,
                MFCN = await GenerateUniqueMFCNAsync(),
                RecipientId = recipient?.Id,
                NonCardReceiverId = receiverDetail?.Id,
                RecipientIdentityCardId = model.IdenityCardId > 0 ? model.IdenityCardId : null,
                RecipientIdentityCardNumber = model.IdentityCardNumber,
                IsApprovedByAdmin = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CashPickups.Add(cashPickup);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cash pickup transaction created. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}, MFCN: {MFCN}",
                transaction.Id, transaction.ReceiptNo, cashPickup.MFCN);

            // Redirect to summary page (similar to bank deposit flow)
            return RedirectToAction("CashPickUpSummary", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cash pickup form");
            ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// GET: CashPickUpSummary - Transaction summary before payment
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CashPickUpSummary(int transactionId)
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

            // Get cash pickup details
            var cashPickup = await _context.CashPickups
                .Include(cp => cp.Recipient)
                .Include(cp => cp.NonCardReceiver)
                .FirstOrDefaultAsync(cp => cp.TransactionId == transactionId);

            if (cashPickup == null)
            {
                _logger.LogWarning("Cash pickup not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);
            var receivingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.ReceivingCountryCode);

            // Get receiver name
            var receiverName = cashPickup.Recipient?.ReceiverName ??
                cashPickup.NonCardReceiver?.FullName ?? "N/A";

            // Build summary view model (similar to bank deposit summary)
            var viewModel = new BankDepositSummaryViewModel
            {
                TransactionId = transactionId,
                ReceiptNo = transaction.ReceiptNo,
                SendingAmount = transaction.SendingAmount,
                ReceivingAmount = transaction.ReceivingAmount,
                Fee = transaction.Fee,
                TotalAmount = transaction.TotalAmount,
                ExchangeRate = transaction.ExchangeRate,
                SendingCurrency = transaction.SendingCurrency,
                ReceivingCurrency = transaction.ReceivingCurrency,
                SendingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                ReceivingCurrencySymbol = receivingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.ReceivingCurrency),
                ReceiverName = receiverName,
                SendingCountry = transaction.SendingCountryCode,
                ReceivingCountry = transaction.ReceivingCountryCode
            };

            // Set ViewBag for view
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Cash PickUp";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = receiverName;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cash pickup summary");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: CashPickUpSummary - Proceed to payment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CashPickUpSummary(BankDepositSummaryViewModel model)
    {
        try
        {
            // Get transaction
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == model.TransactionId);

            if (transaction == null)
            {
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Redirect to payment page (similar to bank deposit flow)
            return RedirectToAction("InternationalPayNow", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cash pickup summary");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// GET: InternationalPayNow - Payment method selection page for cash pickup
    /// Replicates legacy /SenderCashPickUp/InternationalPayNow
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

            // Get cash pickup details
            var cashPickup = await _context.CashPickups
                .Include(cp => cp.Recipient)
                .Include(cp => cp.NonCardReceiver)
                .FirstOrDefaultAsync(cp => cp.TransactionId == transactionId);

            if (cashPickup == null)
            {
                _logger.LogWarning("Cash pickup not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);
            var receivingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.ReceivingCountryCode);

            // Get receiver name
            var receiverName = cashPickup.Recipient?.ReceiverName ??
                cashPickup.NonCardReceiver?.FullName ?? "N/A";

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
            ViewBag.TransferMethod = "Cash PickUp";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = receiverName;
            ViewBag.Fee = transaction.Fee;
            ViewBag.CreditDebitFee = 0.05m; // Static fee for POC
            ViewBag.ManualBankDepositFee = 0.79m; // Static fee for POC
            ViewBag.ErrorMessage = TempData["ErrorMessage"] as string ?? "";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading InternationalPayNow page for cash pickup");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: InternationalPayNow - Process payment method selection for cash pickup
    /// Replicates legacy /SenderCashPickUp/InternationalPayNow POST
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

            // Get cash pickup details
            var cashPickup = await _context.CashPickups
                .Include(cp => cp.Recipient)
                .Include(cp => cp.NonCardReceiver)
                .FirstOrDefaultAsync(cp => cp.TransactionId == model.TransactionId);

            if (cashPickup == null)
            {
                _logger.LogWarning("Cash pickup not found for transaction: {TransactionId}", model.TransactionId);
                ModelState.AddModelError("", "Cash pickup details not found. Please start again.");
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);
            var receivingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.ReceivingCountryCode);

            // Get receiver name
            var receiverName = cashPickup.Recipient?.ReceiverName ??
                cashPickup.NonCardReceiver?.FullName ?? "N/A";

            // Set ViewBag for view (in case of errors)
            ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
            ViewBag.TransferMethod = "Cash PickUp";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = receiverName;
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

            // Get recipient ID for limit validation
            var recipientId = cashPickup.RecipientId ?? cashPickup.NonCardReceiverId ?? 0;

            // Validate transaction limits (legacy pattern)
            var hasExceededReceiverLimit = await _transactionLimitService.HasExceededReceiverLimitAsync(
                transaction.SenderId,
                recipientId,
                transaction.SendingCountryCode,
                transaction.ReceivingCountryCode,
                TransactionType.CashPickup);

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
                TransactionType.CashPickup);

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

            _logger.LogInformation("Payment method selected for cash pickup. TransactionId: {TransactionId}, PaymentMode: {PaymentMode}, SelectedCardId: {SelectedCardId}",
                transaction.Id, model.SenderPaymentMode, selectedCardId);

            // Redirect based on payment mode (legacy pattern)
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
                        // TODO: Validate card and save card details if needed
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
                    // KiiPay Wallet - check balance and complete transaction
                    // TODO: Implement wallet balance check
                    // For POC, complete transaction directly
                    transaction.SenderPaymentMode = PaymentMode.MobileWallet;
                    transaction.Status = TransactionStatus.Paid;
                    await _context.SaveChangesAsync();
                    return RedirectToAction("CashPickUpSuccess", new { transactionId = transaction.Id });

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
            _logger.LogError(ex, "Error processing InternationalPayNow for cash pickup");
            ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
            TempData["ErrorMessage"] = "An error occurred while processing your payment method selection. Please try again.";
            return RedirectToAction("InternationalPayNow", new { transactionId = model.TransactionId });
        }
    }

    /// <summary>
    /// GET: DebitCreditCardDetails - Credit/debit card payment form for cash pickup
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

            // Get cash pickup details
            var cashPickup = await _context.CashPickups
                .Include(cp => cp.Recipient)
                .Include(cp => cp.NonCardReceiver)
                .FirstOrDefaultAsync(cp => cp.TransactionId == transactionId);

            if (cashPickup == null)
            {
                _logger.LogWarning("Cash pickup not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);

            // Get receiver name
            var receiverName = cashPickup.Recipient?.ReceiverName ??
                cashPickup.NonCardReceiver?.FullName ?? "N/A";

            // Build view model
            var viewModel = new CreditDebitCardViewModel
            {
                TransactionId = transactionId,
                ReceiptNo = transaction.ReceiptNo,
                FaxingAmount = transaction.TotalAmount + 0.05m, // Add card fee (legacy pattern)
                FaxingCurrency = transaction.SendingCurrency,
                FaxingCurrencySymbol = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                ReceiverName = receiverName,
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
            ViewBag.TransferMethod = "Cash PickUp";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = receiverName;
            ViewBag.Fee = transaction.Fee;
            ViewBag.IsFromSavedDebitCard = IsFromSavedDebitCard;
            ViewBag.HasOneSavedCard = false; // TODO: Check if sender has saved cards
            ViewBag.CardErrorMessage = TempData["CardErrorMessage"] as string ?? "";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading debit credit card details page for cash pickup");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: DebitCreditCardDetails - Process card payment for cash pickup
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DebitCreditCardDetails(CreditDebitCardViewModel model)
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
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get cash pickup details
            var cashPickup = await _context.CashPickups
                .Include(cp => cp.Recipient)
                .Include(cp => cp.NonCardReceiver)
                .FirstOrDefaultAsync(cp => cp.TransactionId == model.TransactionId);

            if (cashPickup == null)
            {
                _logger.LogWarning("Cash pickup not found for transaction: {TransactionId}", model.TransactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);

            // Get receiver name
            var receiverName = cashPickup.Recipient?.ReceiverName ??
                cashPickup.NonCardReceiver?.FullName ?? "N/A";

            // Validate model
            if (!ModelState.IsValid)
            {
                // Reload ViewBag on error
                ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
                ViewBag.TransferMethod = "Cash PickUp";
                ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
                ViewBag.SendingAmount = transaction.SendingAmount;
                ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
                ViewBag.ReceiverName = receiverName;
                ViewBag.Fee = transaction.Fee;
                ViewBag.IsFromSavedDebitCard = false;
                ViewBag.HasOneSavedCard = false;
                ViewBag.CardErrorMessage = "";
                return View(model);
            }

            // Mask card number for storage (legacy pattern)
            var maskedCardNumber = MaskCardNumber(model.CardNumber);
            var cardType = DetectCardIssuer(model.CardNumber);

            // Validate card expiry
            if (!string.IsNullOrEmpty(model.EndMM) && !string.IsNullOrEmpty(model.EndYY))
            {
                if (int.TryParse(model.EndMM, out var month) && int.TryParse(model.EndYY, out var year))
                {
                    var cardExpiryDate = new DateTime(2000 + year, month, 1).AddMonths(1).AddDays(-1);
                    if (cardExpiryDate < DateTime.UtcNow)
                    {
                        ModelState.AddModelError("EndYY", "Card has expired");
                        // Reload ViewBag on error
                        ViewBag.ReceivingCountryCurrency = transaction.ReceivingCurrency;
                        ViewBag.TransferMethod = "Cash PickUp";
                        ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
                        ViewBag.SendingAmount = transaction.SendingAmount;
                        ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
                        ViewBag.ReceiverName = receiverName;
                        ViewBag.Fee = transaction.Fee;
                        ViewBag.IsFromSavedDebitCard = false;
                        ViewBag.HasOneSavedCard = false;
                        ViewBag.CardErrorMessage = "";
                        return View(model);
                    }
                }
            }

            // TODO: Process payment with payment gateway (Stripe, WorldPay, etc.)
            // For POC, just save card information and mark transaction as paid

            // Build expiry date string (MM/YY format)
            var expiryDate = $"{model.EndMM}/{model.EndYY}";

            // Save card payment information (using actual entity properties)
            var cardPayment = new CardPaymentInformation
            {
                TransactionId = transaction.Id,
                NonCardTransactionId = transaction.Id, // For cash pickup transactions
                CardNumber = maskedCardNumber,
                ExpiryDate = expiryDate,
                NameOnCard = model.NameOnCard,
                TransferType = 2, // Cash Pickup
                IsSavedCard = false,
                AutoRecharged = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.CardPaymentInformations.Add(cardPayment);

            // Update transaction status to Paid
            transaction.Status = TransactionStatus.Paid;
            transaction.SenderPaymentMode = PaymentMode.Card;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Card payment processed successfully for cash pickup. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}",
                transaction.Id, transaction.ReceiptNo);

            // Redirect to success page
            return RedirectToAction("CashPickUpSuccess", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing card payment for cash pickup");
            ModelState.AddModelError("", "An error occurred while processing your payment. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// GET: MoneyFexBankDeposit - Bank deposit instructions for cash pickup
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

            // Get cash pickup details
            var cashPickup = await _context.CashPickups
                .Include(cp => cp.Recipient)
                .Include(cp => cp.NonCardReceiver)
                .FirstOrDefaultAsync(cp => cp.TransactionId == transactionId);

            if (cashPickup == null)
            {
                _logger.LogWarning("Cash pickup not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);

            // Get receiver name
            var receiverName = cashPickup.Recipient?.ReceiverName ??
                cashPickup.NonCardReceiver?.FullName ?? "N/A";

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
            ViewBag.TransferMethod = "Cash PickUp";
            ViewBag.SendingCountryCurrency = transaction.SendingCurrency;
            ViewBag.SendingAmount = transaction.SendingAmount;
            ViewBag.ReceivingCountry = transaction.ReceivingCountryCode.ToLower();
            ViewBag.ReceiverName = receiverName;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading MoneyFex bank deposit page for cash pickup");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// POST: MoneyFexBankDeposit - Confirm manual bank deposit for cash pickup
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
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Update transaction status to PaymentPending (manual bank deposit)
            transaction.Status = TransactionStatus.PaymentPending;
            transaction.SenderPaymentMode = PaymentMode.BankAccount;
            transaction.UpdatedAt = DateTime.UtcNow;

            // Update cash pickup to indicate payment has been made
            var cashPickup = await _context.CashPickups
                .FirstOrDefaultAsync(cp => cp.TransactionId == model.TransactionId);

            if (cashPickup != null)
            {
                // Note: Cash pickup doesn't have HasMadePaymentToBankAccount, but we can add a note or update status
                cashPickup.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Bank deposit confirmation submitted for cash pickup. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}",
                transaction.Id, transaction.ReceiptNo);

            // Redirect to success page
            return RedirectToAction("CashPickUpSuccess", new { transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bank deposit confirmation for cash pickup");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// GET: CashPickUpSuccess - Success page after cash pickup payment
    /// Replicates legacy /SenderCashPickUp/CashPickUpSuccess
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CashPickUpSuccess(int transactionId)
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

            // Get cash pickup details
            var cashPickup = await _context.CashPickups
                .Include(cp => cp.Recipient)
                .Include(cp => cp.NonCardReceiver)
                .FirstOrDefaultAsync(cp => cp.TransactionId == transactionId);

            if (cashPickup == null)
            {
                _logger.LogWarning("Cash pickup not found for transaction: {TransactionId}", transactionId);
                return RedirectToAction("Index", "TransferMoneyNow");
            }

            // Get currency symbols
            var sendingCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == transaction.SendingCountryCode);

            // Get receiver name
            var receiverName = cashPickup.Recipient?.ReceiverName ??
                cashPickup.NonCardReceiver?.FullName ?? "N/A";

            // Build view model (reuse AddMoneyToWalletSuccessViewModel)
            var viewModel = new AddMoneyToWalletSuccessViewModel
            {
                Amount = transaction.SendingAmount,
                Currency = sendingCountry?.CurrencySymbol ?? GetCurrencySymbol(transaction.SendingCurrency),
                ReceiverName = receiverName,
                ReceiptNo = transaction.ReceiptNo,
                TransactionId = transactionId
            };

            // Set ViewBag for MFCN display
            ViewBag.MFCN = cashPickup.MFCN;
            ViewBag.TransferMethod = "Cash PickUp";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cash pickup success page");
            return RedirectToAction("Index", "TransferMoneyNow");
        }
    }

    /// <summary>
    /// GET: GetReceiverInformation - Get receiver details by ID
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReceiverInformation(int receiverId)
    {
        try
        {
            var recipient = await _context.Recipients
                .FirstOrDefaultAsync(r => r.Id == receiverId);

            if (recipient == null)
            {
                return Json(new { });
            }

            // Get country code from transaction (Recipient entity doesn't have CountryCode)
            var transaction = await _context.Transactions
                .Join(_context.CashPickups,
                    t => t.Id,
                    cp => cp.TransactionId,
                    (t, cp) => new { Transaction = t, CashPickup = cp })
                .Where(x => x.CashPickup.RecipientId == receiverId)
                .OrderByDescending(x => x.Transaction.TransactionDate)
                .Select(x => x.Transaction.ReceivingCountryCode)
                .FirstOrDefaultAsync();

            var countryCode = transaction ?? "";

            return Json(new
            {
                FullName = recipient.ReceiverName,
                Country = countryCode,
                CountryPhoneCode = GetCountryPhoneCodeHelper(countryCode)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting receiver information");
            return Json(new { });
        }
    }

    /// <summary>
    /// GET: GetCountryPhoneCode - Get country phone code
    /// </summary>
    [HttpGet]
    public IActionResult GetCountryPhoneCode(string countryCode)
    {
        try
        {
            var phoneCode = GetCountryPhoneCodeHelper(countryCode);
            return Json(new { CountryPhoneCode = phoneCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting country phone code");
            return Json(new { CountryPhoneCode = "" });
        }
    }

    #region Helper Methods

    private int GetSenderIdFromSession()
    {
        var senderIdStr = HttpContext.Session.GetString("SenderId");
        if (int.TryParse(senderIdStr, out var senderId) && senderId > 0)
        {
            return senderId;
        }
        // For POC, default to sender ID 1 if not in session
        return 1;
    }

    private decimal? GetQueryParamAsDecimal(string key)
    {
        var value = Request.Query[key].FirstOrDefault();
        if (decimal.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }

    private string GetCountryPhoneCodeHelper(string countryCode)
    {
        // Simplified phone code mapping (for POC)
        return countryCode switch
        {
            "NG" => "+234",
            "GH" => "+233",
            "KE" => "+254",
            "ZA" => "+27",
            "UG" => "+256",
            "TZ" => "+255",
            "MA" => "+212",
            _ => "+44" // Default to UK
        };
    }

    private string GetCurrencySymbol(string currency)
    {
        return currency switch
        {
            "GBP" => "£",
            "USD" => "$",
            "EUR" => "€",
            "NGN" => "₦",
            "GHS" => "₵",
            "KES" => "KSh",
            "ZAR" => "R",
            _ => currency
        };
    }

    private async Task SetCountriesViewBagAsync()
    {
        var countries = await _context.Countries
            .OrderBy(c => c.CountryName)
            .Select(c => new SelectListItem
            {
                Value = c.CountryCode,
                Text = c.CountryName
            })
            .ToListAsync();

        ViewBag.Countries = countries;
    }

    private async Task SetRecentReceiversViewBagAsync(int senderId, string countryCode)
    {
        if (senderId <= 0)
        {
            ViewBag.RecentReceivers = new List<SelectListItem>();
            return;
        }

        // Get recent cash pickup receivers for this sender
        var recentTransactions = await _context.Transactions
            .Where(t => t.SenderId == senderId &&
                       t.ReceivingCountryCode == countryCode)
            .Join(_context.CashPickups,
                t => t.Id,
                cp => cp.TransactionId,
                (t, cp) => new { Transaction = t, CashPickup = cp })
            .OrderByDescending(x => x.Transaction.TransactionDate)
            .Take(10)
            .ToListAsync();

        var recentReceivers = new List<SelectListItem>();

        foreach (var item in recentTransactions)
        {
            string receiverName = "N/A";
            int? receiverId = null;

            if (item.CashPickup.RecipientId.HasValue)
            {
                var recipient = await _context.Recipients
                    .FirstOrDefaultAsync(r => r.Id == item.CashPickup.RecipientId.Value);
                if (recipient != null)
                {
                    receiverName = recipient.ReceiverName;
                    receiverId = recipient.Id;
                }
            }
            else if (item.CashPickup.NonCardReceiverId.HasValue)
            {
                var receiver = await _context.ReceiverDetails
                    .FirstOrDefaultAsync(r => r.Id == item.CashPickup.NonCardReceiverId.Value);
                if (receiver != null)
                {
                    receiverName = receiver.FullName;
                }
            }

            if (!string.IsNullOrEmpty(receiverName) && receiverName != "N/A" && receiverId.HasValue)
            {
                if (!recentReceivers.Any(r => r.Value == receiverId.Value.ToString()))
                {
                    recentReceivers.Add(new SelectListItem
                    {
                        Value = receiverId.Value.ToString(),
                        Text = receiverName
                    });
                }
            }
        }

        ViewBag.RecentReceivers = recentReceivers;
    }

    private void SetIdCardTypesViewBag()
    {
        // Simplified ID card types (for POC)
        var idCardTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "National ID" },
            new SelectListItem { Value = "2", Text = "Passport" },
            new SelectListItem { Value = "3", Text = "Driver's License" }
        };

        ViewBag.IdCardTypes = idCardTypes;
    }

    private async Task<int> GetRecipientIdAsync(string mobileNumber, string countryCode)
    {
        // Try to find existing recipient by matching cash pickups with same mobile number and country
        // Note: Recipient entity doesn't have MobileNumber or CountryCode, so we search through CashPickups
        var cashPickup = await _context.CashPickups
            .Include(cp => cp.NonCardReceiver)
            .Include(cp => cp.Recipient)
            .Join(_context.Transactions,
                cp => cp.TransactionId,
                t => t.Id,
                (cp, t) => new { CashPickup = cp, Transaction = t })
            .Where(x => x.Transaction.ReceivingCountryCode == countryCode &&
                       (x.CashPickup.NonCardReceiver != null && x.CashPickup.NonCardReceiver.PhoneNumber == mobileNumber))
            .OrderByDescending(x => x.Transaction.TransactionDate)
            .FirstOrDefaultAsync();

        return cashPickup?.CashPickup.RecipientId ?? 0;
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

    private async Task<string> GenerateUniqueMFCNAsync()
    {
        string mfcn;
        var random = new Random();
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            mfcn = $"{DateTime.UtcNow:yyyyMMdd}{random.Next(100000, 999999)}";
            var exists = await _context.CashPickups.AnyAsync(cp => cp.MFCN == mfcn);
            if (!exists)
            {
                return mfcn;
            }
            attempts++;
        } while (attempts < maxAttempts);

        return $"{DateTime.UtcNow:yyyyMMddHHmmss}{random.Next(1000, 9999)}";
    }

    private string MaskCardNumber(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return string.Empty;
        }

        // Remove all non-digit characters
        var digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length < 4)
        {
            return cardNumber; // Return as-is if too short
        }

        // Mask all but last 4 digits
        var lastFour = digitsOnly.Substring(digitsOnly.Length - 4);
        var masked = new string('*', digitsOnly.Length - 4) + lastFour;

        // Format as XXXX-XXXX-XXXX-XXXX
        if (masked.Length == 16)
        {
            return $"{masked.Substring(0, 4)}-{masked.Substring(4, 4)}-{masked.Substring(8, 4)}-{masked.Substring(12, 4)}";
        }

        return masked;
    }

    private string DetectCardIssuer(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return "Unknown";
        }

        // Remove all non-digit characters
        var digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(digitsOnly))
        {
            return "Unknown";
        }

        // Detect card type based on first digits
        if (digitsOnly.StartsWith("4"))
        {
            return "Visa";
        }
        if (digitsOnly.StartsWith("5") || digitsOnly.StartsWith("2"))
        {
            return "MasterCard";
        }
        if (digitsOnly.StartsWith("3"))
        {
            return "American Express";
        }
        if (digitsOnly.StartsWith("6"))
        {
            return "Discover";
        }

        return "Unknown";
    }

    /// <summary>
    /// GET: SenderCashPickUp/Details
    /// Display cash pickup transaction details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int? transactionId, string? receiptNo, string? mfcn)
    {
        try
        {
            CashPickup? cashPickup = null;

            if (transactionId.HasValue)
            {
                cashPickup = await _context.CashPickups
                    .Include(c => c.Recipient)
                    .Include(c => c.NonCardReceiver)
                    .Include(c => c.Transaction)
                        .ThenInclude(t => t.Sender)
                    .Include(c => c.Transaction)
                        .ThenInclude(t => t.ReceivingCountry)
                    .FirstOrDefaultAsync(c => c.TransactionId == transactionId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(receiptNo))
            {
                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.ReceiptNo == receiptNo);
                
                if (transaction != null)
                {
                    cashPickup = await _context.CashPickups
                        .Include(c => c.Recipient)
                        .Include(c => c.NonCardReceiver)
                        .Include(c => c.Transaction)
                            .ThenInclude(t => t.Sender)
                        .Include(c => c.Transaction)
                            .ThenInclude(t => t.ReceivingCountry)
                        .FirstOrDefaultAsync(c => c.TransactionId == transaction.Id);
                }
            }
            else if (!string.IsNullOrWhiteSpace(mfcn))
            {
                cashPickup = await _context.CashPickups
                    .Include(c => c.Recipient)
                    .Include(c => c.NonCardReceiver)
                    .Include(c => c.Transaction)
                        .ThenInclude(t => t.Sender)
                    .Include(c => c.Transaction)
                        .ThenInclude(t => t.ReceivingCountry)
                    .FirstOrDefaultAsync(c => c.MFCN == mfcn);
            }

            if (cashPickup == null)
            {
                return NotFound();
            }

            return View(cashPickup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cash pickup details");
            return NotFound();
        }
    }

    #endregion
}

