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
                        //EnsureMultiTenantColumns(appContext, logger);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Falha no fallback de colunas multi-tenant.");
                    }

                    // [v2] Garante coluna Titulo em RelatorioSecao mesmo se a migration EF
                    // ainda não foi aplicada (ambientes antigos, snapshot dessincronizado).
                    try
                    {
                        //EnsureRelatorioV2Columns(appContext, logger);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Falha no fallback de colunas Relatórios v2.");
                    }

                    // [v2 Relatórios/Conferelist] Se a migration relatorios_checklists_v2 falhar, as
                    // consultas de checklist/fotos quebrariam por coluna inexistente. Idempotente.
                    try
                    {
                        EnsureRelatoriosChecklistsV2(appContext, logger);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Falha no fallback Relatórios/Conferelist v2.");
                    }

                    // [v11] Self-heal de senhas com whitespace nas pontas. Idempotente.
                    try
                    {
                        //TrimUserPasswords(appContext, logger);
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
        /// Fallback aditivo da migration 20261002164839_relatorios_checklists_v2. Só cria o que
        /// estiver faltando (ADD COLUMN / CREATE TABLE IF NOT EXISTS); nunca remove nada.
        /// </summary>
        private static void EnsureRelatoriosChecklistsV2(DbContextClass ctx, ILogger? logger)
        {
            var comandos = new[]
            {
                "ALTER TABLE \"RelatorioSecoes\" ADD COLUMN IF NOT EXISTS \"Titulo\" text NULL;",
                "ALTER TABLE \"Empresas\" ADD COLUMN IF NOT EXISTS \"PrimaryColor\" text NULL;",
                "ALTER TABLE \"RelatorioItemFotos\" ADD COLUMN IF NOT EXISTS \"Legenda\" text NULL;",
                "ALTER TABLE \"RelatorioItemFotos\" ADD COLUMN IF NOT EXISTS \"Ordem\" integer NOT NULL DEFAULT 0;",
                "ALTER TABLE \"ObraChecklists\" ADD COLUMN IF NOT EXISTS \"AssinaturaUrl\" text NULL;",
                "ALTER TABLE \"ObraChecklists\" ADD COLUMN IF NOT EXISTS \"ConcluidoEm\" timestamp with time zone NULL;",
                "ALTER TABLE \"ObraChecklists\" ADD COLUMN IF NOT EXISTS \"ConcluidoPorUserId\" integer NULL;",
                "ALTER TABLE \"ObraChecklists\" ADD COLUMN IF NOT EXISTS \"ObservacaoGeral\" text NULL;",
                "ALTER TABLE \"ObraChecklists\" ADD COLUMN IF NOT EXISTS \"ResponsavelNome\" text NULL;",
                "ALTER TABLE \"ObraChecklists\" ADD COLUMN IF NOT EXISTS \"Titulo\" text NULL;",
                "ALTER TABLE \"ObraChecklistItens\" ADD COLUMN IF NOT EXISTS \"RespondidoEm\" timestamp with time zone NULL;",
                "ALTER TABLE \"ObraChecklistItens\" ADD COLUMN IF NOT EXISTS \"RespondidoPorUserId\" integer NULL;",
                "ALTER TABLE \"ObraChecklistItens\" ADD COLUMN IF NOT EXISTS \"Valor\" text NULL;",
                "ALTER TABLE \"Checklists\" ADD COLUMN IF NOT EXISTS \"Categoria\" text NULL;",
                "ALTER TABLE \"Checklists\" ADD COLUMN IF NOT EXISTS \"Descricao\" text NULL;",
                "ALTER TABLE \"ChecklistItens\" ADD COLUMN IF NOT EXISTS \"Ajuda\" text NULL;",
                "ALTER TABLE \"ChecklistItens\" ADD COLUMN IF NOT EXISTS \"ExigirFotoNaoConforme\" boolean NOT NULL DEFAULT false;",
                "ALTER TABLE \"ChecklistItens\" ADD COLUMN IF NOT EXISTS \"ExigirObservacaoNaoConforme\" boolean NOT NULL DEFAULT false;",
                "ALTER TABLE \"ChecklistItens\" ADD COLUMN IF NOT EXISTS \"Grupo\" text NULL;",
                "ALTER TABLE \"ChecklistItens\" ADD COLUMN IF NOT EXISTS \"Obrigatorio\" boolean NOT NULL DEFAULT false;",
                "ALTER TABLE \"ChecklistItens\" ADD COLUMN IF NOT EXISTS \"Opcoes\" text NULL;",
                "ALTER TABLE \"ChecklistItens\" ADD COLUMN IF NOT EXISTS \"Tipo\" integer NOT NULL DEFAULT 1;",
                "CREATE TABLE IF NOT EXISTS \"ObraChecklistItemFotos\" (" +
                    "\"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, " +
                    "\"ObraChecklistItemId\" integer NOT NULL REFERENCES \"ObraChecklistItens\"(\"Id\") ON DELETE CASCADE, " +
                    "\"S3Url\" text NOT NULL, \"ContentType\" text NOT NULL, \"NomeArquivo\" text NULL, \"Legenda\" text NULL, " +
                    "\"CriadoPorUserId\" integer NULL, \"CreatedDate\" timestamp with time zone NOT NULL, " +
                    "\"UpdatedDate\" timestamp with time zone NOT NULL);",
                "CREATE INDEX IF NOT EXISTS \"IX_ObraChecklistItemFotos_ObraChecklistItemId\" ON \"ObraChecklistItemFotos\" (\"ObraChecklistItemId\");",
            };

            foreach (var sql in comandos)
            {
                try
                {
                    ctx.Database.ExecuteSqlRaw(sql);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Fallback v2: falha ao executar {Sql}", sql);
                }
            }
        }

        /// <summary>
        /// [v2] Fallback aditivo para a refatoração de Relatórios.
        /// Adiciona Titulo em RelatorioSecao se ainda não existir.
        /// </summary>
        private static void EnsureRelatorioV2Columns(DbContextClass ctx, ILogger? logger)
        {
            var sql = "ALTER TABLE \"RelatorioSecoes\" ADD COLUMN IF NOT EXISTS \"Titulo\" text NULL;";
            try
            {
                ctx.Database.ExecuteSqlRaw(sql);
                logger?.LogInformation("Coluna Titulo garantida em RelatorioSecoes.");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Falha ao garantir coluna Titulo em RelatorioSecoes.");
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
