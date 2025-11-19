namespace MoneyFex.Core.Entities;

/// <summary>
/// Receiver detail entity for cash pickup transactions
/// </summary>
public class ReceiverDetail
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public string? CountryCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Country? Country { get; set; }
}

