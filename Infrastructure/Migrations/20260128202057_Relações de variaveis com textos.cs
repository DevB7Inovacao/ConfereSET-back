using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Relaçõesdevariaveiscomtextos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModeloTextoVariavelVinculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    ModeloTextoId = table.Column<int>(type: "integer", nullable: false),
                    ModeloTextoVariavelId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModeloTextoVariavelVinculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModeloTextoVariavelVinculos_ModeloTextoVariaveis_ModeloText~",
                        column: x => x.ModeloTextoVariavelId,
                        principalTable: "ModeloTextoVariaveis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModeloTextoVariavelVinculos_ModeloTextos_ModeloTextoId",
                        column: x => x.ModeloTextoId,
                        principalTable: "ModeloTextos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModeloTextoVariavelVinculos_ModeloTextoId",
                table: "ModeloTextoVariavelVinculos",
                column: "ModeloTextoId");

            migrationBuilder.CreateIndex(
                name: "IX_ModeloTextoVariavelVinculos_ModeloTextoVariavelId",
                table: "ModeloTextoVariavelVinculos",
                column: "ModeloTextoVariavelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModeloTextoVariavelVinculos");
        }
    }
}
