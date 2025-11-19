namespace MoneyFex.Core.Entities.Enums;

/// <summary>
/// Auto payment frequency enumeration
/// Based on legacy FAXER.PORTAL.DB.AutoPaymentFrequency
/// </summary>
public enum AutoPaymentFrequency
{
    None = 0,
    Weekly = 1,
    Monthly = 2,
    Yearly = 3,
    NoLimitSet = 4
}

