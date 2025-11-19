namespace MoneyFex.Core.Entities.Enums;

/// <summary>
/// Transaction status enumeration
/// </summary>
public enum TransactionStatus
{
    InProgress = 0,
    Paid = 1,
    Cancelled = 2,
    Failed = 3,
    PaymentPending = 4,
    IdCheckInProgress = 5,
    Refund = 6,
    FullRefund = 7,
    PartialRefund = 8,
    Abnormal = 9,
    NotReceived = 10,
    Received = 11,
    Completed = 12,
    Held = 13,
    Paused = 14
}

