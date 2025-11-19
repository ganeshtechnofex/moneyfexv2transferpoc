using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Services;

public class TransactionActivityService
{
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<TransactionActivityService> _logger;

    public TransactionActivityService(
        MoneyFexDbContext context,
        ILogger<TransactionActivityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the status of a transaction from the card processor API
    /// </summary>
    public async Task<PGTransactionResultViewModel> CheckPGStatusAsync(string receiptNo, int transactionServiceType, int? staffId = null)
    {
        try
        {
            var transaction = await _context.Transactions
                .Include(t => t.Sender)
                .FirstOrDefaultAsync(t => t.ReceiptNo == receiptNo);

            if (transaction == null)
            {
                return new PGTransactionResultViewModel
                {
                    Status = "Error",
                    Message = "Transaction not found",
                    Date = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"),
                    Reference = receiptNo,
                    Amount = "0"
                };
            }

            // Get payment information
            var cardPayment = await _context.CardPaymentInformations
                .FirstOrDefaultAsync(c => c.TransactionId == transaction.Id);

            // For POC, return transaction status from database
            // In production, this would call the actual payment gateway API
            var status = transaction.Status.ToString();
            var amount = $"{transaction.TotalAmount} {transaction.SendingCurrency}";
            var dateTime = transaction.TransactionDate.ToString("yyyy/MM/dd HH:mm:ss");

            // Log the check
            if (staffId.HasValue)
            {
                _logger.LogInformation("PG Status checked by staff {StaffId} for transaction {ReceiptNo}", 
                    staffId.Value, receiptNo);
            }

            return new PGTransactionResultViewModel
            {
                Status = status,
                Date = dateTime,
                Reference = transaction.PaymentReference ?? receiptNo,
                Amount = amount,
                Message = "Status retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking PG status for receipt {ReceiptNo}", receiptNo);
            return new PGTransactionResultViewModel
            {
                Status = "Error",
                Message = ex.Message,
                Date = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"),
                Reference = receiptNo,
                Amount = "0"
            };
        }
    }

    /// <summary>
    /// Gets status report from API service
    /// </summary>
    public async Task<StatusReportViewModel> GetStatusReportAsync(string identifier, int transactionServiceType, int? staffId = null)
    {
        try
        {
            var transaction = await _context.Transactions
                .Include(t => t.Sender)
                .Include(t => t.SendingCountry)
                .Include(t => t.ReceivingCountry)
                .FirstOrDefaultAsync(t => t.ReceiptNo == identifier);

            if (transaction == null)
            {
                return new StatusReportViewModel
                {
                    Status = "Not Found",
                    Message = "Transaction not found"
                };
            }

            // Get receiver information based on transaction type
            string receiverName = string.Empty;
            string receivingAccountNo = string.Empty;
            string payoutProviderName = string.Empty;

            switch (transactionServiceType)
            {
                case 1: // Mobile Wallet
                    var mobileTransfer = await _context.MobileMoneyTransfers
                        .Include(m => m.WalletOperator)
                        .FirstOrDefaultAsync(m => m.TransactionId == transaction.Id);
                    if (mobileTransfer != null)
                    {
                        receiverName = mobileTransfer.ReceiverName ?? string.Empty;
                        receivingAccountNo = mobileTransfer.PaidToMobileNo ?? string.Empty;
                        payoutProviderName = mobileTransfer.WalletOperator?.Name ?? string.Empty;
                    }
                    break;

                case 2: // Bank Deposit
                case 6: // Bank Deposit (alternative)
                    var bankDeposit = await _context.BankAccountDeposits
                        .Include(b => b.Bank)
                        .FirstOrDefaultAsync(b => b.TransactionId == transaction.Id);
                    if (bankDeposit != null)
                    {
                        receiverName = bankDeposit.ReceiverName ?? string.Empty;
                        receivingAccountNo = bankDeposit.ReceiverAccountNo ?? string.Empty;
                        payoutProviderName = bankDeposit.BankName ?? string.Empty;
                    }
                    break;

                case 3: // KiiBank
                    var kiiBankTransfer = await _context.KiiBankTransfers
                        .FirstOrDefaultAsync(k => k.TransactionId == transaction.Id);
                    if (kiiBankTransfer != null)
                    {
                        receiverName = kiiBankTransfer.ReceiverName ?? string.Empty;
                        receivingAccountNo = kiiBankTransfer.AccountNo ?? string.Empty;
                        payoutProviderName = "KiiBank";
                    }
                    break;

                case 5: // Cash Pickup
                    var cashPickup = await _context.CashPickups
                        .Include(c => c.Recipient)
                        .Include(c => c.NonCardReceiver)
                        .FirstOrDefaultAsync(c => c.TransactionId == transaction.Id);
                    if (cashPickup != null)
                    {
                        // CashPickup can have receiver name from Recipient or NonCardReceiver
                        if (cashPickup.Recipient != null)
                        {
                            receiverName = cashPickup.Recipient.ReceiverName ?? string.Empty;
                        }
                        else if (cashPickup.NonCardReceiver != null)
                        {
                            receiverName = cashPickup.NonCardReceiver.FullName ?? string.Empty;
                        }
                        receivingAccountNo = cashPickup.MFCN ?? string.Empty;
                    }
                    break;
            }

            var sendingCurrencySymbol = GetCurrencySymbol(transaction.SendingCurrency);

            return new StatusReportViewModel
            {
                Status = transaction.Status.ToString(),
                Amount = $"{sendingCurrencySymbol}{transaction.TotalAmount:N2}",
                Name = receiverName,
                AccountNo = receivingAccountNo,
                PayoutProvider = payoutProviderName,
                ReceiptNo = transaction.ReceiptNo,
                PayoutReference = transaction.PaymentReference ?? string.Empty,
                TransactionDate = transaction.TransactionDate.ToString("dd MMM yyyy HH:mm:ss"),
                Message = "Status retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status report for identifier {Identifier}", identifier);
            return new StatusReportViewModel
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Manually approve a transaction
    /// </summary>
    public async Task<ServiceResult<bool>> ManualApproveTransactionAsync(string receiptNo, int transactionServiceType, int? staffId = null)
    {
        try
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.ReceiptNo == receiptNo);

            if (transaction == null)
            {
                return new ServiceResult<bool>
                {
                    Data = false,
                    Status = ResultStatus.Error,
                    Message = "Transaction not found"
                };
            }

            // Check if transaction can be manually approved
            if (transaction.Status != TransactionStatus.Held && 
                transaction.Status != TransactionStatus.PaymentPending &&
                transaction.Status != TransactionStatus.IdCheckInProgress)
            {
                return new ServiceResult<bool>
                {
                    Data = false,
                    Status = ResultStatus.Error,
                    Message = "Transaction cannot be manually approved in current status"
                };
            }

            // Update transaction status
            transaction.Status = TransactionStatus.Paid;
            transaction.UpdatedByStaffId = staffId;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction {ReceiptNo} manually approved by staff {StaffId}", 
                receiptNo, staffId);

            return new ServiceResult<bool>
            {
                Data = true,
                Status = ResultStatus.OK,
                Message = "Transaction approved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error manually approving transaction {ReceiptNo}", receiptNo);
            return new ServiceResult<bool>
            {
                Data = false,
                Status = ResultStatus.Error,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Approve a held transaction
    /// </summary>
    public async Task<ServiceResult<bool>> ApproveHoldTransactionAsync(int transactionId, int transactionServiceType, int? staffId = null)
    {
        try
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return new ServiceResult<bool>
                {
                    Data = false,
                    Status = ResultStatus.Error,
                    Message = "Transaction not found"
                };
            }

            if (transaction.Status != TransactionStatus.Held)
            {
                return new ServiceResult<bool>
                {
                    Data = false,
                    Status = ResultStatus.Error,
                    Message = "Transaction is not in held status"
                };
            }

            // Update transaction status
            transaction.Status = TransactionStatus.Paid;
            transaction.UpdatedByStaffId = staffId;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Held transaction {TransactionId} approved by staff {StaffId}", 
                transactionId, staffId);

            return new ServiceResult<bool>
            {
                Data = true,
                Status = ResultStatus.OK,
                Message = "Transaction approved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving held transaction {TransactionId}", transactionId);
            return new ServiceResult<bool>
            {
                Data = false,
                Status = ResultStatus.Error,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// Cancel a transaction
    /// </summary>
    public async Task<ServiceResult<bool>> CancelTransactionAsync(int transactionId, int transactionServiceType, int? staffId = null)
    {
        try
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return new ServiceResult<bool>
                {
                    Data = false,
                    Status = ResultStatus.Error,
                    Message = "Transaction not found"
                };
            }

            // Check if transaction can be cancelled
            var cancellableStatuses = new[]
            {
                TransactionStatus.PaymentPending,
                TransactionStatus.InProgress,
                TransactionStatus.Held,
                TransactionStatus.IdCheckInProgress
            };

            if (!cancellableStatuses.Contains(transaction.Status))
            {
                return new ServiceResult<bool>
                {
                    Data = false,
                    Status = ResultStatus.Error,
                    Message = $"Transaction cannot be cancelled in {transaction.Status} status"
                };
            }

            // Update transaction status
            transaction.Status = TransactionStatus.Cancelled;
            transaction.UpdatedByStaffId = staffId;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction {TransactionId} cancelled by staff {StaffId}", 
                transactionId, staffId);

            return new ServiceResult<bool>
            {
                Data = true,
                Status = ResultStatus.OK,
                Message = "Transaction cancelled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transaction {TransactionId}", transactionId);
            return new ServiceResult<bool>
            {
                Data = false,
                Status = ResultStatus.Error,
                Message = ex.Message
            };
        }
    }

    private string GetCurrencySymbol(string currencyCode)
    {
        return currencyCode switch
        {
            "USD" => "$",
            "GBP" => "£",
            "EUR" => "€",
            "NGN" => "₦",
            "KES" => "KSh",
            "GHS" => "₵",
            "UGX" => "USh",
            "TZS" => "TSh",
            "ZAR" => "R",
            "INR" => "₹",
            "PKR" => "₨",
            "BDT" => "৳",
            "ETB" => "Br",
            "MAD" => "د.م.",
            "AUD" => "A$",
            "CAD" => "C$",
            _ => currencyCode
        };
    }
}

// View Models
public class PGTransactionResultViewModel
{
    public string Status { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class StatusReportViewModel
{
    public string Status { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string PayoutProvider { get; set; } = string.Empty;
    public string ReceiptNo { get; set; } = string.Empty;
    public string PayoutReference { get; set; } = string.Empty;
    public string TransactionDate { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ServiceResult<T>
{
    public T Data { get; set; } = default!;
    public ResultStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum ResultStatus
{
    OK = 1,
    Error = 0
}

