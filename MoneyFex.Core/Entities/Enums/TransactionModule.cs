namespace MoneyFex.Core.Entities.Enums;

/// <summary>
/// Module/User type who performed the transaction
/// </summary>
public enum TransactionModule
{
    Sender = 0,
    CardUser = 1,
    BusinessMerchant = 2,
    Agent = 3,
    AdminStaff = 4,
    KiiPayBusiness = 5,
    KiiPayPersonal = 6
}

