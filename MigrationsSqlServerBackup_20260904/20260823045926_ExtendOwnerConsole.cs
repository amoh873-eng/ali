using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendOwnerConsole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackupDestination",
                table: "SystemSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BackupDestinationType",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRenewedAt",
                table: "SystemSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRenewalDate",
                table: "SystemSettings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionCycle",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionStartDate",
                table: "SystemSettings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "JoFotaraQrCode",
                table: "SalesInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JoFotaraReferenceNumber",
                table: "SalesInvoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JoFotaraStatus",
                table: "SalesInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoFotaraSubmittedAt",
                table: "SalesInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomReportDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClientRequestNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReportType = table.Column<int>(type: "int", nullable: false),
                    SqlQuery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExistingReportKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByOwnerAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomReportDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettingsHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemSettingsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettingsHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettingsHistory_ChangedAt",
                table: "SystemSettingsHistory",
                column: "ChangedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomReportDefinitions");

            migrationBuilder.DropTable(
                name: "SystemSettingsHistory");

            migrationBuilder.DropColumn(
                name: "BackupDestination",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "BackupDestinationType",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "LastRenewedAt",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "NextRenewalDate",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SubscriptionCycle",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SubscriptionStartDate",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "JoFotaraQrCode",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "JoFotaraReferenceNumber",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "JoFotaraStatus",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "JoFotaraSubmittedAt",
                table: "SalesInvoices");
        }
    }
}
