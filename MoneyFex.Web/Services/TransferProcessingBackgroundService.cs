using Common.Kafka.Interfaces;
using Common.Kafka.Model;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyFex.Core.Kafka;
using MoneyFex.Core.Messages;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace MoneyFex.Web.Services;

/// <summary>
/// Background service for consuming and processing transfer queue messages from Kafka
/// </summary>
[ExcludeFromCodeCoverage]
public class TransferProcessingBackgroundService : BackgroundService
{
    private readonly IKafkaConsumer<string, TransferQueueMessage> _kafkaConsumer;
    private readonly ILogger<TransferProcessingBackgroundService> _logger;

    public TransferProcessingBackgroundService(
        IKafkaConsumer<string, TransferQueueMessage> kafkaConsumer,
        ILogger<TransferProcessingBackgroundService> logger)
    {
        _kafkaConsumer = kafkaConsumer;
        _logger = logger;
    }

    ConsumerGroupIdModel model = new ConsumerGroupIdModel()
    {
        GroupId = "moneyfex-transfer-processor-group",
        EnableAutoCommit = true,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        StatisticsIntervalMs = 5000,
        SessionTimeoutMs = 6000
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Transfer Processing Background Service started");
            await _kafkaConsumer.Consume(KafkaTopic.MoneyTransfers, stoppingToken, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Transfer Processing Background Service");
            Console.WriteLine($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {KafkaTopic.MoneyTransfers}, {ex}");
            throw;
        }
    }

    public override void Dispose()
    {
        _kafkaConsumer.Close();
        _kafkaConsumer.Dispose();
        base.Dispose();
    }
}

