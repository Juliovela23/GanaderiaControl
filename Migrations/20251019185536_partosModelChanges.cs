using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanaderiaControl.Migrations
{
    /// <inheritdoc />
    public partial class partosModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Complicaciones",
                table: "Partos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "userId",
                table: "Partos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Complicaciones",
                table: "Partos");

            migrationBuilder.DropColumn(
                name: "userId",
                table: "Partos");
        }
    }
}
