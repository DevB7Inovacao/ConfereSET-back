using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class add_empresaid_catalogos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Despesas
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Despesas",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Equipamentos
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Equipamentos",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // MaoDeObra
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "MaoDeObra",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // TiposOcorrencia
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "TiposOcorrencia",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // GrupoDeObras
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "GrupoDeObras",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EmpresaId", table: "Despesas");
            migrationBuilder.DropColumn(name: "EmpresaId", table: "Equipamentos");
            migrationBuilder.DropColumn(name: "EmpresaId", table: "MaoDeObra");
            migrationBuilder.DropColumn(name: "EmpresaId", table: "TiposOcorrencia");
            migrationBuilder.DropColumn(name: "EmpresaId", table: "GrupoDeObras");
        }
    }
}
