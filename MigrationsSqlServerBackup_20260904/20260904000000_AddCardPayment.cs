using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ERPSystem.Infrastructure.Data;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <summary>
    /// AddCardPayment — الدفع بالبطاقة (المرحلة 1):
    /// - حقول بطاقة على فواتير المبيعات (رقم الموافقة/آخر 4 أرقام/الشبكة/الوقت) + أسلوب الدفع.
    /// - جدول BankCardStatements لكشف تسوية البنك.
    /// - حساب "ذمم البطاقات (1205)" لتوجيه مدين بطاقات بدل الصندوق.
    /// ⚠️ لا يخزن الجدول أبداً رقم البطاقة الكامل أو CVV أو تاريخ الانتهاء (PCI-DSS).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260904000000_AddCardPayment")]
    public partial class AddCardPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // حقل أسلوب الدفع (1 نقدي/2 بطاقة/3 آجل) — افتراضي 1 للصفوف القائمة
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "SalesInvoices",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "CardApprovalCode",
                table: "SalesInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardLast4",
                table: "SalesInvoices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardNetwork",
                table: "SalesInvoices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CardTransactionAt",
                table: "SalesInvoices",
                type: "datetime2",
                nullable: true);

            // جدول كشف تسوية البنك
            migrationBuilder.CreateTable(
                name: "BankCardStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCardStatements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankCardStatements_Reference",
                table: "BankCardStatements",
                column: "Reference",
                unique: true,
                filter: "[IsDeleted] = 0");

            // حساب "ذمم البطاقات (1205)" — إدراج خام (بدون InsertData لأن كتابة يدوية بلا Designer تحتاج تعيين أعمدة من النموذج)
            migrationBuilder.Sql(
                "INSERT INTO [Accounts] ([Id],[AccountType],[Code],[CreatedAt],[CurrentBalance],[IsActive],[IsDeleted],[IsSystem],[Level],[NameAr],[NameEn],[NormalBalance],[ParentAccountId]) VALUES ('11000000-0000-0000-0000-000000000005', 1, '1205', '2026-01-01T00:00:00.0000000Z', 0, 1, 0, 1, 0, N'ذمم البطاقات (مستحق من البنك)', N'Card Receivables', 1, '10000000-0000-0000-0000-000000000001')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Accounts] WHERE [Id] = '11000000-0000-0000-0000-000000000005'");

            migrationBuilder.DropTable(name: "BankCardStatements");

            migrationBuilder.DropColumn(name: "CardTransactionAt", table: "SalesInvoices");
            migrationBuilder.DropColumn(name: "CardNetwork", table: "SalesInvoices");
            migrationBuilder.DropColumn(name: "CardLast4", table: "SalesInvoices");
            migrationBuilder.DropColumn(name: "CardApprovalCode", table: "SalesInvoices");
            migrationBuilder.DropColumn(name: "PaymentMethod", table: "SalesInvoices");
        }
    }
}