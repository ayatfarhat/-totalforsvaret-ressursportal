using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nabohjelp.Migrations
{
    /// <summary>
    /// Legger til kolonnen Kommunenummer på Behovsliste, slik at behovskøen kan vise
    /// hvilken kommune behovet er i. Eldre behov får tom verdi.
    /// </summary>
    public partial class LeggTilKommuneForBehov : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kommunenummer",
                table: "Behovsliste",
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
                table: "Behovsliste");
        }
    }
}
