using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Interfaces;

namespace MoneyFex.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        return await _transactionRepository.GetByIdWithDetailsAsync(id);
    }

    public async Task<Transaction?> GetTransactionByReceiptNoAsync(string receiptNo)
    {
        return await _transactionRepository.GetByReceiptNoWithDetailsAsync(receiptNo);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsBySenderIdAsync(int senderId, int pageNumber = 1, int pageSize = 10)
    {
        return await _transactionRepository.GetBySenderIdAsync(senderId, pageNumber, pageSize);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime fromDate, DateTime toDate, int pageNumber = 1, int pageSize = 10)
    {
        return await _transactionRepository.GetByDateRangeAsync(fromDate, toDate, pageNumber, pageSize);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByStatusAsync(TransactionStatus status, int pageNumber = 1, int pageSize = 10)
    {
        return await _transactionRepository.GetByStatusAsync(status, pageNumber, pageSize);
    }

    public async Task<IEnumerable<Transaction>> SearchTransactionsAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
    {
        return await _transactionRepository.SearchAsync(searchTerm, pageNumber, pageSize);
    }
}

