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
                }
            }
            return host;
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
                logger?.LogWarning(e