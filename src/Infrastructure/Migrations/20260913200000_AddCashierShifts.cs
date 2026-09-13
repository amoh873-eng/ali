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
    [Migration("20260913200000_AddCashierShifts")]
    public partial class AddCashierShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""CashierShifts""
                (
                    ""Id"" uuid NOT NULL,
                    ""CashierUserId"" character varying(450) NOT NULL,
                    ""StartTime"" timestamp without time zone NOT NULL,
                    ""EndTime"" timestamp without time zone NULL,
                    ""Status"" integer NOT NULL,
                    ""OpeningFloatAmount"" numeric(18,2) NOT NULL,
                    ""OpeningTransferId"" uuid NOT NULL,
                    ""ExpectedCashAmount"" numeric(18,2) NULL,
                    ""CountedCashAmount"" numeric(18,2) NULL,
                    ""VarianceAmount"" numeric(18,2) NULL,
                    ""ClosedByUserId"" character varying(450) NULL,
                    ""Notes"" character varying(500) NULL,
                    ""JournalEntryId"" uuid NULL,
                    ""CreatedAt"" timestamp without time zone NOT NULL,
                    ""UpdatedAt"" timestamp without time zone NOT NULL,
                    CONSTRAINT ""PK_CashierShifts"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM ""Accounts"" WHERE ""Code"" = '5110') THEN
                        INSERT INTO ""Accounts"" (""Id"",""AccountType"",""Code"",""CreatedAt"",""CreatedBy"",""Description"",""HierarchyPath"",""IsActive"",""IsDeleted"",""IsSystem"",""Level"",""NameAr"",""NameEn"",""NormalBalance"",""ParentAccountId"",""RowVersion"",""UpdatedAt"",""UpdatedBy"")
                        VALUES ('15000000-0000-0000-0000-000000000040', 5, '5110', '2026-01-01 00:00:00', NULL, NULL, NULL, TRUE, FALSE, TRUE, 0, 'فروقات الصندوق', 'Cash Over/Short', 1, '10000000-0000-0000-0000-000000000005', decode('', 'hex'), NULL, NULL);
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_CashierShifts_CashierUserId""     ON ""CashierShifts"" (""CashierUserId"");
                CREATE INDEX IF NOT EXISTS ""IX_CashierShifts_Status""            ON ""CashierShifts"" (""Status"");
                CREATE INDEX IF NOT EXISTS ""IX_CashierShifts_StartTime""         ON ""CashierShifts"" (""StartTime"");
                CREATE INDEX IF NOT EXISTS ""IX_CashierShifts_JournalEntryId""    ON ""CashierShifts"" (""JournalEntryId"");
                CREATE INDEX IF NOT EXISTS ""IX_CashierShifts_OpeningTransferId"" ON ""CashierShifts"" (""OpeningTransferId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""CashierShifts"";
                DELETE FROM ""Accounts"" WHERE ""Code"" = '5110';
            ");
        }
    }
}