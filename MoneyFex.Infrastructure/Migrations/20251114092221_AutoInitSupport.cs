using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MoneyFex.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutoInitSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    CountryCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CountryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CurrencySymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_countries", x => x.CountryCode);
                });

            migrationBuilder.CreateTable(
                name: "recipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReceiverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "banks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(3)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_banks_countries_CountryCode",
                        column: x => x.CountryCode,
                        principalTable: "countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mobile_wallet_operators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(3)", nullable: true),
                    MobileNetworkCode = table.Column<string>(type: "text", nullable: true),
                    PayoutProviderId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mobile_wallet_operators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mobile_wallet_operators_countries_CountryCode",
                        column: x => x.CountryCode,
                        principalTable: "countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receiver_details",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receiver_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_receiver_details_countries_CountryCode",
                        column: x => x.CountryCode,
                        principalTable: "countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "senders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address1 = table.Column<string>(type: "text", nullable: true),
                    Address2 = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(3)", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    IsBusiness = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_senders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_senders_countries_CountryCode",
                        column: x => x.CountryCode,
                        principalTable: "countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reinitialize_transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReceiptNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NewReceiptNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: true),
                    CreatedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reinitialize_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reinitialize_transactions_staff_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sender_logins",
                columns: table => new
                {
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sender_logins", x => x.SenderId);
                    table.ForeignKey(
                        name: "FK_sender_logins_senders_SenderId",
                        column: x => x.SenderId,
                        principalTable: "senders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiptNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    SendingCountryCode = table.Column<string>(type: "character varying(3)", nullable: false),
                    ReceivingCountryCode = table.Column<string>(type: "character varying(3)", nullable: false),
                    SendingCurrency = table.Column<string>(type: "text", nullable: false),
                    ReceivingCurrency = table.Column<string>(type: "text", nullable: false),
                    SendingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceivingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Fee = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentReference = table.Column<string>(type: "text", nullable: true),
                    SenderPaymentMode = table.Column<int>(type: "integer", nullable: false),
                    TransactionModule = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApiService = table.Column<int>(type: "integer", nullable: true),
                    TransferReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecipientId = table.Column<int>(type: "integer", nullable: true),
                    IsComplianceNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    IsComplianceApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ComplianceApprovedBy = table.Column<int>(type: "integer", nullable: true),
                    ComplianceApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PayingStaffId = table.Column<int>(type: "integer", nullable: true),
                    PayingStaffName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedByStaffId = table.Column<int>(type: "integer", nullable: true),
                    AgentCommission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExtraFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Margin = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MFRate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    TransferZeroSenderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReasonForTransfer = table.Column<int>(type: "integer", nullable: true),
                    CardProcessorApi = table.Column<int>(type: "integer", nullable: true),
                    IsFromMobile = table.Column<bool>(type: "boolean", nullable: false),
                    TransactionUpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transactions_countries_ReceivingCountryCode",
                        column: x => x.ReceivingCountryCode,
                        principalTable: "countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_countries_SendingCountryCode",
                        column: x => x.SendingCountryCode,
                        principalTable: "countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_senders_SenderId",
                        column: x => x.SenderId,
                        principalTable: "senders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_staff_ComplianceApprovedBy",
                        column: x => x.ComplianceApprovedBy,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_staff_PayingStaffId",
                        column: x => x.PayingStaffId,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_staff_UpdatedByStaffId",
                        column: x => x.UpdatedByStaffId,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bank_account_deposits",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    BankId = table.Column<int>(type: "integer", nullable: true),
                    BankName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BankCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReceiverAccountNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReceiverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceiverCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReceiverCountry = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ReceiverMobileNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RecipientId = table.Column<int>(type: "integer", nullable: true),
                    IsManualDeposit = table.Column<bool>(type: "boolean", nullable: false),
                    IsManualApprovalNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    IsManuallyApproved = table.Column<bool>(type: "boolean", nullable: false),
                    IsEuropeTransfer = table.Column<bool>(type: "boolean", nullable: false),
                    IsTransactionDuplicated = table.Column<bool>(type: "boolean", nullable: false),
                    DuplicateTransactionReceiptNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsBusiness = table.Column<bool>(type: "boolean", nullable: false),
                    HasMadePaymentToBankAccount = table.Column<bool>(type: "boolean", nullable: false),
                    TransactionDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_account_deposits", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_bank_account_deposits_banks_BankId",
                        column: x => x.BankId,
                        principalTable: "banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_account_deposits_recipients_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "recipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_account_deposits_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "card_payment_information",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    CardTransactionId = table.Column<int>(type: "integer", nullable: true),
                    NonCardTransactionId = table.Column<int>(type: "integer", nullable: true),
                    TopUpSomeoneElseTransactionId = table.Column<int>(type: "integer", nullable: true),
                    NameOnCard = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CardNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsSavedCard = table.Column<bool>(type: "boolean", nullable: false),
                    AutoRecharged = table.Column<bool>(type: "boolean", nullable: false),
                    TransferType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_payment_information", x => x.Id);
                    table.ForeignKey(
                        name: "FK_card_payment_information_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cash_pickups",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    MFCN = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RecipientId = table.Column<int>(type: "integer", nullable: true),
                    NonCardReceiverId = table.Column<int>(type: "integer", nullable: true),
                    RecipientIdentityCardId = table.Column<int>(type: "integer", nullable: true),
                    RecipientIdentityCardNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsApprovedByAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    AgentStaffName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_pickups", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_cash_pickups_receiver_details_NonCardReceiverId",
                        column: x => x.NonCardReceiverId,
                        principalTable: "receiver_details",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cash_pickups_recipients_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "recipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cash_pickups_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kiibank_transfers",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    AccountNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReceiverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccountOwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccountHolderPhoneNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankId = table.Column<int>(type: "integer", nullable: true),
                    BankBranchId = table.Column<int>(type: "integer", nullable: true),
                    BankBranchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransactionReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kiibank_transfers", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_kiibank_transfers_banks_BankId",
                        column: x => x.BankId,
                        principalTable: "banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_kiibank_transfers_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mobile_money_transfers",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    WalletOperatorId = table.Column<int>(type: "integer", nullable: false),
                    PaidToMobileNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceiverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceiverCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecipientId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mobile_money_transfers", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_mobile_money_transfers_mobile_wallet_operators_WalletOperat~",
                        column: x => x.WalletOperatorId,
                        principalTable: "mobile_wallet_operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mobile_money_transfers_recipients_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "recipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mobile_money_transfers_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bank_account_deposits_BankId",
                table: "bank_account_deposits",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_bank_account_deposits_RecipientId",
                table: "bank_account_deposits",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_banks_CountryCode",
                table: "banks",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_card_payment_information_CardTransactionId",
                table: "card_payment_information",
                column: "CardTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_card_payment_information_NonCardTransactionId",
                table: "card_payment_information",
                column: "NonCardTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_card_payment_information_TopUpSomeoneElseTransactionId",
                table: "card_payment_information",
                column: "TopUpSomeoneElseTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_card_payment_information_TransactionId",
                table: "card_payment_information",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_card_payment_information_TransferType",
                table: "card_payment_information",
                column: "TransferType");

            migrationBuilder.CreateIndex(
                name: "IX_cash_pickups_MFCN",
                table: "cash_pickups",
                column: "MFCN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cash_pickups_NonCardReceiverId",
                table: "cash_pickups",
                column: "NonCardReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_pickups_RecipientId",
                table: "cash_pickups",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_kiibank_transfers_BankId",
                table: "kiibank_transfers",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_money_transfers_RecipientId",
                table: "mobile_money_transfers",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_money_transfers_WalletOperatorId",
                table: "mobile_money_transfers",
                column: "WalletOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_wallet_operators_CountryCode",
                table: "mobile_wallet_operators",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_receiver_details_CountryCode",
                table: "receiver_details",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_reinitialize_transactions_CreatedById",
                table: "reinitialize_transactions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_reinitialize_transactions_NewReceiptNo",
                table: "reinitialize_transactions",
                column: "NewReceiptNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reinitialize_transactions_ReceiptNo",
                table: "reinitialize_transactions",
                column: "ReceiptNo");

            migrationBuilder.CreateIndex(
                name: "IX_senders_AccountNo",
                table: "senders",
                column: "AccountNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_senders_CountryCode",
                table: "senders",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_senders_Email",
                table: "senders",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_staff_Email",
                table: "staff",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_ComplianceApprovedBy",
                table: "transactions",
                column: "ComplianceApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_PayingStaffId",
                table: "transactions",
                column: "PayingStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_PaymentReference",
                table: "transactions",
                column: "PaymentReference");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_ReceiptNo",
                table: "transactions",
                column: "ReceiptNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_ReceivingCountryCode",
                table: "transactions",
                column: "ReceivingCountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_SenderId",
                table: "transactions",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_SendingCountryCode",
                table: "transactions",
                column: "SendingCountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_Status",
                table: "transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_TransactionDate",
                table: "transactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_UpdatedByStaffId",
                table: "transactions",
                column: "UpdatedByStaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_account_deposits");

            migrationBuilder.DropTable(
                name: "card_payment_information");

            migrationBuilder.DropTable(
                name: "cash_pickups");

            migrationBuilder.DropTable(
                name: "kiibank_transfers");

            migrationBuilder.DropTable(
                name: "mobile_money_transfers");

            migrationBuilder.DropTable(
                name: "reinitialize_transactions");

            migrationBuilder.DropTable(
                name: "sender_logins");

            migrationBuilder.DropTable(
                name: "receiver_details");

            migrationBuilder.DropTable(
                name: "banks");

            migrationBuilder.DropTable(
                name: "mobile_wallet_operators");

            migrationBuilder.DropTable(
                name: "recipients");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "senders");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "countries");
        }
    }
}
