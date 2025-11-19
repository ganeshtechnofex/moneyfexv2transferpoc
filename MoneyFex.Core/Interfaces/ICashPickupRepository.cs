using MoneyFex.Core.Entities;

namespace MoneyFex.Core.Interfaces;

public interface ICashPickupRepository : IRepository<CashPickup>
{
    Task<CashPickup?> GetByTransactionIdAsync(int transactionId);
    Task<CashPickup?> GetByMFCNAsync(string mfcn);
    Task<IEnumerable<CashPickup>> GetByRecipientIdAsync(int recipientId, int pageNumber = 1, int pageSize = 10);
}

