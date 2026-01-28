using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class ModeloTextoRepository : GenericRepository<ModeloTexto>, IModeloTextoRepository
    {
        public ModeloTextoRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<ModeloTexto?> GetById(int id)
        {
            return await _dbContext.Set<ModeloTexto>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsByNome(int empresaId, string nome, int? ignoreId = null)
        {
            var q = _dbContext.Set<ModeloTexto>().AsQueryable()
                .Where(x => x.EmpresaId == empresaId)
                .Where(x => x.Nome.ToLower() == nome.ToLower());

            if (ignoreId.HasValue)
                q = q.Where(x => x.Id != ignoreId.Value);

            return await q.AnyAsync();
        }

        public async Task<PagedResult<ModeloTexto>> GetPaged(FiltersModeloTextoDTO filters)
        {
            var q = _dbContext.Set<ModeloTexto>().AsQueryable();

            if (filters.EmpresaId.HasValue)
                q = q.Where(x => x.EmpresaId == filters.EmpresaId.Value);

            if (filters.Status.HasValue)
                q = q.Where(x => x.Status == filters.Status.Value);

            if (!string.IsNullOrWhiteSpace(filters.Nome))
                q = q.Where(x => EF.Functions.Like(x.Nome.ToLower(), $"%{filters.Nome.ToLower()}%"));

            q = q.OrderByDescending(x => x.Id);

            return await q.GetPagedAsync<ModeloTexto>(filters.pageNumber, filters.pageSize);
        }
    }

    public interface IModeloTextoRepository : IGenericRepository<ModeloTexto>
    {
        Task<ModeloTexto?> GetById(int id);
        Task<bool> ExistsByNome(int empresaId, string nome, int? ignoreId = null);
        Task<PagedResult<ModeloTexto>> GetPaged(FiltersModeloTextoDTO filters);
    }
}