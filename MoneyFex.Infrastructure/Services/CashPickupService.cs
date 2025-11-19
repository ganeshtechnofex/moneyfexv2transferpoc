using MoneyFex.Core.Entities;
using MoneyFex.Core.Interfaces;

namespace MoneyFex.Infrastructure.Services;

public class CashPickupService : ICashPickupService
{
    private readonly ICashPickupRepository _cashPickupRepository;
    private readonly ITransactionRepository _transactionRepository;

    public CashPickupService(
        ICashPickupRepository cashPickupRepository,
        ITransactionRepository transactionRepository)
    {
        _cashPickupRepository = cashPickupRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<CashPickup?> GetCashPickupByTransactionIdAsync(int transactionId)
    {
        return await _cashPickupRepository.GetByTransactionIdAsync(transactionId);
    }

    public async Task<CashPickup?> GetCashPickupByReceiptNoAsync(string receiptNo)
    {
        var transaction = await _transactionRepository.GetByReceiptNoWithDetailsAsync(receiptNo);
        if (transaction == null)
            return null;

        return await _cashPickupRepository.GetByTransactionIdAsync(transaction.Id);
    }

    public async Task<CashPickup?> GetCashPickupByMFCNAsync(string mfcn)
    {
        return await _cashPickupRepository.GetByMFCNAsync(mfcn);
    }

    public async Task<IEnumerable<CashPickup>> GetCashPickupsByRecipientIdAsync(int recipientId, int pageNumber = 1, int pageSize = 10)
    {
        return await _cashPickupRepository.GetByRecipientIdAsync(recipientId, pageNumber, pageSize);
    }
}

