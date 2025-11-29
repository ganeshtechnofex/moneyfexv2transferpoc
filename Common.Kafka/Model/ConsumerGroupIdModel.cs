using Confluent.Kafka;

namespace Common.Kafka.Model;

/// <summary>
/// Holds consumer group configuration that can be supplied at runtime.
/// </summary>
public sealed class ConsumerGroupIdModel
{
    /// <summary>
    /// Kafka consumer group identifier.
    /// </summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// Enables or disables auto commit.
    /// </summary>
    public bool EnableAutoCommit { get; set; } = true;

    /// <summary>
    /// Determines the behavior when the consumer has no committed offset.
    /// </summary>
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;

    /// <summary>
    /// Optional statistics interval reported by the consumer.
    /// </summary>
    public int StatisticsIntervalMs { get; set; } = 0;

    /// <summary>
    /// Optional session timeout for the consumer group.
    /// </summary>
    public int SessionTimeoutMs { get; set; } = 0;
}

