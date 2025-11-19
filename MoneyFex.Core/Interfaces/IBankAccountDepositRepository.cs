using MoneyFex.Core.Entities;

namespace MoneyFex.Core.Interfaces;

public interface IBankAccountDepositRepository : IRepository<BankAccountDeposit>
{
    Task<BankAccountDeposit?> GetByTransactionIdAsync(int transactionId);
    Task<IEnumerable<BankAccountDeposit>> GetByBankIdAsync(int bankId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<BankAccountDeposit>> GetByReceiverAccountNoAsync(string accountNo);
}

