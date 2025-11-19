using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Web.Controllers;

public class CustomerController : Controller
{
    private readonly ILogger<CustomerController> _logger;
    private readonly MoneyFexDbContext _context;
    private const string CUSTOMER_SESSION_KEY = "CustomerLoggedIn";

    public CustomerController(ILogger<CustomerController> logger, MoneyFexDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        if (!IsCustomerLoggedIn())
        {
            return RedirectToAction("Index", "CustomerLogin");
        }

        try
        {
            // Get sender ID from session (for POC, default to 1)
            var senderId = GetSenderIdFromSession();

            // Get statistics
            var today = DateTime.UtcNow.Date;
            var pendingTransactions = await _context.Transactions
                .Where(t => t.SenderId == senderId && 
                           (t.Status == TransactionStatus.PaymentPending || t.Status == TransactionStatus.InProgress))
                .CountAsync();

            var todayVolume = await _context.Transactions
                .Where(t => t.SenderId == senderId && 
                           t.TransactionDate.Date == today &&
                           t.Status == TransactionStatus.Paid)
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;

            // Get sender country for currency symbol
            var sender = await _context.Senders.FirstOrDefaultAsync(s => s.Id == senderId);
            var senderCountry = sender?.CountryCode ?? "GB";
            var country = await _context.Countries.FirstOrDefaultAsync(c => c.CountryCode == senderCountry);
            var currencySymbol = country?.CurrencySymbol ?? "£";

            // Compliance alerts (for POC, hardcoded to 0 - could check ID verification status)
            var complianceAlerts = 0; // TODO: Check sender document approval status

            // Get recent transactions for live activity feed
            var transactions = await _context.Transactions
                .Include(t => t.Sender)
                .Where(t => t.SenderId == senderId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(5)
                .ToListAsync();

            // Get transaction IDs
            var transactionIds = transactions.Select(t => t.Id).ToList();

            // Get bank deposits, mobile transfers, and cash pickups
            var bankDeposits = await _context.BankAccountDeposits
                .Where(bd => transactionIds.Contains(bd.TransactionId))
                .ToDictionaryAsync(bd => bd.TransactionId);

            var mobileTransfers = await _context.MobileMoneyTransfers
                .Where(mm => transactionIds.Contains(mm.TransactionId))
                .ToDictionaryAsync(mm => mm.TransactionId);

            var cashPickups = await _context.CashPickups
                .Include(cp => cp.Recipient)
                .Include(cp => cp.NonCardReceiver)
                .Where(cp => transactionIds.Contains(cp.TransactionId))
                .ToDictionaryAsync(cp => cp.TransactionId);

            // Build recent transactions list with receiver names
            var recentTransactions = transactions.Select(t => new
            {
                t.Id,
                t.ReceiptNo,
                t.Status,
                t.TransactionDate,
                t.ReceivingAmount,
                t.ReceivingCurrency,
                t.ReceivingCountryCode,
                ReceiverName = bankDeposits.ContainsKey(t.Id)
                    ? bankDeposits[t.Id].ReceiverName ?? "N/A"
                    : mobileTransfers.ContainsKey(t.Id)
                        ? mobileTransfers[t.Id].ReceiverName ?? "N/A"
                        : cashPickups.ContainsKey(t.Id)
                            ? (cashPickups[t.Id].RecipientId.HasValue
                                ? cashPickups[t.Id].Recipient?.ReceiverName ?? "N/A"
                                : cashPickups[t.Id].NonCardReceiverId.HasValue
                                    ? cashPickups[t.Id].NonCardReceiver?.FullName ?? "N/A"
                                    : "N/A")
                            : "N/A",
                TransactionType = t.TransactionModule == TransactionModule.Sender ? "Transfer" : "Other"
            }).ToList();

            // Prepare view data
            ViewBag.Username = HttpContext.Session.GetString("CustomerUsername") ?? sender?.FirstName ?? "Customer";
            ViewBag.PendingTransactions = pendingTransactions;
            ViewBag.TodayVolume = todayVolume;
            ViewBag.CurrencySymbol = currencySymbol;
            ViewBag.ComplianceAlerts = complianceAlerts;
            ViewBag.RecentTransactions = recentTransactions;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer dashboard");
            ViewBag.Username = HttpContext.Session.GetString("CustomerUsername") ?? "Customer";
            ViewBag.PendingTransactions = 0;
            ViewBag.TodayVolume = 0;
            ViewBag.CurrencySymbol = "£";
            ViewBag.ComplianceAlerts = 0;
            ViewBag.RecentTransactions = new List<object>();
            return View();
        }
    }

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

    private bool IsCustomerLoggedIn()
    {
        return HttpContext.Session.GetString(CUSTOMER_SESSION_KEY) == "true";
    }
}

