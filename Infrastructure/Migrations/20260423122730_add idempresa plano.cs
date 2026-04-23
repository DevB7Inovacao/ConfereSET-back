using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addidempresaplano : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Planos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Planos_EmpresaId",
                table: "Planos",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Planos_Empresas_EmpresaId",
                table: "Planos",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Planos_Empresas_EmpresaId",
                table: "Planos");

            migrationBuilder.DropIndex(
                name: "IX_Planos_EmpresaId",
                table: "Planos");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Planos");
        }
    }
}
