using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    // Adiciona a cor primária da empresa. Aditiva/idempotente, segura para prod.
    public partial class add_primarycolor_empresa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Empresas"" ADD COLUMN IF NOT EXISTS ""PrimaryColor"" text;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Empresas"" DROP COLUMN IF EXISTS ""PrimaryColor"";");
        }
    }
}
