using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanaderiaControl.Migrations
{
    /// <inheritdoc />
    public partial class PartialUnique_Arete_NotDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChequeoGestacion_Animales_AnimalId",
                table: "ChequeoGestacion");

            migrationBuilder.DropForeignKey(
                name: "FK_ChequeoGestacion_Servicios_ServicioReproductivoId",
                table: "ChequeoGestacion");

            migrationBuilder.DropIndex(
                name: "IX_Animales_Arete",
                table: "Animales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChequeoGestacion",
                table: "ChequeoGestacion");

            migrationBuilder.RenameTable(
                name: "ChequeoGestacion",
                newName: "ChequeosGestacion");

            migrationBuilder.RenameIndex(
                name: "IX_ChequeoGestacion_ServicioReproductivoId",
                table: "ChequeosGestacion",
                newName: "IX_ChequeosGestacion_ServicioReproductivoId");

            migrationBuilder.RenameIndex(
                name: "IX_ChequeoGestacion_AnimalId_FechaChequeo",
                table: "ChequeosGestacion",
                newName: "IX_ChequeosGestacion_AnimalId_FechaChequeo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChequeosGestacion",
                table: "ChequeosGestacion",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Animales_Arete",
                table: "Animales",
                column: "Arete",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.AddForeignKey(
                name: "FK_ChequeosGestacion_Animales_AnimalId",
                table: "ChequeosGestacion",
                column: "AnimalId",
                principalTable: "Animales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChequeosGestacion_Servicios_ServicioReproductivoId",
                table: "ChequeosGestacion",
                column: "ServicioReproductivoId",
                principalTable: "Servicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChequeosGestacion_Animales_AnimalId",
                table: "ChequeosGestacion");

            migrationBuilder.DropForeignKey(
                name: "FK_ChequeosGestacion_Servicios_ServicioReproductivoId",
                table: "ChequeosGestacion");

            migrationBuilder.DropIndex(
                name: "IX_Animales_Arete",
                table: "Animales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChequeosGestacion",
                table: "ChequeosGestacion");

            migrationBuilder.RenameTable(
                name: "ChequeosGestacion",
                newName: "ChequeoGestacion");

            migrationBuilder.RenameIndex(
                name: "IX_ChequeosGestacion_ServicioReproductivoId",
                table: "ChequeoGestacion",
                newName: "IX_ChequeoGestacion_ServicioReproductivoId");

            migrationBuilder.RenameIndex(
                name: "IX_ChequeosGestacion_AnimalId_FechaChequeo",
                table: "ChequeoGestacion",
                newName: "IX_ChequeoGestacion_AnimalId_FechaChequeo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChequeoGestacion",
                table: "ChequeoGestacion",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Animales_Arete",
                table: "Animales",
                column: "Arete",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChequeoGestacion_Animales_AnimalId",
                table: "ChequeoGestacion",
                column: "AnimalId",
                principalTable: "Animales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChequeoGestacion_Servicios_ServicioReproductivoId",
                table: "ChequeoGestacion",
                column: "ServicioReproductivoId",
                principalTable: "Servicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
