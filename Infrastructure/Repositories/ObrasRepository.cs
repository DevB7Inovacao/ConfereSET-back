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
    public class ObrasRepository : GenericRepository<Obras>, IObrasRepository
    {
        public ObrasRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<Obras> GetObraById(int id)
        {
            return await _dbContext.Set<Obras>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<Obras>> GetAllObrasPaged(FiltersObrasDTO filtersDTO)
        {
            return await _dbContext.Set<Obras>()
           .Where(x => string.IsNullOrEmpty(filtersDTO.Name) || EF.Functions.Like(x.Name.ToLower(), $"%{filtersDTO.Name.ToLower()}%"))
           .GetPagedAsync<Obras>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }

        public async Task<List<ObraSimpleDTO>> GetObrasSimple()
        {
            return await _dbContext.Set<Obras>()
                .OrderBy(x => x.Name)
                .Select(x => new ObraSimpleDTO
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();
        }
    }

    public interface IObrasRepository : IGenericRepository<Obras>
    {
        public Task<Obras> GetObraById(int id);
        public Task<PagedResult<Obras>> GetAllObrasPaged(FiltersObrasDTO filtersDTO);
        public Task<List<ObraSimpleDTO>> GetObrasSimple();
    }
}