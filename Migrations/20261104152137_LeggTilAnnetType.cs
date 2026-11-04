using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nabohjelp.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilAnnetType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnnetType",
                table: "Behovsliste",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnetType",
                table: "Behovsliste");
        }
    }
}
