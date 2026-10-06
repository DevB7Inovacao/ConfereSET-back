using Core.Models;
using Infrastructure.MercadoPago;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.ServiceExtension
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddDIServices(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = AjustarConexao(configuration.GetConnectionString("DefaultConnection"), configuration);

            services.AddDbContext<DbContextClass>(options =>
            {
                options.UseNpgsql(connectionString)
               .ConfigureWarnings(warnings =>
							 warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
		});

            services.AddHttpClient<IMercadoPagoClient, MercadoPagoClient>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IEmpresasRepository, EmpresasRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IObrasRepository, ObrasRepository>();
            services.AddScoped<IGrupoDeObrasRepository, GrupoDeObrasRepository>();
            services.AddScoped<IModeloTextoRepository, ModeloTextoRepository>();
            services.AddScoped<IModeloTextoVariavelRepository, ModeloTextoVariavelRepository>();
            services.AddScoped<IModeloTextoVariavelVinculoRepository, ModeloTextoVariavelVinculoRepository>();
            services.AddScoped<IMaoDeObraRepository, MaoDeObraRepository>();
            services.AddScoped<IEquipamentosRepository, EquipamentosRepository>();
            services.AddScoped<ITiposOcorrenciaRepository, TiposOcorrenciaRepository>();
            services.AddScoped<IDespesasRepository, DespesasRepository>();
            services.AddScoped<ISupportTicketsRepository, SupportTicketsRepository>();
            services.AddScoped<IChecklistRepository, ChecklistRepository>();
            services.AddScoped<IObraOperadorRepository, ObraOperadorRepository>();
            services.AddScoped<IObraMaoDeObraRepository, ObraMaoDeObraRepository>();
            services.AddScoped<IObraEquipamentoRepository, ObraEquipamentoRepository>();
            services.AddScoped<IObraTipoOcorrenciaRepository, ObraTipoOcorrenciaRepository>();
            services.AddScoped<IObraModeloTextoRepository, ObraModeloTextoRepository>();
            services.AddScoped<IObraDespesaRepository, ObraDespesaRepository>();
            services.AddScoped<IRelatorioRepository, RelatorioRepository>();
            services.AddScoped<IOcorrenciaRepository, OcorrenciaRepository>();
            services.AddScoped<IChecklistItemRepository, ChecklistItemRepository>();
            services.AddScoped<IObraChecklistRepository, ObraChecklistRepository>();
            services.AddScoped<IObraChecklistItemRepository, ObraChecklistItemRepository>();
            services.AddScoped<IAtividadeRecenteRepository, AtividadeRecenteRepository>();
            services.AddScoped<IPlanoRepository, PlanoRepository>();
            services.AddScoped<IAssinaturaRepository, AssinaturaRepository>();
            services.AddScoped<IPagamentoAssinaturaRepository, PagamentoAssinaturaRepository>();

            return services;
        }

        /// <summary>
        /// Garante o pool de conexões do Npgsql. Com "Pooling=false" (como estava na string de
        /// conexão) cada request abria TCP + TLS + login no Postgres — centenas de ms em toda
        /// chamada. O tamanho do pool respeita o limite de conexões do banco gerenciado
        /// (padrão 15; ajustável em Database:MaxPoolSize) e uma conexão fica sempre aquecida.
        /// </summary>
        private static string AjustarConexao(string? connectionString, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return connectionString ?? string.Empty;
            var csb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
            {
                Pooling = true,
            };
            var maxConfig = int.TryParse(configuration["Database:MaxPoolSize"], out var mp) && mp > 0 ? mp : 15;
            // 100 é o padrão do Npgsql (não foi escolhido por ninguém): troca pelo limite seguro.
            if (csb.MaxPoolSize == 100 || csb.MaxPoolSize > maxConfig) csb.MaxPoolSize = maxConfig;
            if (csb.MinPoolSize < 1) csb.MinPoolSize = 1;
            if (csb.MinPoolSize > csb.MaxPoolSize) csb.MinPoolSize = csb.MaxPoolSize;
            // Detecta conexão morta (proxy/firewall do provedor derruba conexões ociosas).
            if (csb.KeepAlive <= 0) csb.KeepAlive = 30;
            // Abrir conexão não deve esperar 5 minutos: falha rápido e a próxima tentativa segue.
            if (csb.Timeout > 30) csb.Timeout = 15;
            return csb.ConnectionString;
        }
    }
}