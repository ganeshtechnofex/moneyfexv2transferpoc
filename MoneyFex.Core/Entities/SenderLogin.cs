namespace MoneyFex.Core.Entities;

/// <summary>
/// Sender login information
/// </summary>
public class SenderLogin
{
    public int SenderId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public Sender Sender { get; set; } = null!;
}

