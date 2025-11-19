namespace MoneyFex.Web.ViewModels;

public class TransactionHistoryViewModel
{
    public TransactionHistorySearchParamsViewModel SearchParamVm { get; set; } = new();
    public List<TransactionStatementViewModel> SenderTransactionStatement { get; set; } = new();
    public int TotalNumberOfTransaction { get; set; }
    public string TotalAmountWithCurrency { get; set; } = string.Empty;
    public string TotalFeePaidwithCurrency { get; set; } = string.Empty;
}

