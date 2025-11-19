using MoneyFex.Core.Entities;
using MoneyFex.Core.Interfaces;

namespace MoneyFex.Infrastructure.Services;

public class KiiBankTransferService : IKiiBankTransferService
{
    private readonly IKiiBankTransferRepository _kiiBankTransferRepository;
    private readonly ITransactionRepository _transactionRepository;

    public KiiBankTransferService(
        IKiiBankTransferRepository kiiBankTransferRepository,
        ITransactionRepository transactionRepository)
    {
        _kiiBankTransferRepository = kiiBankTransferRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<KiiBankTransfer?> GetKiiBankTransferByTransactionIdAsync(int transactionId)
    {
        return await _kiiBankTransferRepository.GetByTransactionIdAsync(transactionId);
    }

    public async Task<KiiBankTransfer?> GetKiiBankTransferByReceiptNoAsync(string receiptNo)
    {
        var transaction = await _transactionRepository.GetByReceiptNoWithDetailsAsync(receiptNo);
        if (transaction == null)
            return null;

        return await _kiiBankTransferRepository.GetByTransactionIdAsync(transaction.Id);
    }

    public async Task<KiiBankTransfer?> GetKiiBankTransferByTransactionReferenceAsync(string transactionReference)
    {
        return await _kiiBankTransferRepository.GetByTransactionReferenceAsync(transactionReference);
    }

    public async Task<IEnumerable<KiiBankTransfer>> GetKiiBankTransfersByAccountNoAsync(string accountNo)
    {
        return await _kiiBankTransferRepository.GetByAccountNoAsync(accountNo);
    }
}

