using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Common.Kafka.Interfaces;
using Common.Kafka.Model;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Kafka.Consumer;

/// <summary>
/// A simple Kafka consumer that dispatches deserialized messages to scoped handlers.
/// </summary>
/// <typeparam name="TKey">Message key type.</typeparam>
/// <typeparam name="TValue">Message value type.</typeparam>
public sealed class KafkaConsumer<TKey, TValue> : IKafkaConsumer<TKey, TValue>
{
    private readonly ConsumerConfig _baseConfig;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumer<TKey, TValue>> _logger;
    private readonly JsonSerializerOptions _serializerOptions;
    private IConsumer<byte[], byte[]>? _consumer;

    public KafkaConsumer(
        ConsumerConfig baseConfig,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaConsumer<TKey, TValue>> logger,
        JsonSerializerOptions? serializerOptions = null)
    {
        _baseConfig = baseConfig ?? throw new ArgumentNullException(nameof(baseConfig));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task Consume(string topic, CancellationToken cancellationToken, ConsumerGroupIdModel groupIdModel)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic name is required.", nameof(topic));
        }

        if (groupIdModel == null)
        {
            throw new ArgumentNullException(nameof(groupIdModel));
        }

        if (string.IsNullOrWhiteSpace(groupIdModel.GroupId))
        {
            throw new InvalidOperationException("ConsumerGroupIdModel.GroupId must be set.");
        }

        var config = new ConsumerConfig(_baseConfig)
        {
            GroupId = groupIdModel.GroupId,
            EnableAutoCommit = groupIdModel.EnableAutoCommit,
            AutoOffsetReset = groupIdModel.AutoOffsetReset
        };

        if (groupIdModel.SessionTimeoutMs > 0)
        {
            config.SessionTimeoutMs = groupIdModel.SessionTimeoutMs;
        }

        if (groupIdModel.StatisticsIntervalMs > 0)
        {
            config.StatisticsIntervalMs = groupIdModel.StatisticsIntervalMs;
        }

        _consumer?.Dispose();
        _consumer = new ConsumerBuilder<byte[], byte[]>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Reason}", e.Reason))
            .Build();

        _consumer.Subscribe(topic);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<byte[], byte[]>? result = null;

                try
                {
                    result = _consumer.Consume(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume exception from topic {Topic}", topic);
                    continue;
                }

                if (result?.Message == null)
                {
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<TKey, TValue>>();

                    var key = Deserialize<TKey>(result.Message.Key);
                    var value = Deserialize<TValue>(result.Message.Value);

                    if (key is null || value is null)
                    {
                        _logger.LogWarning("Received message with null key or value on topic {Topic}", topic);
                        continue;
                    }

                    await handler.HandleAsync(key, value).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler threw while processing message from topic {Topic}", topic);
                }

                if (!groupIdModel.EnableAutoCommit)
                {
                    _consumer.Commit(result);
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    public void Close()
    {
        try
        {
            _consumer?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while closing Kafka consumer");
        }
    }

    public void Dispose()
    {
        try
        {
            _consumer?.Dispose();
            _consumer = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disposing Kafka consumer");
        }
    }

    private T? Deserialize<T>(byte[]? data)
    {
        if (data is null || data.Length == 0)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(data, _serializerOptions);
    }
}

