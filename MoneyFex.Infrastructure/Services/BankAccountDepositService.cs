using MoneyFex.Core.Entities;
using MoneyFex.Core.Interfaces;

namespace MoneyFex.Infrastructure.Services;

public class BankAccountDepositService : IBankAccountDepositService
{
    private readonly IBankAccountDepositRepository _bankDepositRepository;
    private readonly ITransactionRepository _transactionRepository;

    public BankAccountDepositService(
        IBankAccountDepositRepository bankDepositRepository,
        ITransactionRepository transactionRepository)
    {
        _bankDepositRepository = bankDepositRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<BankAccountDeposit?> GetBankDepositByTransactionIdAsync(int transactionId)
    {
        return await _bankDepositRepository.GetByTransactionIdAsync(transactionId);
    }

    public async Task<BankAccountDeposit?> GetBankDepositByReceiptNoAsync(string receiptNo)
    {
        var transaction = await _transactionRepository.GetByReceiptNoWithDetailsAsync(receiptNo);
        if (transaction == null)
            return null;

        return await _bankDepositRepository.GetByTransactionIdAsync(transaction.Id);
    }

    public async Task<IEnumerable<BankAccountDeposit>> GetBankDepositsByBankIdAsync(int bankId, int pageNumber = 1, int pageSize = 10)
    {
        return await _bankDepositRepository.GetByBankIdAsync(bankId, pageNumber, pageSize);
    }

    public async Task<IEnumerable<BankAccountDeposit>> GetBankDepositsByReceiverAccountNoAsync(string accountNo)
    {
        return await _bankDepositRepository.GetByReceiverAccountNoAsync(accountNo);
    }
}

