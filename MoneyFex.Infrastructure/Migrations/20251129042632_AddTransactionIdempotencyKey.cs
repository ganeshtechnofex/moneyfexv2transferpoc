using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyFex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_IdempotencyKey",
                table: "transactions",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_IdempotencyKey",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "transactions");
        }
    }
}
