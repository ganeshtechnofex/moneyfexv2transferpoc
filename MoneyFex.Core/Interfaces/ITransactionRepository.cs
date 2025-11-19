using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Core.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetBySenderIdAsync(int senderId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<Transaction>> GetByStatusAsync(TransactionStatus status, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<Transaction>> GetByReceiptNoAsync(string receiptNo);
    Task<Transaction?> GetByReceiptNoWithDetailsAsync(string receiptNo);
    Task<Transaction?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Transaction>> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 10);
}

