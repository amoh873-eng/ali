using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPosGridLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // يُضاف العمود بقيمة افتراضية 30: أي قاعدة قائمة (أو جديدة) تحصل على الافتراضي
            // تلقائياً دون إجراء يدوي — تماماً كطلب "القيمة الافتراضية عند التركيب الأول".
            migrationBuilder.AddColumn<int>(
                name: "PosGridLimit",
                table: "SystemSettings",
                type: "integer",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosGridLimit",
                table: "SystemSettings");
        }
    }
}
