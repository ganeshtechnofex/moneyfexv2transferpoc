using MoneyFex.Core.Entities;

namespace MoneyFex.Core.Interfaces;

public interface IMobileMoneyTransferRepository : IRepository<MobileMoneyTransfer>
{
    Task<MobileMoneyTransfer?> GetByTransactionIdAsync(int transactionId);
    Task<IEnumerable<MobileMoneyTransfer>> GetByWalletOperatorIdAsync(int walletOperatorId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<MobileMoneyTransfer>> GetByMobileNumberAsync(string mobileNumber);
}

