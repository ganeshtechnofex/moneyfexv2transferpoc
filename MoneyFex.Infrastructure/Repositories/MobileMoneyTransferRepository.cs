using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Infrastructure.Repositories;

public class MobileMoneyTransferRepository : Repository<MobileMoneyTransfer>, IMobileMoneyTransferRepository
{
    public MobileMoneyTransferRepository(MoneyFexDbContext context) : base(context)
    {
    }

    public async Task<MobileMoneyTransfer?> GetByTransactionIdAsync(int transactionId)
    {
        return await _dbSet
            .Include(m => m.Transaction)
                .ThenInclude(t => t.Sender)
            .Include(m => m.Transaction)
                .ThenInclude(t => t.SendingCountry)
            .Include(m => m.Transaction)
                .ThenInclude(t => t.ReceivingCountry)
            .Include(m => m.WalletOperator)
            .FirstOrDefaultAsync(m => m.TransactionId == transactionId);
    }

    public async Task<IEnumerable<MobileMoneyTransfer>> GetByWalletOperatorIdAsync(int walletOperatorId, int pageNumber = 1, int pageSize = 10)
    {
        return await _dbSet
            .Where(m => m.WalletOperatorId == walletOperatorId)
            .Include(m => m.Transaction)
            .Include(m => m.WalletOperator)
            .OrderByDescending(m => m.Transaction.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<MobileMoneyTransfer>> GetByMobileNumberAsync(string mobileNumber)
    {
        return await _dbSet
            .Where(m => m.PaidToMobileNo == mobileNumber)
            .Include(m => m.Transaction)
            .Include(m => m.WalletOperator)
            .OrderByDescending(m => m.Transaction.TransactionDate)
            .ToListAsync();
    }
}

