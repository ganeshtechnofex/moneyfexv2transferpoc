namespace MoneyFex.Core.Entities;

/// <summary>
/// Recipient entity for cash pickup transactions
/// </summary>
public class Recipient
{
    public int Id { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

