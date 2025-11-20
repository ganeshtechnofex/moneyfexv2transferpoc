namespace MoneyFex.Core.Kafka;

/// <summary>
/// Kafka topic names for money transfer system
/// </summary>
public static class KafkaTopic
{
    public static string MoneyTransfers => "money-transfers";
    public static string TransferStatus => "transfer-status";
    public static string TransferFailed => "transfer-failed";
}

