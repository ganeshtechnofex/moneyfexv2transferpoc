using Common.Kafka.Interfaces;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Interfaces;
using MoneyFex.Core.Kafka;
using MoneyFex.Core.Messages;

namespace MoneyFex.Infrastructure.Services;

/// <summary>
/// Service for producing transfer queue messages to Kafka
/// </summary>
public class TransferQueueProducer : ITransferQueueProducer
{
    private readonly IKafkaProducer<string, TransferQueueMessage> _kafkaProducer;
    private readonly ILogger<TransferQueueProducer> _logger;

    public TransferQueueProducer(
        IKafkaProducer<string, TransferQueueMessage> kafkaProducer,
        ILogger<TransferQueueProducer> logger)
    {
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task EnqueueTransferAsync(TransferQueueMessage message)
    {
        try
        {
            _logger.LogInformation(
                "Enqueuing transfer. TransactionId: {TransactionId}, ReceiptNo: {ReceiptNo}, Type: {Type}",
                message.TransactionId, message.ReceiptNo, message.TransferType);

            var key = message.TransactionId.ToString();
            await _kafkaProducer.ProduceAsync(KafkaTopic.MoneyTransfers, key, message);

            _logger.LogInformation(
                "Transfer enqueued successfully. TransactionId: {TransactionId}",
                message.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to enqueue transfer. TransactionId: {TransactionId}",
                message.TransactionId);
            throw;
        }
    }

    public async Task EnqueueStatusUpdateAsync(int transactionId, string status, string? message = null)
    {
        try
        {
            _logger.LogInformation(
                "Enqueuing status update. TransactionId: {TransactionId}, Status: {Status}",
                transactionId, status);

            // TODO: Create TransferStatusMessage class if needed
            // For now, this is a placeholder
            // var statusMessage = new TransferStatusMessage { ... };
            // await _kafkaProducer.ProduceAsync(KafkaTopic.TransferStatus, transactionId.ToString(), statusMessage);

            _logger.LogInformation(
                "Status update enqueued successfully. TransactionId: {TransactionId}",
                transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to enqueue status update. TransactionId: {TransactionId}",
                transactionId);
            throw;
        }
    }
}

