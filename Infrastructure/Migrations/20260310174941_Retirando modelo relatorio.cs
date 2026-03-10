using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Retirandomodelorelatorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Relatorios_ModeloRelatorios_ModeloRelatorioId",
                table: "Relatorios");

            migrationBuilder.DropTable(
                name: "ModeloRelatorioSecoes");

            migrationBuilder.DropTable(
                name: "ModeloRelatorios");

            migrationBuilder.RenameColumn(
                name: "Titulo",
                table: "RelatorioSecoes",
                newName: "DataSecao");

            migrationBuilder.RenameColumn(
                name: "ModeloRelatorioId",
                table: "Relatorios",
                newName: "ModeloTextoId");

            migrationBuilder.RenameIndex(
                name: "IX_Relatorios_ModeloRelatorioId",
                table: "Relatorios",
                newName: "IX_Relatorios_ModeloTextoId");

            migrationBuilder.AddColumn<string>(
                name: "HtmlSnapshot",
                table: "Relatorios",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Relatorios_ModeloTextos_ModeloTextoId",
                table: "Relatorios",
                column: "ModeloTextoId",
                principalTable: "ModeloTextos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Relatorios_ModeloTextos_ModeloTextoId",
                table: "Relatorios");

            migrationBuilder.DropColumn(
                name: "HtmlSnapshot",
                table: "Relatorios");

            migrationBuilder.RenameColumn(
                name: "DataSecao",
                table: "RelatorioSecoes",
                newName: "Titulo");

            migrationBuilder.RenameColumn(
                name: "ModeloTextoId",
                table: "Relatorios",
                newName: "ModeloRelatorioId");

            migrationBuilder.RenameIndex(
                name: "IX_Relatorios_ModeloTextoId",
                table: "Relatorios",
                newName: "IX_Relatorios_ModeloRelatorioId");

            migrationBuilder.CreateTable(
                name: "ModeloRelatorios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModeloRelatorios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModeloRelatorioSecoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModeloRelatorioId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    TipoSecao = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModeloRelatorioSecoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModeloRelatorioSecoes_ModeloRelatorios_ModeloRelatorioId",
                        column: x => x.ModeloRelatorioId,
                        principalTable: "ModeloRelatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModeloRelatorioSecoes_ModeloRelatorioId",
                table: "ModeloRelatorioSecoes",
                column: "ModeloRelatorioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Relatorios_ModeloRelatorios_ModeloRelatorioId",
                table: "Relatorios",
                column: "ModeloRelatorioId",
                principalTable: "ModeloRelatorios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
