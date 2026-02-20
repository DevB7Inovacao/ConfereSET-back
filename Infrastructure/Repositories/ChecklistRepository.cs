using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class ChecklistRepository : GenericRepository<Checklist>, IChecklistRepository
    {
        public ChecklistRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<Checklist?> GetById(int id)
        {
            return await _dbContext.Set<Checklist>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsByNome(int empresaId, string nome, int? ignoreId = null)
        {
            var q = _dbContext.Set<Checklist>().AsQueryable()
                .Where(x => x.EmpresaId == empresaId)
                .Where(x => x.Nome.ToLower() == nome.ToLower());

            if (ignoreId.HasValue)
                q = q.Where(x => x.Id != ignoreId.Value);

            return await q.AnyAsync();
        }

        public async Task<PagedResult<Checklist>> GetPaged(FiltersChecklistDTO filters)
        {
            var q = _dbContext.Set<Checklist>().AsQueryable();

            if (filters.EmpresaId.HasValue)
                q = q.Where(x => x.EmpresaId == filters.EmpresaId.Value);

            if (filters.Status.HasValue)
                q = q.Where(x => x.Status == filters.Status.Value);

            if (!string.IsNullOrWhiteSpace(filters.Nome))
                q = q.Where(x => EF.Functions.Like(x.Nome.ToLower(), $"%{filters.Nome.ToLower()}%"));

            q = q.OrderByDescending(x => x.Id);

            return await q.GetPagedAsync<Checklist>(filters.pageNumber, filters.pageSize);
        }
    }

    public interface IChecklistRepository : IGenericRepository<Checklist>
    {
        Task<Checklist?> GetById(int id);
        Task<bool> ExistsByNome(int empresaId, string nome, int? ignoreId = null);
        Task<PagedResult<Checklist>> GetPaged(FiltersChecklistDTO filters);
    }
}