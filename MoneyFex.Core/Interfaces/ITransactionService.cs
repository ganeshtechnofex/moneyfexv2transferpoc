using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Core.Interfaces;

public interface ITransactionService
{
    Task<Transaction?> GetTransactionByIdAsync(int id);
    Task<Transaction?> GetTransactionByReceiptNoAsync(string receiptNo);
    Task<IEnumerable<Transaction>> GetTransactionsBySenderIdAsync(int senderId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime fromDate, DateTime toDate, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<Transaction>> GetTransactionsByStatusAsync(TransactionStatus status, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<Transaction>> SearchTransactionsAsync(string searchTerm, int pageNumber = 1, int pageSize = 10);
}

