using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanaderiaControl.Migrations
{
    /// <inheritdoc />
    public partial class partosIdnew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "userId",
                table: "Crias",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "userId",
                table: "Animales",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Animales",
                keyColumn: "Id",
                keyValue: 1,
                column: "userId",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "userId",
                table: "Crias");

            migrationBuilder.DropColumn(
                name: "userId",
                table: "Animales");
        }
    }
}
