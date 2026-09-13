using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepCashCustody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepCashCustody",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Effect = table.Column<int>(type: "integer", nullable: false),
                    RepUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepCashCustody", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepCashCustody_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // الحساب النظامي «عهدة نقدية بيد المندوبين» (1110) — أصل/مدين، تابع للأصول
            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "AccountType", "Code", "CreatedAt", "CreatedBy", "Description", "HierarchyPath", "IsActive", "IsDeleted", "IsSystem", "Level", "NameAr", "NameEn", "NormalBalance", "ParentAccountId", "RowVersion", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("11000000-0000-0000-0000-000000000007"), 1, "1110", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, true, 0, "عهدة نقدية بيد المندوبين", "Rep Cash Custody", 1, new Guid("10000000-0000-0000-0000-000000000001"), new byte[0], null, null });

            migrationBuilder.CreateIndex(
                name: "IX_RepCashCustody_RepUserId",
                table: "RepCashCustody",
                column: "RepUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RepCashCustody_JournalEntryId",
                table: "RepCashCustody",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_RepCashCustody_Timestamp",
                table: "RepCashCustody",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepCashCustody");

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("11000000-0000-0000-0000-000000000007"));
        }
    }
}