using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanaderiaControl.Data.Migrations
{
    /// <inheritdoc />
    public partial class FirstTableCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Animales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Arete = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Raza = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstadoReproductivo = table.Column<int>(type: "int", nullable: false),
                    MadreId = table.Column<int>(type: "int", nullable: true),
                    PadreId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Animales_Animales_MadreId",
                        column: x => x.MadreId,
                        principalTable: "Animales",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Animales_Animales_PadreId",
                        column: x => x.PadreId,
                        principalTable: "Animales",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Alertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    FechaObjetivo = table.Column<DateTime>(type: "date", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Disparador = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alertas_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventosSalud",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    Diagnostico = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Tratamiento = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    Restricciones = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosSalud", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosSalud_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lactancias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "date", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "date", nullable: true),
                    ProduccionPromedioDiaLitros = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lactancias", x => x.Id);
                    table.CheckConstraint("CK_Lactancia_Fechas", "[FechaFin] IS NULL OR [FechaFin] >= [FechaInicio]");
                    table.ForeignKey(
                        name: "FK_Lactancias_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Partos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MadreId = table.Column<int>(type: "int", nullable: false),
                    FechaParto = table.Column<DateTime>(type: "date", nullable: false),
                    TipoParto = table.Column<int>(type: "int", nullable: false),
                    RetencionPlacenta = table.Column<bool>(type: "bit", nullable: false),
                    Asistencia = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Partos_Animales_MadreId",
                        column: x => x.MadreId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosLeche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "date", nullable: false),
                    LitrosDia = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosLeche", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosLeche_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Secados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    FechaSecado = table.Column<DateTime>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Secados_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    FechaServicio = table.Column<DateTime>(type: "date", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ToroOProveedor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servicios_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Crias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartoId = table.Column<int>(type: "int", nullable: false),
                    Sexo = table.Column<int>(type: "int", nullable: false),
                    PesoNacimientoKg = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    AreteAsignado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Crias_Partos_PartoId",
                        column: x => x.PartoId,
                        principalTable: "Partos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Chequeos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    FechaChequeo = table.Column<DateTime>(type: "date", nullable: false),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    Metodo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                    ServicioReproductivoId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chequeos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chequeos_Animales_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Chequeos_Servicios_ServicioReproductivoId",
                        column: x => x.ServicioReproductivoId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Animales",
                columns: new[] { "Id", "Arete", "CreatedAt", "CreatedBy", "EstadoReproductivo", "FechaNacimiento", "IsDeleted", "MadreId", "Nombre", "PadreId", "Raza", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, "BV-0001", new DateTime(2025, 8, 25, 23, 16, 32, 608, DateTimeKind.Utc).AddTicks(7998), null, 0, null, false, null, "Luna", null, "Holstein", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_AnimalId_Tipo_FechaObjetivo",
                table: "Alertas",
                columns: new[] { "AnimalId", "Tipo", "FechaObjetivo" });

            migrationBuilder.CreateIndex(
                name: "IX_Animales_Arete",
                table: "Animales",
                column: "Arete",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Animales_MadreId",
                table: "Animales",
                column: "MadreId");

            migrationBuilder.CreateIndex(
                name: "IX_Animales_PadreId",
                table: "Animales",
                column: "PadreId");

            migrationBuilder.CreateIndex(
                name: "IX_Chequeos_AnimalId_FechaChequeo",
                table: "Chequeos",
                columns: new[] { "AnimalId", "FechaChequeo" });

            migrationBuilder.CreateIndex(
                name: "IX_Chequeos_ServicioReproductivoId",
                table: "Chequeos",
                column: "ServicioReproductivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Crias_PartoId",
                table: "Crias",
                column: "PartoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosSalud_AnimalId",
                table: "EventosSalud",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Lactancias_AnimalId",
                table: "Lactancias",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Partos_MadreId_FechaParto",
                table: "Partos",
                columns: new[] { "MadreId", "FechaParto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosLeche_AnimalId_Fecha",
                table: "RegistrosLeche",
                columns: new[] { "AnimalId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Secados_AnimalId_FechaSecado",
                table: "Secados",
                columns: new[] { "AnimalId", "FechaSecado" });

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_AnimalId_FechaServicio",
                table: "Servicios",
                columns: new[] { "AnimalId", "FechaServicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alertas");

            migrationBuilder.DropTable(
                name: "Chequeos");

            migrationBuilder.DropTable(
                name: "Crias");

            migrationBuilder.DropTable(
                name: "EventosSalud");

            migrationBuilder.DropTable(
                name: "Lactancias");

            migrationBuilder.DropTable(
                name: "RegistrosLeche");

            migrationBuilder.DropTable(
                name: "Secados");

            migrationBuilder.DropTable(
                name: "Servicios");

            migrationBuilder.DropTable(
                name: "Partos");

            migrationBuilder.DropTable(
                name: "Animales");
        }
    }
}
