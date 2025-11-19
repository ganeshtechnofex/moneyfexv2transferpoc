using MoneyFex.Core.Entities;

namespace MoneyFex.Core.Interfaces;

public interface IKiiBankTransferRepository : IRepository<KiiBankTransfer>
{
    Task<KiiBankTransfer?> GetByTransactionIdAsync(int transactionId);
    Task<KiiBankTransfer?> GetByTransactionReferenceAsync(string transactionReference);
    Task<IEnumerable<KiiBankTransfer>> GetByAccountNoAsync(string accountNo);
}

