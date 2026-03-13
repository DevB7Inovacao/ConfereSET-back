using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addRelatorioSecaoTipoOcorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoOcorrenciaId",
                table: "RelatorioSecoes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelatorioSecoes_TipoOcorrenciaId",
                table: "RelatorioSecoes",
                column: "TipoOcorrenciaId");

            migrationBuilder.AddForeignKey(
                name: "FK_RelatorioSecoes_TiposOcorrencia_TipoOcorrenciaId",
                table: "RelatorioSecoes",
                column: "TipoOcorrenciaId",
                principalTable: "TiposOcorrencia",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RelatorioSecoes_TiposOcorrencia_TipoOcorrenciaId",
                table: "RelatorioSecoes");

            migrationBuilder.DropIndex(
                name: "IX_RelatorioSecoes_TipoOcorrenciaId",
                table: "RelatorioSecoes");

            migrationBuilder.DropColumn(
                name: "TipoOcorrenciaId",
                table: "RelatorioSecoes");
        }
    }
}
