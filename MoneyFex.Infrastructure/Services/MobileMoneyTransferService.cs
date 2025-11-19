using MoneyFex.Core.Entities;
using MoneyFex.Core.Interfaces;

namespace MoneyFex.Infrastructure.Services;

public class MobileMoneyTransferService : IMobileMoneyTransferService
{
    private readonly IMobileMoneyTransferRepository _mobileTransferRepository;
    private readonly ITransactionRepository _transactionRepository;

    public MobileMoneyTransferService(
        IMobileMoneyTransferRepository mobileTransferRepository,
        ITransactionRepository transactionRepository)
    {
        _mobileTransferRepository = mobileTransferRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<MobileMoneyTransfer?> GetMobileTransferByTransactionIdAsync(int transactionId)
    {
        return await _mobileTransferRepository.GetByTransactionIdAsync(transactionId);
    }

    public async Task<MobileMoneyTransfer?> GetMobileTransferByReceiptNoAsync(string receiptNo)
    {
        var transaction = await _transactionRepository.GetByReceiptNoWithDetailsAsync(receiptNo);
        if (transaction == null)
            return null;

        return await _mobileTransferRepository.GetByTransactionIdAsync(transaction.Id);
    }

    public async Task<IEnumerable<MobileMoneyTransfer>> GetMobileTransfersByWalletOperatorIdAsync(int walletOperatorId, int pageNumber = 1, int pageSize = 10)
    {
        return await _mobileTransferRepository.GetByWalletOperatorIdAsync(walletOperatorId, pageNumber, pageSize);
    }

    public async Task<IEnumerable<MobileMoneyTransfer>> GetMobileTransfersByMobileNumberAsync(string mobileNumber)
    {
        return await _mobileTransferRepository.GetByMobileNumberAsync(mobileNumber);
    }
}

