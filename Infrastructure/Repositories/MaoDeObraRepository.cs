using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class MaoDeObraRepository : GenericRepository<MaoDeObra>, IMaoDeObraRepository
    {
        public MaoDeObraRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<MaoDeObra> GetMaoDeObraById(int id)
        {
            return await _dbContext.Set<MaoDeObra>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<MaoDeObra>> GetAllMaoDeObraPaged(FiltersMaoDeObraDTO filtersDTO)
        {
            var query = _dbContext.Set<MaoDeObra>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtersDTO.Search))
            {
                var s = filtersDTO.Search.ToLower();
                query = query.Where(x =>
                    EF.Functions.Like((x.Funcao ?? string.Empty).ToLower(), $"%{s}%") ||
                    EF.Functions.Like((x.Descricao ?? string.Empty).ToLower(), $"%{s}%")
                );
            }

            if (filtersDTO.Status.HasValue)
            {
                query = query.Where(x => x.Status == filtersDTO.Status.Value);
            }

            query = query.OrderBy(x => x.Funcao);

            return await query.GetPagedAsync<MaoDeObra>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }

        public async Task<List<MaoDeObraSimpleDTO>> GetMaoDeObraSimple()
        {
            return await _dbContext.Set<MaoDeObra>()
                .OrderBy(x => x.Funcao)
                .Select(x => new MaoDeObraSimpleDTO
                {
                    Id = x.Id,
                    Funcao = x.Funcao,
                    Descricao = x.Descricao
                })
                .ToListAsync();
        }
    }

    public interface IMaoDeObraRepository : IGenericRepository<MaoDeObra>
    {
        Task<MaoDeObra> GetMaoDeObraById(int id);
        Task<PagedResult<MaoDeObra>> GetAllMaoDeObraPaged(FiltersMaoDeObraDTO filtersDTO);
        Task<List<MaoDeObraSimpleDTO>> GetMaoDeObraSimple();
    }
}