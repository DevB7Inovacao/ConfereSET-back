using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class ModeloTextoVariavelRepository : GenericRepository<ModeloTextoVariavel>, IModeloTextoVariavelRepository
    {
        public ModeloTextoVariavelRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<ModeloTextoVariavel?> GetById(int id)
        {
            return await _dbContext.Set<ModeloTextoVariavel>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsByNome(int empresaId, string nome, int? ignoreId = null)
        {
            var q = _dbContext.Set<ModeloTextoVariavel>().AsQueryable()
                .Where(x => x.EmpresaId == empresaId)
                .Where(x => x.Nome.ToLower() == nome.ToLower());

            if (ignoreId.HasValue)
                q = q.Where(x => x.Id != ignoreId.Value);

            return await q.AnyAsync();
        }

        public async Task<PagedResult<ModeloTextoVariavel>> GetPaged(FiltersModeloTextoVariavelDTO filters)
        {
            var q = _dbContext.Set<ModeloTextoVariavel>().AsQueryable();

            if (filters.EmpresaId.HasValue)
                q = q.Where(x => x.EmpresaId == filters.EmpresaId.Value);

            if (filters.Status.HasValue)
                q = q.Where(x => x.Status == filters.Status.Value);

            if (filters.Categoria.HasValue)
                q = q.Where(x => x.Categoria == filters.Categoria.Value);

            if (!string.IsNullOrWhiteSpace(filters.Nome))
                q = q.Where(x => EF.Functions.Like(x.Nome.ToLower(), $"%{filters.Nome.ToLower()}%"));

            if (!string.IsNullOrWhiteSpace(filters.NomeAmigavel))
                q = q.Where(x => EF.Functions.Like(x.NomeAmigavel.ToLower(), $"%{filters.NomeAmigavel.ToLower()}%"));

            if (!string.IsNullOrWhiteSpace(filters.Classe))
                q = q.Where(x => x.Classe != null && EF.Functions.Like(x.Classe.ToLower(), $"%{filters.Classe.ToLower()}%"));

            q = q.OrderBy(x => x.Categoria).ThenBy(x => x.NomeAmigavel);

            return await q.GetPagedAsync<ModeloTextoVariavel>(filters.pageNumber, filters.pageSize);
        }
    }

    public interface IModeloTextoVariavelRepository : IGenericRepository<ModeloTextoVariavel>
    {
        Task<ModeloTextoVariavel?> GetById(int id);
        Task<bool> ExistsByNome(int empresaId, string nome, int? ignoreId = null);
        Task<PagedResult<ModeloTextoVariavel>> GetPaged(FiltersModeloTextoVariavelDTO filters);
    }
}