namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for add money success page
/// Based on legacy FAXER.PORTAL.Models.SenderAddMoneySuccessVM
/// </summary>
public class SenderAddMoneySuccessViewModel
{
    public int Id { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

