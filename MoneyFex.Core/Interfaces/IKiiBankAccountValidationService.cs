using System.Threading.Tasks;

namespace MoneyFex.Core.Interfaces;

/// <summary>
/// Interface for KiiBank account validation service
/// </summary>
public interface IKiiBankAccountValidationService
{
    /// <summary>
    /// Validates a KiiBank account number
    /// </summary>
    /// <param name="accountNumber">The account number to validate</param>
    /// <returns>Validation result with account details if valid</returns>
    Task<KiiBankAccountValidationResult> ValidateAccountAsync(string accountNumber);
}

/// <summary>
/// Result of KiiBank account validation
/// </summary>
public class KiiBankAccountValidationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public KiiBankAccountDetails? AccountDetails { get; set; }
}

/// <summary>
/// KiiBank account details returned from validation
/// </summary>
public class KiiBankAccountDetails
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
}

