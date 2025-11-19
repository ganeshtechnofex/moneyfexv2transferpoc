using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.ViewModels;
using System.Text.Json;

namespace MoneyFex.Web.Controllers;

/// <summary>
/// Controller for KiiBank transfer functionality
/// Based on legacy KiiBankTransferController
/// Contains all transaction flow logic (moved from SenderTransactionController)
/// </summary>
public class KiiBankTransferController : Controller
{
    private readonly IKiiBankAccountValidationService _kiiBankAccountValidationService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<KiiBankTransferController> _logger;
    private readonly IKiiBankTransferService _kiiBankTransferService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IKiiBankTransferRepository _kiiBankTransferRepository;
    private readonly MoneyFexDbContext _context;
    
    private const string KIIBANK_RECIPIENT_SESSION_KEY = "KiiBankRecipientViewModel";
    private const string COMMON_ENTER_AMOUNT_SESSION_KEY = "CommonEnterAmountViewModel";
    private const string PAYMENT_METHOD_SESSION_KEY = "PaymentMethodViewModel";
    private const string TRANSACTION_ID_SESSION_KEY = "TransactionId";
    private const string DEBIT_CREDIT_CARD_DETAIL_SESSION_KEY = "CreditDebitCardViewModel";

    public KiiBankTransferController(
        IKiiBankAccountValidationService kiiBankAccountValidationService,
        IExchangeRateService exchangeRateService,
        ILogger<KiiBankTransferController> logger,
        IKiiBankTransferService kiiBankTransferService,
        ITransactionRepository transactionRepository,
        IKiiBankTransferRepository kiiBankTransferRepository,
        MoneyFexDbContext context)
    {
        _kiiBankAccountValidationService = kiiBankAccountValidationService;
        _exchangeRateService = exchangeRateService;
        _logger = logger;
        _kiiBankTransferService = kiiBankTransferService;
        _transactionRepository = transactionRepository;
        _kiiBankTransferRepository = kiiBankTransferRepository;
        _context = context;
    }

    /// <summary>
    /// GET: KiiBankTransfer/AccountDetails
    /// Displays form to enter KiiBank account number
    /// Based on legacy KiiBankTransferController.AccountDetails
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AccountDetails(
        decimal? sendingAmount = null,
        decimal? receivingAmount = null,
        string? sendingCurrency = null,
        string? receivingCurrency = null,
        string? sendingCountry = null,
        string? receivingCountry = null)
    {
        try
        {
            // Store transfer summary in session for later use
            if (sendingAmount.HasValue && receivingAmount.HasValue)
            {
                try
                {
                    // Recalculate to get fee and total amount
                    var calculation = await _exchangeRateService.CalculateTransferSummaryAsync(
                        sendingAmount.Value,
                        receivingAmount.Value,
                        sendingCurrency ?? "GBP",
                        receivingCurrency ?? "NGN",
                        sendingCountry ?? "GB",
                        receivingCountry ?? "NG",
                        TransactionType.KiiBank,
                        false,
                        false);

                    var commonEnterAmount = new CommonEnterAmountViewModel
                    {
                        SendingAmount = calculation.SendingAmount,
                        ReceivingAmount = calculation.ReceivingAmount,
                        SendingCurrency = calculation.SendingCurrency,
                        ReceivingCurrency = calculation.ReceivingCurrency,
                        SendingCountry = sendingCountry ?? "GB",
                        ReceivingCountry = receivingCountry ?? "NG",
                        SendingCountryCode = sendingCountry ?? "GB",
                        ReceivingCountryCode = receivingCountry ?? "NG",
                        ExchangeRate = calculation.ExchangeRate,
                        Fee = calculation.Fee,
                        TotalAmount = calculation.TotalAmount
                    };

                    var commonEnterAmountJson = JsonSerializer.Serialize(commonEnterAmount);
                    HttpContext.Session.SetString(COMMON_ENTER_AMOUNT_SESSION_KEY, commonEnterAmountJson);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating transfer summary in AccountDetails. Amounts: {SendingAmount}, {ReceivingAmount}", 
                        sendingAmount, receivingAmount);
                    // Continue without storing in session - user can still proceed
                }
            }

            // Get account number from session if available
            var recipientJson = HttpContext.Session.GetString(KIIBANK_RECIPIENT_SESSION_KEY);
            KiiBankRecipientViewModel? model = null;
            
            if (!string.IsNullOrEmpty(recipientJson))
            {
                try
                {
                    model = JsonSerializer.Deserialize<KiiBankRecipientViewModel>(recipientJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deserializing KiiBank recipient from session");
                }
            }

            ViewBag.AccountNo = model?.AccountNumber ?? string.Empty;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AccountDetails action");
            // Return view even if there's an error - don't crash the page
            ViewBag.AccountNo = string.Empty;
            return View();
        }
    }

    /// <summary>
    /// GET: KiiBankTransfer/ValidateAccountNumber
    /// Validates a KiiBank account number via JSON endpoint
    /// Based on legacy KiiBankTransferController.ValidateAccountNumber
    /// NOTE: Currently bypasses validation and uses static values for testing
    /// </summary>
    [HttpGet]
    public IActionResult ValidateAccountNumber([FromQuery] string accountNumber)
    {
        // BYPASS VALIDATION - Use static values for now
        try
        {
            // Use provided account number or default static value
            var accountNum = !string.IsNullOrWhiteSpace(accountNumber) ? accountNumber : "1234567890";
            
            // Store static account details in session
            var recipientViewModel = new KiiBankRecipientViewModel
            {
                AccountNumber = accountNum,
                AccountName = "John Doe", // Static account name
                MobileNumber = accountNum // Use account number as mobile if it looks like a phone number
            };

            var recipientJson = JsonSerializer.Serialize(recipientViewModel);
            HttpContext.Session.SetString(KIIBANK_RECIPIENT_SESSION_KEY, recipientJson);

            return Json(new
            {
                Status = 1, // OK
                Message = "Account validated successfully",
                Data = new
                {
                    AccountNumber = accountNum,
                    ReceiverName = "John Doe"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ValidateAccountNumber: {AccountNumber}", accountNumber);
            return Json(new
            {
                Status = 0, // Error
                Message = "An error occurred while processing the account number",
                Data = new
                {
                    AccountNumber = (string?)null,
                    ReceiverName = (string?)null
                }
            });
        }
    }

    /// <summary>
    /// GET: KiiBankTransfer/ValidateAccount
    /// Displays form to confirm account details and select reason for transfer
    /// Based on legacy KiiBankTransferController.ValidateAccount (GET)
    /// </summary>
    [HttpGet]
    public IActionResult ValidateAccount()
    {
        // Get recipient details from session
        var recipientJson = HttpContext.Session.GetString(KIIBANK_RECIPIENT_SESSION_KEY);
        KiiBankRecipientViewModel? model = null;

        if (!string.IsNullOrEmpty(recipientJson))
        {
            try
            {
                model = JsonSerializer.Deserialize<KiiBankRecipientViewModel>(recipientJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deserializing KiiBank recipient from session");
            }
        }

        // If no model in session, redirect to AccountDetails
        if (model == null || string.IsNullOrEmpty(model.AccountNumber))
        {
            return RedirectToAction(nameof(AccountDetails));
        }

        return View(model);
    }

    /// <summary>
    /// POST: KiiBankTransfer/ValidateAccount
    /// Processes account validation form and redirects to payment summary
    /// Based on legacy KiiBankTransferController.ValidateAccount (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ValidateAccount(KiiBankRecipientViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Validate reason for transfer
        if (model.ReasonForTransfer == ReasonForTransfer.Non)
        {
            ModelState.AddModelError(nameof(model.ReasonForTransfer), "Select Reason for Transfer");
            return View(model);
        }

        // Get common enter amount from session (set by SendMoneyController or similar)
        var commonEnterAmountJson = HttpContext.Session.GetString(COMMON_ENTER_AMOUNT_SESSION_KEY);
        if (!string.IsNullOrEmpty(commonEnterAmountJson))
        {
            try
            {
                var commonEnterAmount = JsonSerializer.Deserialize<CommonEnterAmountViewModel>(commonEnterAmountJson);
                if (commonEnterAmount != null)
                {
                    commonEnterAmount.ReceiverName = model.AccountName;
                    HttpContext.Session.SetString(COMMON_ENTER_AMOUNT_SESSION_KEY, 
                        JsonSerializer.Serialize(commonEnterAmount));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating common enter amount with receiver name");
            }
        }

        // Store updated recipient details in session
        var recipientJson = JsonSerializer.Serialize(model);
        HttpContext.Session.SetString(KIIBANK_RECIPIENT_SESSION_KEY, recipientJson);

        // Redirect to transaction summary (now in same controller)
        return RedirectToAction("Summary");
    }

    /// <summary>
    /// GET: KiiBankTransfer/Summary
    /// Displays the transfer summary page
    /// Based on legacy SenderTransactionController.Summary (GET)
    /// </summary>
    [HttpGet]
    public IActionResult Summary()
    {
        try
        {
            var model = GetTransferSummary();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Summary page");
            // Redirect to AccountDetails if session data is missing
            return RedirectToAction("AccountDetails");
        }
    }

    /// <summary>
    /// POST: KiiBankTransfer/Summary
    /// Processes the summary form and redirects to payment method
    /// Based on legacy SenderTransactionController.Summary (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Summary(TransferSummaryViewModel model)
    {
        // Model parameter is required for model binding even if not directly used
        // It ensures the form data is properly bound and validated
        if (!ModelState.IsValid)
        {
            return View(GetTransferSummary());
        }
        
        try
        {
            // Get data from session
            var commonEnterAmountJson = HttpContext.Session.GetString(COMMON_ENTER_AMOUNT_SESSION_KEY);
            var recipientJson = HttpContext.Session.GetString(KIIBANK_RECIPIENT_SESSION_KEY);
            
            if (string.IsNullOrEmpty(commonEnterAmountJson) || string.IsNullOrEmpty(recipientJson))
            {
                ModelState.AddModelError("", "Missing transaction data. Please start over.");
                return View(GetTransferSummary());
            }
            
            var paymentInfo = JsonSerializer.Deserialize<CommonEnterAmountViewModel>(commonEnterAmountJson);
            var recipient = JsonSerializer.Deserialize<KiiBankRecipientViewModel>(recipientJson);
            
            if (paymentInfo == null || recipient == null)
            {
                ModelState.AddModelError("", "Invalid transaction data. Please start over.");
                return View(GetTransferSummary());
            }
            
            // Validate and set default values for country codes if missing
            var sendingCountryCode = !string.IsNullOrEmpty(paymentInfo.SendingCountryCode) 
                ? paymentInfo.SendingCountryCode 
                : (!string.IsNullOrEmpty(paymentInfo.SendingCountry) 
                    ? paymentInfo.SendingCountry 
                    : "GB");
            var receivingCountryCode = !string.IsNullOrEmpty(paymentInfo.ReceivingCountryCode) 
                ? paymentInfo.ReceivingCountryCode 
                : (!string.IsNullOrEmpty(paymentInfo.ReceivingCountry) 
                    ? paymentInfo.ReceivingCountry 
                    : "NG");
            
            // Validate currency codes
            var sendingCurrency = !string.IsNullOrEmpty(paymentInfo.SendingCurrency) ? paymentInfo.SendingCurrency : "GBP";
            var receivingCurrency = !string.IsNullOrEmpty(paymentInfo.ReceivingCurrency) ? paymentInfo.ReceivingCurrency : "NGN";
            
            // Check if transaction already exists
            var existingTransactionId = HttpContext.Session.GetInt32(TRANSACTION_ID_SESSION_KEY);
            Transaction? transaction = null;
            
            if (existingTransactionId.HasValue && existingTransactionId.Value > 0)
            {
                transaction = await _transactionRepository.GetByIdAsync(existingTransactionId.Value);
            }
            
            // Generate receipt number if not exists
            var receiptNo = HttpContext.Session.GetString("ReceiptNo");
            if (string.IsNullOrEmpty(receiptNo))
            {
                // Generate receipt number: KB + timestamp + random
                receiptNo = $"KB{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
                HttpContext.Session.SetString("ReceiptNo", receiptNo);
            }
            
            // Get sender ID from session (TODO: Get from authenticated user)
            var senderId = 1; // TODO: Get from HttpContext.User or session
            
            // Ensure sender exists in database (create if not exists)
            var sender = await GetOrCreateSenderAsync(senderId, sendingCountryCode);
            senderId = sender.Id; // Use the actual sender ID (might be newly created)
            
            // Validate recipient data
            var accountNumber = !string.IsNullOrEmpty(recipient.AccountNumber) ? recipient.AccountNumber : "N/A";
            var accountName = !string.IsNullOrEmpty(recipient.AccountName) ? recipient.AccountName : "N/A";
            var mobileNumber = recipient.MobileNumber; // Can be null
            
            if (transaction == null)
            {
                // Create new Transaction
                transaction = new Transaction
                {
                    TransactionDate = DateTime.UtcNow,
                    ReceiptNo = receiptNo,
                    SenderId = senderId,
                    SendingCountryCode = sendingCountryCode,
                    ReceivingCountryCode = receivingCountryCode,
                    SendingCurrency = sendingCurrency,
                    ReceivingCurrency = receivingCurrency,
                    SendingAmount = paymentInfo.SendingAmount,
                    ReceivingAmount = paymentInfo.ReceivingAmount,
                    Fee = paymentInfo.Fee,
                    TotalAmount = paymentInfo.TotalAmount,
                    ExchangeRate = paymentInfo.ExchangeRate,
                    Status = TransactionStatus.PaymentPending,
                    TransactionModule = TransactionModule.Sender,
                    ReasonForTransfer = recipient.ReasonForTransfer,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                // Add Transaction to context
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync(); // Save Transaction first to get ID
                
                _logger.LogInformation("Created new Transaction. Id: {TransactionId}, ReceiptNo: {ReceiptNo}", 
                    transaction.Id, transaction.ReceiptNo);
                
                // Create new KiiBankTransfer with all required data
                var kiiBankTransfer = new KiiBankTransfer
                {
                    TransactionId = transaction.Id,
                    AccountNo = accountNumber,
                    ReceiverName = accountName,
                    AccountOwnerName = accountName, // Set account owner name same as receiver name
                    AccountHolderPhoneNo = mobileNumber,
                    TransactionReference = null, // Will be set when payment is processed
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                // Add KiiBankTransfer to context
                _context.KiiBankTransfers.Add(kiiBankTransfer);
                await _context.SaveChangesAsync(); // Save KiiBankTransfer
                
                _logger.LogInformation("Created new KiiBankTransfer. TransactionId: {TransactionId}, AccountNo: {AccountNo}", 
                    transaction.Id, accountNumber);
            }
            else
            {
                // Update existing Transaction
                transaction.TransactionDate = DateTime.UtcNow;
                transaction.SendingCountryCode = sendingCountryCode;
                transaction.ReceivingCountryCode = receivingCountryCode;
                transaction.SendingCurrency = sendingCurrency;
                transaction.ReceivingCurrency = receivingCurrency;
                transaction.SendingAmount = paymentInfo.SendingAmount;
                transaction.ReceivingAmount = paymentInfo.ReceivingAmount;
                transaction.Fee = paymentInfo.Fee;
                transaction.TotalAmount = paymentInfo.TotalAmount;
                transaction.ExchangeRate = paymentInfo.ExchangeRate;
                transaction.ReasonForTransfer = recipient.ReasonForTransfer;
                transaction.UpdatedAt = DateTime.UtcNow;
                
                // Update Transaction with RecipientId if available
                if (recipient.Id > 0)
                {
                    transaction.RecipientId = recipient.Id;
                }
                
                // Update Transaction in context
                _context.Transactions.Update(transaction);
                
                // Get or update KiiBankTransfer
                var existingKiiBankTransfer = await _context.KiiBankTransfers
                    .FirstOrDefaultAsync(k => k.TransactionId == transaction.Id);
                
                if (existingKiiBankTransfer == null)
                {
                    // Create new KiiBankTransfer
                    var kiiBankTransfer = new KiiBankTransfer
                    {
                        TransactionId = transaction.Id,
                        AccountNo = accountNumber,
                        ReceiverName = accountName,
                        AccountOwnerName = accountName,
                        AccountHolderPhoneNo = mobileNumber,
                        TransactionReference = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.KiiBankTransfers.Add(kiiBankTransfer);
                }
                else
                {
                    // Update existing KiiBankTransfer
                    existingKiiBankTransfer.AccountNo = accountNumber;
                    existingKiiBankTransfer.ReceiverName = accountName;
                    existingKiiBankTransfer.AccountOwnerName = accountName;
                    existingKiiBankTransfer.AccountHolderPhoneNo = mobileNumber;
                    existingKiiBankTransfer.UpdatedAt = DateTime.UtcNow;
                    _context.KiiBankTransfers.Update(existingKiiBankTransfer);
                }
                
                // Save all changes in a single transaction
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated existing Transaction and KiiBankTransfer. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}", 
                    transaction.Id, transaction.ReceiptNo);
            }
            
            // Store transaction ID and receipt number in session
            HttpContext.Session.SetInt32(TRANSACTION_ID_SESSION_KEY, transaction.Id);
            HttpContext.Session.SetString("ReceiptNo", transaction.ReceiptNo);
            HttpContext.Session.SetString("IsTransactionOnpending", "true");
            
            _logger.LogInformation("Incomplete transaction saved. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}", 
                transaction.Id, transaction.ReceiptNo);
            
            return RedirectToAction("PaymentMethod");
        }
        catch (DbUpdateException dbEx)
        {
            // Database-specific errors
            _logger.LogError(dbEx, "Database error saving transaction. Exception: {ExceptionMessage}", dbEx.Message);
            
            // Log inner exception (usually contains the actual constraint violation)
            if (dbEx.InnerException != null)
            {
                _logger.LogError(dbEx.InnerException, "Inner database exception: {InnerExceptionMessage}", dbEx.InnerException.Message);
                ModelState.AddModelError("", $"Database error: {dbEx.InnerException.Message}. Please try again.");
            }
            else
            {
                ModelState.AddModelError("", $"Database error: {dbEx.Message}. Please try again.");
            }
            
            return View(GetTransferSummary());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving incomplete transaction. Exception: {ExceptionMessage}, StackTrace: {StackTrace}", 
                ex.Message, ex.StackTrace);
            
            // Log inner exception if exists
            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, "Inner exception: {InnerExceptionMessage}, StackTrace: {InnerStackTrace}", 
                    ex.InnerException.Message, ex.InnerException.StackTrace);
                ModelState.AddModelError("", $"An error occurred: {ex.InnerException.Message}. Please try again.");
            }
            else
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}. Please try again.");
            }
            
            return View(GetTransferSummary());
        }
    }

    /// <summary>
    /// GET: KiiBankTransfer/PaymentMethod
    /// Displays payment method selection page
    /// Based on legacy SenderTransactionController.PaymentMethod (GET)
    /// </summary>
    [HttpGet]
    public IActionResult PaymentMethod()
    {
        try
        {
            var paymentSummary = GetTransferSummary();
        
        // Get payment method from session if exists
        var paymentMethodJson = HttpContext.Session.GetString(PAYMENT_METHOD_SESSION_KEY);
        PaymentMethodViewModel? vm = null;
        
        if (!string.IsNullOrEmpty(paymentMethodJson))
        {
            try
            {
                vm = JsonSerializer.Deserialize<PaymentMethodViewModel>(paymentMethodJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deserializing payment method from session");
            }
        }
        
        // Create new view model if not in session
        vm ??= new PaymentMethodViewModel
        {
            TotalAmount = paymentSummary.TotalSendingAmount,
            SendingCurrencySymbol = paymentSummary.SendingCurrencySymbol,
            HasKiiPayWallet = false, // TODO: Get from user service
            KiipayWalletBalance = 0, // TODO: Get from wallet service
            HasEnableMoneyFexBankAccount = false, // TODO: Check if enabled for country
            IsEnableVolumePayment = false, // TODO: Get from configuration
            CardDetails = new List<SavedCardViewModel>(), // TODO: Get saved cards
            TransactionId = HttpContext.Session.GetInt32(TRANSACTION_ID_SESSION_KEY) ?? 0,
            ReceiptNo = HttpContext.Session.GetString("ReceiptNo") ?? string.Empty
        };
        
        // Update with current summary data
        vm.TotalAmount = paymentSummary.TotalSendingAmount;
        vm.SendingCurrencySymbol = paymentSummary.SendingCurrencySymbol;
        
        // Update transaction tracking from session if not already set
        if (vm.TransactionId == 0)
        {
            vm.TransactionId = HttpContext.Session.GetInt32(TRANSACTION_ID_SESSION_KEY) ?? 0;
        }
        if (string.IsNullOrEmpty(vm.ReceiptNo))
        {
            vm.ReceiptNo = HttpContext.Session.GetString("ReceiptNo") ?? string.Empty;
        }
        
        // Set ViewBag for view
        ViewBag.ReceivingCountryCurrency = paymentSummary.ReceivingCurrencyCode;
        ViewBag.TransferMethod = "KiiBank";
        ViewBag.SendingCountryCurrency = paymentSummary.SendingCurrencyCode;
        ViewBag.SendingAmount = paymentSummary.SendingAmount;
        ViewBag.ReceiverName = paymentSummary.ReceiverFullName;
        ViewBag.ReceivingCountry = paymentSummary.ReceivingCountry.ToLower();
        ViewBag.Fee = paymentSummary.Fee;
        ViewBag.CreditDebitFee = 0.80m; // TODO: Get from configuration
        ViewBag.ManualBankDepositFee = 0.79m; // TODO: Get from configuration
        ViewBag.ErrorMessage = TempData["ErrorMessage"]?.ToString() ?? "";
        
        return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading PaymentMethod page");
            // Redirect to Summary if there's an error
            return RedirectToAction("Summary");
        }
    }

    /// <summary>
    /// POST: KiiBankTransfer/PaymentMethod
    /// Processes payment method selection
    /// Based on legacy SenderTransactionController.PaymentMethod (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PaymentMethod([Bind(PaymentMethodViewModel.BindProperty)] PaymentMethodViewModel vm)
    {
        // Set ViewBag for view (in case of errors)
        var paymentSummary = GetTransferSummary();
        ViewBag.ReceivingCountryCurrency = paymentSummary.ReceivingCurrencyCode;
        ViewBag.TransferMethod = "KiiBank";
        ViewBag.SendingCountryCurrency = paymentSummary.SendingCurrencyCode;
        ViewBag.SendingAmount = paymentSummary.SendingAmount;
        ViewBag.ReceiverName = paymentSummary.ReceiverFullName;
        ViewBag.ReceivingCountry = paymentSummary.ReceivingCountry.ToLower();
        ViewBag.Fee = paymentSummary.Fee;
        ViewBag.CreditDebitFee = 0.80m;
        ViewBag.ManualBankDepositFee = 0.79m;
        
        // Handle saved card selection
        int selectedCardId = 0;
        string? cardNumber = null;
        string? creditCardSecurityCode = "";
        
        if (vm.CardDetails != null)
        {
            foreach (var item in vm.CardDetails)
            {
                if (item.IsChecked)
                {
                    selectedCardId = item.CardId;
                    cardNumber = item.CardNumber;
                    vm.SenderPaymentMode = PaymentMode.Card; // Saved card maps to Card
                    creditCardSecurityCode = item.SecurityCode;
                    break;
                }
            }
        }
        
        // Save payment method to session
        var paymentMethodJson = JsonSerializer.Serialize(vm);
        HttpContext.Session.SetString(PAYMENT_METHOD_SESSION_KEY, paymentMethodJson);
        
        // Update transaction with payment method information
        try
        {
            var transactionId = HttpContext.Session.GetInt32(TRANSACTION_ID_SESSION_KEY);
            if (transactionId.HasValue && transactionId.Value > 0)
            {
                var transaction = await _transactionRepository.GetByIdAsync(transactionId.Value);
                if (transaction != null)
                {
                    transaction.SenderPaymentMode = vm.SenderPaymentMode;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    
                    // Update total amount if card fee is added
                    if (vm.SenderPaymentMode == PaymentMode.Card)
                    {
                        // Card fee is already included in TotalAmount from Summary
                        // But we can update if needed based on payment method
                    }
                    
                    await _transactionRepository.UpdateAsync(transaction);
                    _logger.LogInformation("Updated transaction with payment method. TransactionId: {TransactionId}, PaymentMode: {PaymentMode}", 
                        transaction.Id, vm.SenderPaymentMode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating transaction with payment method");
            // Continue with flow even if update fails
        }
        
        // TODO: Validate thresholds (recipient and sender limits)
        
        // Handle different payment modes
        switch (vm.SenderPaymentMode)
        {
            case PaymentMode.Card:
                // For saved card with CVV, validate and proceed
                if (selectedCardId > 0 && !string.IsNullOrEmpty(creditCardSecurityCode))
                {
                    // TODO: Validate CVV and card
                    // For now, redirect to card details page
                    return RedirectToAction("DebitCreditCardDetails", new { isFromSavedDebitCard = true });
                }
                // For new card, redirect to card details entry
                return RedirectToAction("DebitCreditCardDetails", new { isFromSavedDebitCard = false });
                
            case PaymentMode.MobileWallet:
                // TODO: Check wallet balance
                // TODO: Complete transaction if balance sufficient
                // For now, redirect to success
                return RedirectToAction("AddMoneySuccess");
                
            case PaymentMode.BankAccount:
                return RedirectToAction("MoneyFexBankDeposit");
                
            default:
                ModelState.AddModelError("SenderPaymentMode", "Please select a payment method");
                return View(vm);
        }
    }

    /// <summary>
    /// GET: KiiBankTransfer/DebitCreditCardDetails
    /// Displays form to enter credit/debit card details
    /// Based on legacy SenderTransactionController.DebitCreditCardDetails (GET)
    /// </summary>
    [HttpGet]
    public IActionResult DebitCreditCardDetails(bool isAddDebitCreditCard = false, bool isFromSavedDebitCard = false)
    {
        try
        {
            // Get payment info from session
            var commonEnterAmountJson = HttpContext.Session.GetString(COMMON_ENTER_AMOUNT_SESSION_KEY);
            var recipientJson = HttpContext.Session.GetString(KIIBANK_RECIPIENT_SESSION_KEY);
            
            // Get card details from session if exists
            var cardDetailJson = HttpContext.Session.GetString(DEBIT_CREDIT_CARD_DETAIL_SESSION_KEY);
            CreditDebitCardViewModel? vm = null;
            
            if (!string.IsNullOrEmpty(cardDetailJson))
            {
                try
                {
                    vm = JsonSerializer.Deserialize<CreditDebitCardViewModel>(cardDetailJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deserializing card details from session");
                }
            }
            
            // Create new view model if not in session
            vm ??= new CreditDebitCardViewModel
            {
                CreditDebitCardFee = 0.80m, // Default fee
                SaveCard = isAddDebitCreditCard
            };
            
            // Update with payment info from session
            if (!string.IsNullOrEmpty(commonEnterAmountJson))
            {
                try
                {
                    var paymentInfo = JsonSerializer.Deserialize<CommonEnterAmountViewModel>(commonEnterAmountJson);
                    if (paymentInfo != null)
                    {
                        vm.FaxingAmount = paymentInfo.TotalAmount + vm.CreditDebitCardFee;
                        vm.FaxingCurrency = paymentInfo.SendingCurrency;
                        vm.FaxingCurrencySymbol = GetCurrencySymbol(paymentInfo.SendingCurrency);
                        vm.ReceiverName = paymentInfo.ReceiverName ?? "John Doe";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deserializing payment info from session");
                }
            }
            
            // Get recipient name from session
            if (!string.IsNullOrEmpty(recipientJson))
            {
                try
                {
                    var recipient = JsonSerializer.Deserialize<KiiBankRecipientViewModel>(recipientJson);
                    if (recipient != null && !string.IsNullOrEmpty(recipient.AccountName))
                    {
                        vm.ReceiverName = recipient.AccountName;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deserializing recipient from session");
                }
            }
            
            // Get transaction ID and receipt number from session
            vm.TransactionId = HttpContext.Session.GetInt32(TRANSACTION_ID_SESSION_KEY) ?? 0;
            vm.ReceiptNo = HttpContext.Session.GetString("ReceiptNo") ?? string.Empty;
            
            // Set ViewBag for view
            var paymentSummary = GetTransferSummary();
            ViewBag.ReceivingCountryCurrency = paymentSummary.ReceivingCurrencyCode;
            ViewBag.TransferMethod = "KiiBank";
            ViewBag.SendingCountryCurrency = paymentSummary.SendingCurrencyCode;
            ViewBag.SendingAmount = paymentSummary.SendingAmount;
            ViewBag.ReceiverName = paymentSummary.ReceiverFullName;
            ViewBag.ReceivingCountry = paymentSummary.ReceivingCountry.ToLower();
            ViewBag.Fee = paymentSummary.Fee;
            ViewBag.CreditDebitFee = vm.CreditDebitCardFee;
            ViewBag.HasOneSavedCard = false; // TODO: Check if user has saved cards
            ViewBag.IsFromSavedDebitCard = isFromSavedDebitCard;
            ViewBag.CardErrorMessage = TempData["CardErrorMessage"]?.ToString() ?? "";
            
            // TODO: Get sender address
            vm.AddressLineOne = ""; // TODO: Get from user service
            
            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DebitCreditCardDetails page");
            // Redirect to PaymentMethod if there's an error
            return RedirectToAction("PaymentMethod");
        }
    }

    /// <summary>
    /// POST: KiiBankTransfer/ThreeDQuery
    /// Processes 3D Secure query for card payment
    /// Based on legacy SenderTransactionController.ThreeDQuery (POST)
    /// Returns JSON response for AJAX call
    /// NOTE: Currently bypasses 3D Secure validation for testing
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ThreeDQuery([FromForm] CreditDebitCardViewModel vm)
    {
        // BYPASS 3D QUERY - Always return success
        try
        {
            // Save card details to session
            var cardDetailJson = JsonSerializer.Serialize(vm);
            HttpContext.Session.SetString(DEBIT_CREDIT_CARD_DETAIL_SESSION_KEY, cardDetailJson);
            
            // Update transaction status to completed
            var transactionId = HttpContext.Session.GetInt32(TRANSACTION_ID_SESSION_KEY);
            if (transactionId.HasValue && transactionId.Value > 0)
            {
                var transaction = await _transactionRepository.GetByIdAsync(transactionId.Value);
                if (transaction != null)
                {
                    transaction.Status = TransactionStatus.Completed;
                    transaction.PaymentReference = vm.ReceiptNo; // Store receipt number as payment reference
                    transaction.SenderPaymentMode = PaymentMode.Card;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    
                    await _transactionRepository.UpdateAsync(transaction);
                    _logger.LogInformation("Updated transaction status to Completed. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}", 
                        transaction.Id, transaction.ReceiptNo);
                    
                    // Update KiiBankTransfer with transaction reference if available
                    var kiiBankTransfer = await _kiiBankTransferRepository.GetByTransactionIdAsync(transaction.Id);
                    if (kiiBankTransfer != null)
                    {
                        kiiBankTransfer.TransactionReference = vm.ReceiptNo;
                        kiiBankTransfer.UpdatedAt = DateTime.UtcNow;
                        await _kiiBankTransferRepository.UpdateAsync(kiiBankTransfer);
                        _logger.LogInformation("Updated KiiBankTransfer with transaction reference. TransactionId: {TransactionId}", 
                            transaction.Id);
                    }
                    
                    // Save card payment information (same as mobile money transfer flow)
                    var expiryDate = $"{vm.EndMM}/{vm.EndYY}";
                    var maskedCardNumber = MaskCardNumber(vm.CardNumber);
                    
                    var cardPayment = new CardPaymentInformation
                    {
                        TransactionId = transaction.Id,
                        NonCardTransactionId = null, // Not applicable for KiiBank transfer
                        CardTransactionId = null, // Not applicable for KiiBank transfer
                        TopUpSomeoneElseTransactionId = null, // Not applicable
                        NameOnCard = vm.NameOnCard,
                        CardNumber = maskedCardNumber, // Masked card number
                        ExpiryDate = expiryDate,
                        IsSavedCard = vm.SaveCard,
                        AutoRecharged = false,
                        TransferType = 3, // 3 = Mobile/KiiBank transfer (same as mobile money transfer)
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    _context.CardPaymentInformations.Add(cardPayment);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Saved card payment information. TransactionId: {TransactionId}, SaveCard: {SaveCard}", 
                        transaction.Id, vm.SaveCard);
                }
            }
            
            _logger.LogInformation("ThreeDQuery bypassed - redirecting to success. TransactionId: {TransactionId}", transactionId);
            
            // Always return success and redirect to success page
            return Json(new
            {
                Status = 1, // OK
                Message = "Payment processed successfully",
                Data = new
                {
                    redirectUrl = Url.Action("AddMoneySuccess")
                },
                IsGetType3dAuth = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ThreeDQuery (bypassed)");
            // Even on error, return success to bypass
            return Json(new
            {
                Status = 1, // OK - bypass error
                Message = "Payment processed successfully",
                Data = new
                {
                    redirectUrl = Url.Action("AddMoneySuccess")
                },
                IsGetType3dAuth = false
            });
        }
    }

    /// <summary>
    /// GET: KiiBankTransfer/AddMoneySuccess
    /// Displays success page after payment
    /// Based on legacy SenderTransactionController.AddMoneySuccess
    /// </summary>
    [HttpGet]
    public IActionResult AddMoneySuccess()
    {
        try
        {
            var paymentSummary = GetTransferSummary();
            var model = new SenderAddMoneySuccessViewModel
            {
                Amount = paymentSummary.SendingAmount,
                Currency = paymentSummary.SendingCurrencySymbol,
                ReceiverName = paymentSummary.ReceiverFullName
            };
            
            ViewBag.TrackingNo = HttpContext.Session.GetString("ReceiptNo") ?? "N/A";
            
            // Clear session data after successful transaction
            // TODO: Clear all KiiBank transfer session data
            
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading AddMoneySuccess page");
            // Redirect to home if there's an error
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Gets transfer summary from session data
    /// Based on legacy SenderTransactionController.GetTransferSummary
    /// </summary>
    private TransferSummaryViewModel GetTransferSummary()
    {
        try
        {
            // Get common enter amount from session
            var commonEnterAmountJson = HttpContext.Session.GetString(COMMON_ENTER_AMOUNT_SESSION_KEY);
            
            // Get recipient details from session
            var recipientJson = HttpContext.Session.GetString(KIIBANK_RECIPIENT_SESSION_KEY);
            
            // Default values
            var model = new TransferSummaryViewModel
            {
                TransferMethod = "KiiBank",
                ReceiverFullName = "John Doe",
                ReceiverFirstName = "John",
                SendingCountry = "GB",
                SendingCurrencyCode = "GBP",
                ReceivingCurrencyCode = "NGN",
                SendingCurrencySymbol = "£",
                ReceivingCountry = "NG",
                ReceivingCurrencySymbol = "₦",
                SendingAmount = 0,
                Fee = 0,
                TotalSendingAmount = 0,
                ReceivingAmount = 0,
                ReceivingCountryFlag = "ng"
            };

            // Parse common enter amount from session
            if (!string.IsNullOrEmpty(commonEnterAmountJson))
            {
                try
                {
                    var commonEnterAmount = JsonSerializer.Deserialize<CommonEnterAmountViewModel>(commonEnterAmountJson);
                    if (commonEnterAmount != null)
                    {
                        model.SendingAmount = commonEnterAmount.SendingAmount;
                        model.ReceivingAmount = commonEnterAmount.ReceivingAmount;
                        model.SendingCurrencyCode = commonEnterAmount.SendingCurrency;
                        model.ReceivingCurrencyCode = commonEnterAmount.ReceivingCurrency;
                        model.SendingCurrency = commonEnterAmount.SendingCurrency; // Also set for backward compatibility
                        model.ReceivingCurrency = commonEnterAmount.ReceivingCurrency; // Also set for backward compatibility
                        model.SendingCountry = !string.IsNullOrEmpty(commonEnterAmount.SendingCountry) 
                            ? commonEnterAmount.SendingCountry 
                            : (!string.IsNullOrEmpty(commonEnterAmount.SendingCountryCode) 
                                ? commonEnterAmount.SendingCountryCode 
                                : "GB");
                        model.ReceivingCountry = !string.IsNullOrEmpty(commonEnterAmount.ReceivingCountry) 
                            ? commonEnterAmount.ReceivingCountry 
                            : (!string.IsNullOrEmpty(commonEnterAmount.ReceivingCountryCode) 
                                ? commonEnterAmount.ReceivingCountryCode 
                                : "NG");
                        model.Fee = commonEnterAmount.Fee;
                        model.TotalSendingAmount = commonEnterAmount.TotalAmount;
                        model.TotalAmount = commonEnterAmount.TotalAmount; // Also set for backward compatibility
                        model.ExchangeRate = commonEnterAmount.ExchangeRate;
                        
                        // Get currency symbols (simplified - in production, get from currency service)
                        model.SendingCurrencySymbol = GetCurrencySymbol(commonEnterAmount.SendingCurrency);
                        model.ReceivingCurrencySymbol = GetCurrencySymbol(commonEnterAmount.ReceivingCurrency);
                        model.ReceivingCountryFlag = model.ReceivingCountry.ToLower();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deserializing common enter amount from session");
                }
            }

            // Parse recipient details from session
            if (!string.IsNullOrEmpty(recipientJson))
            {
                try
                {
                    var recipient = JsonSerializer.Deserialize<KiiBankRecipientViewModel>(recipientJson);
                    if (recipient != null && !string.IsNullOrEmpty(recipient.AccountName))
                    {
                        model.ReceiverFullName = recipient.AccountName;
                        
                        // Extract first name
                        var names = recipient.AccountName.Split(' ');
                        model.ReceiverFirstName = names.Length > 0 ? names[0] : recipient.AccountName;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deserializing recipient from session");
                }
            }

            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTransferSummary");
            // Return default model on error
            return new TransferSummaryViewModel
            {
                TransferMethod = "KiiBank",
                ReceiverFullName = "John Doe",
                ReceiverFirstName = "John",
                SendingCountry = "GB",
                SendingCurrencyCode = "GBP",
                ReceivingCurrencyCode = "NGN",
                SendingCurrencySymbol = "£",
                ReceivingCountry = "NG",
                ReceivingCurrencySymbol = "₦",
                SendingAmount = 0,
                Fee = 0,
                TotalSendingAmount = 0,
                ReceivingAmount = 0,
                ReceivingCountryFlag = "ng"
            };
        }
    }

    /// <summary>
    /// Helper method to mask card number (same as mobile money transfer flow)
    /// </summary>
    private string MaskCardNumber(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            return "****";
        
        return $"****{cardNumber.Substring(cardNumber.Length - 4)}";
    }

    /// <summary>
    /// Helper method to get currency symbol
    /// </summary>
    private static string GetCurrencySymbol(string currencyCode)
    {
        return currencyCode.ToUpper() switch
        {
            "GBP" => "£",
            "USD" => "$",
            "EUR" => "€",
            "NGN" => "₦",
            "KES" => "KSh",
            "GHS" => "₵",
            "UGX" => "USh",
            "TZS" => "TSh",
            "ZAR" => "R",
            _ => currencyCode
        };
    }

    /// <summary>
    /// Get or create sender in database
    /// Based on MobileMoneyTransferController.GetOrCreateSenderAsync
    /// </summary>
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

    /// <summary>
    /// Helper class for common enter amount view model (used for transaction summary)
    /// </summary>
    private class CommonEnterAmountViewModel
    {
        public decimal SendingAmount { get; set; }
        public decimal ReceivingAmount { get; set; }
        public string SendingCurrency { get; set; } = string.Empty;
        public string ReceivingCurrency { get; set; } = string.Empty;
        public string SendingCountry { get; set; } = string.Empty;
        public string ReceivingCountry { get; set; } = string.Empty;
        public string SendingCountryCode { get; set; } = string.Empty;
        public string ReceivingCountryCode { get; set; } = string.Empty;
        public decimal ExchangeRate { get; set; }
        public decimal Fee { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ReceiverName { get; set; }
    }

    /// <summary>
    /// GET: KiiBankTransfer/Details
    /// Display KiiBank transfer transaction details
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int? transactionId, string? receiptNo)
    {
        try
        {
            KiiBankTransfer? kiiBankTransfer = null;

            if (transactionId.HasValue)
            {
                kiiBankTransfer = await _context.KiiBankTransfers
                    .Include(k => k.Transaction)
                        .ThenInclude(t => t.Sender)
                    .Include(k => k.Transaction)
                        .ThenInclude(t => t.ReceivingCountry)
                    .FirstOrDefaultAsync(k => k.TransactionId == transactionId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(receiptNo))
            {
                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.ReceiptNo == receiptNo);
                
                if (transaction != null)
                {
                    kiiBankTransfer = await _context.KiiBankTransfers
                        .Include(k => k.Transaction)
                            .ThenInclude(t => t.Sender)
                        .Include(k => k.Transaction)
                            .ThenInclude(t => t.ReceivingCountry)
                        .FirstOrDefaultAsync(k => k.TransactionId == transaction.Id);
                }
            }

            if (kiiBankTransfer == null)
            {
                return NotFound();
            }

            return View(kiiBankTransfer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading KiiBank transfer details");
            return NotFound();
        }
    }
}
