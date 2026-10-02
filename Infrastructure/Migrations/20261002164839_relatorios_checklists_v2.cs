using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class relatorios_checklists_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ObraChecklists_ObraId_ChecklistId",
                table: "ObraChecklists");

            // RelatorioSecoes.Titulo e Empresas.PrimaryColor já existiam no modelo, mas as migrations
            // manuais que os criavam (20260522120000/20260524090000) não têm [Migration] e nunca são
            // aplicadas pelo EF; o snapshot também não os tinha. Idempotente: não falha se a coluna
            // já tiver sido criada manualmente/pelo fallback antigo do MigrationManager.
            migrationBuilder.Sql("ALTER TABLE \"RelatorioSecoes\" ADD COLUMN IF NOT EXISTS \"Titulo\" text NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Empresas\" ADD COLUMN IF NOT EXISTS \"PrimaryColor\" text NULL;");

            migrationBuilder.AddColumn<string>(
                name: "Legenda",
                table: "RelatorioItemFotos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Ordem",
                table: "RelatorioItemFotos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AssinaturaUrl",
                table: "ObraChecklists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConcluidoEm",
                table: "ObraChecklists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConcluidoPorUserId",
                table: "ObraChecklists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacaoGeral",
                table: "ObraChecklists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelNome",
                table: "ObraChecklists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Titulo",
                table: "ObraChecklists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondidoEm",
                table: "ObraChecklistItens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RespondidoPorUserId",
                table: "ObraChecklistItens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Valor",
                table: "ObraChecklistItens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Checklists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Checklists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ajuda",
                table: "ChecklistItens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExigirFotoNaoConforme",
                table: "ChecklistItens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ExigirObservacaoNaoConforme",
                table: "ChecklistItens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Grupo",
                table: "ChecklistItens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Obrigatorio",
                table: "ChecklistItens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Opcoes",
                table: "ChecklistItens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "ChecklistItens",
                type: "integer",
                nullable: false,
                defaultValue: 1); // 1 = Conforme/Não conforme/N/A (legado)

            migrationBuilder.CreateTable(
                name: "ObraChecklistItemFotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObraChecklistItemId = table.Column<int>(type: "integer", nullable: false),
                    S3Url = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    NomeArquivo = table.Column<string>(type: "text", nullable: true),
                    Legenda = table.Column<string>(type: "text", nullable: true),
                    CriadoPorUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObraChecklistItemFotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObraChecklistItemFotos_ObraChecklistItens_ObraChecklistItem~",
                        column: x => x.ObraChecklistItemId,
                        principalTable: "ObraChecklistItens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObraChecklists_ObraId_ChecklistId",
                table: "ObraChecklists",
                columns: new[] { "ObraId", "ChecklistId" });

            migrationBuilder.CreateIndex(
                name: "IX_ObraChecklistItemFotos_ObraChecklistItemId",
                table: "ObraChecklistItemFotos",
                column: "ObraChecklistItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObraChecklistItemFotos");

            migrationBuilder.DropIndex(
                name: "IX_ObraChecklists_ObraId_ChecklistId",
                table: "ObraChecklists");

            // RelatorioSecoes.Titulo e Empresas.PrimaryColor não são removidos aqui: pertencem a
            // funcionalidades anteriores (ver comentário no Up).

            migrationBuilder.DropColumn(
                name: "Legenda",
                table: "RelatorioItemFotos");

            migrationBuilder.DropColumn(
                name: "Ordem",
                table: "RelatorioItemFotos");

            migrationBuilder.DropColumn(
                name: "AssinaturaUrl",
                table: "ObraChecklists");

            migrationBuilder.DropColumn(
                name: "ConcluidoEm",
                table: "ObraChecklists");

            migrationBuilder.DropColumn(
                name: "ConcluidoPorUserId",
                table: "ObraChecklists");

            migrationBuilder.DropColumn(
                name: "ObservacaoGeral",
                table: "ObraChecklists");

            migrationBuilder.DropColumn(
                name: "ResponsavelNome",
                table: "ObraChecklists");

            migrationBuilder.DropColumn(
                name: "Titulo",
                table: "ObraChecklists");

            migrationBuilder.DropColumn(
                name: "RespondidoEm",
                table: "ObraChecklistItens");

            migrationBuilder.DropColumn(
                name: "RespondidoPorUserId",
                table: "ObraChecklistItens");

            migrationBuilder.DropColumn(
                name: "Valor",
                table: "ObraChecklistItens");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Checklists");

            migrationBuilder.DropColumn(
                name: "Ajuda",
                table: "ChecklistItens");

            migrationBuilder.DropColumn(
                name: "ExigirFotoNaoConforme",
                table: "ChecklistItens");

            migrationBuilder.DropColumn(
                name: "ExigirObservacaoNaoConforme",
                table: "ChecklistItens");

            migrationBuilder.DropColumn(
                name: "Grupo",
                table: "ChecklistItens");

            migrationBuilder.DropColumn(
                name: "Obrigatorio",
                table: "ChecklistItens");

            migrationBuilder.DropColumn(
                name: "Opcoes",
                table: "ChecklistItens");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "ChecklistItens");

            migrationBuilder.CreateIndex(
                name: "IX_ObraChecklists_ObraId_ChecklistId",
                table: "ObraChecklists",
                columns: new[] { "ObraId", "ChecklistId" },
                unique: true);
        }
    }
}
