using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPgTrgmIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // فهارس GIN (pg_trgm) لعمليات ILIKE '%term%' على شاشات الأصناف/العملاء/الموردين.
            // نمط البداية الجديدة (%) لا يستخدم فهارس B-Tree العادية (Seq Scan لكل بحث)،
            // وفهارس GIN + gin_trgm_ops تسرّعه عبر Bitmap Index Scan.
            migrationBuilder.Sql(@"
                CREATE EXTENSION IF NOT EXISTS pg_trgm;

                CREATE INDEX IF NOT EXISTS idx_items_namear_trgm  ON ""Items""     USING gin (""NameAr""  gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_items_nameen_trgm  ON ""Items""     USING gin (""NameEn""  gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_items_code_trgm    ON ""Items""     USING gin (""Code""    gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_items_barcode_trgm ON ""Items""     USING gin (""Barcode"" gin_trgm_ops);

                CREATE INDEX IF NOT EXISTS idx_customers_code_trgm   ON ""Customers"" USING gin (""Code""   gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_customers_namear_trgm ON ""Customers"" USING gin (""NameAr"" gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_customers_nameen_trgm ON ""Customers"" USING gin (""NameEn"" gin_trgm_ops);

                CREATE INDEX IF NOT EXISTS idx_suppliers_code_trgm   ON ""Suppliers"" USING gin (""Code""   gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_suppliers_namear_trgm ON ""Suppliers"" USING gin (""NameAr"" gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_suppliers_nameen_trgm ON ""Suppliers"" USING gin (""NameEn"" gin_trgm_ops);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // عكس تام: تُحذف الفهارس وأخيراً الامتداد (يُحذف فقط إن لم تعد تُستخدم).
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS idx_items_namear_trgm;
                DROP INDEX IF EXISTS idx_items_nameen_trgm;
                DROP INDEX IF EXISTS idx_items_code_trgm;
                DROP INDEX IF EXISTS idx_items_barcode_trgm;

                DROP INDEX IF EXISTS idx_customers_code_trgm;
                DROP INDEX IF EXISTS idx_customers_namear_trgm;
                DROP INDEX IF EXISTS idx_customers_nameen_trgm;

                DROP INDEX IF EXISTS idx_suppliers_code_trgm;
                DROP INDEX IF EXISTS idx_suppliers_namear_trgm;
                DROP INDEX IF EXISTS idx_suppliers_nameen_trgm;

                DROP EXTENSION IF EXISTS pg_trgm;
            ");
        }
    }
}