namespace MoneyFex.Core.Entities;

/// <summary>
/// Mobile wallet operator entity
/// </summary>
public class MobileWalletOperator
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string? MobileNetworkCode { get; set; }
    public int? PayoutProviderId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Country? Country { get; set; }
}

