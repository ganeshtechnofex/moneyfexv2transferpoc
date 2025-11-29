namespace Common.Kafka.Interfaces;

/// <summary>
/// Abstraction for producing messages to Kafka topics.
/// </summary>
/// <typeparam name="TKey">Type used for the message key.</typeparam>
/// <typeparam name="TValue">Type used for the message value.</typeparam>
public interface IKafkaProducer<TKey, TValue> : IDisposable
{
    /// <summary>
    /// Produces a message to the specified topic.
    /// </summary>
    Task ProduceAsync(string topic, TKey key, TValue value, CancellationToken cancellationToken = default);
}

