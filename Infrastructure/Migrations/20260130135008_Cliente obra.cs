using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Clienteobra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientDocument",
                table: "Obras",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientEmail",
                table: "Obras",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "Obras",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientPhone",
                table: "Obras",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientDocument",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "ClientEmail",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "ClientPhone",
                table: "Obras");
        }
    }
}
