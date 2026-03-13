using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addOcorrencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ocorrencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObraId = table.Column<int>(type: "integer", nullable: false),
                    TipoOcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Localizacao = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataOcorrencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoPorUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_TiposOcorrencia_TipoOcorrenciaId",
                        column: x => x.TipoOcorrenciaId,
                        principalTable: "TiposOcorrencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_User_CriadoPorUserId",
                        column: x => x.CriadoPorUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_CriadoPorUserId",
                table: "Ocorrencias",
                column: "CriadoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_ObraId",
                table: "Ocorrencias",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_TipoOcorrenciaId",
                table: "Ocorrencias",
                column: "TipoOcorrenciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ocorrencias");
        }
    }
}
