using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaBrandingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Empresas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppName",
                table: "Empresas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Empresas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoBase64",
                table: "Empresas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                table: "Empresas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Empresas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Empresas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradeName",
                table: "Empresas",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "AppName",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "LogoBase64",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "LogoContentType",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "TradeName",
                table: "Empresas");
        }
    }
}
