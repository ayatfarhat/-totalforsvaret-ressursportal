using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nabohjelp.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTildelinger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tildelinger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BehovId = table.Column<int>(type: "int", nullable: false),
                    RessursId = table.Column<int>(type: "int", nullable: false),
                    TildeltAvBrukerId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TildeltTid = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tildelinger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tildelinger_AspNetUsers_TildeltAvBrukerId",
                        column: x => x.TildeltAvBrukerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tildelinger_Behovsliste_BehovId",
                        column: x => x.BehovId,
                        principalTable: "Behovsliste",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tildelinger_Ressurser_RessursId",
                        column: x => x.RessursId,
                        principalTable: "Ressurser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tildelinger_BehovId",
                table: "Tildelinger",
                column: "BehovId");

            migrationBuilder.CreateIndex(
                name: "IX_Tildelinger_RessursId",
                table: "Tildelinger",
                column: "RessursId");

            migrationBuilder.CreateIndex(
                name: "IX_Tildelinger_TildeltAvBrukerId",
                table: "Tildelinger",
                column: "TildeltAvBrukerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tildelinger");
        }
    }
}
