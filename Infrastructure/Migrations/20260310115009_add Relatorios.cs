using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addRelatorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModeloRelatorios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    TipoSecao = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Relatorios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModeloRelatorioId = table.Column<int>(type: "integer", nullable: false),
                    ObraId = table.Column<int>(type: "integer", nullable: false),
                    CriadoPorUserId = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataRelatorio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relatorios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relatorios_ModeloRelatorios_ModeloRelatorioId",
                        column: x => x.ModeloRelatorioId,
                        principalTable: "ModeloRelatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relatorios_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relatorios_User_CriadoPorUserId",
                        column: x => x.CriadoPorUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RelatorioSecoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RelatorioId = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    TipoSecao = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    ConteudoJson = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatorioSecoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatorioSecoes_Relatorios_RelatorioId",
                        column: x => x.RelatorioId,
                        principalTable: "Relatorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelatorioSecaoItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RelatorioSecaoId = table.Column<int>(type: "integer", nullable: false),
                    ReferenciaId = table.Column<int>(type: "integer", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatorioSecaoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatorioSecaoItens_RelatorioSecoes_RelatorioSecaoId",
                        column: x => x.RelatorioSecaoId,
                        principalTable: "RelatorioSecoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelatorioItemFotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RelatorioSecaoItemId = table.Column<int>(type: "integer", nullable: false),
                    ImagemBytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    NomeArquivo = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatorioItemFotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatorioItemFotos_RelatorioSecaoItens_RelatorioSecaoItemId",
                        column: x => x.RelatorioSecaoItemId,
                        principalTable: "RelatorioSecaoItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModeloRelatorioSecoes_ModeloRelatorioId",
                table: "ModeloRelatorioSecoes",
                column: "ModeloRelatorioId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioItemFotos_RelatorioSecaoItemId",
                table: "RelatorioItemFotos",
                column: "RelatorioSecaoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Relatorios_CriadoPorUserId",
                table: "Relatorios",
                column: "CriadoPorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Relatorios_ModeloRelatorioId",
                table: "Relatorios",
                column: "ModeloRelatorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Relatorios_ObraId",
                table: "Relatorios",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioSecaoItens_RelatorioSecaoId",
                table: "RelatorioSecaoItens",
                column: "RelatorioSecaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioSecoes_RelatorioId",
                table: "RelatorioSecoes",
                column: "RelatorioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModeloRelatorioSecoes");

            migrationBuilder.DropTable(
                name: "RelatorioItemFotos");

            migrationBuilder.DropTable(
                name: "RelatorioSecaoItens");

            migrationBuilder.DropTable(
                name: "RelatorioSecoes");

            migrationBuilder.DropTable(
                name: "Relatorios");

            migrationBuilder.DropTable(
                name: "ModeloRelatorios");
        }
    }
}
