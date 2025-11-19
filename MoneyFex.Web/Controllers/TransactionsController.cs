using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Web.Controllers;

public class TransactionsController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        MoneyFexDbContext context,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _context = context;
        _logger = logger;
    }

    // GET: Transactions
    public async Task<IActionResult> Index(
        int? senderId,
        DateTime? fromDate,
        DateTime? toDate,
        TransactionStatus? status,
        string? searchTerm,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            IEnumerable<Transaction> transactions;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                transactions = await _transactionService.SearchTransactionsAsync(searchTerm, pageNumber, pageSize);
            }
            else if (senderId.HasValue)
            {
                transactions = await _transactionService.GetTransactionsBySenderIdAsync(senderId.Value, pageNumber, pageSize);
            }
            else if (fromDate.HasValue && toDate.HasValue)
            {
                transactions = await _transactionService.GetTransactionsByDateRangeAsync(fromDate.Value, toDate.Value, pageNumber, pageSize);
            }
            else if (status.HasValue)
            {
                transactions = await _transactionService.GetTransactionsByStatusAsync(status.Value, pageNumber, pageSize);
            }
            else
            {
                // Default: last 30 days
                transactions = await _transactionService.GetTransactionsByDateRangeAsync(
                    DateTime.UtcNow.AddDays(-30),
                    DateTime.UtcNow,
                    pageNumber,
                    pageSize);
            }

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.SenderId = senderId;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Status = status;
            ViewBag.SearchTerm = searchTerm;

            return View(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading transactions");
            return View(new List<Transaction>());
        }
    }

    // GET: Transactions/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var transaction = await _transactionService.GetTransactionByIdAsync(id.Value);
            if (transaction == null)
            {
                return NotFound();
            }

            // Load transaction-specific data based on type
            var bankDeposit = await _context.BankAccountDeposits
                .Include(b => b.Bank)
                .FirstOrDefaultAsync(b => b.TransactionId == transaction.Id);

            var mobileTransfer = await _context.MobileMoneyTransfers
                .Include(m => m.WalletOperator)
                .FirstOrDefaultAsync(m => m.TransactionId == transaction.Id);

            var cashPickup = await _context.CashPickups
                .Include(c => c.Recipient)
                .Include(c => c.NonCardReceiver)
                .FirstOrDefaultAsync(c => c.TransactionId == transaction.Id);

            var kiiBankTransfer = await _context.KiiBankTransfers
                .FirstOrDefaultAsync(k => k.TransactionId == transaction.Id);

            ViewBag.BankDeposit = bankDeposit;
            ViewBag.MobileTransfer = mobileTransfer;
            ViewBag.CashPickup = cashPickup;
            ViewBag.KiiBankTransfer = kiiBankTransfer;

            // Determine transaction type
            string transactionType = "Unknown";
            if (bankDeposit != null) transactionType = "Bank Deposit";
            else if (mobileTransfer != null) transactionType = "Mobile Money Transfer";
            else if (cashPickup != null) transactionType = "Cash Pickup";
            else if (kiiBankTransfer != null) transactionType = "KiiBank Transfer";

            ViewBag.TransactionType = transactionType;

            return View(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading transaction details");
            return NotFound();
        }
    }

    // GET: Transactions/DetailsByReceipt/ABC123
    public async Task<IActionResult> DetailsByReceipt(string? receiptNo)
    {
        if (string.IsNullOrWhiteSpace(receiptNo))
        {
            return NotFound();
        }

        try
        {
            var transaction = await _transactionService.GetTransactionByReceiptNoAsync(receiptNo);
            if (transaction == null)
            {
                return NotFound();
            }

            return View("Details", transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading transaction by receipt number");
            return NotFound();
        }
    }
}

