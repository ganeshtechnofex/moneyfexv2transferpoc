using MoneyFex.Core.Entities;

namespace MoneyFex.Core.Interfaces;

public interface ICashPickupService
{
    Task<CashPickup?> GetCashPickupByTransactionIdAsync(int transactionId);
    Task<CashPickup?> GetCashPickupByReceiptNoAsync(string receiptNo);
    Task<CashPickup?> GetCashPickupByMFCNAsync(string mfcn);
    Task<IEnumerable<CashPickup>> GetCashPickupsByRecipientIdAsync(int recipientId, int pageNumber = 1, int pageSize = 10);
}

