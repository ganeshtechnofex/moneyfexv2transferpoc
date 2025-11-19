namespace MoneyFex.Core.Entities;

/// <summary>
/// Bank entity
/// </summary>
public class Bank
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? CountryCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Country? Country { get; set; }
}

