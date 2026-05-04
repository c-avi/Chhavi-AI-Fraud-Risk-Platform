using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraudRiskApi.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseScoringIdempotencyAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScoringError",
                table: "Transactions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ScoringStatus",
                table: "Transactions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<string>(
                name: "FeatureSetJson",
                table: "Alerts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TransactionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdempotencyRecords_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ScoringStatus",
                table: "Transactions",
                column: "ScoringStatus");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Key",
                table: "IdempotencyRecords",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_TransactionId",
                table: "IdempotencyRecords",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ScoringStatus",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ScoringError",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ScoringStatus",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FeatureSetJson",
                table: "Alerts");
        }
    }
}
