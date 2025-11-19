using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;

namespace MoneyFex.Infrastructure.Data;

public class MoneyFexDbContext : DbContext
{
    public MoneyFexDbContext(DbContextOptions<MoneyFexDbContext> options) : base(options)
    {
    }

    // Reference tables
    public DbSet<Country> Countries { get; set; }
    public DbSet<Bank> Banks { get; set; }
    public DbSet<MobileWalletOperator> MobileWalletOperators { get; set; }
    public DbSet<Sender> Senders { get; set; }
    public DbSet<SenderLogin> SenderLogins { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<Recipient> Recipients { get; set; }
    public DbSet<ReceiverDetail> ReceiverDetails { get; set; }

    // Transaction tables
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<BankAccountDeposit> BankAccountDeposits { get; set; }
    public DbSet<MobileMoneyTransfer> MobileMoneyTransfers { get; set; }
    public DbSet<CashPickup> CashPickups { get; set; }
    public DbSet<KiiBankTransfer> KiiBankTransfers { get; set; }

    // Payment and management tables
    public DbSet<CardPaymentInformation> CardPaymentInformations { get; set; }
    public DbSet<ReinitializeTransaction> ReinitializeTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure enums - PostgreSQL enums will be created by migration
        // Note: If enums don't exist, they will be created automatically by EF Core migrations

        // Country configuration
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("countries");
            entity.HasKey(e => e.CountryCode);
            entity.Property(e => e.CountryCode).HasMaxLength(3);
            entity.Property(e => e.CountryName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.CurrencySymbol).HasMaxLength(10).IsRequired();
        });

        // Bank configuration
        modelBuilder.Entity<Bank>(entity =>
        {
            entity.ToTable("banks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Mobile Wallet Operator configuration
        modelBuilder.Entity<MobileWalletOperator>(entity =>
        {
            entity.ToTable("mobile_wallet_operators");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Sender configuration
        modelBuilder.Entity<Sender>(entity =>
        {
            entity.ToTable("senders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.AccountNo).HasMaxLength(50);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.AccountNo).IsUnique();
            entity.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Sender Login configuration
        modelBuilder.Entity<SenderLogin>(entity =>
        {
            entity.ToTable("sender_logins");
            entity.HasKey(e => e.SenderId);
            entity.HasOne(e => e.Sender)
                .WithOne(s => s.Login)
                .HasForeignKey<SenderLogin>(e => e.SenderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Staff configuration
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("staff");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReceiptNo).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.ReceiptNo).IsUnique();
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PaymentReference);
            
            // Configure enums as integers
            entity.Property(e => e.Status)
                .HasConversion<int>();
            entity.Property(e => e.TransactionModule)
                .HasConversion<int>();
            entity.Property(e => e.SenderPaymentMode)
                .HasConversion<int>();
            entity.Property(e => e.ApiService)
                .HasConversion<int?>();
            entity.Property(e => e.ReasonForTransfer)
                .HasConversion<int?>();
            entity.Property(e => e.CardProcessorApi)
                .HasConversion<int?>();
            
            // Configure string properties with max lengths
            entity.Property(e => e.PayingStaffName).HasMaxLength(200);
            entity.Property(e => e.TransferZeroSenderId).HasMaxLength(100);
            entity.Property(e => e.TransferReference).HasMaxLength(100);
            
            // Configure decimal properties with precision
            entity.Property(e => e.AgentCommission).HasPrecision(18, 2);
            entity.Property(e => e.ExtraFee).HasPrecision(18, 2);
            entity.Property(e => e.Margin).HasPrecision(18, 2);
            entity.Property(e => e.MFRate).HasPrecision(18, 6);
            
            entity.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.SendingCountry)
                .WithMany()
                .HasForeignKey(e => e.SendingCountryCode)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.ReceivingCountry)
                .WithMany()
                .HasForeignKey(e => e.ReceivingCountryCode)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.PayingStaff)
                .WithMany()
                .HasForeignKey(e => e.PayingStaffId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.UpdatedByStaff)
                .WithMany()
                .HasForeignKey(e => e.UpdatedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.ComplianceApprovedByStaff)
                .WithMany()
                .HasForeignKey(e => e.ComplianceApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Bank Account Deposit configuration
        modelBuilder.Entity<BankAccountDeposit>(entity =>
        {
            entity.ToTable("bank_account_deposits");
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.BankName).HasMaxLength(200);
            entity.Property(e => e.BankCode).HasMaxLength(50);
            entity.Property(e => e.ReceiverAccountNo).HasMaxLength(100);
            entity.Property(e => e.ReceiverName).HasMaxLength(200);
            entity.Property(e => e.ReceiverCity).HasMaxLength(100);
            entity.Property(e => e.ReceiverCountry).HasMaxLength(3);
            entity.Property(e => e.ReceiverMobileNo).HasMaxLength(50);
            entity.Property(e => e.DuplicateTransactionReceiptNo).HasMaxLength(50);
            entity.Property(e => e.TransactionDescription).HasMaxLength(500);
            
            entity.HasOne(e => e.Recipient)
                .WithMany()
                .HasForeignKey(e => e.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Transaction)
                .WithOne()
                .HasForeignKey<BankAccountDeposit>(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Bank)
                .WithMany()
                .HasForeignKey(e => e.BankId)
                .OnDelete(DeleteBehavior.Restrict);
            
        });

        // Mobile Money Transfer configuration
        modelBuilder.Entity<MobileMoneyTransfer>(entity =>
        {
            entity.ToTable("mobile_money_transfers");
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.PaidToMobileNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ReceiverName).HasMaxLength(200);
            entity.Property(e => e.ReceiverCity).HasMaxLength(100);
            
            entity.HasOne(e => e.Recipient)
                .WithMany()
                .HasForeignKey(e => e.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Transaction)
                .WithOne()
                .HasForeignKey<MobileMoneyTransfer>(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.WalletOperator)
                .WithMany()
                .HasForeignKey(e => e.WalletOperatorId)
                .OnDelete(DeleteBehavior.Restrict);
            
        });

        // Cash Pickup configuration
        modelBuilder.Entity<CashPickup>(entity =>
        {
            entity.ToTable("cash_pickups");
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.MFCN).HasMaxLength(50);
            entity.Property(e => e.RecipientIdentityCardNumber).HasMaxLength(100);
            entity.Property(e => e.AgentStaffName).HasMaxLength(200);
            entity.HasIndex(e => e.MFCN).IsUnique();
            
            entity.HasOne(e => e.Recipient)
                .WithMany()
                .HasForeignKey(e => e.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.NonCardReceiver)
                .WithMany()
                .HasForeignKey(e => e.NonCardReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Transaction)
                .WithOne()
                .HasForeignKey<CashPickup>(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            
        });
        // KiiBank Transfer configuration
        modelBuilder.Entity<KiiBankTransfer>(entity =>
        {
            entity.ToTable("kiibank_transfers");
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.AccountNo).HasMaxLength(100);
            entity.Property(e => e.ReceiverName).HasMaxLength(200);
            entity.Property(e => e.AccountOwnerName).HasMaxLength(200);
            entity.Property(e => e.AccountHolderPhoneNo).HasMaxLength(50);
            entity.Property(e => e.BankBranchCode).HasMaxLength(50);
            entity.Property(e => e.TransactionReference).HasMaxLength(100);
            
            entity.HasOne(e => e.Transaction)
                .WithOne()
                .HasForeignKey<KiiBankTransfer>(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Bank)
                .WithMany()
                .HasForeignKey(e => e.BankId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Card Payment Information configuration
        modelBuilder.Entity<CardPaymentInformation>(entity =>
        {
            entity.ToTable("card_payment_information");
            entity.HasKey(e => e.Id);
            
            // Card information properties
            entity.Property(e => e.NameOnCard).HasMaxLength(200);
            entity.Property(e => e.CardNumber).HasMaxLength(50); // Masked
            entity.Property(e => e.ExpiryDate).HasMaxLength(10); // MM/YY or MM/YYYY
            
            // Indexes for performance
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.CardTransactionId);
            entity.HasIndex(e => e.NonCardTransactionId);
            entity.HasIndex(e => e.TopUpSomeoneElseTransactionId);
            entity.HasIndex(e => e.TransferType);
            
            // Foreign key to Transaction (optional, as legacy uses different IDs)
            entity.HasOne(e => e.Transaction)
                .WithMany()
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.SetNull); // Set null if transaction is deleted
            
            // Note: CardTransactionId, NonCardTransactionId, TopUpSomeoneElseTransactionId
            // are legacy references and may not have direct foreign keys in new structure
        });

        // Reinitialize Transaction configuration
        modelBuilder.Entity<ReinitializeTransaction>(entity =>
        {
            entity.ToTable("reinitialize_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReceiptNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.NewReceiptNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedByName).HasMaxLength(200);
            entity.HasIndex(e => e.ReceiptNo);
            entity.HasIndex(e => e.NewReceiptNo).IsUnique();
            
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Recipient configuration
        modelBuilder.Entity<Recipient>(entity =>
        {
            entity.ToTable("recipients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReceiverName).HasMaxLength(200).IsRequired();
        });

        // Receiver Detail configuration
        modelBuilder.Entity<ReceiverDetail>(entity =>
        {
            entity.ToTable("receiver_details");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(100);
            
            entity.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

