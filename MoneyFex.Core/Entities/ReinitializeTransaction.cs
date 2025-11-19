namespace MoneyFex.Core.Entities;

/// <summary>
/// Reinitialize transaction entity
/// </summary>
public class ReinitializeTransaction
{
    public int Id { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public string NewReceiptNo { get; set; } = string.Empty;
    public int? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public Staff? CreatedBy { get; set; }
}

