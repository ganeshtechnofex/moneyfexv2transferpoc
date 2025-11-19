using Npgsql;
using System.Text;
using System.Text.RegularExpressions;

namespace MigrationTool;

public class SchemaExecutor
{
    public static async Task ExecuteSchemaAsync(string connectionString, string sqlFilePath)
    {
        if (!File.Exists(sqlFilePath))
        {
            throw new FileNotFoundException($"SQL file not found: {sqlFilePath}");
        }

        var sqlScript = await File.ReadAllTextAsync(sqlFilePath);
        
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        
        Console.WriteLine("Executing database schema creation...");
        
        // Execute the entire script at once (PostgreSQL supports multi-statement execution)
        try
        {
            using var cmd = new NpgsqlCommand(sqlScript, conn);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("✓ Schema creation completed successfully!");
        }
        catch (PostgresException ex)
        {
            // Handle specific PostgreSQL errors
            if (ex.SqlState == "42P07" || ex.Message.Contains("already exists"))
            {
                Console.WriteLine("⚠ Some objects already exist. Continuing...");
            }
            else
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"SQL State: {ex.SqlState}");
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing schema: {ex.Message}");
            throw;
        }
    }
}

