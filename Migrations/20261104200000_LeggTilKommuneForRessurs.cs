using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nabohjelp.Migrations
{
    /// <summary>
    /// Legger til kolonnen Kommunenummer på Ressurser, slik at ressurslisten
    /// kan skrives ut per kommune og fylke. Eldre ressurser får tom verdi.
    /// </summary>
    public partial class LeggTilKommuneForRessurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kommunenummer",
                table: "Ressurser",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kommunenummer",
                table: "Ressurser");
        }
    }
}
