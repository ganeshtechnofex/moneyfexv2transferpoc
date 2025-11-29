using Common.Kafka.Model;

namespace Common.Kafka.Interfaces;

/// <summary>
/// Abstraction over a Kafka consumer that can read typed messages.
/// </summary>
/// <typeparam name="TKey">The consumed message key type.</typeparam>
/// <typeparam name="TValue">The consumed message value type.</typeparam>
public interface IKafkaConsumer<TKey, TValue> : IDisposable
{
    /// <summary>
    /// Starts consuming messages from the provided topic until cancellation is requested.
    /// </summary>
    Task Consume(string topic, CancellationToken cancellationToken, ConsumerGroupIdModel groupIdModel);

    /// <summary>
    /// Gracefully closes the consumer connection.
    /// </summary>
    void Close();
}

