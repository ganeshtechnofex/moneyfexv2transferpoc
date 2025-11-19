using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using MoneyFex.Infrastructure.Data;
using MoneyFex.Web.ViewModels;

namespace MoneyFex.Web.Services;

public class TransactionHistoryService
{
    private readonly MoneyFexDbContext _context;
    private readonly ILogger<TransactionHistoryService> _logger;

    public TransactionHistoryService(
        MoneyFexDbContext context,
        ILogger<TransactionHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TransactionHistoryViewModel> GetTransactionHistoryAsync(
        TransactionHistorySearchParamsViewModel searchParams)
    {
        // Optimized query with eager loading
        var query = _context.Transactions
            .Include(t => t.Sender)
            .Include(t => t.SendingCountry)
            .Include(t => t.ReceivingCountry)
            .Include(t => t.PayingStaff)
            .Include(t => t.UpdatedByStaff)
            .AsQueryable();

        // Apply filters
        query = ApplyFilters(query, searchParams);

        // Get total count before pagination (optimized - no materialization)
        var totalCount = await query.CountAsync();

        // Apply pagination
        var skip = (searchParams.PageNum - 1) * searchParams.PageSize;
        var transactions = await query
            // Prioritize pending transactions first (PaymentPending, InProgress, Held), then order by date descending (newest first)
            .OrderByDescending(t => 
                t.Status == TransactionStatus.PaymentPending || 
                t.Status == TransactionStatus.InProgress || 
                t.Status == TransactionStatus.Held ? 1 : 0)
            .ThenByDescending(t => t.TransactionDate)
            .Skip(skip)
            .Take(searchParams.PageSize)
            .ToListAsync();

        // Pre-load related data in batches for better performance
        var transactionIds = transactions.Select(t => t.Id).ToList();
        
        // Load all related data - execute queries separately to avoid DbContext threading issues
        var bankDeposits = await _context.BankAccountDeposits
            .Include(b => b.Bank)
            .Where(b => transactionIds.Contains(b.TransactionId))
            .ToListAsync();
        
        var mobileTransfers = await _context.MobileMoneyTransfers
            .Include(m => m.WalletOperator)
            .Where(m => transactionIds.Contains(m.TransactionId))
            .ToListAsync();
        
        var cashPickups = await _context.CashPickups
            .Include(c => c.Recipient)
            .Include(c => c.NonCardReceiver)
            .Where(c => transactionIds.Contains(c.TransactionId))
            .ToListAsync();
        
        var kiiBankTransfers = await _context.KiiBankTransfers
            .Where(k => transactionIds.Contains(k.TransactionId))
            .ToListAsync();

        // Create lookup dictionaries for O(1) access
        var bankDepositDict = bankDeposits.ToDictionary(b => b.TransactionId);
        var mobileTransferDict = mobileTransfers.ToDictionary(m => m.TransactionId);
        var cashPickupDict = cashPickups.ToDictionary(c => c.TransactionId);
        var kiiBankTransferDict = kiiBankTransfers.ToDictionary(k => k.TransactionId);

        // Convert to ViewModels (optimized - no async calls in loop)
        var transactionStatements = new List<TransactionStatementViewModel>();
        
        foreach (var transaction in transactions)
        {
            var statement = ConvertToTransactionStatement(
                transaction, 
                bankDepositDict.GetValueOrDefault(transaction.Id),
                mobileTransferDict.GetValueOrDefault(transaction.Id),
                cashPickupDict.GetValueOrDefault(transaction.Id),
                kiiBankTransferDict.GetValueOrDefault(transaction.Id));
            statement.TotalCount = totalCount;
            transactionStatements.Add(statement);
        }

        // Calculate totals (optimized - use the filtered query)
        var totalsQuery = query.Select(t => new { t.TotalAmount, t.Fee, t.SendingCurrency });
        var totals = await totalsQuery.ToListAsync();
        var totalAmount = totals.Sum(t => t.TotalAmount);
        var totalFee = totals.Sum(t => t.Fee);
        var defaultCurrency = totals.FirstOrDefault()?.SendingCurrency ?? "USD";

        var viewModel = new TransactionHistoryViewModel
        {
            SearchParamVm = searchParams,
            SenderTransactionStatement = transactionStatements,
            TotalNumberOfTransaction = totalCount,
            TotalAmountWithCurrency = FormatCurrency(totalAmount, defaultCurrency),
            TotalFeePaidwithCurrency = FormatCurrency(totalFee, defaultCurrency)
        };

        return viewModel;
    }

    private IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> query,
        TransactionHistorySearchParamsViewModel searchParams)
    {
        // Transaction type filter
        // Map: 0=All, 1=CashPickup, 2=BankDeposit, 3=MobileTransfer, 5=CashPickup, 6=BankDeposit
        if (searchParams.TransactionServiceType > 0 && searchParams.TransactionServiceType != 7)
        {
            // Use subqueries for better performance
            switch (searchParams.TransactionServiceType)
            {
                case 1: // Mobile Wallet
                    var mobileTransactionIds = _context.MobileMoneyTransfers
                        .Select(m => m.TransactionId);
                    query = query.Where(t => mobileTransactionIds.Contains(t.Id));
                    break;
                    
                case 2: // Bank Deposit
                case 6: // Bank Deposit (alternative)
                    var bankTransactionIds = _context.BankAccountDeposits
                        .Select(b => b.TransactionId);
                    query = query.Where(t => bankTransactionIds.Contains(t.Id));
                    break;
                    
                case 3: // KiiBank
                    var kiiBankTransactionIds = _context.KiiBankTransfers
                        .Select(k => k.TransactionId);
                    query = query.Where(t => kiiBankTransactionIds.Contains(t.Id));
                    break;
                    
                case 5: // Cash Pickup
                    var cashPickupTransactionIds = _context.CashPickups
                        .Select(c => c.TransactionId);
                    query = query.Where(t => cashPickupTransactionIds.Contains(t.Id));
                    break;
            }
        }

        // Date range filter
        if (searchParams.FromDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= searchParams.FromDate.Value);
        }
        if (searchParams.ToDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= searchParams.ToDate.Value);
        }

        // Country filters
        if (!string.IsNullOrEmpty(searchParams.SendingCountry))
        {
            query = query.Where(t => t.SendingCountryCode == searchParams.SendingCountry);
        }
        if (!string.IsNullOrEmpty(searchParams.ReceivingCountry))
        {
            query = query.Where(t => t.ReceivingCountryCode == searchParams.ReceivingCountry);
        }

        // Currency filters
        if (!string.IsNullOrEmpty(searchParams.SendingCurrency))
        {
            query = query.Where(t => t.SendingCurrency == searchParams.SendingCurrency);
        }
        if (!string.IsNullOrEmpty(searchParams.ReceivingCurrency))
        {
            query = query.Where(t => t.ReceivingCurrency == searchParams.ReceivingCurrency);
        }

        // Receipt number search
        if (!string.IsNullOrEmpty(searchParams.searchString))
        {
            query = query.Where(t => t.ReceiptNo.Contains(searchParams.searchString));
        }

        // Sender name search
        if (!string.IsNullOrEmpty(searchParams.SenderName))
        {
            query = query.Where(t => 
                (t.Sender.FirstName + " " + t.Sender.LastName).Contains(searchParams.SenderName));
        }

        // Sender email search
        if (!string.IsNullOrEmpty(searchParams.SenderEmail))
        {
            query = query.Where(t => t.Sender.Email.Contains(searchParams.SenderEmail));
        }

        // Receiver name search (optimized - use subquery instead of materializing)
        if (!string.IsNullOrEmpty(searchParams.ReceiverName))
        {
            var receiverNameLower = searchParams.ReceiverName.ToLower();
            
            var bankDepositTransactionIds = _context.BankAccountDeposits
                .Where(b => b.ReceiverName != null && b.ReceiverName.ToLower().Contains(receiverNameLower))
                .Select(b => b.TransactionId);
            
            var mobileTransferTransactionIds = _context.MobileMoneyTransfers
                .Where(m => m.ReceiverName != null && m.ReceiverName.ToLower().Contains(receiverNameLower))
                .Select(m => m.TransactionId);
            
            // CashPickup uses Recipient.ReceiverName or NonCardReceiver.FullName
            // Get recipient IDs first, then find cash pickups
            var recipientIds = _context.Recipients
                .Where(r => r.ReceiverName.ToLower().Contains(receiverNameLower))
                .Select(r => r.Id);
            
            var nonCardReceiverIds = _context.ReceiverDetails
                .Where(r => r.FullName.ToLower().Contains(receiverNameLower))
                .Select(r => r.Id);
            
            var cashPickupFromRecipientIds = _context.CashPickups
                .Where(c => c.RecipientId.HasValue && recipientIds.Contains(c.RecipientId.Value))
                .Select(c => c.TransactionId);
            
            var cashPickupFromNonCardIds = _context.CashPickups
                .Where(c => c.NonCardReceiverId.HasValue && nonCardReceiverIds.Contains(c.NonCardReceiverId.Value))
                .Select(c => c.TransactionId);
            
            var cashPickupTransactionIds = cashPickupFromRecipientIds.Union(cashPickupFromNonCardIds);
            
            var kiiBankTransactionIds = _context.KiiBankTransfers
                .Where(k => k.ReceiverName != null && k.ReceiverName.ToLower().Contains(receiverNameLower))
                .Select(k => k.TransactionId);
            
            var allReceiverTransactionIds = bankDepositTransactionIds
                .Union(mobileTransferTransactionIds)
                .Union(cashPickupTransactionIds)
                .Union(kiiBankTransactionIds);
            
            query = query.Where(t => allReceiverTransactionIds.Contains(t.Id));
        }

        // Phone number search
        if (!string.IsNullOrEmpty(searchParams.PhoneNumber))
        {
            query = query.Where(t => t.Sender.PhoneNumber != null && t.Sender.PhoneNumber.Contains(searchParams.PhoneNumber));
        }

        // MF Code (Sender Account Number) search
        if (!string.IsNullOrEmpty(searchParams.MFCode))
        {
            query = query.Where(t => t.Sender.AccountNo != null && t.Sender.AccountNo.Contains(searchParams.MFCode));
        }

        // Payout provider search (optimized)
        if (!string.IsNullOrEmpty(searchParams.PayoutProviderName))
        {
            var providerNameLower = searchParams.PayoutProviderName.ToLower();
            
            var bankProviderIds = _context.BankAccountDeposits
                .Where(b => b.BankName != null && b.BankName.ToLower().Contains(providerNameLower))
                .Select(b => b.TransactionId);
            
            var mobileProviderIds = _context.MobileMoneyTransfers
                .Where(m => m.WalletOperator != null && m.WalletOperator.Name.ToLower().Contains(providerNameLower))
                .Select(m => m.TransactionId);
            
            var kiiBankProviderIds = _context.KiiBankTransfers
                .Where(k => "kiibank".Contains(providerNameLower))
                .Select(k => k.TransactionId);
            
            var allProviderIds = bankProviderIds.Union(mobileProviderIds).Union(kiiBankProviderIds);
            query = query.Where(t => allProviderIds.Contains(t.Id));
        }

        // Transaction with/without fee filter
        if (searchParams.TransactionWithAndWithoutFee.HasValue)
        {
            if (searchParams.TransactionWithAndWithoutFee.Value == 0) // Without fee
            {
                query = query.Where(t => t.Fee == 0);
            }
            else if (searchParams.TransactionWithAndWithoutFee.Value == 1) // With fee
            {
                query = query.Where(t => t.Fee > 0);
            }
        }

        // Responsible person filter
        if (!string.IsNullOrEmpty(searchParams.ResponsiblePerson))
        {
            switch (searchParams.ResponsiblePerson.ToLower())
            {
                case "sender":
                    query = query.Where(t => !t.PayingStaffId.HasValue && !t.UpdatedByStaffId.HasValue);
                    break;
                case "agent":
                    query = query.Where(t => t.PayingStaffId.HasValue);
                    break;
                case "admin":
                    query = query.Where(t => t.UpdatedByStaffId.HasValue && !t.PayingStaffId.HasValue);
                    break;
            }
        }

        // Search by status filter
        if (!string.IsNullOrEmpty(searchParams.SearchByStatus))
        {
            // Map status strings to enum values
            var statusMap = new Dictionary<string, TransactionStatus>
            {
                { "Paid", TransactionStatus.Paid },
                { "Cancelled", TransactionStatus.Cancelled },
                { "Payment Pending", TransactionStatus.PaymentPending },
                { "Refunded", TransactionStatus.Refund },
                { "In Progress (ID Check)", TransactionStatus.IdCheckInProgress },
                { "In Progress ", TransactionStatus.InProgress },
                { "In Progress", TransactionStatus.InProgress }
            };
            
            if (statusMap.TryGetValue(searchParams.SearchByStatus, out var status))
            {
                query = query.Where(t => t.Status == status);
            }
        }

        // Status filter
        if (!string.IsNullOrEmpty(searchParams.Status))
        {
            if (Enum.TryParse<TransactionStatus>(searchParams.Status, out var status))
            {
                query = query.Where(t => t.Status == status);
            }
        }

        // Staff filter
        if (searchParams.StaffId.HasValue)
        {
            query = query.Where(t => t.PayingStaffId == searchParams.StaffId || 
                                     t.UpdatedByStaffId == searchParams.StaffId);
        }

        return query;
    }

    private TransactionStatementViewModel ConvertToTransactionStatement(
        Transaction transaction,
        BankAccountDeposit? bankDeposit,
        MobileMoneyTransfer? mobileTransfer,
        CashPickup? cashPickup,
        KiiBankTransfer? kiiBankTransfer)
    {
        // Get receiver information based on transaction type
        string receiverName = string.Empty;
        string receivingAccountNo = string.Empty;
        string payoutProviderName = string.Empty;
        string transferMethod = "Unknown";

        // Determine transaction type and get receiver details
        if (bankDeposit != null)
        {
            receiverName = bankDeposit.ReceiverName ?? string.Empty;
            receivingAccountNo = bankDeposit.ReceiverAccountNo ?? string.Empty;
            payoutProviderName = bankDeposit.BankName ?? string.Empty;
            transferMethod = "Bank Deposit";
        }
        else if (mobileTransfer != null)
        {
            receiverName = mobileTransfer.ReceiverName ?? string.Empty;
            receivingAccountNo = mobileTransfer.PaidToMobileNo ?? string.Empty;
            payoutProviderName = mobileTransfer.WalletOperator?.Name ?? string.Empty;
            transferMethod = "Mobile Wallet";
        }
        else if (cashPickup != null)
        {
            // CashPickup can have receiver name from Recipient or NonCardReceiver
            if (cashPickup.Recipient != null)
            {
                receiverName = cashPickup.Recipient.ReceiverName ?? string.Empty;
            }
            else if (cashPickup.NonCardReceiver != null)
            {
                receiverName = cashPickup.NonCardReceiver.FullName ?? string.Empty;
            }
            receivingAccountNo = cashPickup.MFCN ?? string.Empty;
            transferMethod = "Cash PickUp";
        }
        else if (kiiBankTransfer != null)
        {
            receiverName = kiiBankTransfer.ReceiverName ?? string.Empty;
            receivingAccountNo = kiiBankTransfer.AccountNo ?? string.Empty;
            payoutProviderName = "KiiBank";
            transferMethod = "KiiBank";
        }

        // Get sender information
        var sender = transaction.Sender;
        var senderName = $"{sender.FirstName} {sender.LastName}".Trim();
        var senderPhone = sender.PhoneNumber ?? string.Empty;

        // Format amounts with currency symbols
        var sendingCurrencySymbol = GetCurrencySymbol(transaction.SendingCurrency);
        var receivingCurrencySymbol = GetCurrencySymbol(transaction.ReceivingCurrency);

        // Determine responsible person
        string responsiblePerson = "Sender";
        if (transaction.PayingStaffId.HasValue)
        {
            responsiblePerson = "Agent";
        }
        else if (transaction.UpdatedByStaffId.HasValue)
        {
            responsiblePerson = "Admin Staff";
        }

        // Determine transaction status for admin
        string statusForAdmin = transaction.Status.ToString();
        if (!string.IsNullOrEmpty(transaction.PaymentReference))
        {
            statusForAdmin += $" ({transaction.PaymentReference})";
        }

        // Determine if transaction can be cancelled
        bool canCancel = transaction.Status == TransactionStatus.PaymentPending ||
                        transaction.Status == TransactionStatus.InProgress;

        // Determine if awaiting approval
        bool awaitingApproval = transaction.Status == TransactionStatus.Held ||
                               (transaction.IsComplianceNeeded && !transaction.IsComplianceApproved);

        // Map transaction type to legacy service type values
        // 1 = Mobile Wallet, 2 = Bank Deposit, 3 = KiiBank, 5 = Cash Pickup, 6 = Bank Deposit (alternative)
        int transactionServiceType = 0;
        if (bankDeposit != null)
        {
            transactionServiceType = 6; // Bank Deposit
        }
        else if (mobileTransfer != null)
        {
            transactionServiceType = 1; // Mobile Wallet
        }
        else if (kiiBankTransfer != null)
        {
            transactionServiceType = 3; // KiiBank
        }
        else if (cashPickup != null)
        {
            transactionServiceType = 5; // Cash Pickup
        }

        return new TransactionStatementViewModel
        {
            TransactionId = transaction.Id,
            identifier = transaction.ReceiptNo,
            TransferMethod = transferMethod,
            TransactionServiceType = transactionServiceType,
            SendingCountry = transaction.SendingCountry?.CountryName ?? transaction.SendingCountryCode,
            ReceivingCountry = transaction.ReceivingCountry?.CountryName ?? transaction.ReceivingCountryCode,
            SendingCurrency = transaction.SendingCurrency,
            ReceivingCurrency = transaction.ReceivingCurrency,
            SenderId = transaction.SenderId,
            SenderName = senderName,
            SenderEmail = sender.Email ?? string.Empty,
            SenderPhoneNumber = senderPhone,
            SenderMFAccountNo = sender.AccountNo ?? string.Empty,
            RecipentId = transaction.RecipientId,
            ReceiverName = receiverName,
            ReceivingAccountNo = receivingAccountNo,
            Amount = $"{sendingCurrencySymbol}{transaction.SendingAmount:N2}",
            Fee = $"{sendingCurrencySymbol}{transaction.Fee:N2}",
            ReceivingAmount = transaction.ReceivingAmount,
            GrossAmount = transaction.TotalAmount,
            DateTime = transaction.TransactionDate.ToString("dd MMM yyyy"),
            TransactionTime = transaction.TransactionDate.ToString("HH:mm:ss"),
            TransactionDate = transaction.TransactionDate,
            Reference = transaction.PaymentReference ?? string.Empty,
            TransactionStatusForAdmin = statusForAdmin,
            TransactionPerformedBy = responsiblePerson,
            PayoutType = transferMethod,
            UpdatedByStaffName = transaction.UpdatedByStaff != null 
                ? $"{transaction.UpdatedByStaff.FirstName} {transaction.UpdatedByStaff.LastName}".Trim()
                : string.Empty,
            PayoutProviderName = payoutProviderName,
            NoteCount = 0, // TODO: Implement note counting
            IsManualConfimationNeed = awaitingApproval,
            IsTransactionCancelAble = canCancel,
            IsAwaitForApproval = awaitingApproval,
            IsReInitializedTransaction = false, // TODO: Check reinitialization status
            ReInitializedReceiptNo = null
        };
    }

    private string GetCurrencySymbol(string currencyCode)
    {
        return currencyCode switch
        {
            "USD" => "$",
            "GBP" => "£",
            "EUR" => "€",
            "NGN" => "₦",
            "KES" => "KSh",
            "GHS" => "₵",
            "UGX" => "USh",
            "TZS" => "TSh",
            "ZAR" => "R",
            "INR" => "₹",
            "PKR" => "₨",
            "BDT" => "৳",
            "ETB" => "Br",
            "MAD" => "د.م.",
            "AUD" => "A$",
            "CAD" => "C$",
            _ => currencyCode
        };
    }

    private string FormatCurrency(decimal amount, string currency)
    {
        var symbol = GetCurrencySymbol(currency);
        return $"{symbol}{amount:N2}";
    }
}

