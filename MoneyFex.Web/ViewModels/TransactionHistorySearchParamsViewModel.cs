using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Web.ViewModels;

public class TransactionHistorySearchParamsViewModel
{
    public int PageSize { get; set; } = 10;
    public int PageNum { get; set; } = 1;
    public int CurrentpageCount { get; set; } = 0;
    public bool IsBusiness { get; set; } = false;
    
    // Transaction type filter (0=All, 1=CashPickup, 2=BankDeposit, 3=MobileTransfer, etc.)
    public int TransactionServiceType { get; set; } = 0;
    
    // Date range
    public string? DateRange { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Country filters
    public string? SendingCountry { get; set; }
    public string? ReceivingCountry { get; set; }
    
    // Currency filters
    public string? SendingCurrency { get; set; }
    public string? ReceivingCurrency { get; set; }
    
    // Search filters
    public string? searchString { get; set; } // Receipt number search
    public string? SenderName { get; set; }
    public string? SenderEmail { get; set; }
    public string? ReceiverName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Status { get; set; }
    public string? MFCode { get; set; } // Sender account number
    public string? PayoutProviderName { get; set; }
    
    // Additional filters
    public int? TransactionWithAndWithoutFee { get; set; } // 0=Without Fee, 1=With Fee
    public string? ResponsiblePerson { get; set; } // sender, agent, admin
    public string? SearchByStatus { get; set; }
    public int? StaffId { get; set; }
}

