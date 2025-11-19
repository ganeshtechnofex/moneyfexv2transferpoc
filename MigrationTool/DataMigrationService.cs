using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;
using MoneyFex.Core.Entities;
using MoneyFex.Core.Entities.Enums;
using System;

namespace MoneyFex.Infrastructure.MigrationTool;

/// <summary>
/// Service for migrating data from legacy SQL Server database to new PostgreSQL database
/// </summary>
public class DataMigrationService
{
    private readonly string _sourceConnectionString;
    private readonly string _targetConnectionString;
    private readonly int _batchSize;
    private readonly bool _enableValidation;
    private readonly string _logPath;
    private readonly MigrationLogger _logger;

    public DataMigrationService(
        string sourceConnectionString,
        string targetConnectionString,
        int batchSize = 1000,
        bool enableValidation = true,
        string logPath = "logs/migration.log")
    {
        _sourceConnectionString = sourceConnectionString;
        _targetConnectionString = targetConnectionString;
        _batchSize = batchSize;
        _enableValidation = enableValidation;
        _logPath = logPath;
        _logger = new MigrationLogger(logPath);
    }

    /// <summary>
    /// Migrate all data from legacy to new database
    /// </summary>
    public async Task<MigrationResult> MigrateAllAsync()
    {
        var result = new MigrationResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInfo("Starting full database migration");

            // Phase 1: Reference Data
            _logger.LogInfo("Phase 1: Migrating reference data");
            await MigrateCountriesAsync();
            await MigrateBanksAsync();
            await MigrateMobileWalletOperatorsAsync();
            await MigrateStaffAsync();

            // Phase 2: User Data
            _logger.LogInfo("Phase 2: Migrating user data");
            await MigrateSendersAsync();
            await MigrateSenderLoginsAsync();
            await MigrateRecipientsAsync();
            await MigrateReceiverDetailsAsync();

            // Phase 3: Transaction Data
            _logger.LogInfo("Phase 3: Migrating transaction data");
            await MigrateBankAccountDepositsAsync();
            await MigrateMobileMoneyTransfersAsync();
            await MigrateCashPickupsAsync();
            await MigrateKiiBankTransfersAsync();
            await MigrateCardPaymentInformationAsync();
            await MigrateReinitializeTransactionsAsync();

            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogInfo($"Migration completed successfully in {result.Duration.TotalMinutes:F2} minutes");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            _logger.LogError($"Migration failed: {ex.Message}", ex);
            throw;
        }

        return result;
    }

    #region Reference Data Migration

    private async Task MigrateCountriesAsync()
    {
        _logger.LogInfo("Migrating countries...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT CountryCode, CountryName, Currency, CurrencySymbol, 
                   CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END AS IsActive
            FROM Country
            WHERE IsDeleted = 0", sourceConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        while (await reader.ReadAsync())
        {
            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO countries (""CountryCode"", ""CountryName"", ""Currency"", ""CurrencySymbol"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@code, @name, @currency, @symbol, @active, @created, @updated)
                ON CONFLICT (""CountryCode"") DO NOTHING", targetConn);

            insertCmd.Parameters.AddWithValue("code", reader["CountryCode"].ToString() ?? "");
            insertCmd.Parameters.AddWithValue("name", reader["CountryName"].ToString() ?? "");
            insertCmd.Parameters.AddWithValue("currency", reader["Currency"].ToString() ?? "");
            insertCmd.Parameters.AddWithValue("symbol", reader["CurrencySymbol"].ToString() ?? "");
            insertCmd.Parameters.AddWithValue("active", reader["IsActive"] as bool? ?? true);
            insertCmd.Parameters.AddWithValue("created", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();
            count++;
        }

        _logger.LogInfo($"Migrated {count} countries");
    }

    private async Task MigrateBanksAsync()
    {
        _logger.LogInfo("Migrating banks...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, Name, Code, CountryCode, IsDeleted
            FROM Bank
            WHERE IsDeleted = 0", sourceConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        while (await reader.ReadAsync())
        {
            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO banks (""Id"", ""Name"", ""Code"", ""CountryCode"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@id, @name, @code, @country, @active, @created, @updated)
                ON CONFLICT (""Id"") DO UPDATE SET
                    ""Name"" = EXCLUDED.""Name"",
                    ""Code"" = EXCLUDED.""Code"",
                    ""CountryCode"" = EXCLUDED.""CountryCode"",
                    ""IsActive"" = EXCLUDED.""IsActive"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            insertCmd.Parameters.AddWithValue("id", GetIntValue(reader["Id"]));
            insertCmd.Parameters.AddWithValue("name", reader["Name"].ToString() ?? "");
            insertCmd.Parameters.AddWithValue("code", reader["Code"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("country", reader["CountryCode"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("active", !(reader["IsDeleted"] as bool? ?? false));
            insertCmd.Parameters.AddWithValue("created", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();
            count++;
        }

        _logger.LogInfo($"Migrated {count} banks");
    }

    private async Task MigrateMobileWalletOperatorsAsync()
    {
        _logger.LogInfo("Migrating mobile wallet operators...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, Code, Name, Country, MobileNetworkCode, PayoutProviderId, IsDeleted
            FROM MobileWalletOperator
            WHERE IsDeleted = 0", sourceConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        while (await reader.ReadAsync())
        {
            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO mobile_wallet_operators (""Id"", ""Code"", ""Name"", ""CountryCode"", ""MobileNetworkCode"", ""PayoutProviderId"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@id, @code, @name, @country, @mnc, @provider, @active, @created, @updated)
                ON CONFLICT (""Id"") DO UPDATE SET
                    ""Code"" = EXCLUDED.""Code"",
                    ""Name"" = EXCLUDED.""Name"",
                    ""CountryCode"" = EXCLUDED.""CountryCode"",
                    ""MobileNetworkCode"" = EXCLUDED.""MobileNetworkCode"",
                    ""PayoutProviderId"" = EXCLUDED.""PayoutProviderId"",
                    ""IsActive"" = EXCLUDED.""IsActive"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            insertCmd.Parameters.AddWithValue("id", GetIntValue(reader["Id"]));
            insertCmd.Parameters.AddWithValue("code", reader["Code"].ToString() ?? "");
            insertCmd.Parameters.AddWithValue("name", reader["Name"].ToString() ?? "");
            insertCmd.Parameters.AddWithValue("country", reader["Country"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("mnc", reader["MobileNetworkCode"]?.ToString() ?? (object)DBNull.Value);
            var providerId = GetIntValueOrNull(reader["PayoutProviderId"]);
            insertCmd.Parameters.AddWithValue("provider", providerId.HasValue ? (object)providerId.Value : DBNull.Value);
            insertCmd.Parameters.AddWithValue("active", !(reader["IsDeleted"] as bool? ?? false));
            insertCmd.Parameters.AddWithValue("created", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();
            count++;
        }

        _logger.LogInfo($"Migrated {count} mobile wallet operators");
    }

    private async Task MigrateStaffAsync()
    {
        _logger.LogInfo("Migrating staff...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, FirstName, MiddleName, LastName, EmailAddress
            FROM StaffInformation", sourceConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        while (await reader.ReadAsync())
        {
            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO staff (""Id"", ""FirstName"", ""MiddleName"", ""LastName"", ""Email"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@id, @first, @middle, @last, @email, @active, @created, @updated)
                ON CONFLICT (""Id"") DO UPDATE SET
                    ""FirstName"" = EXCLUDED.""FirstName"",
                    ""MiddleName"" = EXCLUDED.""MiddleName"",
                    ""LastName"" = EXCLUDED.""LastName"",
                    ""Email"" = EXCLUDED.""Email"",
                    ""IsActive"" = EXCLUDED.""IsActive"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            insertCmd.Parameters.AddWithValue("id", GetIntValue(reader["Id"]));
            insertCmd.Parameters.AddWithValue("first", reader["FirstName"]?.ToString() ?? "");
            insertCmd.Parameters.AddWithValue("middle", reader["MiddleName"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("last", reader["LastName"]?.ToString() ?? "");
            insertCmd.Parameters.AddWithValue("email", reader["EmailAddress"]?.ToString() ?? "");
            // StaffInformation table doesn't have IsDeleted, so set all as active
            insertCmd.Parameters.AddWithValue("active", true);
            insertCmd.Parameters.AddWithValue("created", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();
            count++;
        }

        _logger.LogInfo($"Migrated {count} staff members");
    }

    #endregion

    #region User Data Migration

    private async Task MigrateSendersAsync()
    {
        _logger.LogInfo("Migrating senders...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, FirstName, MiddleName, LastName, Email, PhoneNumber, AccountNo,
                   Address1, Address2, City, State, Country, PostalCode, IsBusiness, CreatedDate, IsDeleted
            FROM FaxerInformation
            WHERE IsDeleted = 0", sourceConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        // First, get list of valid country codes
        var validCountryCodes = new HashSet<string>();
        using (var countryCheckCmd = new NpgsqlCommand(@"SELECT ""CountryCode"" FROM countries", targetConn))
        using (var countryReader = await countryCheckCmd.ExecuteReaderAsync())
        {
            while (await countryReader.ReadAsync())
            {
                validCountryCodes.Add(countryReader["CountryCode"].ToString() ?? "");
            }
        }

        while (await reader.ReadAsync())
        {
            // Validate country code exists
            var countryCode = reader["Country"]?.ToString();
            if (!string.IsNullOrEmpty(countryCode) && !validCountryCodes.Contains(countryCode))
            {
                _logger.LogInfo($"Warning: Sender {reader["Id"]} has invalid country code '{countryCode}', setting to NULL");
                countryCode = null;
            }

            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO senders (""Id"", ""FirstName"", ""MiddleName"", ""LastName"", ""Email"", ""PhoneNumber"", ""AccountNo"",
                                    ""Address1"", ""Address2"", ""City"", ""State"", ""CountryCode"", ""PostalCode"", ""IsBusiness"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@id, @first, @middle, @last, @email, @phone, @account, @addr1, @addr2, @city, @state, @country, @postal, @business, @active, @created, @updated)
                ON CONFLICT (""Id"") DO UPDATE SET
                    ""FirstName"" = EXCLUDED.""FirstName"",
                    ""MiddleName"" = EXCLUDED.""MiddleName"",
                    ""LastName"" = EXCLUDED.""LastName"",
                    ""Email"" = EXCLUDED.""Email"",
                    ""PhoneNumber"" = EXCLUDED.""PhoneNumber"",
                    ""AccountNo"" = EXCLUDED.""AccountNo"",
                    ""Address1"" = EXCLUDED.""Address1"",
                    ""Address2"" = EXCLUDED.""Address2"",
                    ""City"" = EXCLUDED.""City"",
                    ""State"" = EXCLUDED.""State"",
                    ""CountryCode"" = EXCLUDED.""CountryCode"",
                    ""PostalCode"" = EXCLUDED.""PostalCode"",
                    ""IsBusiness"" = EXCLUDED.""IsBusiness"",
                    ""IsActive"" = EXCLUDED.""IsActive"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            insertCmd.Parameters.AddWithValue("id", GetIntValue(reader["Id"]));
            insertCmd.Parameters.AddWithValue("first", reader["FirstName"]?.ToString() ?? "");
            insertCmd.Parameters.AddWithValue("middle", reader["MiddleName"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("last", reader["LastName"]?.ToString() ?? "");
            insertCmd.Parameters.AddWithValue("email", reader["Email"]?.ToString() ?? "");
            insertCmd.Parameters.AddWithValue("phone", reader["PhoneNumber"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("account", reader["AccountNo"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("addr1", reader["Address1"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("addr2", reader["Address2"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("city", reader["City"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("state", reader["State"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("country", string.IsNullOrEmpty(countryCode) ? (object)DBNull.Value : countryCode);
            insertCmd.Parameters.AddWithValue("postal", reader["PostalCode"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("business", reader["IsBusiness"] as bool? ?? false);
            insertCmd.Parameters.AddWithValue("active", true);
            insertCmd.Parameters.AddWithValue("created", reader["CreatedDate"] as DateTime? ?? DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            try
            {
                await insertCmd.ExecuteNonQueryAsync();
                count++;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // Duplicate key error - log and continue
                _logger.LogInfo($"Warning: Duplicate sender ID {reader["Id"]} detected, skipping duplicate record");
                continue;
            }
        }

        _logger.LogInfo($"Migrated {count} senders");
    }

    private async Task MigrateSenderLoginsAsync()
    {
        _logger.LogInfo("Migrating sender logins...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT FaxerId, IsActive
            FROM FaxerLogin
            WHERE IsActive = 1", sourceConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        while (await reader.ReadAsync())
        {
            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO sender_logins (""SenderId"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@senderId, @active, @created, @updated)
                ON CONFLICT (""SenderId"") DO UPDATE SET
                    ""IsActive"" = EXCLUDED.""IsActive"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            insertCmd.Parameters.AddWithValue("senderId", GetIntValue(reader["FaxerId"]));
            insertCmd.Parameters.AddWithValue("active", reader["IsActive"] as bool? ?? true);
            insertCmd.Parameters.AddWithValue("created", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();
            count++;
        }

        _logger.LogInfo($"Migrated {count} sender logins");
    }

    private async Task MigrateRecipientsAsync()
    {
        _logger.LogInfo("Migrating recipients...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, ReceiverName
            FROM Recipients
            WHERE IsDeleted = 0", sourceConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        while (await reader.ReadAsync())
        {
            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO recipients (""Id"", ""ReceiverName"", ""CreatedAt"")
                VALUES (@id, @name, @created)
                ON CONFLICT (""Id"") DO UPDATE SET
                    ""ReceiverName"" = EXCLUDED.""ReceiverName""", targetConn);

            insertCmd.Parameters.AddWithValue("id", GetIntValue(reader["Id"]));
            insertCmd.Parameters.AddWithValue("name", reader["ReceiverName"]?.ToString() ?? "");
            insertCmd.Parameters.AddWithValue("created", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();
            count++;
        }

        _logger.LogInfo($"Migrated {count} recipients");
    }

    private async Task MigrateReceiverDetailsAsync()
    {
        _logger.LogInfo("Migrating receiver details...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, FirstName, MiddleName, LastName, PhoneNumber, City, Country
            FROM ReceiversDetails", sourceConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        while (await reader.ReadAsync())
        {
            var fullName = $"{reader["FirstName"]} {reader["MiddleName"]} {reader["LastName"]}".Trim();
            
            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO receiver_details (""Id"", ""FullName"", ""PhoneNumber"", ""City"", ""CountryCode"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@id, @name, @phone, @city, @country, @created, @updated)
                ON CONFLICT (""Id"") DO UPDATE SET
                    ""FullName"" = EXCLUDED.""FullName"",
                    ""PhoneNumber"" = EXCLUDED.""PhoneNumber"",
                    ""City"" = EXCLUDED.""City"",
                    ""CountryCode"" = EXCLUDED.""CountryCode"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            insertCmd.Parameters.AddWithValue("id", GetIntValue(reader["Id"]));
            insertCmd.Parameters.AddWithValue("name", fullName);
            insertCmd.Parameters.AddWithValue("phone", reader["PhoneNumber"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("city", reader["City"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("country", reader["Country"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("created", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();
            count++;
        }

        _logger.LogInfo($"Migrated {count} receiver details");
    }

    #endregion

    #region Transaction Data Migration

    private async Task MigrateBankAccountDepositsAsync()
    {
        _logger.LogInfo("Migrating bank account deposits...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT TransactionId, ReceiptNo, TransactionDate, SenderId, SendingCountry, ReceivingCountry,
                   SendingCurrency, ReceivingCurrency, SendingAmount, ReceivingAmount, Fee, TotalAmount,
                   ExchangeRate, PaymentReference, SenderPaymentMode, Status, PayingStaffId, Apiservice,
                   BankId, BankName, BankCode, ReceiverAccountNo, ReceiverName, ReceiverCity,
                   IsManualDeposit, IsManualApproveNeeded, ManuallyApproved, IsEuropeTransfer,
                   IsTransactionDuplicated, DuplicateTransactionReceiptNo, AgentCommission, ExtraFee,
                   Margin, MFRate, TransferZeroSenderId, TransferReference, ReasonForTransfer,
                   CardProcessorApi, IsFromMobile, TransactionUpdateDate, IsComplianceNeededForTrans,
                   IsComplianceApproved, ComplianceApprovedBy, ComplianceApprovedDate, PayingStaffName,
                   UpdateByStaffId
            FROM BankAccountDeposit", sourceConn);

        // Get valid staff IDs for foreign key validation
        var validStaffIds = new HashSet<int>();
        using (var staffCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM staff", targetConn))
        using (var staffReader = await staffCheckCmd.ExecuteReaderAsync())
        {
            while (await staffReader.ReadAsync())
            {
                validStaffIds.Add(GetIntValue(staffReader["Id"]));
            }
        }

        // Get valid sender IDs for foreign key validation
        var validSenderIds = new HashSet<int>();
        using (var senderCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM senders", targetConn))
        using (var senderReader = await senderCheckCmd.ExecuteReaderAsync())
        {
            while (await senderReader.ReadAsync())
            {
                validSenderIds.Add(GetIntValue(senderReader["Id"]));
            }
        }

        using var reader = await selectCmd.ExecuteReaderAsync();
        var transactionCount = 0;
        var depositCount = 0;

        while (await reader.ReadAsync())
        {
            // Insert Transaction
            var transactionId = await InsertTransactionAsync(targetConn, reader, TransactionModule.Sender, "ReceiptNo", "SendingAmount", "Fee", "Status", validStaffIds, validSenderIds);
            if (transactionId == -1)
            {
                _logger.LogInfo($"Warning: Skipping transaction {reader["ReceiptNo"]} due to invalid sender ID");
                continue;
            }
            transactionCount++;

            // Insert BankAccountDeposit
            var depositCmd = new NpgsqlCommand(@"
                INSERT INTO bank_account_deposits (""TransactionId"", ""BankId"", ""BankName"", ""BankCode"", ""ReceiverAccountNo"",
                                                   ""ReceiverName"", ""ReceiverCity"", ""IsManualDeposit"", ""IsManualApprovalNeeded"",
                                                   ""IsManuallyApproved"", ""IsEuropeTransfer"", ""IsTransactionDuplicated"",
                                                   ""DuplicateTransactionReceiptNo"", ""IsBusiness"", ""HasMadePaymentToBankAccount"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@tid, @bankId, @bankName, @bankCode, @accountNo, @name, @city, @manual, @manualNeeded,
                        @approved, @europe, @duplicated, @dupReceipt, @isBusiness, @hasMadePayment, @created, @updated)
                ON CONFLICT (""TransactionId"") DO UPDATE SET
                    ""BankId"" = EXCLUDED.""BankId"",
                    ""BankName"" = EXCLUDED.""BankName"",
                    ""BankCode"" = EXCLUDED.""BankCode"",
                    ""ReceiverAccountNo"" = EXCLUDED.""ReceiverAccountNo"",
                    ""ReceiverName"" = EXCLUDED.""ReceiverName"",
                    ""ReceiverCity"" = EXCLUDED.""ReceiverCity"",
                    ""IsManualDeposit"" = EXCLUDED.""IsManualDeposit"",
                    ""IsManualApprovalNeeded"" = EXCLUDED.""IsManualApprovalNeeded"",
                    ""IsManuallyApproved"" = EXCLUDED.""IsManuallyApproved"",
                    ""IsEuropeTransfer"" = EXCLUDED.""IsEuropeTransfer"",
                    ""IsTransactionDuplicated"" = EXCLUDED.""IsTransactionDuplicated"",
                    ""DuplicateTransactionReceiptNo"" = EXCLUDED.""DuplicateTransactionReceiptNo"",
                    ""IsBusiness"" = EXCLUDED.""IsBusiness"",
                    ""HasMadePaymentToBankAccount"" = EXCLUDED.""HasMadePaymentToBankAccount"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            depositCmd.Parameters.AddWithValue("tid", transactionId);
            var bankId = GetIntValueOrNull(reader["BankId"]);
            depositCmd.Parameters.AddWithValue("bankId", bankId.HasValue ? (object)bankId.Value : DBNull.Value);
            depositCmd.Parameters.AddWithValue("bankName", reader["BankName"]?.ToString() ?? (object)DBNull.Value);
            depositCmd.Parameters.AddWithValue("bankCode", reader["BankCode"]?.ToString() ?? (object)DBNull.Value);
            depositCmd.Parameters.AddWithValue("accountNo", reader["ReceiverAccountNo"]?.ToString() ?? (object)DBNull.Value);
            depositCmd.Parameters.AddWithValue("name", reader["ReceiverName"]?.ToString() ?? (object)DBNull.Value);
            depositCmd.Parameters.AddWithValue("city", reader["ReceiverCity"]?.ToString() ?? (object)DBNull.Value);
            depositCmd.Parameters.AddWithValue("manual", reader["IsManualDeposit"] as bool? ?? false);
            depositCmd.Parameters.AddWithValue("manualNeeded", reader["IsManualApproveNeeded"] as bool? ?? false);
            depositCmd.Parameters.AddWithValue("approved", reader["ManuallyApproved"] as bool? ?? false);
            depositCmd.Parameters.AddWithValue("europe", reader["IsEuropeTransfer"] as bool? ?? false);
            depositCmd.Parameters.AddWithValue("duplicated", reader["IsTransactionDuplicated"] as bool? ?? false);
            depositCmd.Parameters.AddWithValue("dupReceipt", reader["DuplicateTransactionReceiptNo"]?.ToString() ?? (object)DBNull.Value);
            depositCmd.Parameters.AddWithValue("isBusiness", GetBoolOrFalse(reader, "IsBusiness"));
            depositCmd.Parameters.AddWithValue("hasMadePayment", GetBoolOrFalse(reader, "HasMadePaymentToBankAccount"));
            depositCmd.Parameters.AddWithValue("created", reader["TransactionDate"] as DateTime? ?? DateTime.UtcNow);
            depositCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            await depositCmd.ExecuteNonQueryAsync();
            depositCount++;
        }

        _logger.LogInfo($"Migrated {transactionCount} transactions and {depositCount} bank account deposits");
    }

    private async Task MigrateMobileMoneyTransfersAsync()
    {
        _logger.LogInfo("Migrating mobile money transfers...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, ReceiptNo, TransactionDate, SenderId, SendingCountry, ReceivingCountry,
                   SendingCurrency, ReceivingCurrency, SendingAmount, ReceivingAmount, Fee, TotalAmount,
                   ExchangeRate, PaymentReference, SenderPaymentMode, Status, PayingStaffId, Apiservice,
                   WalletOperatorId, PaidToMobileNo, ReceiverName, ReceiverCity, AgentCommission, ExtraFee,
                   Margin, MFRate, TransferZeroSenderId, TransferReference, ReasonForTransfer,
                   CardProcessorApi, IsFromMobile, IsComplianceNeededForTrans, IsComplianceApproved,
                   ComplianceApprovedBy, ComplianceApprovedDate, PayingStaffName, UpdateByStaffId
            FROM MobileMoneyTransfer", sourceConn);

        // Get valid staff IDs for foreign key validation
        var validStaffIds = new HashSet<int>();
        using (var staffCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM staff", targetConn))
        using (var staffReader = await staffCheckCmd.ExecuteReaderAsync())
        {
            while (await staffReader.ReadAsync())
            {
                validStaffIds.Add(GetIntValue(staffReader["Id"]));
            }
        }

        // Get valid sender IDs for foreign key validation
        var validSenderIds = new HashSet<int>();
        using (var senderCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM senders", targetConn))
        using (var senderReader = await senderCheckCmd.ExecuteReaderAsync())
        {
            while (await senderReader.ReadAsync())
            {
                validSenderIds.Add(GetIntValue(senderReader["Id"]));
            }
        }

        // Get valid wallet operator IDs for foreign key validation
        var validWalletOperatorIds = new HashSet<int>();
        using (var walletCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM mobile_wallet_operators", targetConn))
        using (var walletReader = await walletCheckCmd.ExecuteReaderAsync())
        {
            while (await walletReader.ReadAsync())
            {
                validWalletOperatorIds.Add(GetIntValue(walletReader["Id"]));
            }
        }

        using var reader = await selectCmd.ExecuteReaderAsync();
        var transactionCount = 0;
        var transferCount = 0;

        while (await reader.ReadAsync())
        {
            // Insert Transaction
            var transactionId = await InsertTransactionAsync(targetConn, reader, TransactionModule.Sender, "ReceiptNo", "SendingAmount", "Fee", "Status", validStaffIds, validSenderIds);
            if (transactionId == -1)
            {
                _logger.LogInfo($"Warning: Skipping transaction {reader["ReceiptNo"]} due to invalid sender ID");
                continue;
            }
            transactionCount++;

            // Validate wallet operator ID
            var walletId = GetIntValue(reader["WalletOperatorId"]);
            if (!validWalletOperatorIds.Contains(walletId))
            {
                _logger.LogInfo($"Warning: Mobile money transfer {reader["ReceiptNo"]} has invalid wallet operator ID {walletId}, skipping transfer record");
                continue; // Skip this transfer but transaction is already inserted
            }

            // Insert MobileMoneyTransfer
            var transferCmd = new NpgsqlCommand(@"
                INSERT INTO mobile_money_transfers (""TransactionId"", ""WalletOperatorId"", ""PaidToMobileNo"",
                                                   ""ReceiverName"", ""ReceiverCity"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@tid, @walletId, @mobile, @name, @city, @created, @updated)
                ON CONFLICT (""TransactionId"") DO UPDATE SET
                    ""WalletOperatorId"" = EXCLUDED.""WalletOperatorId"",
                    ""PaidToMobileNo"" = EXCLUDED.""PaidToMobileNo"",
                    ""ReceiverName"" = EXCLUDED.""ReceiverName"",
                    ""ReceiverCity"" = EXCLUDED.""ReceiverCity"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            transferCmd.Parameters.AddWithValue("tid", transactionId);
            transferCmd.Parameters.AddWithValue("walletId", walletId);
            transferCmd.Parameters.AddWithValue("mobile", reader["PaidToMobileNo"]?.ToString() ?? "");
            transferCmd.Parameters.AddWithValue("name", reader["ReceiverName"]?.ToString() ?? (object)DBNull.Value);
            transferCmd.Parameters.AddWithValue("city", reader["ReceiverCity"]?.ToString() ?? (object)DBNull.Value);
            transferCmd.Parameters.AddWithValue("created", reader["TransactionDate"] as DateTime? ?? DateTime.UtcNow);
            transferCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            await transferCmd.ExecuteNonQueryAsync();
            transferCount++;
        }

        _logger.LogInfo($"Migrated {transactionCount} transactions and {transferCount} mobile money transfers");
    }

    private async Task MigrateCashPickupsAsync()
    {
        _logger.LogInfo("Migrating cash pickups...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, ReceiptNumber, TransactionDate, SenderId, SendingCountry, ReceivingCountry,
                   SendingCurrency, ReceivingCurrency, FaxingAmount, ReceivingAmount, FaxingFee, TotalAmount,
                   ExchangeRate, PaymentReference, SenderPaymentMode, FaxingStatus, PayingStaffId, Apiservice,
                   MFCN, RecipientId, NonCardRecieverId, AgentCommission, ExtraFee, Margin, MFRate,
                   TransferZeroSenderId, TransferReference, Reason, CardProcessorApi, IsFromMobile,
                   IsComplianceNeededForTrans, IsComplianceApproved, ComplianceApprovedBy, ComplianceApprovedDate,
                   AgentStaffName, UpdatedByStaffId
            FROM FaxingNonCardTransaction", sourceConn);

        // Get valid staff IDs for foreign key validation
        var validStaffIds = new HashSet<int>();
        using (var staffCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM staff", targetConn))
        using (var staffReader = await staffCheckCmd.ExecuteReaderAsync())
        {
            while (await staffReader.ReadAsync())
            {
                validStaffIds.Add(GetIntValue(staffReader["Id"]));
            }
        }

        // Get valid sender IDs for foreign key validation
        var validSenderIds = new HashSet<int>();
        using (var senderCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM senders", targetConn))
        using (var senderReader = await senderCheckCmd.ExecuteReaderAsync())
        {
            while (await senderReader.ReadAsync())
            {
                validSenderIds.Add(GetIntValue(senderReader["Id"]));
            }
        }

        // Get valid recipient IDs for foreign key validation
        var validRecipientIds = new HashSet<int>();
        using (var recipientCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM recipients", targetConn))
        using (var recipientReader = await recipientCheckCmd.ExecuteReaderAsync())
        {
            while (await recipientReader.ReadAsync())
            {
                validRecipientIds.Add(GetIntValue(recipientReader["Id"]));
            }
        }

        using var reader = await selectCmd.ExecuteReaderAsync();
        var transactionCount = 0;
        var pickupCount = 0;

        while (await reader.ReadAsync())
        {
            // Insert Transaction
            var transactionId = await InsertTransactionAsync(targetConn, reader, TransactionModule.Sender, "ReceiptNumber", "FaxingAmount", "FaxingFee", "FaxingStatus", validStaffIds, validSenderIds);
            if (transactionId == -1)
            {
                _logger.LogInfo($"Warning: Skipping transaction {reader["ReceiptNumber"]} due to invalid sender ID");
                continue;
            }
            transactionCount++;

            // Validate recipient ID
            var recipientId = GetIntValueOrNull(reader["RecipientId"]);
            if (recipientId.HasValue && !validRecipientIds.Contains(recipientId.Value))
            {
                _logger.LogInfo($"Warning: Cash pickup {reader["ReceiptNumber"]} has invalid recipient ID {recipientId.Value}, setting to NULL");
                recipientId = null;
            }

            // Insert CashPickup
            var pickupCmd = new NpgsqlCommand(@"
                INSERT INTO cash_pickups (""TransactionId"", ""MFCN"", ""RecipientId"", ""NonCardReceiverId"", ""IsApprovedByAdmin"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@tid, @mfcn, @recipientId, @receiverId, @approved, @created, @updated)
                ON CONFLICT (""TransactionId"") DO UPDATE SET
                    ""MFCN"" = EXCLUDED.""MFCN"",
                    ""RecipientId"" = EXCLUDED.""RecipientId"",
                    ""NonCardReceiverId"" = EXCLUDED.""NonCardReceiverId"",
                    ""IsApprovedByAdmin"" = EXCLUDED.""IsApprovedByAdmin"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            pickupCmd.Parameters.AddWithValue("tid", transactionId);
            pickupCmd.Parameters.AddWithValue("mfcn", reader["MFCN"]?.ToString() ?? (object)DBNull.Value);
            pickupCmd.Parameters.AddWithValue("recipientId", recipientId.HasValue ? (object)recipientId.Value : DBNull.Value);
            var receiverId = GetIntValueOrNull(reader["NonCardRecieverId"]);
            pickupCmd.Parameters.AddWithValue("receiverId", receiverId.HasValue ? (object)receiverId.Value : DBNull.Value);
            pickupCmd.Parameters.AddWithValue("approved", GetBoolOrFalse(reader, "IsApprovedByAdmin"));
            pickupCmd.Parameters.AddWithValue("created", reader["TransactionDate"] as DateTime? ?? DateTime.UtcNow);
            pickupCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

            await pickupCmd.ExecuteNonQueryAsync();
            pickupCount++;
        }

        _logger.LogInfo($"Migrated {transactionCount} transactions and {pickupCount} cash pickups");
    }

    private async Task MigrateCardPaymentInformationAsync()
    {
        _logger.LogInfo("Migrating card payment information...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, CardTransactionId, NonCardTransactionId, TopUpSomeoneElseTransactionId,
                   NameOnCard, CardNumber, ExpiryDate, IsSavedCard, AutoRecharged, TransferType, CreatedDate
            FROM CardTopUpCreditDebitInformation", sourceConn);

        // Build a mapping of legacy transaction IDs to new transaction IDs by receipt number
        // First, get all receipt numbers from target database
        var receiptToTransactionIdMap = new Dictionary<string, int>();
        using (var receiptCmd = new NpgsqlCommand(@"SELECT ""Id"", ""ReceiptNo"" FROM transactions", targetConn))
        using (var receiptReader = await receiptCmd.ExecuteReaderAsync())
        {
            while (await receiptReader.ReadAsync())
            {
                var receiptNo = receiptReader["ReceiptNo"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(receiptNo))
                {
                    receiptToTransactionIdMap[receiptNo] = GetIntValue(receiptReader["Id"]);
                }
            }
        }

        var transactionIdMap = new Dictionary<int, int>();
        
        // Map from BankAccountDeposit
        using (var mapCmd = new SqlCommand(@"
            SELECT TransactionId, ReceiptNo
            FROM BankAccountDeposit", sourceConn))
        using (var mapReader = await mapCmd.ExecuteReaderAsync())
        {
            while (await mapReader.ReadAsync())
            {
                var legacyId = GetIntValue(mapReader["TransactionId"]);
                var receiptNo = mapReader["ReceiptNo"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(receiptNo) && receiptToTransactionIdMap.ContainsKey(receiptNo))
                {
                    transactionIdMap[legacyId] = receiptToTransactionIdMap[receiptNo];
                }
            }
        }

        // Map from MobileMoneyTransfer
        using (var mapCmd = new SqlCommand(@"
            SELECT Id, ReceiptNo
            FROM MobileMoneyTransfer", sourceConn))
        using (var mapReader = await mapCmd.ExecuteReaderAsync())
        {
            while (await mapReader.ReadAsync())
            {
                var legacyId = GetIntValue(mapReader["Id"]);
                var receiptNo = mapReader["ReceiptNo"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(receiptNo) && receiptToTransactionIdMap.ContainsKey(receiptNo))
                {
                    transactionIdMap[legacyId] = receiptToTransactionIdMap[receiptNo];
                }
            }
        }

        // Map from FaxingNonCardTransaction
        using (var mapCmd = new SqlCommand(@"
            SELECT Id, ReceiptNumber
            FROM FaxingNonCardTransaction", sourceConn))
        using (var mapReader = await mapCmd.ExecuteReaderAsync())
        {
            while (await mapReader.ReadAsync())
            {
                var legacyId = GetIntValue(mapReader["Id"]);
                var receiptNo = mapReader["ReceiptNumber"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(receiptNo) && receiptToTransactionIdMap.ContainsKey(receiptNo))
                {
                    transactionIdMap[legacyId] = receiptToTransactionIdMap[receiptNo];
                }
            }
        }

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        while (await reader.ReadAsync())
        {
            // Determine the TransactionId from the legacy transaction IDs
            int? transactionId = null;
            
            var cardTransactionId = GetIntValueOrNull(reader["CardTransactionId"]);
            var nonCardTransactionId = GetIntValueOrNull(reader["NonCardTransactionId"]);
            var topUpTransactionId = GetIntValueOrNull(reader["TopUpSomeoneElseTransactionId"]);

            // Try to find the new transaction ID from the mapping
            if (cardTransactionId.HasValue && transactionIdMap.ContainsKey(cardTransactionId.Value))
            {
                transactionId = transactionIdMap[cardTransactionId.Value];
            }
            else if (nonCardTransactionId.HasValue && transactionIdMap.ContainsKey(nonCardTransactionId.Value))
            {
                transactionId = transactionIdMap[nonCardTransactionId.Value];
            }
            else if (topUpTransactionId.HasValue && transactionIdMap.ContainsKey(topUpTransactionId.Value))
            {
                transactionId = transactionIdMap[topUpTransactionId.Value];
            }

            // Try to preserve original ID if possible, but since Id is SERIAL, we'll let it auto-generate
            // Use a combination of CardTransactionId/NonCardTransactionId/TopUpTransactionId to detect duplicates
            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO card_payment_information (""TransactionId"", ""CardTransactionId"", ""NonCardTransactionId"", 
                                                      ""TopUpSomeoneElseTransactionId"", ""NameOnCard"", ""CardNumber"", 
                                                      ""ExpiryDate"", ""IsSavedCard"", ""AutoRecharged"", ""TransferType"", ""CreatedAt"")
                VALUES (@transactionId, @cardTransactionId, @nonCardTransactionId, @topUpTransactionId, @nameOnCard, 
                        @cardNumber, @expiryDate, @isSavedCard, @autoRecharged, @transferType, @createdAt)", targetConn);

            insertCmd.Parameters.AddWithValue("transactionId", transactionId.HasValue ? (object)transactionId.Value : DBNull.Value);
            insertCmd.Parameters.AddWithValue("cardTransactionId", cardTransactionId.HasValue ? (object)cardTransactionId.Value : DBNull.Value);
            insertCmd.Parameters.AddWithValue("nonCardTransactionId", nonCardTransactionId.HasValue ? (object)nonCardTransactionId.Value : DBNull.Value);
            insertCmd.Parameters.AddWithValue("topUpTransactionId", topUpTransactionId.HasValue ? (object)topUpTransactionId.Value : DBNull.Value);
            insertCmd.Parameters.AddWithValue("nameOnCard", reader["NameOnCard"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("cardNumber", reader["CardNumber"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("expiryDate", reader["ExpiryDate"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("isSavedCard", reader["IsSavedCard"] as bool? ?? false);
            insertCmd.Parameters.AddWithValue("autoRecharged", reader["AutoRecharged"] as bool? ?? false);
            insertCmd.Parameters.AddWithValue("transferType", GetIntValue(reader["TransferType"]));
            insertCmd.Parameters.AddWithValue("createdAt", reader["CreatedDate"] as DateTime? ?? DateTime.UtcNow);

            // Note: We're not setting the Id parameter, so PostgreSQL will auto-generate it
            // If we need to preserve the original ID, we should add it to the INSERT statement

            try
            {
                await insertCmd.ExecuteNonQueryAsync();
                count++;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // Duplicate key error - log and continue (might be duplicate transaction reference)
                _logger.LogInfo($"Warning: Duplicate card payment information detected for transaction ID {transactionId}, skipping duplicate record");
                continue;
            }
        }

        _logger.LogInfo($"Migrated {count} card payment information records");
    }

    private async Task MigrateReinitializeTransactionsAsync()
    {
        _logger.LogInfo("Migrating reinitialize transactions...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        var selectCmd = new SqlCommand(@"
            SELECT Id, ReceiptNo, NewReceiptNo, Date, CreatedById, CreatedByName
            FROM ReinitializeTransaction", sourceConn);

        // Get valid staff IDs for foreign key validation
        var validStaffIds = new HashSet<int>();
        using (var staffCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM staff", targetConn))
        using (var staffReader = await staffCheckCmd.ExecuteReaderAsync())
        {
            while (await staffReader.ReadAsync())
            {
                validStaffIds.Add(GetIntValue(staffReader["Id"]));
            }
        }

        using var reader = await selectCmd.ExecuteReaderAsync();
        var count = 0;

        while (await reader.ReadAsync())
        {
            // Validate CreatedById staff ID
            var createdById = GetIntValueOrNull(reader["CreatedById"]);
            if (createdById.HasValue && !validStaffIds.Contains(createdById.Value))
            {
                _logger.LogInfo($"Warning: Reinitialize transaction {reader["ReceiptNo"]} has invalid CreatedById {createdById.Value}, setting to NULL");
                createdById = null;
            }

            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO reinitialize_transactions (""ReceiptNo"", ""NewReceiptNo"", ""CreatedById"", ""CreatedByName"", ""CreatedAt"")
                VALUES (@receiptNo, @newReceiptNo, @createdById, @createdByName, @createdAt)
                ON CONFLICT (""NewReceiptNo"") DO UPDATE SET
                    ""ReceiptNo"" = EXCLUDED.""ReceiptNo"",
                    ""CreatedById"" = EXCLUDED.""CreatedById"",
                    ""CreatedByName"" = EXCLUDED.""CreatedByName"",
                    ""CreatedAt"" = EXCLUDED.""CreatedAt""", targetConn);

            insertCmd.Parameters.AddWithValue("receiptNo", reader["ReceiptNo"]?.ToString() ?? "");
            insertCmd.Parameters.AddWithValue("newReceiptNo", reader["NewReceiptNo"]?.ToString() ?? "");
            insertCmd.Parameters.AddWithValue("createdById", createdById.HasValue ? (object)createdById.Value : DBNull.Value);
            insertCmd.Parameters.AddWithValue("createdByName", reader["CreatedByName"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("createdAt", reader["Date"] as DateTime? ?? DateTime.UtcNow);

            try
            {
                await insertCmd.ExecuteNonQueryAsync();
                count++;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // Duplicate key error - log and continue
                _logger.LogInfo($"Warning: Duplicate reinitialize transaction with NewReceiptNo {reader["NewReceiptNo"]} detected, skipping duplicate record");
                continue;
            }
        }

        _logger.LogInfo($"Migrated {count} reinitialize transactions");
    }

    private async Task MigrateKiiBankTransfersAsync()
    {
        _logger.LogInfo("Migrating KiiBank transfers...");

        using var sourceConn = new SqlConnection(_sourceConnectionString);
        await sourceConn.OpenAsync();

        using var targetConn = new NpgsqlConnection(_targetConnectionString);
        await targetConn.OpenAsync();

        // Get valid staff IDs for foreign key validation
        var validStaffIds = new HashSet<int>();
        using (var staffCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM staff", targetConn))
        using (var staffReader = await staffCheckCmd.ExecuteReaderAsync())
        {
            while (await staffReader.ReadAsync())
            {
                validStaffIds.Add(GetIntValue(staffReader["Id"]));
            }
        }

        // Get valid sender IDs for foreign key validation
        var validSenderIds = new HashSet<int>();
        using (var senderCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM senders", targetConn))
        using (var senderReader = await senderCheckCmd.ExecuteReaderAsync())
        {
            while (await senderReader.ReadAsync())
            {
                validSenderIds.Add(GetIntValue(senderReader["Id"]));
            }
        }

        // Get valid bank IDs for foreign key validation
        var validBankIds = new HashSet<int>();
        using (var bankCheckCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM banks", targetConn))
        using (var bankReader = await bankCheckCmd.ExecuteReaderAsync())
        {
            while (await bankReader.ReadAsync())
            {
                validBankIds.Add(GetIntValue(bankReader["Id"]));
            }
        }

        var selectCmd = new SqlCommand(@"
            SELECT Id, ReceiptNo, TransactionDate, SenderId, SendingCountry, ReceivingCountry,
                   SendingCurrency, ReceivingCurrency, SendingAmount, ReceivingAmount, Fee, TotalAmount,
                   ExchangeRate, PaymentReference, SenderPaymentMode, Status, PayingStaffId, Apiservice,
                   AccountNo, ReceiverName, TransactionReference, RecipientId,
                   ReasonForTransfer, CardProcessorApi, IsFromMobile, IsComplianceNeededForTrans,
                   IsComplianceApproved, ComplianceApprovedBy, ComplianceApprovedDate, UpdateByStaffId
            FROM KiiBankTransfer", sourceConn);

        using var reader = await selectCmd.ExecuteReaderAsync();
        var transactionCount = 0;
        var transferCount = 0;

        while (await reader.ReadAsync())
        {
            // First, migrate as a transaction
            var transactionId = await InsertTransactionAsync(
                targetConn,
                reader,
                TransactionModule.Sender,
                "ReceiptNo",
                "SendingAmount",
                "Fee",
                "Status",
                validStaffIds,
                validSenderIds
            );

            if (transactionId == -1)
            {
                _logger.LogInfo($"Warning: Skipping KiiBank transfer {reader["Id"]} due to invalid sender ID");
                continue;
            }

            transactionCount++;

            // BankId doesn't exist in legacy table, set to NULL
            int? bankId = null;

            // Try to get AccountOwnerName and AccountHolderPhoneNo from RecipientId if available
            // Note: These fields might not exist in the legacy table, so we'll set them to NULL if not found
            string? accountOwnerName = null;
            string? accountHolderPhoneNo = null;

            try
            {
                var recipientId = GetIntValueOrNull(reader["RecipientId"]);
                if (recipientId.HasValue)
                {
                    // Try to get recipient details from the new database
                    using var recipientCmd = new NpgsqlCommand(@"
                        SELECT ""FirstName"", ""LastName"", ""PhoneNumber""
                        FROM recipients
                        WHERE ""Id"" = @recipientId", targetConn);
                    recipientCmd.Parameters.AddWithValue("recipientId", recipientId.Value);
                    using var recipientReader = await recipientCmd.ExecuteReaderAsync();
                    if (await recipientReader.ReadAsync())
                    {
                        var firstName = recipientReader["FirstName"]?.ToString() ?? "";
                        var lastName = recipientReader["LastName"]?.ToString() ?? "";
                        accountOwnerName = $"{firstName} {lastName}".Trim();
                        accountHolderPhoneNo = recipientReader["PhoneNumber"]?.ToString();
                    }
                }
            }
            catch
            {
                // If RecipientId doesn't exist in the source table, that's fine
            }

            // Use ReceiverName as AccountOwnerName if AccountOwnerName is not available
            if (string.IsNullOrEmpty(accountOwnerName))
            {
                accountOwnerName = reader["ReceiverName"]?.ToString();
            }

            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO kiibank_transfers (""TransactionId"", ""AccountNo"", ""ReceiverName"", 
                                               ""AccountOwnerName"", ""AccountHolderPhoneNo"", ""BankId"", 
                                               ""BankBranchId"", ""BankBranchCode"", ""TransactionReference"", 
                                               ""CreatedAt"", ""UpdatedAt"")
                VALUES (@transactionId, @accountNo, @receiverName, @accountOwnerName, @accountHolderPhoneNo, 
                        @bankId, @bankBranchId, @bankBranchCode, @transactionReference, @createdAt, @updatedAt)
                ON CONFLICT (""TransactionId"") DO UPDATE SET
                    ""AccountNo"" = EXCLUDED.""AccountNo"",
                    ""ReceiverName"" = EXCLUDED.""ReceiverName"",
                    ""AccountOwnerName"" = EXCLUDED.""AccountOwnerName"",
                    ""AccountHolderPhoneNo"" = EXCLUDED.""AccountHolderPhoneNo"",
                    ""BankId"" = EXCLUDED.""BankId"",
                    ""BankBranchId"" = EXCLUDED.""BankBranchId"",
                    ""BankBranchCode"" = EXCLUDED.""BankBranchCode"",
                    ""TransactionReference"" = EXCLUDED.""TransactionReference"",
                    ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", targetConn);

            insertCmd.Parameters.AddWithValue("transactionId", transactionId);
            insertCmd.Parameters.AddWithValue("accountNo", reader["AccountNo"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("receiverName", reader["ReceiverName"]?.ToString() ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("accountOwnerName", accountOwnerName ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("accountHolderPhoneNo", accountHolderPhoneNo ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("bankId", bankId.HasValue ? (object)bankId.Value : DBNull.Value);
            
            // Handle BankBranchId and BankBranchCode - these don't exist in legacy table, set to NULL
            insertCmd.Parameters.AddWithValue("bankBranchId", DBNull.Value);
            insertCmd.Parameters.AddWithValue("bankBranchCode", DBNull.Value);
            insertCmd.Parameters.AddWithValue("transactionReference", reader["TransactionReference"]?.ToString() ?? (object)DBNull.Value);
            
            var transactionDate = reader["TransactionDate"] as DateTime? ?? DateTime.UtcNow;
            insertCmd.Parameters.AddWithValue("createdAt", transactionDate);
            insertCmd.Parameters.AddWithValue("updatedAt", transactionDate);

            try
            {
                await insertCmd.ExecuteNonQueryAsync();
                transferCount++;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // Duplicate key error - log and continue
                _logger.LogInfo($"Warning: Duplicate KiiBank transfer for transaction ID {transactionId} detected, skipping duplicate record");
                continue;
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                // Foreign key violation - log and continue
                _logger.LogInfo($"Warning: KiiBank transfer {reader["Id"]} has foreign key violation, skipping record");
                continue;
            }
        }

        _logger.LogInfo($"Migrated {transactionCount} transactions and {transferCount} KiiBank transfers");
    }

    private async Task<int> InsertTransactionAsync(
        NpgsqlConnection conn,
        IDataReader reader,
        TransactionModule module,
        string receiptNoField = "ReceiptNo",
        string sendingAmountField = "SendingAmount",
        string feeField = "Fee",
        string statusField = "Status",
        HashSet<int>? validStaffIds = null,
        HashSet<int>? validSenderIds = null)
    {
        var receiptNo = reader[receiptNoField]?.ToString() ?? "";
        var status = MapTransactionStatus(reader[statusField]);

        var transactionCmd = new NpgsqlCommand(@"
            INSERT INTO transactions (""ReceiptNo"", ""TransactionDate"", ""SenderId"", ""SendingCountryCode"", ""ReceivingCountryCode"",
                                     ""SendingCurrency"", ""ReceivingCurrency"", ""SendingAmount"", ""ReceivingAmount"", ""Fee"", ""TotalAmount"",
                                     ""ExchangeRate"", ""PaymentReference"", ""SenderPaymentMode"", ""TransactionModule"", ""Status"",
                                     ""ApiService"", ""PayingStaffId"", ""AgentCommission"", ""ExtraFee"", ""Margin"", ""MFRate"",
                                     ""TransferZeroSenderId"", ""TransferReference"", ""ReasonForTransfer"", ""CardProcessorApi"",
                                     ""IsFromMobile"", ""TransactionUpdateDate"", ""IsComplianceNeeded"", ""IsComplianceApproved"",
                                     ""ComplianceApprovedBy"", ""ComplianceApprovedAt"", ""PayingStaffName"", ""UpdatedByStaffId"",
                                     ""CreatedAt"", ""UpdatedAt"")
            VALUES (@receipt, @date, @sender, @sendCountry, @recvCountry, @sendCurr, @recvCurr, @sendAmt, @recvAmt,
                    @fee, @total, @rate, @paymentRef, @paymentMode, @module, @status, @api, @staff, @agentComm,
                    @extraFee, @margin, @mfRate, @transferZero, @transferRef, @reason, @cardProcessor, @isMobile,
                    @transUpdateDate, @isComplianceNeeded, @isComplianceApproved, @complianceBy, @complianceAt,
                    @payingStaffName, @updatedByStaff, @created, @updated)
            ON CONFLICT (""ReceiptNo"") DO UPDATE SET
                ""TransactionDate"" = EXCLUDED.""TransactionDate"",
                ""SenderId"" = EXCLUDED.""SenderId"",
                ""SendingCountryCode"" = EXCLUDED.""SendingCountryCode"",
                ""ReceivingCountryCode"" = EXCLUDED.""ReceivingCountryCode"",
                ""SendingCurrency"" = EXCLUDED.""SendingCurrency"",
                ""ReceivingCurrency"" = EXCLUDED.""ReceivingCurrency"",
                ""SendingAmount"" = EXCLUDED.""SendingAmount"",
                ""ReceivingAmount"" = EXCLUDED.""ReceivingAmount"",
                ""Fee"" = EXCLUDED.""Fee"",
                ""TotalAmount"" = EXCLUDED.""TotalAmount"",
                ""ExchangeRate"" = EXCLUDED.""ExchangeRate"",
                ""PaymentReference"" = EXCLUDED.""PaymentReference"",
                ""SenderPaymentMode"" = EXCLUDED.""SenderPaymentMode"",
                ""TransactionModule"" = EXCLUDED.""TransactionModule"",
                ""Status"" = EXCLUDED.""Status"",
                ""ApiService"" = EXCLUDED.""ApiService"",
                ""PayingStaffId"" = EXCLUDED.""PayingStaffId"",
                ""AgentCommission"" = EXCLUDED.""AgentCommission"",
                ""ExtraFee"" = EXCLUDED.""ExtraFee"",
                ""Margin"" = EXCLUDED.""Margin"",
                ""MFRate"" = EXCLUDED.""MFRate"",
                ""TransferZeroSenderId"" = EXCLUDED.""TransferZeroSenderId"",
                ""TransferReference"" = EXCLUDED.""TransferReference"",
                ""ReasonForTransfer"" = EXCLUDED.""ReasonForTransfer"",
                ""CardProcessorApi"" = EXCLUDED.""CardProcessorApi"",
                ""IsFromMobile"" = EXCLUDED.""IsFromMobile"",
                ""TransactionUpdateDate"" = EXCLUDED.""TransactionUpdateDate"",
                ""IsComplianceNeeded"" = EXCLUDED.""IsComplianceNeeded"",
                ""IsComplianceApproved"" = EXCLUDED.""IsComplianceApproved"",
                ""ComplianceApprovedBy"" = EXCLUDED.""ComplianceApprovedBy"",
                ""ComplianceApprovedAt"" = EXCLUDED.""ComplianceApprovedAt"",
                ""PayingStaffName"" = EXCLUDED.""PayingStaffName"",
                ""UpdatedByStaffId"" = EXCLUDED.""UpdatedByStaffId"",
                ""UpdatedAt"" = EXCLUDED.""UpdatedAt""
            RETURNING ""Id""", conn);

        transactionCmd.Parameters.AddWithValue("receipt", receiptNo);
        transactionCmd.Parameters.AddWithValue("date", reader["TransactionDate"] as DateTime? ?? DateTime.UtcNow);
        
        // Validate sender ID
        var senderId = GetIntValue(reader["SenderId"]);
        if (validSenderIds != null && !validSenderIds.Contains(senderId))
        {
            // Return -1 to indicate this transaction should be skipped
            return -1;
        }
        transactionCmd.Parameters.AddWithValue("sender", senderId);
        transactionCmd.Parameters.AddWithValue("sendCountry", reader["SendingCountry"]?.ToString() ?? "");
        transactionCmd.Parameters.AddWithValue("recvCountry", reader["ReceivingCountry"]?.ToString() ?? "");
        transactionCmd.Parameters.AddWithValue("sendCurr", reader["SendingCurrency"]?.ToString() ?? "");
        transactionCmd.Parameters.AddWithValue("recvCurr", reader["ReceivingCurrency"]?.ToString() ?? "");
        transactionCmd.Parameters.AddWithValue("sendAmt", reader[sendingAmountField] as decimal? ?? 0m);
        transactionCmd.Parameters.AddWithValue("recvAmt", reader["ReceivingAmount"] as decimal? ?? 0m);
        transactionCmd.Parameters.AddWithValue("fee", reader[feeField] as decimal? ?? 0m);
        transactionCmd.Parameters.AddWithValue("total", reader["TotalAmount"] as decimal? ?? 0m);
        transactionCmd.Parameters.AddWithValue("rate", reader["ExchangeRate"] as decimal? ?? 0m);
        transactionCmd.Parameters.AddWithValue("paymentRef", reader["PaymentReference"]?.ToString() ?? (object)DBNull.Value);
        
        // Map payment mode - handle both int and long from database
        var paymentModeValue = MapPaymentMode(reader["SenderPaymentMode"]);
        transactionCmd.Parameters.AddWithValue("paymentMode", (object)(int)paymentModeValue);
        
        transactionCmd.Parameters.AddWithValue("module", (object)(int)module);
        transactionCmd.Parameters.AddWithValue("status", (object)(int)status);
        
        // Map API service - handle null values
        var apiService = reader["Apiservice"] != null && reader["Apiservice"] != DBNull.Value 
            ? MapApiService(reader["Apiservice"]) 
            : null;
        transactionCmd.Parameters.AddWithValue("api", apiService.HasValue ? (object)(int)apiService.Value : DBNull.Value);
        
        // Validate PayingStaffId
        var staffId = GetIntValueOrNull(reader["PayingStaffId"]);
        if (staffId.HasValue && validStaffIds != null && !validStaffIds.Contains(staffId.Value))
        {
            staffId = null; // Set to null if staff doesn't exist
        }
        transactionCmd.Parameters.AddWithValue("staff", staffId.HasValue ? (object)staffId.Value : DBNull.Value);
        
        // Additional financial fields
        var agentComm = GetDecimalOrNull(reader, "AgentCommission");
        transactionCmd.Parameters.AddWithValue("agentComm", agentComm.HasValue ? (object)agentComm.Value : DBNull.Value);
        
        var extraFee = GetDecimalOrNull(reader, "ExtraFee");
        transactionCmd.Parameters.AddWithValue("extraFee", extraFee.HasValue ? (object)extraFee.Value : DBNull.Value);
        
        var margin = GetDecimalOrNull(reader, "Margin");
        transactionCmd.Parameters.AddWithValue("margin", margin.HasValue ? (object)margin.Value : DBNull.Value);
        
        var mfRate = GetDecimalOrNull(reader, "MFRate");
        transactionCmd.Parameters.AddWithValue("mfRate", mfRate.HasValue ? (object)mfRate.Value : DBNull.Value);
        
        // Transfer metadata - handle missing columns safely
        string? transferZero = null;
        try
        {
            transferZero = reader["TransferZeroSenderId"]?.ToString();
        }
        catch
        {
            // Column doesn't exist, use null
        }
        transactionCmd.Parameters.AddWithValue("transferZero", transferZero ?? (object)DBNull.Value);
        
        string? transferRef = null;
        try
        {
            transferRef = reader["TransferReference"]?.ToString();
        }
        catch
        {
            // Column doesn't exist, use null
        }
        transactionCmd.Parameters.AddWithValue("transferRef", transferRef ?? (object)DBNull.Value);
        
        // Reason for transfer - handle both "ReasonForTransfer" and "Reason" field names safely
        object? reasonField = null;
        try
        {
            reasonField = reader["ReasonForTransfer"];
        }
        catch
        {
            try
            {
                reasonField = reader["Reason"];
            }
            catch
            {
                // Field doesn't exist
            }
        }
        var reasonValue = reasonField != null && reasonField != DBNull.Value ? MapReasonForTransfer(reasonField) : null;
        transactionCmd.Parameters.AddWithValue("reason", reasonValue.HasValue ? (object)(int)reasonValue.Value : DBNull.Value);
        
        // Card processor API
        var cardProcessor = reader["CardProcessorApi"] != null && reader["CardProcessorApi"] != DBNull.Value
            ? MapCardProcessorApi(reader["CardProcessorApi"])
            : null;
        transactionCmd.Parameters.AddWithValue("cardProcessor", cardProcessor.HasValue ? (object)(int)cardProcessor.Value : DBNull.Value);
        
        // Mobile flag
        var isMobile = GetBoolOrFalse(reader, "IsFromMobile");
        transactionCmd.Parameters.AddWithValue("isMobile", isMobile);
        
        // Transaction update date - handle both field names safely
        DateTime? transUpdateDate = null;
        try
        {
            if (reader["TransactionUpdateDate"] != null && reader["TransactionUpdateDate"] != DBNull.Value)
            {
                transUpdateDate = reader["TransactionUpdateDate"] as DateTime?;
            }
        }
        catch
        {
            try
            {
                if (reader["StatusChangedDate"] != null && reader["StatusChangedDate"] != DBNull.Value)
                {
                    transUpdateDate = reader["StatusChangedDate"] as DateTime?;
                }
            }
            catch
            {
                // Field doesn't exist
            }
        }
        transactionCmd.Parameters.AddWithValue("transUpdateDate", transUpdateDate.HasValue ? (object)transUpdateDate.Value : DBNull.Value);
        
        // Compliance fields
        var isComplianceNeeded = GetBoolOrFalse(reader, "IsComplianceNeededForTrans");
        transactionCmd.Parameters.AddWithValue("isComplianceNeeded", isComplianceNeeded);
        
        var isComplianceApproved = GetBoolOrFalse(reader, "IsComplianceApproved");
        transactionCmd.Parameters.AddWithValue("isComplianceApproved", isComplianceApproved);
        
        // Validate ComplianceApprovedBy staff ID
        var complianceBy = GetIntValueOrNull(reader["ComplianceApprovedBy"]);
        if (complianceBy.HasValue && validStaffIds != null && !validStaffIds.Contains(complianceBy.Value))
        {
            complianceBy = null; // Set to null if staff doesn't exist
        }
        transactionCmd.Parameters.AddWithValue("complianceBy", complianceBy.HasValue ? (object)complianceBy.Value : DBNull.Value);
        
        var complianceAt = reader["ComplianceApprovedDate"] != null && reader["ComplianceApprovedDate"] != DBNull.Value
            ? reader["ComplianceApprovedDate"] as DateTime?
            : null;
        transactionCmd.Parameters.AddWithValue("complianceAt", complianceAt.HasValue ? (object)complianceAt.Value : DBNull.Value);
        
        // Staff names - handle both "PayingStaffName" and "AgentStaffName"
        string? payingStaffNameValue = null;
        try
        {
            payingStaffNameValue = reader["PayingStaffName"]?.ToString();
        }
        catch
        {
            try
            {
                payingStaffNameValue = reader["AgentStaffName"]?.ToString();
            }
            catch
            {
                // Field doesn't exist
            }
        }
        transactionCmd.Parameters.AddWithValue("payingStaffName", payingStaffNameValue ?? (object)DBNull.Value);
        
        // Updated by staff - handle both "UpdateByStaffId" and "UpdatedByStaffId"
        object? updatedByStaffValue = null;
        try
        {
            updatedByStaffValue = reader["UpdateByStaffId"];
        }
        catch
        {
            try
            {
                updatedByStaffValue = reader["UpdatedByStaffId"];
            }
            catch
            {
                // Field doesn't exist
            }
        }
        var updatedByStaff = GetIntValueOrNull(updatedByStaffValue);
        if (updatedByStaff.HasValue && validStaffIds != null && !validStaffIds.Contains(updatedByStaff.Value))
        {
            updatedByStaff = null; // Set to null if staff doesn't exist
        }
        transactionCmd.Parameters.AddWithValue("updatedByStaff", updatedByStaff.HasValue ? (object)updatedByStaff.Value : DBNull.Value);
        
        transactionCmd.Parameters.AddWithValue("created", reader["TransactionDate"] as DateTime? ?? DateTime.UtcNow);
        transactionCmd.Parameters.AddWithValue("updated", DateTime.UtcNow);

        var result = await transactionCmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException("Failed to insert transaction - no ID returned");
        
        // Handle both int and long return types
        return result switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            _ => Convert.ToInt32(result)
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Safely converts a database value to int, handling both int and long types
    /// </summary>
    private static int GetIntValue(object? value)
    {
        if (value == null || value == DBNull.Value)
            throw new ArgumentException("Value cannot be null or DBNull");

        return value switch
        {
            int intValue => intValue,
            long longValue => checked((int)longValue), // Use checked to prevent overflow
            short shortValue => shortValue,
            byte byteValue => byteValue,
            _ => Convert.ToInt32(value)
        };
    }

    /// <summary>
    /// Safely converts a database value to int, returning null if value is null or DBNull
    /// </summary>
    private static int? GetIntValueOrNull(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        return value switch
        {
            int intValue => intValue,
            long longValue => checked((int)longValue),
            short shortValue => shortValue,
            byte byteValue => byteValue,
            _ => Convert.ToInt32(value)
        };
    }

    /// <summary>
    /// Safely converts a database value to decimal, returning null if value is null or DBNull
    /// </summary>
    private static decimal? GetDecimalOrNull(IDataReader reader, string fieldName)
    {
        try
        {
            var value = reader[fieldName];
            if (value == null || value == DBNull.Value)
                return null;
            return Convert.ToDecimal(value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Safely converts a database value to bool, returning false if value is null or DBNull
    /// </summary>
    private static bool GetBoolOrFalse(IDataReader reader, string fieldName)
    {
        try
        {
            var value = reader[fieldName];
            if (value == null || value == DBNull.Value)
                return false;
            return Convert.ToBoolean(value);
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Enum Mapping

    private TransactionStatus MapTransactionStatus(object? status)
    {
        if (status == null || status == DBNull.Value) return TransactionStatus.InProgress;

        // Handle both int and long types from database
        var statusInt = status switch
        {
            int i => i,
            long l => checked((int)l),
            _ => Convert.ToInt32(status)
        };

        // Map based on enum type and value
        return statusInt switch
        {
            0 => TransactionStatus.Failed, // MobileMoneyTransferStatus.Failed
            1 => TransactionStatus.InProgress, // MobileMoneyTransferStatus.InProgress or FaxingStatus.NotReceived
            2 => TransactionStatus.Paid, // MobileMoneyTransferStatus.Paid or FaxingStatus.Received
            3 => TransactionStatus.Cancelled, // MobileMoneyTransferStatus.Cancel or FaxingStatus.Cancel
            4 => TransactionStatus.PaymentPending, // MobileMoneyTransferStatus.PaymentPending
            5 => TransactionStatus.IdCheckInProgress, // MobileMoneyTransferStatus.IdCheckInProgress
            6 => TransactionStatus.Completed, // FaxingStatus.Completed
            7 => TransactionStatus.FullRefund, // FaxingStatus.Refund
            8 => TransactionStatus.Held, // MobileMoneyTransferStatus.Held
            10 => TransactionStatus.FullRefund, // MobileMoneyTransferStatus.FullRefund
            11 => TransactionStatus.PartialRefund, // MobileMoneyTransferStatus.PartailRefund
            12 => TransactionStatus.Paused, // MobileMoneyTransferStatus.Paused
            _ => TransactionStatus.InProgress
        };
    }

    private PaymentMode MapPaymentMode(object? paymentMode)
    {
        if (paymentMode == null || paymentMode == DBNull.Value) return PaymentMode.Card;
        
        // Handle both int and long from database
        var intValue = paymentMode switch
        {
            int i => i,
            long l => checked((int)l),
            _ => Convert.ToInt32(paymentMode)
        };
        
        return (PaymentMode)intValue;
    }

    private ApiService? MapApiService(object? apiService)
    {
        if (apiService == null || apiService == DBNull.Value) return null;
        
        // Handle both int and long from database
        var intValue = apiService switch
        {
            int i => i,
            long l => checked((int)l),
            _ => Convert.ToInt32(apiService)
        };
        
        return (ApiService)intValue;
    }

    private ReasonForTransfer? MapReasonForTransfer(object? reason)
    {
        if (reason == null || reason == DBNull.Value) return null;
        
        // Handle both int and long from database
        var intValue = reason switch
        {
            int i => i,
            long l => checked((int)l),
            _ => Convert.ToInt32(reason)
        };
        
        // Validate enum value
        if (Enum.IsDefined(typeof(ReasonForTransfer), intValue))
            return (ReasonForTransfer)intValue;
        
        return null;
    }

    private CardProcessorApi? MapCardProcessorApi(object? cardProcessor)
    {
        if (cardProcessor == null || cardProcessor == DBNull.Value) return null;
        
        // Handle both int and long from database
        var intValue = cardProcessor switch
        {
            int i => i,
            long l => checked((int)l),
            _ => Convert.ToInt32(cardProcessor)
        };
        
        // Validate enum value
        if (Enum.IsDefined(typeof(CardProcessorApi), intValue))
            return (CardProcessorApi)intValue;
        
        return null;
    }

    #endregion
}

public class MigrationResult
{
    public bool Success { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, int> RecordCounts { get; set; } = new();
}

public class MigrationLogger
{
    private readonly string _logPath;

    public MigrationLogger(string logPath)
    {
        _logPath = logPath;
        Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? "logs");
    }

    public void LogInfo(string message)
    {
        var logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] INFO: {message}";
        Console.WriteLine(logMessage);
        File.AppendAllText(_logPath, logMessage + Environment.NewLine);
    }

    public void LogError(string message, Exception? ex = null)
    {
        var logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: {message}";
        if (ex != null)
        {
            logMessage += $"\n{ex}";
        }
        Console.WriteLine(logMessage);
        File.AppendAllText(_logPath, logMessage + Environment.NewLine);
    }
}

