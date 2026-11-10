using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nabohjelp.Migrations
{
    /// <summary>
    /// Legger til kolonnen AnnetType på Ressurser og OnskedeRessurser.
    /// Der lagres det brukeren skriver selv når typen er «Annet».
    /// Eldre rader får tom verdi.
    /// </summary>
    public partial class LeggTilAnnetTypeForRessurser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnnetType",
                table: "Ressurser",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AnnetType",
                table: "OnskedeRessurser",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnetType",
                table: "Ressurser");

            migrationBuilder.DropColumn(
                name: "AnnetType",
                table: "OnskedeRessurser");
        }
    }
}
