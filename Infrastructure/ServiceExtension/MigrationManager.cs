using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ServiceExtension
{
    public static class MigrationManager
    {
        public static IHost MigrateDatabase(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("MigrationManager");
                using (var appContext = scope.ServiceProvider.GetRequiredService<DbContextClass>())
                {
                    try
                    {
                        logger?.LogInformation("Aplicando migrations pendentes...");
                        appContext.Database.Migrate();
                        logger?.LogInformation("Migrations aplicadas.");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Falha ao aplicar migrations.");
                        // Não derruba a app — segue para o fallback de colunas críticas.
                    }

                    try
                    {
                        EnsureMultiTenantColumns(appContext, logger);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Falha no fallback de colunas multi-tenant.");
                    }

                    // [v2] Garante coluna Titulo em RelatorioSecao mesmo se a migration EF
                    // ainda não foi aplicada (ambientes antigos, snapshot dessincronizado).
                    try
                    {
                        EnsureRelatorioV2Columns(appContext, logger);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Falha no fallback de colunas Relatórios v2.");
                    }

                    // [v11] Self-heal de senhas com whitespace nas pontas. Idempotente.
                    try
                    {
                        TrimUserPasswords(appContext, logger);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Falha no trim de senhas de usuários.");
                    }
                }
            }
            return host;
        }

        /// <summary>
        /// [v11] Remove whitespace nas pontas das senhas armazenadas.
        /// BCrypt hashes nunca têm whitespace, então é seguro. Resolve o caso
        /// de senhas que ficaram com espaço acidental por bug de UI antigo.
        /// </summary>
        private static void TrimUserPasswords(DbContextClass ctx, ILogger? logger)
        {
            var sql = "UPDATE \"User\" SET \"Password\" = TRIM(\"Password\") WHERE \"Password\" IS NOT NULL AND \"Password\" <> TRIM(\"Password\");";
            try
            {
                var affected = ctx.Database.ExecuteSqlRaw(sql);
                if (affected > 0)
                    logger?.LogInformation("Trim aplicado em {Affected} senhas de usuário.", affected);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Falha ao aplicar trim em senhas.");
            }
        }

        /// <summary>
        /// [v2] Fallback aditivo para a refatoração de Relatórios.
        /// Adiciona Titulo em RelatorioSecao se ainda não existir.
        /// </summary>
        private static void EnsureRelatorioV2Columns(DbContextClass ctx, ILogger? logger)
        {
            var sql = "ALTER TABLE \"RelatorioSecao\" ADD COLUMN IF NOT EXISTS \"Titulo\" text NULL;";
            try
            {
                ctx.Database.ExecuteSqlRaw(sql);
                logger?.LogInformation("Coluna Titulo garantida em RelatorioSecao.");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Falha ao garantir coluna Titulo em RelatorioSecao.");
            }
        }

        /// <summary>
        /// Fallback defensivo: garante que as colunas EmpresaId existem nos catálogos compartilhados
        /// mesmo se a migration EF não foi aplicada (ex.: snapshot dessincronizado, ambiente antigo).
        /// Postgres ignora "ADD COLUMN IF NOT EXISTS" se a coluna já existir, então é seguro rodar várias vezes.
        /// </summary>
        private static void EnsureMultiTenantColumns(DbContextClass ctx, ILogger? logger)
        {
            var tabelas = new[] { "Despesas", "Equipamentos", "MaoDeObra", "TiposOcorrencia", "GrupoDeObras" };
            foreach (var t in tabelas)
            {
                var sql = $"ALTER TABLE \"{t}\" ADD COLUMN IF NOT EXISTS \"EmpresaId\" integer NOT NULL DEFAULT 1;";
                try
                {
                    ctx.Database.ExecuteSqlRaw(sql);
                    logger?.LogInformation("Coluna EmpresaId garantida em {Tabela}.", t);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Falha ao garantir coluna EmpresaId em {Tabela}.", t);
                }
            }
        }
    }
}
