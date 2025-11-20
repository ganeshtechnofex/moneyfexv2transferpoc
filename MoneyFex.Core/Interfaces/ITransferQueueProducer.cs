using MoneyFex.Core.Messages;

namespace MoneyFex.Core.Interfaces;

/// <summary>
/// Interface for producing transfer queue messages to Kafka
/// </summary>
public interface ITransferQueueProducer
{
    /// <summary>
    /// Enqueue a transfer for background processing
    /// </summary>
    Task EnqueueTransferAsync(TransferQueueMessage message);

    /// <summary>
    /// Enqueue a status update for a transfer
    /// </summary>
    Task EnqueueStatusUpdateAsync(int transactionId, string status, string? message = null);
}

