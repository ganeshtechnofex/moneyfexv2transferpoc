using MoneyFex.Core.Entities;

namespace MoneyFex.Core.Interfaces;

public interface IBankAccountDepositService
{
    Task<BankAccountDeposit?> GetBankDepositByTransactionIdAsync(int transactionId);
    Task<BankAccountDeposit?> GetBankDepositByReceiptNoAsync(string receiptNo);
    Task<IEnumerable<BankAccountDeposit>> GetBankDepositsByBankIdAsync(int bankId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<BankAccountDeposit>> GetBankDepositsByReceiverAccountNoAsync(string accountNo);
}

