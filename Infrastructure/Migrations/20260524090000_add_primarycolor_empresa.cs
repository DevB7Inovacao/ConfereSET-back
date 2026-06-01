using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
	// Adiciona a cor primária da empresa. Aditiva/idempotente, segura para prod.
	public partial class add_primarycolor_empresa : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
 name: "PrimaryColor",
 table: "Empresas",
 type: "text",
 nullable: true);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
 name: "PrimaryColor",
 table: "Empresas");
		}
	}
}
