using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class TiposOcorrenciaRepository : GenericRepository<TiposOcorrencia>, ITiposOcorrenciaRepository
    {
        public TiposOcorrenciaRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<TiposOcorrencia> GetTipoById(int id)
        {
            return await _dbContext.Set<TiposOcorrencia>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<TiposOcorrencia>> GetAllPaged(FiltersTiposOcorrenciaDTO filtersDTO)
        {
            var query = _dbContext.Set<TiposOcorrencia>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtersDTO.Search))
            {
                var s = filtersDTO.Search.ToLower();
                query = query.Where(x =>
                    EF.Functions.Like((x.Nome ?? string.Empty).ToLower(), $"%{s}%") ||
                    EF.Functions.Like((x.Descricao ?? string.Empty).ToLower(), $"%{s}%")
                );
            }

            if (filtersDTO.Status.HasValue)
            {
                query = query.Where(x => x.Status == filtersDTO.Status.Value);
            }

            if (filtersDTO.Gravidade.HasValue)
            {
                query = query.Where(x => x.Gravidade == filtersDTO.Gravidade.Value);
            }

            query = query.OrderBy(x => x.Nome);

            return await query.GetPagedAsync<TiposOcorrencia>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }

        public async Task<List<TipoOcorrenciaSimpleDTO>> GetSimple()
        {
            return await _dbContext.Set<TiposOcorrencia>()
                .OrderBy(x => x.Nome)
                .Select(x => new TipoOcorrenciaSimpleDTO
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = x.Descricao
                })
                .ToListAsync();
        }
    }

    public interface ITiposOcorrenciaRepository : IGenericRepository<TiposOcorrencia>
    {
        Task<TiposOcorrencia> GetTipoById(int id);
        Task<PagedResult<TiposOcorrencia>> GetAllPaged(FiltersTiposOcorrenciaDTO filtersDTO);
        Task<List<TipoOcorrenciaSimpleDTO>> GetSimple();
    }
}