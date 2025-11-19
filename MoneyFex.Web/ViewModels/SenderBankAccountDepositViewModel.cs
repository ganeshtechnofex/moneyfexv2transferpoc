using System.ComponentModel.DataAnnotations;
using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Web.ViewModels;

/// <summary>
/// ViewModel for sender bank account deposit form
/// Based on legacy SenderBankAccountDepositVm
/// </summary>
public class SenderBankAccountDepositViewModel
{
    public int Id { get; set; }
    
    [Range(0, int.MaxValue)]
    public int WalletId { get; set; }
    
    [Range(0, int.MaxValue)]
    public int ReceipientId { get; set; }

    [Required(ErrorMessage = "Select Country")]
    public string CountryCode { get; set; } = string.Empty;

    [StringLength(20)]
    public string? RecentAccountNumber { get; set; }

    [Required(ErrorMessage = "Enter owner name")]
    public string AccountOwnerName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string CountryPhoneCode { get; set; } = string.Empty;

    public string? MobileNumber { get; set; }

    /// <summary>
    /// If Transfer To Europe used as IBAN Number
    /// </summary>
    [Required(ErrorMessage = "Enter account number")]
    public string AccountNumber { get; set; } = string.Empty;

    public int BankId { get; set; }

    public int? BranchId { get; set; }

    /// <summary>
    /// If Transfer To Europe used as BIC/Swift code
    /// </summary>
    public string? BranchCode { get; set; }
    
    public string? SwiftCode { get; set; }

    public bool IsManualDeposit { get; set; }

    public bool IsBusiness { get; set; }
    
    public ReasonForTransfer? ReasonForTransfer { get; set; }

    public bool IsEuropeTransfer { get; set; }

    /// <summary>
    /// Used for Europe bank transfer
    /// </summary>
    public string? BankName { get; set; }
    
    public int TransactionSummaryId { get; set; }

    /// <summary>
    /// Used for South african countries bank transfer
    /// </summary>
    public bool IsSouthAfricaTransfer { get; set; }

    public bool IsWestAfricaTransfer { get; set; }
    
    public string? ReceiverStreet { get; set; }
    
    public string? ReceiverPostalCode { get; set; }
    
    [EmailAddress]
    public string? ReceiverEmail { get; set; }
    
    public string? ReceiverCity { get; set; }
    
    public int IdenityCardId { get; set; }
    
    public int SenderId { get; set; }
    
    public string? SenderCountry { get; set; }
    
    public string? IdentityCardNumber { get; set; }
    
    public string? SendingCurrency { get; set; }
    
    public string? ReceivingCurrency { get; set; }

    // Additional fields for view display (transaction summary)
    public decimal SendingAmount { get; set; }
    public decimal ReceivingAmount { get; set; }
    public string SendingCountry { get; set; } = string.Empty;
    public string ReceivingCountry { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal Fee { get; set; }
    public decimal TotalAmount { get; set; }
}

