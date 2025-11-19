using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Web.Controllers;

public class StaffController : Controller
{
    private readonly ILogger<StaffController> _logger;
    private readonly MoneyFexDbContext _context;
    private const string STAFF_SESSION_KEY = "StaffLoggedIn";

    public StaffController(ILogger<StaffController> logger, MoneyFexDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        // Check if staff is logged in
        if (!IsStaffLoggedIn())
        {
            return RedirectToAction("Index", "StaffLogin");
        }

        try
        {
            // Get statistics for all transactions (staff view)
            var today = DateTime.UtcNow.Date;
            
            // Pending transactions (all customers)
            var pendingTransactions = await _context.Transactions
                .Where(t => t.Status == TransactionStatus.PaymentPending || t.Status == TransactionStatus.InProgress)
                .CountAsync();

            // Today's volume (all paid transactions)
            var todayVolume = await _context.Transactions
                .Where(t => t.TransactionDate.Date == today && t.Status == TransactionStatus.Paid)
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;

            // Get default currency symbol (for POC, use GBP)
            var defaultCountry = await _context.Countries.FirstOrDefaultAsync(c => c.CountryCode == "GB");
            var currencySymbol = defaultCountry?.CurrencySymbol ?? "£";

            // Compliance alerts (for POC, hardcoded - could check ID verification status)
            var complianceAlerts = 0; // TODO: Check pending ID verifications

            // Get recent transactions for live activity feed (all customers)
            var transactions = await _context.Transactions
                .Include(t => t.Sender)
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
            ViewBag.Username = HttpContext.Session.GetString("StaffUsername") ?? "Admin";
            ViewBag.PendingTransactions = pendingTransactions;
            ViewBag.TodayVolume = todayVolume;
            ViewBag.CurrencySymbol = currencySymbol;
            ViewBag.ComplianceAlerts = complianceAlerts;
            ViewBag.RecentTransactions = recentTransactions;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading staff dashboard");
            ViewBag.Username = HttpContext.Session.GetString("StaffUsername") ?? "Admin";
            ViewBag.PendingTransactions = 0;
            ViewBag.TodayVolume = 0;
            ViewBag.CurrencySymbol = "£";
            ViewBag.ComplianceAlerts = 0;
            ViewBag.RecentTransactions = new List<object>();
            return View();
        }
    }

    private bool IsStaffLoggedIn()
    {
        return HttpContext.Session.GetString(STAFF_SESSION_KEY) == "true";
    }
}

