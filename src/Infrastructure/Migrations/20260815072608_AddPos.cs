using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPos",
                table: "SalesInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Address", "Code", "CreatedAt", "CreatedBy", "CreditLimit", "Email", "IsActive", "IsDeleted", "IsSystem", "NameAr", "NameEn", "Notes", "Phone", "TaxNumber", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("90000000-0000-0000-0000-000000000001"), null, "CASH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, true, "زبون نقدي", "Walk-in Cash Customer", null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("90000000-0000-0000-0000-000000000001"));

            migrationBuilder.DropColumn(
                name: "IsPos",
                table: "SalesInvoices");
        }
    }
}
