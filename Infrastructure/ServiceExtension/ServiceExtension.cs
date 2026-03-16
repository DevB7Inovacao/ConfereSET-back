using Core.Models;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.ServiceExtension
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddDIServices(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<DbContextClass>(options =>
            {
                options.UseNpgsql(connectionString);
            });
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

            return services;
        }
    }
}