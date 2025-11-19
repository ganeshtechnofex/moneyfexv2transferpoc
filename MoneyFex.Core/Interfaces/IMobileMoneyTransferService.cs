using MoneyFex.Core.Entities;

namespace MoneyFex.Core.Interfaces;

public interface IMobileMoneyTransferService
{
    Task<MobileMoneyTransfer?> GetMobileTransferByTransactionIdAsync(int transactionId);
    Task<MobileMoneyTransfer?> GetMobileTransferByReceiptNoAsync(string receiptNo);
    Task<IEnumerable<MobileMoneyTransfer>> GetMobileTransfersByWalletOperatorIdAsync(int walletOperatorId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<MobileMoneyTransfer>> GetMobileTransfersByMobileNumberAsync(string mobileNumber);
}

