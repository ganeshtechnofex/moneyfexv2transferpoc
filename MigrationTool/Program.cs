using System.Text.Json;
using MoneyFex.Infrastructure.MigrationTool;

namespace MigrationTool;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("MoneyFex Data Migration Tool");
        Console.WriteLine("Legacy SQL Server -> New PostgreSQL");
        Console.WriteLine("==========================================");
        Console.WriteLine();

        try
        {
            // Load configuration
            var configPath = "appsettings.json";
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"ERROR: Configuration file not found: {configPath}");
                Console.WriteLine("Please create appsettings.json with migration settings.");
                return;
            }

            var configJson = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<MigrationConfig>(configJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config?.MigrationSettings == null)
            {
                Console.WriteLine("ERROR: Invalid configuration file.");
                return;
            }

            var settings = config.MigrationSettings;

            // Validate connection strings
            Console.WriteLine("Validating connection strings...");
            if (string.IsNullOrEmpty(settings.SourceConnectionString))
            {
                Console.WriteLine("ERROR: Source connection string is required.");
                return;
            }

            if (string.IsNullOrEmpty(settings.TargetConnectionString))
            {
                Console.WriteLine("ERROR: Target connection string is required.");
                return;
            }

            Console.WriteLine("✓ Connection strings configured");
            Console.WriteLine();

            // Create migration service
            var migrationService = new DataMigrationService(
                settings.SourceConnectionString,
                settings.TargetConnectionString,
                settings.BatchSize,
                settings.EnableValidation,
                settings.LogPath ?? "logs/migration.log"
            );

            // Check for schema creation mode
            if (args.Length > 0 && args[0] == "--create-schema")
            {
                Console.WriteLine("Creating database schema...");
                var schemaPath = args.Length > 1 ? args[1] : "create_schema.sql";
                await SchemaExecutor.ExecuteSchemaAsync(settings.TargetConnectionString, schemaPath);
                Console.WriteLine("Schema creation completed successfully!");
                return;
            }

            // Determine migration mode
            var mode = args.Length > 0 && args[0] == "--mode" && args.Length > 1
                ? args[1].ToLower()
                : "full";

            Console.WriteLine($"Migration Mode: {mode}");
            Console.WriteLine();

            // Execute migration
            MigrationResult result;
            switch (mode)
            {
                case "full":
                    Console.WriteLine("Starting FULL migration...");
                    Console.WriteLine("This will migrate all data from legacy to new database.");
                    Console.WriteLine();
                    result = await migrationService.MigrateAllAsync();
                    break;

                case "validate":
                    Console.WriteLine("Starting VALIDATION...");
                    Console.WriteLine("This will validate data without migrating.");
                    Console.WriteLine();
                    result = await ValidateDataAsync(settings);
                    break;

                case "incremental":
                    var batchSize = args.Length > 3 && args[2] == "--batch-size"
                        ? int.Parse(args[3])
                        : settings.BatchSize;
                    Console.WriteLine($"Starting INCREMENTAL migration (batch size: {batchSize})...");
                    Console.WriteLine();
                    result = await migrationService.MigrateAllAsync(); // For now, same as full
                    break;

                default:
                    Console.WriteLine($"ERROR: Unknown migration mode: {mode}");
                    Console.WriteLine("Available modes: full, validate, incremental");
                    return;
            }

            // Display results
            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("Migration Results");
            Console.WriteLine("==========================================");
            Console.WriteLine($"Status: {(result.Success ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"Duration: {result.Duration.TotalMinutes:F2} minutes");
            Console.WriteLine($"Start Time: {result.StartTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"End Time: {result.EndTime:yyyy-MM-dd HH:mm:ss}");

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }

            if (result.RecordCounts.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Record Counts:");
                foreach (var count in result.RecordCounts)
                {
                    Console.WriteLine($"  {count.Key}: {count.Value:N0}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Migration completed. Check logs for details.");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("FATAL ERROR");
            Console.WriteLine("==========================================");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static async Task<MigrationResult> ValidateDataAsync(MigrationSettings settings)
    {
        var result = new MigrationResult
        {
            StartTime = DateTime.UtcNow,
            Success = true
        };

        try
        {
            Console.WriteLine("Validating source database...");
            await ValidateSourceDatabaseAsync(settings.SourceConnectionString);

            Console.WriteLine("Validating target database...");
            await ValidateTargetDatabaseAsync(settings.TargetConnectionString);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
        }

        return result;
    }

    private static async Task ValidateSourceDatabaseAsync(string connectionString)
    {
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync();

        var tables = new[] { "FaxerInformation", "BankAccountDeposit", "MobileMoneyTransfer", "FaxingNonCardTransaction", "Country", "Bank", "MobileWalletOperator" };

        foreach (var table in tables)
        {
            var cmd = new Microsoft.Data.SqlClient.SqlCommand($"SELECT COUNT(*) FROM {table}", conn);
            var count = await cmd.ExecuteScalarAsync();
            Console.WriteLine($"  {table}: {count:N0} records");
        }
    }

    private static async Task ValidateTargetDatabaseAsync(string connectionString)
    {
        using var conn = new Npgsql.NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var tables = new[] { "senders", "transactions", "bank_account_deposits", "mobile_money_transfers", "cash_pickups", "countries", "banks", "mobile_wallet_operators" };

        foreach (var table in tables)
        {
            var cmd = new Npgsql.NpgsqlCommand($"SELECT COUNT(*) FROM {table}", conn);
            var count = await cmd.ExecuteScalarAsync();
            Console.WriteLine($"  {table}: {count:N0} records");
        }
    }
}

public class MigrationConfig
{
    public MigrationSettings? MigrationSettings { get; set; }
}

public class MigrationSettings
{
    public string SourceConnectionString { get; set; } = string.Empty;
    public string TargetConnectionString { get; set; } = string.Empty;
    public int BatchSize { get; set; } = 1000;
    public bool EnableValidation { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public bool ResumeFromCheckpoint { get; set; } = false;
    public string? LogPath { get; set; }
}

