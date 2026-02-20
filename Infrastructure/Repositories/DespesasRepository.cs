using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class DespesasRepository : GenericRepository<Despesas>, IDespesasRepository
    {
        public DespesasRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<Despesas> GetDespesaById(int id)
        {
            return await _dbContext.Set<Despesas>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<Despesas>> GetAllDespesasPaged(FiltersDespesasDTO filtersDTO)
        {
            var query = _dbContext.Set<Despesas>().AsQueryable();

            if (!string.IsNullOrEmpty(filtersDTO.Name))
                query = query.Where(x => EF.Functions.Like(x.Name.ToLower(), $"%{filtersDTO.Name.ToLower()}%"));

            if (filtersDTO.Status.HasValue)
                query = query.Where(x => x.Status == filtersDTO.Status.Value);

            if (filtersDTO.ObraId.HasValue && filtersDTO.ObraId.Value > 0)
                query = query.Where(x => x.ObraId == filtersDTO.ObraId.Value);

            if (!string.IsNullOrEmpty(filtersDTO.Category))
                query = query.Where(x => x.Category != null && EF.Functions.Like(x.Category.ToLower(), $"%{filtersDTO.Category.ToLower()}%"));

            if (filtersDTO.DateFrom.HasValue)
                query = query.Where(x => x.Date >= filtersDTO.DateFrom.Value);

            if (filtersDTO.DateTo.HasValue)
                query = query.Where(x => x.Date <= filtersDTO.DateTo.Value);

            query = query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id);

            return await query.GetPagedAsync<Despesas>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }

        public async Task<List<DespesaSimpleDTO>> GetDespesasSimple(int? obraId)
        {
            var query = _dbContext.Set<Despesas>().AsQueryable();

            if (obraId.HasValue && obraId.Value > 0)
                query = query.Where(x => x.ObraId == obraId.Value);

            return await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Select(x => new DespesaSimpleDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Amount = x.Amount,
                    Date = x.Date,
                    ObraId = x.ObraId
                })
                .ToListAsync();
        }

        public async Task<List<Despesas>> GetDespesasParaRelatorio(FiltrosRelatorioDTO filtros)
        {
            var query = _dbContext.Set<Despesas>().AsQueryable();

            if (filtros.ObraId.HasValue && filtros.ObraId.Value > 0)
                query = query.Where(x => x.ObraId == filtros.ObraId.Value);

            if (filtros.Status.HasValue)
                query = query.Where(x => x.Status == filtros.Status.Value);

            if (!string.IsNullOrEmpty(filtros.Categoria))
                query = query.Where(x => x.Category != null && EF.Functions.Like(x.Category.ToLower(), $"%{filtros.Categoria.ToLower()}%"));

            if (filtros.DataInicio.HasValue)
                query = query.Where(x => x.Date >= filtros.DataInicio.Value);

            if (filtros.DataFim.HasValue)
                query = query.Where(x => x.Date <= filtros.DataFim.Value);

            return await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }
    }

    public interface IDespesasRepository : IGenericRepository<Despesas>
    {
        public Task<Despesas> GetDespesaById(int id);
        public Task<PagedResult<Despesas>> GetAllDespesasPaged(FiltersDespesasDTO filtersDTO);
        public Task<List<DespesaSimpleDTO>> GetDespesasSimple(int? obraId);
        public Task<List<Despesas>> GetDespesasParaRelatorio(FiltrosRelatorioDTO filtros);
    }
}