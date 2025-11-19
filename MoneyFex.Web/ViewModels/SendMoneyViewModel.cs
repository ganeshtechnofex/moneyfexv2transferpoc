namespace MoneyFex.Web.ViewModels;

public class SendMoneyViewModel
{
    public string TransferMethod { get; set; } = string.Empty; // BankAccount, MobileWallet, CashPickup, KiiBank
    public decimal SendingAmount { get; set; }
    public decimal ReceivingAmount { get; set; }
    public string SendingCountry { get; set; } = string.Empty;
    public string ReceivingCountry { get; set; } = string.Empty;
    public string SendingCurrency { get; set; } = string.Empty;
    public string ReceivingCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal Fee { get; set; }
    public decimal TotalAmount { get; set; }
}

