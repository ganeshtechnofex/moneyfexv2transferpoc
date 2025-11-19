using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;

namespace MoneyFex.Infrastructure.Data;

/// <summary>
/// Deterministic database seeder that populates every core table with at least 12 rows
/// so QA and demos have a realistic baseline without running the legacy ETL.
/// </summary>
public static class DbSeeder
{
    private const int MinimumRows = 12;

    public static void SeedDatabase(MoneyFexDbContext context)
    {
        try
        {
            context.Database.EnsureCreated();

            SeedCountries(context);
            SeedBanks(context);
            SeedMobileWalletOperators(context);
            SeedStaff(context);
            SeedSenders(context);
            SeedRecipients(context);
            SeedReceiverDetails(context);

            SeedTransactions(context);
            SeedBankAccountDeposits(context);
            SeedMobileMoneyTransfers(context);
            SeedCashPickups(context);
            SeedKiiBankTransfers(context);
            SeedCardPaymentInformation(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding database: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    #region Reference data

    private static void SeedCountries(MoneyFexDbContext context)
    {
        if (context.Countries.Count() >= MinimumRows)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var countries = new[]
        {
            new Country { CountryCode = "GB", CountryName = "United Kingdom", Currency = "GBP", CurrencySymbol = "£", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "NG", CountryName = "Nigeria", Currency = "NGN", CurrencySymbol = "₦", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "GH", CountryName = "Ghana", Currency = "GHS", CurrencySymbol = "₵", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "KE", CountryName = "Kenya", Currency = "KES", CurrencySymbol = "KSh", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "US", CountryName = "United States", Currency = "USD", CurrencySymbol = "$", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "CA", CountryName = "Canada", Currency = "CAD", CurrencySymbol = "C$", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "AU", CountryName = "Australia", Currency = "AUD", CurrencySymbol = "A$", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "FR", CountryName = "France", Currency = "EUR", CurrencySymbol = "€", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "IE", CountryName = "Ireland", Currency = "EUR", CurrencySymbol = "€", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "IT", CountryName = "Italy", Currency = "EUR", CurrencySymbol = "€", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "ES", CountryName = "Spain", Currency = "EUR", CurrencySymbol = "€", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "DE", CountryName = "Germany", Currency = "EUR", CurrencySymbol = "€", CreatedAt = now, UpdatedAt = now },
            new Country { CountryCode = "ZA", CountryName = "South Africa", Currency = "ZAR", CurrencySymbol = "R", CreatedAt = now, UpdatedAt = now }
        };

        context.Countries.AddRange(countries);
        context.SaveChanges();
    }

    private static void SeedBanks(MoneyFexDbContext context)
    {
        if (context.Banks.Count() >= MinimumRows)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var banks = new List<Bank>
        {
            new Bank { Name = "Access Bank", Code = "ACC", CountryCode = "NG", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "First Bank Nigeria", Code = "FBN", CountryCode = "NG", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "Zenith Bank", Code = "ZEN", CountryCode = "NG", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "GCB Bank", Code = "GCB", CountryCode = "GH", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "Ecobank Ghana", Code = "ECO", CountryCode = "GH", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "Absa Ghana", Code = "ABS", CountryCode = "GH", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "Equity Bank", Code = "EQB", CountryCode = "KE", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "KCB Bank", Code = "KCB", CountryCode = "KE", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "Cooperative Bank Kenya", Code = "COB", CountryCode = "KE", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "Barclays UK", Code = "BAR", CountryCode = "GB", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "Lloyds UK", Code = "LLY", CountryCode = "GB", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "HSBC UK", Code = "HSB", CountryCode = "GB", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Bank { Name = "Bank of America", Code = "BOA", CountryCode = "US", IsActive = true, CreatedAt = now, UpdatedAt = now }
        };

        context.Banks.AddRange(banks);
        context.SaveChanges();
    }

    private static void SeedMobileWalletOperators(MoneyFexDbContext context)
    {
        if (context.MobileWalletOperators.Count() >= MinimumRows)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var operators = new[]
        {
            new MobileWalletOperator { Code = "MTN-NG", Name = "MTN Nigeria", CountryCode = "NG", MobileNetworkCode = "23401", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "AIRTEL-NG", Name = "Airtel Nigeria", CountryCode = "NG", MobileNetworkCode = "23402", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "GLO-NG", Name = "Glo Nigeria", CountryCode = "NG", MobileNetworkCode = "23403", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "MTN-GH", Name = "MTN Ghana", CountryCode = "GH", MobileNetworkCode = "23301", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "AIRTEL-GH", Name = "Airtel Ghana", CountryCode = "GH", MobileNetworkCode = "23302", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "VODA-GH", Name = "Vodafone Cash Ghana", CountryCode = "GH", MobileNetworkCode = "23303", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "MPESA-KE", Name = "M-Pesa Kenya", CountryCode = "KE", MobileNetworkCode = "25401", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "AIRTEL-KE", Name = "Airtel Money Kenya", CountryCode = "KE", MobileNetworkCode = "25402", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "TKASH-KE", Name = "T-Kash Kenya", CountryCode = "KE", MobileNetworkCode = "25403", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "CASHAPP-US", Name = "CashApp USA", CountryCode = "US", MobileNetworkCode = "1-CA", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "VENMO-US", Name = "Venmo USA", CountryCode = "US", MobileNetworkCode = "1-VE", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new MobileWalletOperator { Code = "PAYPAL-US", Name = "PayPal Wallet", CountryCode = "US", MobileNetworkCode = "1-PP", IsActive = true, CreatedAt = now, UpdatedAt = now }
        };

        context.MobileWalletOperators.AddRange(operators);
        context.SaveChanges();
    }

    private static void SeedStaff(MoneyFexDbContext context)
    {
        if (context.Staff.Count() >= MinimumRows)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var titles = new[]
        {
            "Admin", "Compliance", "Payout", "Operations", "Risk", "Audit",
            "Support", "Settlement", "Treasury", "Finance", "AML", "AgentCare"
        };

        var staffMembers = titles.Select((title, index) => new Staff
        {
            FirstName = title,
            LastName = "User",
            Email = $"{title.ToLowerInvariant()}@moneyfex.com",
            IsActive = true,
            CreatedAt = now.AddDays(-30 * (index + 1)),
            UpdatedAt = now
        }).ToList();

        context.Staff.AddRange(staffMembers);
        context.SaveChanges();
    }

    private static void SeedSenders(MoneyFexDbContext context)
    {
        if (context.Senders.Count() >= MinimumRows)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var firstNames = new[] { "Oliver", "Sandra", "Peter", "Grace", "Michael", "Mary", "Daniel", "Sophia", "David", "Emma", "Brian", "Amelia" };
        var lastNames = new[] { "Walker", "Adams", "Mensah", "Browne", "Johnson", "Ibrahim", "Cole", "Williams", "Smith", "Okoro", "Hughes", "Mensima" };
        var countries = new[] { "GB", "US", "CA", "AU" };
        var countryPhone = new Dictionary<string, string> { { "GB", "+44" }, { "US", "+1" }, { "CA", "+1" }, { "AU", "+61" } };

        var senders = new List<Sender>();
        for (int i = 0; i < MinimumRows; i++)
        {
            var country = countries[i % countries.Length];
            senders.Add(new Sender
            {
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Email = $"{firstNames[i].ToLowerInvariant()}.{lastNames[i].ToLowerInvariant()}@example.com",
                PhoneNumber = $"{countryPhone[country]}7700900{i + 100}",
                AccountNo = $"MFX{country}{(1000 + i).ToString()}",
                Address1 = $"{i + 10} High Street",
                City = country == "GB" ? "London" : country == "US" ? "New York" : country == "CA" ? "Toronto" : "Sydney",
                CountryCode = country,
                PostalCode = country == "GB" ? $"SW1A {i + 1}AA" : "10001",
                IsBusiness = i % 4 == 0,
                IsActive = true,
                CreatedAt = now.AddDays(-10 * (i + 1)),
                UpdatedAt = now
            });
        }

        context.Senders.AddRange(senders);
        context.SaveChanges();
    }

    private static void SeedRecipients(MoneyFexDbContext context)
    {
        if (context.Recipients.Count() >= MinimumRows)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var names = new[]
        {
            "Chinedu Okoro", "Ama Koomson", "Joseph Mwangi", "Fatima Ibrahim",
            "Samuel Osei", "Grace Adongo", "Mary Njeri", "Benjamin Banda",
            "Aisha Bello", "Kwame Boateng", "David Otieno", "Linda Mensah"
        };

        var recipients = names.Select(name => new Recipient
        {
            ReceiverName = name,
            CreatedAt = now.AddDays(-3)
        }).ToList();

        context.Recipients.AddRange(recipients);
        context.SaveChanges();
    }

    private static void SeedReceiverDetails(MoneyFexDbContext context)
    {
        if (context.ReceiverDetails.Count() >= MinimumRows)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var cities = new[]
        {
            "Lagos", "Abuja", "Port Harcourt", "Accra", "Kumasi", "Nairobi",
            "Mombasa", "Kisumu", "Johannesburg", "Cape Town", "Pretoria", "Durban"
        };
        var countries = new[] { "NG", "GH", "KE", "ZA" };

        var receiverDetails = new List<ReceiverDetail>();
        for (int i = 0; i < MinimumRows; i++)
        {
            receiverDetails.Add(new ReceiverDetail
            {
                FullName = $"Cash Pickup Receiver {i + 1}",
                PhoneNumber = $"+23480300{i + 1000}",
                City = cities[i],
                CountryCode = countries[i % countries.Length],
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now
            });
        }

        context.ReceiverDetails.AddRange(receiverDetails);
        context.SaveChanges();
    }

    #endregion

    #region Transactions & details

    private static void SeedTransactions(MoneyFexDbContext context)
    {
        if (context.Transactions.Count() >= MinimumRows * 4)
        {
            return;
        }

        var random = new Random(2025);
        var now = DateTime.UtcNow;
        var senders = context.Senders.ToList();
        var staff = context.Staff.ToList();
        var recipients = context.Recipients.ToList();
        var countries = context.Countries.ToDictionary(c => c.CountryCode);

        var receivingCountryCodes = new[] { "NG", "GH", "KE" };
        var paymentModes = new[] { PaymentMode.Card, PaymentMode.BankAccount, PaymentMode.MobileWallet };
        var statuses = new[] { TransactionStatus.Completed, TransactionStatus.Paid, TransactionStatus.InProgress };
        var apiServices = new[] { ApiService.TransferZero, ApiService.VGG, ApiService.Wari, ApiService.EmergentApi };

        var transactions = new List<Transaction>();

        void AddTransaction(string typePrefix, int index, string receivingCountryCode)
        {
            var sender = senders[index % senders.Count];
            var recipient = recipients[index % recipients.Count];
            var staffMember = staff[index % staff.Count];
            var sendingCountry = sender.CountryCode ?? "GB";
            var sendingCountryEntity = countries[sendingCountry];
            var receivingCountry = countries[receivingCountryCode];

            var sendingAmount = 100 + (index * 25);
            var exchangeRate = receivingCountry.Currency switch
            {
                "NGN" => 1200m,
                "GHS" => 13m,
                "KES" => 160m,
                _ => 100m
            };
            var receivingAmount = sendingAmount * exchangeRate;

            var transaction = new Transaction
            {
                ReceiptNo = $"{typePrefix}-{index + 1:0000}",
                TransactionDate = now.AddDays(-index - 1),
                SenderId = sender.Id,
                SendingCountryCode = sendingCountry,
                ReceivingCountryCode = receivingCountryCode,
                SendingCurrency = sendingCountryEntity.Currency,
                ReceivingCurrency = receivingCountry.Currency,
                SendingAmount = sendingAmount,
                ReceivingAmount = receivingAmount,
                Fee = Math.Round(sendingAmount * 0.01m, 2),
                TotalAmount = sendingAmount + Math.Round(sendingAmount * 0.01m, 2),
                ExchangeRate = exchangeRate,
                PaymentReference = $"{typePrefix}-PAY-{index + 1:0000}",
                SenderPaymentMode = paymentModes[index % paymentModes.Length],
                TransactionModule = TransactionModule.Sender,
                Status = statuses[index % statuses.Length],
                ApiService = apiServices[index % apiServices.Length],
                TransferReference = $"{typePrefix}-REF-{index + 1:0000}",
                RecipientId = recipient.Id,
                IsComplianceNeeded = index % 5 == 0,
                IsComplianceApproved = index % 5 != 0,
                ComplianceApprovedBy = index % 5 == 0 ? staffMember.Id : null,
                ComplianceApprovedAt = index % 5 == 0 ? now.AddDays(-index) : null,
                PayingStaffId = staffMember.Id,
                PayingStaffName = $"{staffMember.FirstName} {staffMember.LastName}",
                UpdatedByStaffId = staffMember.Id,
                AgentCommission = Math.Round(sendingAmount * 0.005m, 2),
                ExtraFee = index % 3 == 0 ? 1.50m : (decimal?)null,
                Margin = Math.Round(exchangeRate * 0.01m, 2),
                MFRate = exchangeRate + 0.5m,
                TransferZeroSenderId = index % 4 == 0 ? $"TZ-{1000 + index}" : null,
                ReasonForTransfer = (ReasonForTransfer)(index % Enum.GetValues<ReasonForTransfer>().Length),
                CardProcessorApi = index % 2 == 0 ? CardProcessorApi.TrustPayment : CardProcessorApi.WorldPay,
                IsFromMobile = index % 2 == 1,
                TransactionUpdateDate = now.AddDays(-index / 2),
                CreatedAt = now.AddDays(-index - 2),
                UpdatedAt = now.AddDays(-index)
            };

            transactions.Add(transaction);
        }

        for (int i = 0; i < MinimumRows; i++)
        {
            AddTransaction("BD", i, receivingCountryCodes[0]); // Bank deposits
        }

        for (int i = 0; i < MinimumRows; i++)
        {
            AddTransaction("MM", i + MinimumRows, receivingCountryCodes[1]); // Mobile money
        }

        for (int i = 0; i < MinimumRows; i++)
        {
            AddTransaction("CP", i + MinimumRows * 2, receivingCountryCodes[0]); // Cash pickup
        }

        for (int i = 0; i < MinimumRows; i++)
        {
            AddTransaction("KB", i + MinimumRows * 3, receivingCountryCodes[2]); // KiiBank
        }

        context.Transactions.AddRange(transactions);
        context.SaveChanges();
    }

    private static void SeedBankAccountDeposits(MoneyFexDbContext context)
    {
        if (context.BankAccountDeposits.Count() >= MinimumRows)
        {
            return;
        }

        var transactions = context.Transactions
            .Where(t => t.TransferReference.StartsWith("BD-"))
            .OrderBy(t => t.Id)
            .Take(MinimumRows)
            .ToList();
        var banks = context.Banks.Where(b => b.CountryCode == "NG" || b.CountryCode == "GH").ToList();
        var recipients = context.Recipients.ToList();

        var deposits = transactions.Select((transaction, index) =>
        {
            var bank = banks[index % banks.Count];
            var recipient = recipients[index % recipients.Count];
            return new BankAccountDeposit
            {
                TransactionId = transaction.Id,
                BankId = bank.Id,
                BankName = bank.Name,
                BankCode = bank.Code,
                ReceiverAccountNo = $"{1000000000 + index:D10}",
                ReceiverName = recipient.ReceiverName,
                ReceiverCity = transaction.ReceivingCountryCode == "NG" ? "Lagos" : "Accra",
                ReceiverCountry = transaction.ReceivingCountryCode,
                ReceiverMobileNo = $"+23480300{index + 2000}",
                RecipientId = recipient.Id,
                IsManualDeposit = index % 4 == 0,
                IsManualApprovalNeeded = index % 5 == 0,
                IsManuallyApproved = index % 5 != 0,
                IsEuropeTransfer = transaction.ReceivingCountryCode == "GB" || transaction.ReceivingCountryCode == "FR",
                IsTransactionDuplicated = false,
                IsBusiness = transaction.Sender.IsBusiness,
                HasMadePaymentToBankAccount = true,
                TransactionDescription = $"Deposit for {recipient.ReceiverName}",
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            };
        }).ToList();

        context.BankAccountDeposits.AddRange(deposits);
        context.SaveChanges();
    }

    private static void SeedMobileMoneyTransfers(MoneyFexDbContext context)
    {
        if (context.MobileMoneyTransfers.Count() >= MinimumRows)
        {
            return;
        }

        var transactions = context.Transactions
            .Where(t => t.TransferReference.StartsWith("MM-"))
            .OrderBy(t => t.Id)
            .Take(MinimumRows)
            .ToList();
        var walletOperators = context.MobileWalletOperators.ToList();
        var recipients = context.Recipients.ToList();

        var transfers = transactions.Select((transaction, index) =>
        {
            var wallet = walletOperators[index % walletOperators.Count];
            var recipient = recipients[(index + 3) % recipients.Count];
            return new MobileMoneyTransfer
            {
                TransactionId = transaction.Id,
                WalletOperatorId = wallet.Id,
                PaidToMobileNo = $"+{transaction.ReceivingCountryCode switch { "GH" => "233", "NG" => "234", "KE" => "254", _ => "233" }}2000{index + 1000}",
                ReceiverName = recipient.ReceiverName,
                ReceiverCity = transaction.ReceivingCountryCode == "GH" ? "Accra" : "Lagos",
                RecipientId = recipient.Id,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            };
        }).ToList();

        context.MobileMoneyTransfers.AddRange(transfers);
        context.SaveChanges();
    }

    private static void SeedCashPickups(MoneyFexDbContext context)
    {
        if (context.CashPickups.Count() >= MinimumRows)
        {
            return;
        }

        var transactions = context.Transactions
            .Where(t => t.TransferReference.StartsWith("CP-"))
            .OrderBy(t => t.Id)
            .Take(MinimumRows)
            .ToList();
        var recipients = context.Recipients.ToList();
        var receiverDetails = context.ReceiverDetails.ToList();

        var pickups = transactions.Select((transaction, index) =>
        {
            var recipient = recipients[index % recipients.Count];
            var receiverDetail = receiverDetails[index % receiverDetails.Count];
            return new CashPickup
            {
                TransactionId = transaction.Id,
                MFCN = $"MFX{transaction.Id:000000}",
                RecipientId = recipient.Id,
                NonCardReceiverId = receiverDetail.Id,
                RecipientIdentityCardId = 100 + index,
                RecipientIdentityCardNumber = $"ID-{transaction.Id:000000}",
                IsApprovedByAdmin = index % 2 == 0,
                AgentStaffName = index % 2 == 0 ? "Pickup Agent" : null,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            };
        }).ToList();

        context.CashPickups.AddRange(pickups);
        context.SaveChanges();
    }

    private static void SeedKiiBankTransfers(MoneyFexDbContext context)
    {
        if (context.KiiBankTransfers.Count() >= MinimumRows)
        {
            return;
        }

        var transactions = context.Transactions
            .Where(t => t.TransferReference.StartsWith("KB-"))
            .OrderBy(t => t.Id)
            .Take(MinimumRows)
            .ToList();
        var banks = context.Banks.Where(b => b.CountryCode == "KE" || b.CountryCode == "NG").ToList();

        var kiiTransfers = transactions.Select((transaction, index) =>
        {
            var bank = banks[index % banks.Count];
            return new KiiBankTransfer
            {
                TransactionId = transaction.Id,
                AccountNo = $"998877{index + 1000}",
                ReceiverName = $"Kii Receiver {index + 1}",
                AccountOwnerName = $"Account Owner {index + 1}",
                AccountHolderPhoneNo = $"+25471200{index + 2000}",
                BankId = bank.Id,
                BankBranchId = 10 + index,
                BankBranchCode = $"KEB{index + 1:000}",
                TransactionReference = $"KB-REF-{transaction.Id:0000}",
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            };
        }).ToList();

        context.KiiBankTransfers.AddRange(kiiTransfers);
        context.SaveChanges();
    }

    private static void SeedCardPaymentInformation(MoneyFexDbContext context)
    {
        if (context.CardPaymentInformations.Count() >= MinimumRows)
        {
            return;
        }

        var transactions = context.Transactions
            .OrderBy(t => t.Id)
            .Take(MinimumRows)
            .ToList();

        var now = DateTime.UtcNow;
        var cards = transactions.Select((transaction, index) => new CardPaymentInformation
        {
            TransactionId = transaction.Id,
            CardTransactionId = transaction.Id,
            NameOnCard = $"Card Holder {index + 1}",
            CardNumber = $"****{4000 + index}",
            ExpiryDate = $"{(1 + (index % 12)):00}/{28 + (index % 2)}",
            IsSavedCard = index % 3 == 0,
            AutoRecharged = index % 4 == 0,
            TransferType = (index % 4) switch
            {
                0 => 4, // Bank deposit
                1 => 3, // Mobile wallet
                2 => 2, // Cash pickup
                _ => 7  // KiiBank
            },
            CreatedAt = now.AddDays(-index)
        }).ToList();

        context.CardPaymentInformations.AddRange(cards);
        context.SaveChanges();
    }

    #endregion
}

