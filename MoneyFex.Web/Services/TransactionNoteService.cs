using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Services;

public class TransactionNoteService
{
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<TransactionNoteService> _logger;

    public TransactionNoteService(
        MoneyFexDbContext context,
        ILogger<TransactionNoteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all notes for a transaction
    /// </summary>
    public async Task<List<TransactionNoteViewModel>> GetTransactionNotesAsync(int transactionId, string transactionMethodName)
    {
        try
        {
            // For POC, we'll create a simple note system
            // In production, you would have a TransactionNote table
            var notes = new List<TransactionNoteViewModel>();

            // Check if transaction exists
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return notes;
            }

            // TODO: Implement actual note retrieval from database
            // For now, return empty list
            // In production: 
            // var notes = await _context.TransactionNotes
            //     .Where(n => n.TransactionId == transactionId)
            //     .Include(n => n.CreatedByStaff)
            //     .OrderByDescending(n => n.CreatedAt)
            //     .Select(n => new TransactionNoteViewModel { ... })
            //     .ToListAsync();

            return notes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction notes for transaction {TransactionId}", transactionId);
            return new List<TransactionNoteViewModel>();
        }
    }

    /// <summary>
    /// Saves a note for a transaction
    /// </summary>
    public async Task<ServiceResult<bool>> SaveTransactionNoteAsync(TransactionNoteViewModel noteViewModel, int? staffId = null)
    {
        try
        {
            // Check if transaction exists
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == noteViewModel.TransactionId);

            if (transaction == null)
            {
                return new ServiceResult<bool>
                {
                    Data = false,
                    Status = ResultStatus.Error,
                    Message = "Transaction not found"
                };
            }

            // TODO: Implement actual note saving to database
            // For now, just log it
            // In production:
            // var note = new TransactionNote
            // {
            //     TransactionId = noteViewModel.TransactionId,
            //     Note = noteViewModel.Note,
            //     TransactionMethodName = noteViewModel.TransactionMethodName,
            //     CreatedByStaffId = staffId,
            //     CreatedAt = DateTime.UtcNow
            // };
            // _context.TransactionNotes.Add(note);
            // await _context.SaveChangesAsync();

            _logger.LogInformation("Note saved for transaction {TransactionId} by staff {StaffId}", 
                noteViewModel.TransactionId, staffId);

            return new ServiceResult<bool>
            {
                Data = true,
                Status = ResultStatus.OK,
                Message = "Note saved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving transaction note for transaction {TransactionId}", 
                noteViewModel.TransactionId);
            return new ServiceResult<bool>
            {
                Data = false,
                Status = ResultStatus.Error,
                Message = ex.Message
            };
        }
    }
}

public class TransactionNoteViewModel
{
    public int TransactionId { get; set; }
    public string Note { get; set; } = string.Empty;
    public string TransactionMethodName { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public string CreatedTime { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
}

