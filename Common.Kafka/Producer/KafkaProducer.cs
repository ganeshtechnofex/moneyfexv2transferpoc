using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Common.Kafka.Interfaces;

namespace Common.Kafka.Producer;

/// <summary>
/// Produces typed messages to Kafka using JSON serialization for both key and value.
/// </summary>
/// <typeparam name="TKey">Key type to produce.</typeparam>
/// <typeparam name="TValue">Value type to produce.</typeparam>
public sealed class KafkaProducer<TKey, TValue> : IKafkaProducer<TKey, TValue>
{
    private readonly IProducer<byte[], byte[]> _producer;
    private readonly JsonSerializerOptions _serializerOptions;

    public KafkaProducer(ProducerConfig producerConfig, JsonSerializerOptions? serializerOptions = null)
    {
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        _producer = new ProducerBuilder<byte[], byte[]>(producerConfig)
            .SetErrorHandler((_, e) => Console.Error.WriteLine($"Kafka producer error: {e.Reason}"))
            .Build();
    }

    public async Task ProduceAsync(string topic, TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic cannot be null or whitespace.", nameof(topic));
        }

        var message = new Message<byte[], byte[]>
        {
            Key = Serialize(key),
            Value = Serialize(value)
        };

        await _producer.ProduceAsync(topic, message, cancellationToken).ConfigureAwait(false);
    }

    private byte[] Serialize<T>(T data)
    {
        if (data is null)
        {
            return Array.Empty<byte>();
        }

        return JsonSerializer.SerializeToUtf8Bytes(data, _serializerOptions);
    }

    public void Dispose()
    {
        try
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Ignore flush failures, the consumer will log if needed
        }

        _producer.Dispose();
    }
}

