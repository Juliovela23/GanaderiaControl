using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanaderiaControl.Migrations
{
    /// <inheritdoc />
    public partial class Alerta_DestinatarioUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinatarioUserId",
                table: "Alertas",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_DestinatarioUserId",
                table: "Alertas",
                column: "DestinatarioUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alertas_AspNetUsers_DestinatarioUserId",
                table: "Alertas",
                column: "DestinatarioUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alertas_AspNetUsers_DestinatarioUserId",
                table: "Alertas");

            migrationBuilder.DropIndex(
                name: "IX_Alertas_DestinatarioUserId",
                table: "Alertas");

            migrationBuilder.DropColumn(
                name: "DestinatarioUserId",
                table: "Alertas");
        }
    }
}
