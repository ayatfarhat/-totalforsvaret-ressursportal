using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nabohjelp.Migrations
{
    /// <summary>
    /// Legger til historikk for behovene (tabellen StatusEndringer) og kolonnen
    /// EierHarSett på Tildelinger, som styrer varselet til den som eier ressursen.
    /// Tildelinger som fantes fra før, regnes som sett, så eierne ikke får gamle varsler.
    /// </summary>
    public partial class LeggTilStatushistorikkOgTildelingsvarsel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EierHarSett",
                table: "Tildelinger",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "StatusEndringer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BehovId = table.Column<int>(type: "int", nullable: false),
                    FraStatus = table.Column<int>(type: "int", nullable: false),
                    TilStatus = table.Column<int>(type: "int", nullable: false),
                    Tid = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndretAvBrukerId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RessursType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RessursAnnetType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErAngret = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusEndringer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusEndringer_Behovsliste_BehovId",
                        column: x => x.BehovId,
                        principalTable: "Behovsliste",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StatusEndringer_BehovId",
                table: "StatusEndringer",
                column: "BehovId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatusEndringer");

            migrationBuilder.DropColumn(
                name: "EierHarSett",
                table: "Tildelinger");
        }
    }
}
