using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Services;

public class TransferMoneyNowService
{
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<TransferMoneyNowService> _logger;

    public TransferMoneyNowService(MoneyFexDbContext context, ILogger<TransferMoneyNowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RecentTransferAndRecipientViewModel> GetRecentTransferAndRecipientsAsync(int senderId)
    {
        var recentTransfers = await GetRecentTransfersAsync(senderId);
        var recipients = await GetRecipientsAsync(senderId);
        var monthlyMeter = await GetMonthlyTransactionMeterAsync(senderId);

        return new RecentTransferAndRecipientViewModel
        {
            RecentTransfer = recentTransfers,
            Recipients = recipients,
            SenderMonthlyTransaction = monthlyMeter
        };
    }

    private async Task<List<RecentTransferViewModel>> GetRecentTransfersAsync(int senderId)
    {
        var currentDate = DateTime.UtcNow;
        var fiveDaysAgo = currentDate.AddDays(-30); // Get last 30 days for better coverage

        // Get bank deposits
        var bankDepositTransactionIds = await _context.BankAccountDeposits
            .Select(b => b.TransactionId)
            .ToListAsync();

        var bankDepositsData = await _context.Transactions
            .Include(t => t.Sender)
            .Where(t => t.SenderId == senderId &&
                       t.TransactionDate >= fiveDaysAgo &&
                       t.Status != TransactionStatus.Cancelled &&
                       bankDepositTransactionIds.Contains(t.Id))
            .Join(_context.BankAccountDeposits,
                t => t.Id,
                b => b.TransactionId,
                (t, b) => new { Transaction = t, BankDeposit = b })
            .Join(_context.Banks.DefaultIfEmpty(),
                tb => tb.BankDeposit.BankId,
                bank => bank != null ? bank.Id : (int?)null,
                (tb, bank) => new { tb.Transaction, tb.BankDeposit })
            .OrderByDescending(tb => tb.Transaction.TransactionDate)
            .Take(5)
            .ToListAsync();

        var bankDeposits = bankDepositsData.Select(tb => new RecentTransferViewModel
        {
            Id = tb.Transaction.Id,
            ReceiverName = tb.BankDeposit.ReceiverName ?? "N/A",
            ReceivingCurrency = tb.Transaction.ReceivingCurrency,
            ReceivingAmount = tb.Transaction.ReceivingAmount,
            StatusName = GetStatusName(tb.Transaction.Status),
            Date = tb.Transaction.TransactionDate.ToString("dd MMM yyyy"),
            TransactionDate = tb.Transaction.TransactionDate,
            TransactionServiceType = 2, // BankDeposit
            StatusOfBankDeposit = tb.Transaction.Status.ToString()
        }).ToList();

        // Get mobile transfers
        var mobileTransferTransactionIds = await _context.MobileMoneyTransfers
            .Select(m => m.TransactionId)
            .ToListAsync();

        var mobileTransfersData = await _context.Transactions
            .Include(t => t.Sender)
            .Where(t => t.SenderId == senderId &&
                       t.TransactionDate >= fiveDaysAgo &&
                       t.Status != TransactionStatus.Cancelled &&
                       mobileTransferTransactionIds.Contains(t.Id))
            .Join(_context.MobileMoneyTransfers,
                t => t.Id,
                m => m.TransactionId,
                (t, m) => new { Transaction = t, MobileTransfer = m })
            .Join(_context.MobileWalletOperators,
                tm => tm.MobileTransfer.WalletOperatorId,
                w => w.Id,
                (tm, w) => new { tm.Transaction, tm.MobileTransfer })
            .OrderByDescending(tm => tm.Transaction.TransactionDate)
            .Take(5)
            .ToListAsync();

        var mobileTransfers = mobileTransfersData.Select(tm => new RecentTransferViewModel
        {
            Id = tm.Transaction.Id,
            ReceiverName = tm.MobileTransfer.ReceiverName ?? "N/A",
            ReceivingCurrency = tm.Transaction.ReceivingCurrency,
            ReceivingAmount = tm.Transaction.ReceivingAmount,
            StatusName = GetStatusName(tm.Transaction.Status),
            Date = tm.Transaction.TransactionDate.ToString("dd MMM yyyy"),
            TransactionDate = tm.Transaction.TransactionDate,
            TransactionServiceType = 1, // MobileWallet
            StatusOfMobileTransfer = tm.Transaction.Status.ToString()
        }).ToList();

        // Get cash pickups
        var cashPickupTransactionIds = await _context.CashPickups
            .Select(c => c.TransactionId)
            .ToListAsync();

        var cashPickupsQuery = await _context.Transactions
            .Include(t => t.Sender)
            .Where(t => t.SenderId == senderId &&
                       t.TransactionDate >= fiveDaysAgo &&
                       t.Status != TransactionStatus.Cancelled &&
                       cashPickupTransactionIds.Contains(t.Id))
            .Join(_context.CashPickups,
                t => t.Id,
                c => c.TransactionId,
                (t, c) => new { Transaction = t, CashPickup = c })
            .ToListAsync();

        var cashPickups = new List<RecentTransferViewModel>();
        foreach (var item in cashPickupsQuery.Take(5))
        {
            string receiverName = "N/A";
            if (item.CashPickup.RecipientId.HasValue)
            {
                var recipient = await _context.Recipients
                    .FirstOrDefaultAsync(r => r.Id == item.CashPickup.RecipientId.Value);
                receiverName = recipient?.ReceiverName ?? "N/A";
            }
            else if (item.CashPickup.NonCardReceiverId.HasValue)
            {
                var receiver = await _context.ReceiverDetails
                    .FirstOrDefaultAsync(r => r.Id == item.CashPickup.NonCardReceiverId.Value);
                receiverName = receiver?.FullName ?? "N/A";
            }

            cashPickups.Add(new RecentTransferViewModel
            {
                Id = item.Transaction.Id,
                ReceiverName = receiverName,
                ReceivingCurrency = item.Transaction.ReceivingCurrency,
                ReceivingAmount = item.Transaction.ReceivingAmount,
                StatusName = GetStatusName(item.Transaction.Status),
                Date = item.Transaction.TransactionDate.ToString("dd MMM yyyy"),
                TransactionDate = item.Transaction.TransactionDate,
                TransactionServiceType = 5 // CashPickup
            });
        }

        // Combine and sort
        var allTransfers = new List<RecentTransferViewModel>();
        allTransfers.AddRange(bankDeposits);
        allTransfers.AddRange(mobileTransfers);
        allTransfers.AddRange(cashPickups);

        return allTransfers
            .OrderByDescending(t => t.TransactionDate)
            .Take(5)
            .ToList();
    }

    private async Task<List<RecipientViewModel>> GetRecipientsAsync(int senderId)
    {
        // Get recipients from recent transactions
        // Since Recipient entity is minimal, we'll extract from transactions
        var recipients = new List<RecipientViewModel>();

        // Get unique recipients from bank deposits
        var bankRecipients = await _context.Transactions
            .Where(t => t.SenderId == senderId)
            .Join(_context.BankAccountDeposits,
                t => t.Id,
                b => b.TransactionId,
                (t, b) => new { Transaction = t, BankDeposit = b })
            .GroupBy(tb => new
            {
                ReceiverName = tb.BankDeposit.ReceiverName,
                BankId = tb.BankDeposit.BankId,
                ReceivingCountry = tb.Transaction.ReceivingCountryCode
            })
            .Select(g => g.First())
            .Take(10)
            .ToListAsync();

        foreach (var item in bankRecipients)
        {
            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == item.Transaction.ReceivingCountryCode);
            
            recipients.Add(new RecipientViewModel
            {
                Id = item.BankDeposit.TransactionId, // Use transaction ID as recipient ID
                SenderId = senderId,
                Service = "BankAccount",
                ServiceName = "Bank Account",
                ReceiverName = item.BankDeposit.ReceiverName ?? "N/A",
                Country = item.Transaction.ReceivingCountryCode ?? "",
                Currency = country?.Currency ?? "",
                ReceiverCountryLower = item.Transaction.ReceivingCountryCode?.ToLower() ?? "",
                ReceiverFirstLetter = !string.IsNullOrEmpty(item.BankDeposit.ReceiverName)
                    ? item.BankDeposit.ReceiverName[0].ToString()
                    : "N",
                BankId = item.BankDeposit.BankId,
                BankName = item.BankDeposit.BankName,
                AccountNo = item.BankDeposit.ReceiverAccountNo
            });
        }

        // Get unique recipients from mobile transfers
        var mobileRecipients = await _context.Transactions
            .Where(t => t.SenderId == senderId)
            .Join(_context.MobileMoneyTransfers,
                t => t.Id,
                m => m.TransactionId,
                (t, m) => new { Transaction = t, MobileTransfer = m })
            .Join(_context.MobileWalletOperators,
                tm => tm.MobileTransfer.WalletOperatorId,
                w => w.Id,
                (tm, w) => new { tm.Transaction, tm.MobileTransfer, WalletOperator = w })
            .GroupBy(tmw => new
            {
                ReceiverName = tmw.MobileTransfer.ReceiverName,
                WalletOperatorId = tmw.MobileTransfer.WalletOperatorId,
                ReceivingCountry = tmw.Transaction.ReceivingCountryCode
            })
            .Select(g => g.First())
            .Take(10)
            .ToListAsync();

        foreach (var item in mobileRecipients)
        {
            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == item.Transaction.ReceivingCountryCode);
            
            recipients.Add(new RecipientViewModel
            {
                Id = item.MobileTransfer.TransactionId, // Use transaction ID as recipient ID
                SenderId = senderId,
                Service = "MobileWallet",
                ServiceName = "Mobile Wallet",
                ReceiverName = item.MobileTransfer.ReceiverName ?? "N/A",
                Country = item.Transaction.ReceivingCountryCode ?? "",
                Currency = country?.Currency ?? "",
                ReceiverCountryLower = item.Transaction.ReceivingCountryCode?.ToLower() ?? "",
                ReceiverFirstLetter = !string.IsNullOrEmpty(item.MobileTransfer.ReceiverName)
                    ? item.MobileTransfer.ReceiverName[0].ToString()
                    : "N",
                MobileWalletProvider = item.MobileTransfer.WalletOperatorId,
                MobileWalletProviderName = item.WalletOperator.Name,
                MobileNo = item.MobileTransfer.PaidToMobileNo
            });
        }

        // Get unique recipients from cash pickups
        var cashRecipients = await _context.Transactions
            .Where(t => t.SenderId == senderId)
            .Join(_context.CashPickups,
                t => t.Id,
                c => c.TransactionId,
                (t, c) => new { Transaction = t, CashPickup = c })
            .GroupBy(tc => new
            {
                RecipientId = tc.CashPickup.RecipientId,
                NonCardReceiverId = tc.CashPickup.NonCardReceiverId,
                ReceivingCountry = tc.Transaction.ReceivingCountryCode
            })
            .Select(g => g.First())
            .Take(10)
            .ToListAsync();

        foreach (var item in cashRecipients)
        {
            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.CountryCode == item.Transaction.ReceivingCountryCode);
            
            string receiverName = "N/A";
            if (item.CashPickup.RecipientId.HasValue)
            {
                var recipient = await _context.Recipients
                    .FirstOrDefaultAsync(r => r.Id == item.CashPickup.RecipientId.Value);
                receiverName = recipient?.ReceiverName ?? "N/A";
            }
            else if (item.CashPickup.NonCardReceiverId.HasValue)
            {
                var receiver = await _context.ReceiverDetails
                    .FirstOrDefaultAsync(r => r.Id == item.CashPickup.NonCardReceiverId.Value);
                receiverName = receiver?.FullName ?? "N/A";
            }
            
            recipients.Add(new RecipientViewModel
            {
                Id = item.CashPickup.TransactionId, // Use transaction ID as recipient ID
                SenderId = senderId,
                Service = "CashPickUP",
                ServiceName = "Cash Pickup",
                ReceiverName = receiverName,
                Country = item.Transaction.ReceivingCountryCode ?? "",
                Currency = country?.Currency ?? "",
                ReceiverCountryLower = item.Transaction.ReceivingCountryCode?.ToLower() ?? "",
                ReceiverFirstLetter = !string.IsNullOrEmpty(receiverName) && receiverName != "N/A"
                    ? receiverName[0].ToString()
                    : "N"
            });
        }

        return recipients
            .OrderByDescending(r => r.Id)
            .Take(10)
            .ToList();
    }

    private async Task<SenderMonthlyTransactionMeterViewModel> GetMonthlyTransactionMeterAsync(int senderId)
    {
        try
        {
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;

            // Get sender to determine currency
            var sender = await _context.Senders
                .Include(s => s.Country)
                .FirstOrDefaultAsync(s => s.Id == senderId);

            if (sender == null)
            {
                return new SenderMonthlyTransactionMeterViewModel
                {
                    SenderMonthlyTransactionMeterBalance = 0,
                    SenderCurrencySymbol = "£"
                };
            }

            // Calculate monthly transaction meter from all transaction types
            var monthlyTotal = await _context.Transactions
                .Where(t => t.SenderId == senderId &&
                           t.TransactionDate.Year == currentYear &&
                           t.TransactionDate.Month == currentMonth &&
                           (t.Status == TransactionStatus.Paid ||
                            t.Status == TransactionStatus.Completed ||
                            t.Status == TransactionStatus.Received))
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;

            var currencySymbol = sender.Country?.CurrencySymbol ?? "£";

            return new SenderMonthlyTransactionMeterViewModel
            {
                SenderMonthlyTransactionMeterBalance = monthlyTotal,
                SenderCurrencySymbol = currencySymbol
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating monthly transaction meter for sender {SenderId}", senderId);
            return new SenderMonthlyTransactionMeterViewModel
            {
                SenderMonthlyTransactionMeterBalance = 0,
                SenderCurrencySymbol = "£"
            };
        }
    }

    private static string GetStatusName(TransactionStatus status)
    {
        return status switch
        {
            TransactionStatus.Paid => "Paid",
            TransactionStatus.Completed => "Completed",
            TransactionStatus.Received => "Received",
            TransactionStatus.PaymentPending => "Payment Pending",
            TransactionStatus.InProgress => "In Progress",
            TransactionStatus.Cancelled => "Cancelled",
            TransactionStatus.Failed => "Failed",
            TransactionStatus.Held => "Held",
            _ => status.ToString()
        };
    }
}

