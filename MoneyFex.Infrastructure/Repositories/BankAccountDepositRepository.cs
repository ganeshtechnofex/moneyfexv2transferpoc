using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Infrastructure.Repositories;

public class BankAccountDepositRepository : Repository<BankAccountDeposit>, IBankAccountDepositRepository
{
    public BankAccountDepositRepository(MoneyFexDbContext context) : base(context)
    {
    }

    public async Task<BankAccountDeposit?> GetByTransactionIdAsync(int transactionId)
    {
        return await _dbSet
            .Include(b => b.Transaction)
                .ThenInclude(t => t.Sender)
            .Include(b => b.Transaction)
                .ThenInclude(t => t.SendingCountry)
            .Include(b => b.Transaction)
                .ThenInclude(t => t.ReceivingCountry)
            .Include(b => b.Bank)
            .FirstOrDefaultAsync(b => b.TransactionId == transactionId);
    }

    public async Task<IEnumerable<BankAccountDeposit>> GetByBankIdAsync(int bankId, int pageNumber = 1, int pageSize = 10)
    {
        return await _dbSet
            .Where(b => b.BankId == bankId)
            .Include(b => b.Transaction)
            .Include(b => b.Bank)
            .OrderByDescending(b => b.Transaction.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<BankAccountDeposit>> GetByReceiverAccountNoAsync(string accountNo)
    {
        return await _dbSet
            .Where(b => b.ReceiverAccountNo == accountNo)
            .Include(b => b.Transaction)
            .Include(b => b.Bank)
            .OrderByDescending(b => b.Transaction.TransactionDate)
            .ToListAsync();
    }
}

