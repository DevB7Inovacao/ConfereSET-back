using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class AtividadeRecenteRepository : GenericRepository<AtividadeRecente>, IAtividadeRecenteRepository
    {
        public AtividadeRecenteRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<PagedResult<AtividadeRecente>> GetPagedByOperadorId(int operadorId, FiltersAtividadeRecenteDTO filters)
        {
            var query = _dbContext.Set<AtividadeRecente>()
                .Include(x => x.Obra)
                .Include(x => x.Operador)
                .Where(x => x.OperadorId == operadorId)
                .OrderByDescending(x => x.CreatedDate)
                .AsQueryable();

            return await query.GetPagedAsync<AtividadeRecente>(filters.PageNumber, filters.PageSize);
        }

        public async Task<PagedResult<AtividadeRecente>> GetPagedByEmpresaId(int empresaId, FiltersAtividadeRecenteDTO filters)
        {
            var query = _dbContext.Set<AtividadeRecente>()
                .Include(x => x.Obra)
                .Include(x => x.Operador)
                .Where(x => x.Obra != null && x.Obra.EmpresaId == empresaId)
                .OrderByDescending(x => x.CreatedDate)
                .AsQueryable();

            return await query.GetPagedAsync<AtividadeRecente>(filters.PageNumber, filters.PageSize);
        }
    }

    public interface IAtividadeRecenteRepository : IGenericRepository<AtividadeRecente>
    {
        Task<PagedResult<AtividadeRecente>> GetPagedByOperadorId(int operadorId, FiltersAtividadeRecenteDTO filters);
        Task<PagedResult<AtividadeRecente>> GetPagedByEmpresaId(int empresaId, FiltersAtividadeRecenteDTO filters);
    }
}