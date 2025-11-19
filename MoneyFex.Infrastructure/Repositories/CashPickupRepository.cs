using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Infrastructure.Repositories;

public class CashPickupRepository : Repository<CashPickup>, ICashPickupRepository
{
    public CashPickupRepository(MoneyFexDbContext context) : base(context)
    {
    }

    public async Task<CashPickup?> GetByTransactionIdAsync(int transactionId)
    {
        return await _dbSet
            .Include(c => c.Transaction)
                .ThenInclude(t => t.Sender)
            .Include(c => c.Transaction)
                .ThenInclude(t => t.SendingCountry)
            .Include(c => c.Transaction)
                .ThenInclude(t => t.ReceivingCountry)
            .Include(c => c.Recipient)
            .Include(c => c.NonCardReceiver)
            .FirstOrDefaultAsync(c => c.TransactionId == transactionId);
    }

    public async Task<CashPickup?> GetByMFCNAsync(string mfcn)
    {
        return await _dbSet
            .Where(c => c.MFCN == mfcn)
            .Include(c => c.Transaction)
            .Include(c => c.Recipient)
            .Include(c => c.NonCardReceiver)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CashPickup>> GetByRecipientIdAsync(int recipientId, int pageNumber = 1, int pageSize = 10)
    {
        return await _dbSet
            .Where(c => c.RecipientId == recipientId)
            .Include(c => c.Transaction)
            .Include(c => c.Recipient)
            .OrderByDescending(c => c.Transaction.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}

