using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Web.Services;

/// <summary>
/// Service for validating transaction limits
/// Based on legacy Common.HasExceededReceiverLimit and HasExceededSenderTransactionLimit
/// </summary>
public class TransactionLimitService
{
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<TransactionLimitService> _logger;

    public TransactionLimitService(
        MoneyFexDbContext context,
        ILogger<TransactionLimitService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Check if receiver has exceeded daily transaction limit
    /// </summary>
    public async Task<bool> HasExceededReceiverLimitAsync(
        int senderId,
        int? recipientId,
        string sendingCountry,
        string receivingCountry,
        TransactionType transferMethod)
    {
        try
        {
            // For POC, set default limits
            // In production, these should come from configuration/database
            const decimal maxReceiverDailyAmount = 10000m; // Max amount per receiver per day
            const int maxReceiverDailyCount = 10; // Max transactions per receiver per day

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Get transactions for this receiver today
            var receiverTransactions = await _context.Transactions
                .Where(t => t.SenderId == senderId &&
                           t.ReceivingCountryCode == receivingCountry &&
                           t.SendingCountryCode == sendingCountry &&
                           t.TransactionDate >= today &&
                           t.TransactionDate < tomorrow &&
                           t.Status != TransactionStatus.Cancelled &&
                           t.Status != TransactionStatus.Failed)
                .ToListAsync();

            // If recipientId is provided, filter by specific recipient
            if (recipientId.HasValue)
            {
                // For mobile transfers, check by mobile number
                var mobileTransfers = await _context.MobileMoneyTransfers
                    .Where(m => m.Transaction.SenderId == senderId &&
                               m.Transaction.ReceivingCountryCode == receivingCountry &&
                               m.Transaction.TransactionDate >= today &&
                               m.Transaction.TransactionDate < tomorrow)
                    .Select(m => m.TransactionId)
                    .ToListAsync();

                receiverTransactions = receiverTransactions
                    .Where(t => mobileTransfers.Contains(t.Id))
                    .ToList();
            }

            // Check count limit
            if (receiverTransactions.Count >= maxReceiverDailyCount)
            {
                _logger.LogWarning(
                    "Receiver daily transaction count limit exceeded. Count: {Count}, Limit: {Limit}",
                    receiverTransactions.Count, maxReceiverDailyCount);
                return true;
            }

            // Check amount limit
            var totalAmount = receiverTransactions.Sum(t => t.SendingAmount);
            if (totalAmount >= maxReceiverDailyAmount)
            {
                _logger.LogWarning(
                    "Receiver daily transaction amount limit exceeded. Amount: {Amount}, Limit: {Limit}",
                    totalAmount, maxReceiverDailyAmount);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking receiver transaction limit");
            return false; // Fail open for POC
        }
    }

    /// <summary>
    /// Check if sender has exceeded daily transaction limit
    /// </summary>
    public async Task<bool> HasExceededSenderTransactionLimitAsync(
        int senderId,
        string sendingCountry,
        string receivingCountry,
        TransactionType transferMethod)
    {
        try
        {
            // For POC, set default limits
            const decimal maxSenderDailyAmount = 50000m; // Max amount per sender per day
            const int maxSenderDailyCount = 50; // Max transactions per sender per day

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Get transactions for this sender today
            var senderTransactions = await _context.Transactions
                .Where(t => t.SenderId == senderId &&
                           t.ReceivingCountryCode == receivingCountry &&
                           t.SendingCountryCode == sendingCountry &&
                           t.TransactionDate >= today &&
                           t.TransactionDate < tomorrow &&
                           t.Status != TransactionStatus.Cancelled &&
                           t.Status != TransactionStatus.Failed)
                .ToListAsync();

            // Check count limit
            if (senderTransactions.Count >= maxSenderDailyCount)
            {
                _logger.LogWarning(
                    "Sender daily transaction count limit exceeded. Count: {Count}, Limit: {Limit}",
                    senderTransactions.Count, maxSenderDailyCount);
                return true;
            }

            // Check amount limit
            var totalAmount = senderTransactions.Sum(t => t.SendingAmount);
            if (totalAmount >= maxSenderDailyAmount)
            {
                _logger.LogWarning(
                    "Sender daily transaction amount limit exceeded. Amount: {Amount}, Limit: {Limit}",
                    totalAmount, maxSenderDailyAmount);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking sender transaction limit");
            return false; // Fail open for POC
        }
    }
}

