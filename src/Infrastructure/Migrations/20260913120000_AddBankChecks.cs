using System;
using ERPSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260913120000_AddBankChecks")]
    public partial class AddBankChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // جميع أوامر هذه الهجرة مكتوبة بصيغة محصّنة (IF NOT EXISTS / DO $$)
            // لأن مشغّل هجرات Npgsql قد ينفّذ الجملة أكثر من مرة في الجلسة الواحدة
            // (كما في هجرة pg_trgm) — فيبقى التطبيق آمناً عند أي إعادة تنفيذ منتصفة.

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""BankChecks""
                (
                    ""Id"" uuid NOT NULL,
                    ""CheckNumber"" character varying(100) NOT NULL,
                    ""BankName"" character varying(150) NOT NULL,
                    ""BranchName"" character varying(150) NULL,
                    ""Direction"" integer NOT NULL,
                    ""CustomerId"" uuid NULL,
                    ""SupplierId"" uuid NULL,
                    ""RelatedInvoiceId"" uuid NULL,
                    ""Amount"" numeric(18,2) NOT NULL,
                    ""IssueDate"" timestamp without time zone NOT NULL,
                    ""DueDate"" timestamp without time zone NOT NULL,
                    ""ReceivedOrIssuedDate"" timestamp without time zone NOT NULL,
                    ""Status"" integer NOT NULL,
                    ""DepositDate"" timestamp without time zone NULL,
                    ""ClearedDate"" timestamp without time zone NULL,
                    ""BouncedDate"" timestamp without time zone NULL,
                    ""Notes"" character varying(500) NULL,
                    ""JournalEntryId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp without time zone NOT NULL,
                    ""UpdatedAt"" timestamp without time zone NOT NULL,
                    CONSTRAINT ""PK_BankChecks"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM ""Accounts"" WHERE ""Code"" = '1102') THEN
                        INSERT INTO ""Accounts"" (""Id"",""AccountType"",""Code"",""CreatedAt"",""CreatedBy"",""Description"",""HierarchyPath"",""IsActive"",""IsDeleted"",""IsSystem"",""Level"",""NameAr"",""NameEn"",""NormalBalance"",""ParentAccountId"",""RowVersion"",""UpdatedAt"",""UpdatedBy"")
                        VALUES ('11000000-0000-0000-0000-000000000012', 1, '1102', '2026-01-01 00:00:00', NULL, NULL, NULL, TRUE, FALSE, TRUE, 0, 'شيكات برسم التحصيل', 'Checks Receivable', 1, '10000000-0000-0000-0000-000000000001', decode('', 'hex'), NULL, NULL);
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM ""Accounts"" WHERE ""Code"" = '2400') THEN
                        INSERT INTO ""Accounts"" (""Id"",""AccountType"",""Code"",""CreatedAt"",""CreatedBy"",""Description"",""HierarchyPath"",""IsActive"",""IsDeleted"",""IsSystem"",""Level"",""NameAr"",""NameEn"",""NormalBalance"",""ParentAccountId"",""RowVersion"",""UpdatedAt"",""UpdatedBy"")
                        VALUES ('12000000-0000-0000-0000-000000000013', 2, '2400', '2026-01-01 00:00:00', NULL, NULL, NULL, TRUE, FALSE, TRUE, 0, 'شيكات برسم السداد', 'Checks Payable', 2, '10000000-0000-0000-0000-000000000002', decode('', 'hex'), NULL, NULL);
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_BankChecks_DueDate""        ON ""BankChecks"" (""DueDate"");
                CREATE INDEX IF NOT EXISTS ""IX_BankChecks_Status""         ON ""BankChecks"" (""Status"");
                CREATE INDEX IF NOT EXISTS ""IX_BankChecks_Direction""      ON ""BankChecks"" (""Direction"");
                CREATE INDEX IF NOT EXISTS ""IX_BankChecks_CustomerId""     ON ""BankChecks"" (""CustomerId"");
                CREATE INDEX IF NOT EXISTS ""IX_BankChecks_SupplierId""     ON ""BankChecks"" (""SupplierId"");
                CREATE INDEX IF NOT EXISTS ""IX_BankChecks_JournalEntryId"" ON ""BankChecks"" (""JournalEntryId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""BankChecks"";
                DELETE FROM ""Accounts"" WHERE ""Code"" IN ('1102','2400');
            ");
        }
    }
}