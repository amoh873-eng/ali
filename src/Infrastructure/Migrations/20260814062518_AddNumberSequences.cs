using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NumberSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_SequenceKey",
                table: "NumberSequences",
                column: "SequenceKey",
                unique: true);

            // زرع العدّادات من الأرقام الموجودة مسبقاً (حتى لا تتكرر الأرقام بعد الترحيل من النمط القديم CountAsync+1)
            migrationBuilder.Sql(@"
                INSERT INTO NumberSequences (Id, SequenceKey, LastValue)
                SELECT NEWID(), LEFT(EntryNumber, 11), MAX(CAST(RIGHT(EntryNumber, 4) AS INT))
                FROM JournalEntries WHERE EntryNumber LIKE 'JE-%' GROUP BY LEFT(EntryNumber, 11);

                INSERT INTO NumberSequences (Id, SequenceKey, LastValue)
                SELECT NEWID(), LEFT(InvoiceNumber, 11), MAX(CAST(RIGHT(InvoiceNumber, 4) AS INT))
                FROM SalesInvoices WHERE InvoiceNumber LIKE 'SI-%' GROUP BY LEFT(InvoiceNumber, 11);

                INSERT INTO NumberSequences (Id, SequenceKey, LastValue)
                SELECT NEWID(), LEFT(InvoiceNumber, 11), MAX(CAST(RIGHT(InvoiceNumber, 4) AS INT))
                FROM PurchaseInvoices WHERE InvoiceNumber LIKE 'PI-%' GROUP BY LEFT(InvoiceNumber, 11);

                INSERT INTO NumberSequences (Id, SequenceKey, LastValue)
                SELECT NEWID(), LEFT(ReturnNumber, 11), MAX(CAST(RIGHT(ReturnNumber, 4) AS INT))
                FROM SalesReturns WHERE ReturnNumber LIKE 'SR-%' GROUP BY LEFT(ReturnNumber, 11);

                INSERT INTO NumberSequences (Id, SequenceKey, LastValue)
                SELECT NEWID(), LEFT(ReturnNumber, 11), MAX(CAST(RIGHT(ReturnNumber, 4) AS INT))
                FROM PurchaseReturns WHERE ReturnNumber LIKE 'PR-%' GROUP BY LEFT(ReturnNumber, 11);

                INSERT INTO NumberSequences (Id, SequenceKey, LastValue)
                SELECT NEWID(), LEFT(QuoteNumber, 11), MAX(CAST(RIGHT(QuoteNumber, 4) AS INT))
                FROM SalesQuotes WHERE QuoteNumber LIKE 'SQ-%' GROUP BY LEFT(QuoteNumber, 11);

                INSERT INTO NumberSequences (Id, SequenceKey, LastValue)
                SELECT NEWID(), LEFT(OrderNumber, 11), MAX(CAST(RIGHT(OrderNumber, 4) AS INT))
                FROM PurchaseOrders WHERE OrderNumber LIKE 'PO-%' GROUP BY LEFT(OrderNumber, 11);

                INSERT INTO NumberSequences (Id, SequenceKey, LastValue)
                SELECT NEWID(), LEFT(StockCountNumber, 11), MAX(CAST(RIGHT(StockCountNumber, 4) AS INT))
                FROM StockCounts WHERE StockCountNumber LIKE 'SC-%' GROUP BY LEFT(StockCountNumber, 11);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NumberSequences");
        }
    }
}
