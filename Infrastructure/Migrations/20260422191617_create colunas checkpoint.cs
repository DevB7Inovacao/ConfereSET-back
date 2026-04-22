using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class createcolunascheckpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataHora",
                table: "ObraChecklistItens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "ObraChecklistItens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Equipamento",
                table: "ObraChecklistItens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "ObraChecklistItens",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataHora",
                table: "ObraChecklistItens");

            migrationBuilder.DropColumn(
                name: "Empresa",
                table: "ObraChecklistItens");

            migrationBuilder.DropColumn(
                name: "Equipamento",
                table: "ObraChecklistItens");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "ObraChecklistItens");
        }
    }
}
