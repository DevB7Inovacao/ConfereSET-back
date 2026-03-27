using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Assinaturasalerado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LimiteUsuarios",
                table: "Planos",
                newName: "LimiteOperadores");

            migrationBuilder.AddColumn<int>(
                name: "LimiteGestores",
                table: "Planos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LimiteGestores",
                table: "Planos");

            migrationBuilder.RenameColumn(
                name: "LimiteOperadores",
                table: "Planos",
                newName: "LimiteUsuarios");
        }
    }
}
