using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PlanoRepository : GenericRepository<Plano>, IPlanoRepository
    {
        public PlanoRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<Plano?> GetPlanoById(int id)
        {
            return await _dbContext.Set<Plano>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Plano>> GetAllAtivos()
        {
            return await _dbContext.Set<Plano>()
                .AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.Valor)
                .ToListAsync();
        }

        public async Task<List<Plano>> GetAll(int empresaid)
        {
            return await _dbContext.Set<Plano>()
                .AsNoTracking()
                .Where(x=>x.EmpresaId==empresaid)
                .OrderBy(x => x.Valor)
                .ToListAsync();
        }
    }

    public interface IPlanoRepository : IGenericRepository<Plano>
    {
        Task<Plano?> GetPlanoById(int id);
        Task<List<Plano>> GetAllAtivos();
        Task<List<Plano>> GetAll(int empresaid);
    }
}