using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Core.Interfaces;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Infrastructure.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(MoneyFexDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Transaction>> GetBySenderIdAsync(int senderId, int pageNumber = 1, int pageSize = 10)
    {
        return await _dbSet
            .Where(t => t.SenderId == senderId)
            .Include(t => t.Sender)
            .Include(t => t.SendingCountry)
            .Include(t => t.ReceivingCountry)
            .OrderByDescending(t => t.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, int pageNumber = 1, int pageSize = 10)
    {
        return await _dbSet
            .Where(t => t.TransactionDate >= fromDate && t.TransactionDate <= toDate)
            .Include(t => t.Sender)
            .Include(t => t.SendingCountry)
            .Include(t => t.ReceivingCountry)
            .OrderByDescending(t => t.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByStatusAsync(TransactionStatus status, int pageNumber = 1, int pageSize = 10)
    {
        return await _dbSet
            .Where(t => t.Status == status)
            .Include(t => t.Sender)
            .Include(t => t.SendingCountry)
            .Include(t => t.ReceivingCountry)
            .OrderByDescending(t => t.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByReceiptNoAsync(string receiptNo)
    {
        return await _dbSet
            .Where(t => t.ReceiptNo == receiptNo)
            .Include(t => t.Sender)
            .Include(t => t.SendingCountry)
            .Include(t => t.ReceivingCountry)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByReceiptNoWithDetailsAsync(string receiptNo)
    {
        return await _dbSet
            .Where(t => t.ReceiptNo == receiptNo)
            .Include(t => t.Sender)
                .ThenInclude(s => s!.Login)
            .Include(t => t.SendingCountry)
            .Include(t => t.ReceivingCountry)
            .Include(t => t.PayingStaff)
            .Include(t => t.UpdatedByStaff)
            .Include(t => t.ComplianceApprovedByStaff)
            .FirstOrDefaultAsync();
    }

    public async Task<Transaction?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Where(t => t.Id == id)
            .Include(t => t.Sender)
            .Include(t => t.SendingCountry)
            .Include(t => t.ReceivingCountry)
            .Include(t => t.PayingStaff)
            .Include(t => t.UpdatedByStaff)
            .Include(t => t.ComplianceApprovedByStaff)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Transaction>> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
    {
        return await _dbSet
            .Where(t => 
                t.ReceiptNo.Contains(searchTerm) ||
                t.PaymentReference != null && t.PaymentReference.Contains(searchTerm) ||
                t.TransferReference != null && t.TransferReference.Contains(searchTerm))
            .Include(t => t.Sender)
            .Include(t => t.SendingCountry)
            .Include(t => t.ReceivingCountry)
            .OrderByDescending(t => t.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}

