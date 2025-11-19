using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Infrastructure.Repositories;

public class KiiBankTransferRepository : Repository<KiiBankTransfer>, IKiiBankTransferRepository
{
    public KiiBankTransferRepository(MoneyFexDbContext context) : base(context)
    {
    }

    public async Task<KiiBankTransfer?> GetByTransactionIdAsync(int transactionId)
    {
        return await _dbSet
            .Include(k => k.Transaction)
                .ThenInclude(t => t.Sender)
            .Include(k => k.Transaction)
                .ThenInclude(t => t.SendingCountry)
            .Include(k => k.Transaction)
                .ThenInclude(t => t.ReceivingCountry)
            .FirstOrDefaultAsync(k => k.TransactionId == transactionId);
    }

    public async Task<KiiBankTransfer?> GetByTransactionReferenceAsync(string transactionReference)
    {
        return await _dbSet
            .Where(k => k.TransactionReference == transactionReference)
            .Include(k => k.Transaction)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<KiiBankTransfer>> GetByAccountNoAsync(string accountNo)
    {
        return await _dbSet
            .Where(k => k.AccountNo == accountNo)
            .Include(k => k.Transaction)
            .OrderByDescending(k => k.Transaction.TransactionDate)
            .ToListAsync();
    }
}

