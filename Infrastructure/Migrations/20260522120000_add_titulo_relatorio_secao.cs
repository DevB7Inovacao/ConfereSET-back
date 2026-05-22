using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    // [v2] Adiciona coluna Titulo em RelatorioSecao. Aditiva, segura para rodar em prod.
    public partial class add_titulo_relatorio_secao : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Titulo",
                table: "RelatorioSecoes",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Titulo", table: "RelatorioSecoes");
        }
    }
}
