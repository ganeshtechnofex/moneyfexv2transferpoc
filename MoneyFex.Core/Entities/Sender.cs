namespace MoneyFex.Core.Entities;

/// <summary>
/// Sender (FaxerInformation) entity
/// </summary>
public class Sender
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AccountNo { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? CountryCode { get; set; }
    public string? PostalCode { get; set; }
    public bool IsBusiness { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Country? Country { get; set; }
    public SenderLogin? Login { get; set; }
}

