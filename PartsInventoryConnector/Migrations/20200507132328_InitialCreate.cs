using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PartsInventoryConnector.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    PartNumber = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Price = table.Column<double>(nullable: false),
                    Inventory = table.Column<int>(nullable: false),
                    Appliances = table.Column<string>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                    LastUpdated = table.Column<DateTime>(nullable: false, defaultValueSql: "datetime()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.PartNumber);
                });

            // Set trigger to update the LastUpdated property
            migrationBuilder.Sql(@"CREATE TRIGGER set_last_updated AFTER UPDATE
            ON Parts
            BEGIN
                UPDATE Parts
                SET LastUpdated = datetime('now')
                WHERE PartNumber = NEW.PartNumber;
            END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER set_last_updated");

            migrationBuilder.DropTable(
                name: "Parts");
        }
    }
}
