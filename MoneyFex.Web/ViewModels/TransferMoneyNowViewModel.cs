namespace MoneyFex.Web.ViewModels;

public class RecentTransferAndRecipientViewModel
{
    public List<RecentTransferViewModel> RecentTransfer { get; set; } = new();
    public List<RecipientViewModel> Recipients { get; set; } = new();
    public SenderMonthlyTransactionMeterViewModel SenderMonthlyTransaction { get; set; } = new();
}

public class SenderMonthlyTransactionMeterViewModel
{
    public decimal SenderMonthlyTransactionMeterBalance { get; set; }
    public string SenderCurrencySymbol { get; set; } = string.Empty;
}

public class RecentTransferViewModel
{
    public int Id { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    public decimal ReceivingAmount { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public int TransactionServiceType { get; set; }
    public string? StatusOfBankDeposit { get; set; }
    public string? StatusOfMobileTransfer { get; set; }
}

public class RecipientViewModel
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string Service { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string ReceiverCountryLower { get; set; } = string.Empty;
    public string ReceiverFirstLetter { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public int? BankId { get; set; }
    public string? BankName { get; set; }
    public string? AccountNo { get; set; }
    public string? BranchCode { get; set; }
    public int? MobileWalletProvider { get; set; }
    public string? MobileWalletProviderName { get; set; }
    public bool IBusiness { get; set; }
    public string? ReceiverPostalCode { get; set; }
    public string? ReceiverStreet { get; set; }
    public string? ReceiverCity { get; set; }
    public string? ReceiverEmail { get; set; }
}

