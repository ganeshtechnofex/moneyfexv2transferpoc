namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for success page after mobile money transfer payment
/// Based on legacy SenderAddMoneySuccessVM
/// </summary>
public class AddMoneyToWalletSuccessViewModel
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiptNo { get; set; } = string.Empty;
    public int TransactionId { get; set; }
}

