using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    // [v11] Self-heal: garante que todas as senhas armazenadas estão sem whitespace
    // nas pontas. BCrypt hashes nunca têm whitespace por design, então é seguro;
    // senhas legadas em texto puro (importadas) também ficam consistentes.
    //
    // Esta migration roda automaticamente na inicialização via MigrationManager
    // (Database.Migrate). Idempotente — pode rodar quantas vezes quiser.
    public partial class trim_user_passwords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""User"" SET ""Password"" = TRIM(""Password"") WHERE ""Password"" IS NOT NULL AND ""Password"" <> TRIM(""Password"");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sem rollback — não dá pra restaurar whitespace que já foi removido.
        }
    }
}
