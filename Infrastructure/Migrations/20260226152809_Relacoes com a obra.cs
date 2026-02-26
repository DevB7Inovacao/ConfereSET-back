using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Relacoescomaobra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ObraDespesas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObraId = table.Column<int>(type: "integer", nullable: false),
                    DespesaId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObraDespesas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObraDespesas_Despesas_DespesaId",
                        column: x => x.DespesaId,
                        principalTable: "Despesas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObraDespesas_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObraEquipamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObraId = table.Column<int>(type: "integer", nullable: false),
                    EquipamentoId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObraEquipamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObraEquipamentos_Equipamentos_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "Equipamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObraEquipamentos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObraMaoDeObra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObraId = table.Column<int>(type: "integer", nullable: false),
                    MaoDeObraId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObraMaoDeObra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObraMaoDeObra_MaoDeObra_MaoDeObraId",
                        column: x => x.MaoDeObraId,
                        principalTable: "MaoDeObra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObraMaoDeObra_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObraModelosTexto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObraId = table.Column<int>(type: "integer", nullable: false),
                    ModeloTextoId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObraModelosTexto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObraModelosTexto_ModeloTextos_ModeloTextoId",
                        column: x => x.ModeloTextoId,
                        principalTable: "ModeloTextos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObraModelosTexto_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObraTiposOcorrencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObraId = table.Column<int>(type: "integer", nullable: false),
                    TipoOcorrenciaId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObraTiposOcorrencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObraTiposOcorrencia_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObraTiposOcorrencia_TiposOcorrencia_TipoOcorrenciaId",
                        column: x => x.TipoOcorrenciaId,
                        principalTable: "TiposOcorrencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObraDespesas_DespesaId",
                table: "ObraDespesas",
                column: "DespesaId");

            migrationBuilder.CreateIndex(
                name: "IX_ObraDespesas_ObraId_DespesaId",
                table: "ObraDespesas",
                columns: new[] { "ObraId", "DespesaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObraEquipamentos_EquipamentoId",
                table: "ObraEquipamentos",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ObraEquipamentos_ObraId_EquipamentoId",
                table: "ObraEquipamentos",
                columns: new[] { "ObraId", "EquipamentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObraMaoDeObra_MaoDeObraId",
                table: "ObraMaoDeObra",
                column: "MaoDeObraId");

            migrationBuilder.CreateIndex(
                name: "IX_ObraMaoDeObra_ObraId_MaoDeObraId",
                table: "ObraMaoDeObra",
                columns: new[] { "ObraId", "MaoDeObraId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObraModelosTexto_ModeloTextoId",
                table: "ObraModelosTexto",
                column: "ModeloTextoId");

            migrationBuilder.CreateIndex(
                name: "IX_ObraModelosTexto_ObraId_ModeloTextoId",
                table: "ObraModelosTexto",
                columns: new[] { "ObraId", "ModeloTextoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObraTiposOcorrencia_ObraId_TipoOcorrenciaId",
                table: "ObraTiposOcorrencia",
                columns: new[] { "ObraId", "TipoOcorrenciaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObraTiposOcorrencia_TipoOcorrenciaId",
                table: "ObraTiposOcorrencia",
                column: "TipoOcorrenciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObraDespesas");

            migrationBuilder.DropTable(
                name: "ObraEquipamentos");

            migrationBuilder.DropTable(
                name: "ObraMaoDeObra");

            migrationBuilder.DropTable(
                name: "ObraModelosTexto");

            migrationBuilder.DropTable(
                name: "ObraTiposOcorrencia");
        }
    }
}
