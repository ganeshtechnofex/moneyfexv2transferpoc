using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Infrastructure.Data;

namespace MoneyFex.Web.Services;

/// <summary>
/// Provides helpers for normalizing, generating, and looking up idempotency keys for transactions.
/// </summary>
public interface ITransactionIdempotencyService
{
    /// <summary>
    /// Returns the provided key trimmed, or null when the value is null/whitespace.
    /// </summary>
    string? NormalizeKey(string? key);

    /// <summary>
    /// Generates a new idempotency key that can be stored with a transaction.
    /// </summary>
    string GenerateKey();

    /// <summary>
    /// Tries to find an existing transaction for the given sender that already recorded the idempotency key.
    /// </summary>
    Task<Transaction?> FindExistingAsync(int senderId, string? idempotencyKey);
}

/// <summary>
/// Default implementation that relies on the MoneyFexDbContext.
/// </summary>
public sealed class TransactionIdempotencyService : ITransactionIdempotencyService
{
    private readonly MoneyFexDbContext _context;

    public TransactionIdempotencyService(MoneyFexDbContext context)
    {
        _context = context;
    }

    public string? NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return key.Trim();
    }

    public string GenerateKey()
    {
        return Guid.NewGuid().ToString("N");
    }

    public Task<Transaction?> FindExistingAsync(int senderId, string? idempotencyKey)
    {
        var normalized = NormalizeKey(idempotencyKey);
        if (normalized == null)
        {
            return Task.FromResult<Transaction?>(null);
        }

        return _context.Transactions
            .FirstOrDefaultAsync(t => t.SenderId == senderId && t.IdempotencyKey == normalized);
    }
}

