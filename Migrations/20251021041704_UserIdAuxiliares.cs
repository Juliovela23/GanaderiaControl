using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanaderiaControl.Migrations
{
    /// <inheritdoc />
    public partial class UserIdAuxiliares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Servicios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Secados",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "RegistrosLeche",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Lactancias",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "EventosSalud",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ChequeosGestacion",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "userId",
                table: "Alertas",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Secados");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RegistrosLeche");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Lactancias");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "EventosSalud");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChequeosGestacion");

            migrationBuilder.DropColumn(
                name: "userId",
                table: "Alertas");
        }
    }
}
