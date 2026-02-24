using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdEmpresanasobras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Obras",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "Obras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Obras",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Obras_EmpresaId",
                table: "Obras",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Obras_Empresas_EmpresaId",
                table: "Obras",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Obras_Empresas_EmpresaId",
                table: "Obras");

            migrationBuilder.DropIndex(
                name: "IX_Obras_EmpresaId",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "Obras");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Obras");
        }
    }
}
