using Common.Kafka.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Messages;
using MoneyFex.Infrastructure.Data;
using System.Text.Json;

namespace MoneyFex.Web.Services;

/// <summary>
/// Handler for processing transfer queue messages from Kafka
/// </summary>
public class TransferProcessingHandler : IKafkaHandler<string, TransferQueueMessage>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransferProcessingHandler> _logger;

    public TransferProcessingHandler(
        IServiceProvider serviceProvider,
        ILogger<TransferProcessingHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task HandleAsync(string key, TransferQueueMessage value)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MoneyFexDbContext>();

        try
        {
            _logger.LogInformation(
                "Processing transfer. TransactionId: {TransactionId}, Type: {Type}, RetryCount: {RetryCount}",
                value.TransactionId, value.TransferType, value.RetryCount);

            // Get transaction from database
            var transaction = await context.Transactions
                .FirstOrDefaultAsync(t => t.Id == value.TransactionId);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found. TransactionId: {TransactionId}", value.TransactionId);
                return;
            }

            // Update status to InProgress if not already
            if (transaction.Status == TransactionStatus.PaymentPending)
            {
                transaction.Status = TransactionStatus.InProgress;
                transaction.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            // Process based on transfer type
            var success = value.TransferType switch
            {
                TransferType.MobileMoneyTransfer => await ProcessMobileMoneyTransferAsync(
                    context, transaction, value),
                TransferType.BankAccountDeposit => await ProcessBankAccountDepositAsync(
                    context, transaction, value),
                TransferType.CashPickup => await ProcessCashPickupAsync(
                    context, transaction, value),
                TransferType.KiiBankTransfer => await ProcessKiiBankTransferAsync(
                    context, transaction, value),
                _ => false
            };

            if (success)
            {
                transaction.Status = TransactionStatus.Completed;
                transaction.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Transfer processed successfully. TransactionId: {TransactionId}",
                    value.TransactionId);
            }
            else
            {
                await HandleTransferFailureAsync(context, transaction, value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing transfer. TransactionId: {TransactionId}",
                value.TransactionId);
            
            // Mark transaction as failed
            try
            {
                var transaction = await context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == value.TransactionId);
                
                if (transaction != null)
                {
                    transaction.Status = TransactionStatus.Failed;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to update transaction status after error");
            }
            
            throw;
        }
    }

    private async Task<bool> ProcessMobileMoneyTransferAsync(
        MoneyFexDbContext context,
        Transaction transaction,
        TransferQueueMessage message)
    {
        try
        {
            var mobileTransfer = await context.MobileMoneyTransfers
                .FirstOrDefaultAsync(m => m.TransactionId == transaction.Id);

            if (mobileTransfer == null)
            {
                _logger.LogWarning(
                    "Mobile money transfer not found. TransactionId: {TransactionId}",
                    transaction.Id);
                return false;
            }

            // ============================================================
            // STEP 1: Currency Conversion (if needed)
            // ============================================================
            // Note: Exchange rate is usually calculated before payment,
            // but if real-time conversion is needed, do it here:
            // 
            // var exchangeRateService = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
            // var conversion = await exchangeRateService.GetRealTimeRateAsync(
            //     transaction.SendingCurrency, 
            //     transaction.ReceivingCurrency);
            // 
            // if (conversion == null)
            // {
            //     _logger.LogError("Failed to get exchange rate. TransactionId: {TransactionId}", transaction.Id);
            //     return false;
            // }
            // 
            // transaction.ReceivingAmount = transaction.SendingAmount * conversion.Rate;
            // transaction.ExchangeRate = conversion.Rate;

            // ============================================================
            // STEP 2: Cash Out - Call Mobile Wallet API Provider
            // ============================================================
            // This is where you integrate with external wallet APIs:
            // - MTN Mobile Money API
            // - Airtel Money API
            // - Orange Money API
            // - M-Pesa API
            // etc.
            
            // Example implementation:
            // var walletOperator = await context.MobileWalletOperators
            //     .FirstOrDefaultAsync(w => w.Id == mobileTransfer.WalletOperatorId);
            // 
            // var walletApiService = scope.ServiceProvider.GetRequiredService<IWalletApiService>();
            // 
            // var cashOutRequest = new WalletCashOutRequest
            // {
            //     WalletOperator = walletOperator.Code, // e.g., "MTN", "AIRTEL"
            //     MobileNumber = mobileTransfer.PaidToMobileNo,
            //     Amount = transaction.ReceivingAmount,
            //     Currency = transaction.ReceivingCurrency,
            //     TransactionId = transaction.Id,
            //     ReceiptNo = transaction.ReceiptNo,
            //     ReceiverName = mobileTransfer.ReceiverName
            // };
            // 
            // var apiResponse = await walletApiService.CashOutAsync(cashOutRequest);
            // 
            // if (!apiResponse.Success)
            // {
            //     _logger.LogWarning(
            //         "Wallet API cash out failed. TransactionId: {TransactionId}, Error: {Error}",
            //         transaction.Id, apiResponse.ErrorMessage);
            //     return false;
            // }

            // For now, simulate API call delay
            await Task.Delay(1000);

            // ============================================================
            // STEP 3: Update Transaction with Transfer Reference
            // ============================================================
            // Save the reference from the wallet API response
            // transaction.TransferReference = apiResponse.TransferReference;
            transaction.TransferReference = $"MMT-{transaction.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            transaction.Status = TransactionStatus.Completed;
            transaction.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Mobile money transfer processed successfully. TransactionId: {TransactionId}, Reference: {Reference}",
                transaction.Id, transaction.TransferReference);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing mobile money transfer. TransactionId: {TransactionId}",
                transaction.Id);
            return false;
        }
    }

    private async Task<bool> ProcessBankAccountDepositAsync(
        MoneyFexDbContext context,
        Transaction transaction,
        TransferQueueMessage message)
    {
        try
        {
            var bankDeposit = await context.BankAccountDeposits
                .FirstOrDefaultAsync(b => b.TransactionId == transaction.Id);

            if (bankDeposit == null)
            {
                _logger.LogWarning(
                    "Bank account deposit not found. TransactionId: {TransactionId}",
                    transaction.Id);
                return false;
            }

            // TODO: Call external bank API here
            await Task.Delay(1000);

            transaction.TransferReference = $"BAD-{transaction.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            transaction.Status = TransactionStatus.Completed;
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Bank account deposit processed. TransactionId: {TransactionId}",
                transaction.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing bank account deposit. TransactionId: {TransactionId}",
                transaction.Id);
            return false;
        }
    }

    private async Task<bool> ProcessCashPickupAsync(
        MoneyFexDbContext context,
        Transaction transaction,
        TransferQueueMessage message)
    {
        try
        {
            var cashPickup = await context.CashPickups
                .FirstOrDefaultAsync(c => c.TransactionId == transaction.Id);

            if (cashPickup == null)
            {
                _logger.LogWarning(
                    "Cash pickup not found. TransactionId: {TransactionId}",
                    transaction.Id);
                return false;
            }

            // TODO: Call external cash pickup API here
            await Task.Delay(1000);

            transaction.TransferReference = $"CPU-{transaction.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            transaction.Status = TransactionStatus.Completed;
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Cash pickup processed. TransactionId: {TransactionId}",
                transaction.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing cash pickup. TransactionId: {TransactionId}",
                transaction.Id);
            return false;
        }
    }

    private async Task<bool> ProcessKiiBankTransferAsync(
        MoneyFexDbContext context,
        Transaction transaction,
        TransferQueueMessage message)
    {
        try
        {
            var kiiBankTransfer = await context.KiiBankTransfers
                .FirstOrDefaultAsync(k => k.TransactionId == transaction.Id);

            if (kiiBankTransfer == null)
            {
                _logger.LogWarning(
                    "KiiBank transfer not found. TransactionId: {TransactionId}",
                    transaction.Id);
                return false;
            }

            // TODO: Call external KiiBank API here
            await Task.Delay(1000);

            transaction.TransferReference = $"KBT-{transaction.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            transaction.Status = TransactionStatus.Completed;
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "KiiBank transfer processed. TransactionId: {TransactionId}",
                transaction.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing KiiBank transfer. TransactionId: {TransactionId}",
                transaction.Id);
            return false;
        }
    }

    private async Task HandleTransferFailureAsync(
        MoneyFexDbContext context,
        Transaction transaction,
        TransferQueueMessage message)
    {
        message.RetryCount++;

        if (message.RetryCount <= 3)
        {
            _logger.LogWarning(
                "Transfer failed, will retry. TransactionId: {TransactionId}, RetryCount: {RetryCount}",
                transaction.Id, message.RetryCount);

            transaction.Status = TransactionStatus.Failed;
            transaction.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Note: Re-queuing would require re-producing the message
            // You might want to implement a retry mechanism with exponential backoff
        }
        else
        {
            _logger.LogError(
                "Transfer failed after max retries. TransactionId: {TransactionId}",
                transaction.Id);

            transaction.Status = TransactionStatus.Failed;
            transaction.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // TODO: Move to dead letter queue or notify administrators
        }
    }
}

