namespace Common.Kafka.Interfaces;

/// <summary>
/// Handles Kafka messages of the specified key/value types.
/// </summary>
/// <typeparam name="TKey">Key type used by the Kafka topic.</typeparam>
/// <typeparam name="TValue">Value type deserialized from the Kafka topic.</typeparam>
public interface IKafkaHandler<TKey, TValue>
{
    /// <summary>
    /// Processes an incoming Kafka message.
    /// </summary>
    Task HandleAsync(TKey key, TValue value);
}

