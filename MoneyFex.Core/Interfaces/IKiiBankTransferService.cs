using MoneyFex.Core.Entities;

namespace MoneyFex.Core.Interfaces;

public interface IKiiBankTransferService
{
    Task<KiiBankTransfer?> GetKiiBankTransferByTransactionIdAsync(int transactionId);
    Task<KiiBankTransfer?> GetKiiBankTransferByReceiptNoAsync(string receiptNo);
    Task<KiiBankTransfer?> GetKiiBankTransferByTransactionReferenceAsync(string transactionReference);
    Task<IEnumerable<KiiBankTransfer>> GetKiiBankTransfersByAccountNoAsync(string accountNo);
}

