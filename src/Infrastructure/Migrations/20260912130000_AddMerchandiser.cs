using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchandiser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShelfParLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ParLevel = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_ShelfParLevels", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "ShelfChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ObservedQty = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ParLevel = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PhotoOnePath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PhotoTwoPath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_ShelfChecks", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "ShelfPriceCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PhotoPath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_ShelfPriceCaptures", x => x.Id); });

            migrationBuilder.CreateIndex(name: "IX_ShelfParLevels_ItemId", table: "ShelfParLevels", column: "ItemId");
            migrationBuilder.CreateIndex(name: "IX_ShelfParLevels_Location", table: "ShelfParLevels", column: "Location");
            migrationBuilder.CreateIndex(name: "IX_ShelfChecks_ItemId", table: "ShelfChecks", column: "ItemId");
            migrationBuilder.CreateIndex(name: "IX_ShelfChecks_Location", table: "ShelfChecks", column: "Location");
            migrationBuilder.CreateIndex(name: "IX_ShelfChecks_CheckedAt", table: "ShelfChecks", column: "CheckedAt");
            migrationBuilder.CreateIndex(name: "IX_ShelfPriceCaptures_ItemId", table: "ShelfPriceCaptures", column: "ItemId");
            migrationBuilder.CreateIndex(name: "IX_ShelfPriceCaptures_CapturedAt", table: "ShelfPriceCaptures", column: "CapturedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ShelfPriceCaptures");
            migrationBuilder.DropTable(name: "ShelfChecks");
            migrationBuilder.DropTable(name: "ShelfParLevels");
        }
    }
}