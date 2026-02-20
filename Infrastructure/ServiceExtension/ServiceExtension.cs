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
			services.AddScoped<IChecklistVariavelRepository, ChecklistVariavelRepository>();

            return services;
		}
	}
}
