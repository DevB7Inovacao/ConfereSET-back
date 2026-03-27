using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AssinaturaRepository : GenericRepository<Assinatura>, IAssinaturaRepository
    {
        public AssinaturaRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<Assinatura?> GetAssinaturaById(int id)
        {
            return await _dbContext.Set<Assinatura>()
                .Include(x => x.Empresa)
                .Include(x => x.Plano)
                .Include(x => x.Pagamentos)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Assinatura?> GetAssinaturaAtivaByEmpresaId(int empresaId)
        {
            return await _dbContext.Set<Assinatura>()
                .Include(x => x.Plano)
                .Where(x => x.EmpresaId == empresaId && x.Status == Core.Enums.StatusAssinatura.Ativa)
                .FirstOrDefaultAsync();
        }

        public async Task<Assinatura?> GetByMPSubscriptionId(string mpSubscriptionId)
        {
            return await _dbContext.Set<Assinatura>()
                .Include(x => x.Plano)
                .FirstOrDefaultAsync(x => x.MPSubscriptionId == mpSubscriptionId);
        }

        public async Task<List<Assinatura>> GetAllPaged(int page, int pageSize)
        {
            return await _dbContext.Set<Assinatura>()
                .AsNoTracking()
                .Include(x => x.Empresa)
                .Include(x => x.Plano)
                .OrderByDescending(x => x.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAll()
        {
            return await _dbContext.Set<Assinatura>().CountAsync();
        }
    }

    public interface IAssinaturaRepository : IGenericRepository<Assinatura>
    {
        Task<Assinatura?> GetAssinaturaById(int id);
        Task<Assinatura?> GetAssinaturaAtivaByEmpresaId(int empresaId);
        Task<Assinatura?> GetByMPSubscriptionId(string mpSubscriptionId);
        Task<List<Assinatura>> GetAllPaged(int page, int pageSize);
        Task<int> CountAll();
    }
}