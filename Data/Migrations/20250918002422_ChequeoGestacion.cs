using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanaderiaControl.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChequeoGestacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chequeos_Animales_AnimalId",
                table: "Chequeos");

            migrationBuilder.DropForeignKey(
                name: "FK_Chequeos_Servicios_ServicioReproductivoId",
                table: "Chequeos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Chequeos",
                table: "Chequeos");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Chequeos");

            migrationBuilder.DropColumn(
                name: "Metodo",
                table: "Chequeos");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Chequeos");

            migrationBuilder.RenameTable(
                name: "Chequeos",
                newName: "ChequeoGestacion");

            migrationBuilder.RenameIndex(
                name: "IX_Chequeos_ServicioReproductivoId",
                table: "ChequeoGestacion",
                newName: "IX_ChequeoGestacion_ServicioReproductivoId");

            migrationBuilder.RenameIndex(
                name: "IX_Chequeos_AnimalId_FechaChequeo",
                table: "ChequeoGestacion",
                newName: "IX_ChequeoGestacion_AnimalId_FechaChequeo");

            migrationBuilder.AlterColumn<string>(
                name: "ToroOProveedor",
                table: "Servicios",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "Servicios",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(240)",
                oldMaxLength: 240,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "ChequeoGestacion",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(240)",
                oldMaxLength: 240,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChequeoGestacion",
                table: "ChequeoGestacion",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Animales",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 18, 0, 24, 22, 248, DateTimeKind.Utc).AddTicks(1326));

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChequeoGestacion_Animales_AnimalId",
                table: "ChequeoGestacion");

            migrationBuilder.DropForeignKey(
                name: "FK_ChequeoGestacion_Servicios_ServicioReproductivoId",
                table: "ChequeoGestacion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChequeoGestacion",
                table: "ChequeoGestacion");

            migrationBuilder.RenameTable(
                name: "ChequeoGestacion",
                newName: "Chequeos");

            migrationBuilder.RenameIndex(
                name: "IX_ChequeoGestacion_ServicioReproductivoId",
                table: "Chequeos",
                newName: "IX_Chequeos_ServicioReproductivoId");

            migrationBuilder.RenameIndex(
                name: "IX_ChequeoGestacion_AnimalId_FechaChequeo",
                table: "Chequeos",
                newName: "IX_Chequeos_AnimalId_FechaChequeo");

            migrationBuilder.AlterColumn<string>(
                name: "ToroOProveedor",
                table: "Servicios",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "Servicios",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Servicios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Servicios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "Chequeos",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Chequeos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metodo",
                table: "Chequeos",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Chequeos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Chequeos",
                table: "Chequeos",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "Animales",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 8, 25, 23, 16, 32, 608, DateTimeKind.Utc).AddTicks(7998));

            migrationBuilder.AddForeignKey(
                name: "FK_Chequeos_Animales_AnimalId",
                table: "Chequeos",
                column: "AnimalId",
                principalTable: "Animales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chequeos_Servicios_ServicioReproductivoId",
                table: "Chequeos",
                column: "ServicioReproductivoId",
                principalTable: "Servicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
