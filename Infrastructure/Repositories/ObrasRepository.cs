using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class ObrasRepository : GenericRepository<Obras>, IObrasRepository
    {
        public ObrasRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<Obras?> GetObraById(int id)
        {
            return await _dbContext.Set<Obras>()
                .Include(x => x.Empresa)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<Obras>> GetAllObrasPaged(FiltersObrasDTO filtersDTO)
        {
            var query = _dbContext.Set<Obras>()
                .Include(x => x.Empresa)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filtersDTO.Name))
            {
                query = query.Where(x => EF.Functions.Like(x.Name.ToLower(), $"%{filtersDTO.Name.ToLower()}%"));
            }

            if (filtersDTO.Status.HasValue)
            {
                query = query.Where(x => x.Status == filtersDTO.Status.Value);
            }

            if (filtersDTO.EmpresaId.HasValue)
            {
                query = query.Where(x => x.EmpresaId == filtersDTO.EmpresaId.Value);
            }

            if (filtersDTO.OperadorId.HasValue)
            {
                query = query.Where(o => _dbContext.Set<ObraOperador>()
                    .Any(oo => oo.ObraId == o.Id && oo.OperadorId == filtersDTO.OperadorId.Value));
            }

            return await query.GetPagedAsync<Obras>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }

        public async Task<List<ObraSimpleDTO>> GetObrasSimple()
        {
            return await _dbContext.Set<Obras>()
                .AsNoTracking()
                .Where(x => x.Status == 1)
                .Select(x => new ObraSimpleDTO
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();
        }

        public async Task<List<ObraCardDTO>> GetObrasCardsByEmpresaId(int empresaId)
        {
            var obras = await _dbContext.Set<Obras>()
                .AsNoTracking()
                .Where(x => x.EmpresaId == empresaId)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.City,
                    x.State,
                    x.Status,
                    x.ProgressPercentage,
                    x.StartDate
                })
                .ToListAsync();

            var obraIds = obras.Select(x => x.Id).ToList();

            var operadoresCounts = await _dbContext.Set<ObraOperador>()
                .AsNoTracking()
                .Where(oo => obraIds.Contains(oo.ObraId))
                .GroupBy(oo => oo.ObraId)
                .Select(g => new { ObraId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ObraId, x => x.Count);

            return obras.Select(o => new ObraCardDTO
            {
                Id = o.Id,
                Name = o.Name,
                Location = FormatLocation(o.City, o.State),
                Status = GetStatusText(o.Status),
                ProgressPercentage = o.ProgressPercentage,
                StartDate = o.StartDate,
                OperadoresCount = operadoresCounts.ContainsKey(o.Id) ? operadoresCounts[o.Id] : 0
            }).ToList();
        }

        public async Task<List<ObraCardDTO>> GetObrasCardsByOperadorId(int operadorId)
        {
            var obraIds = await _dbContext.Set<ObraOperador>()
                .AsNoTracking()
                .Where(oo => oo.OperadorId == operadorId)
                .Select(oo => oo.ObraId)
                .ToListAsync();

            var obras = await _dbContext.Set<Obras>()
                .AsNoTracking()
                .Where(x => obraIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.City,
                    x.State,
                    x.Status,
                    x.ProgressPercentage,
                    x.StartDate
                })
                .ToListAsync();

            var operadoresCounts = await _dbContext.Set<ObraOperador>()
                .AsNoTracking()
                .Where(oo => obraIds.Contains(oo.ObraId))
                .GroupBy(oo => oo.ObraId)
                .Select(g => new { ObraId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ObraId, x => x.Count);

            return obras.Select(o => new ObraCardDTO
            {
                Id = o.Id,
                Name = o.Name,
                Location = FormatLocation(o.City, o.State),
                Status = GetStatusText(o.Status),
                ProgressPercentage = o.ProgressPercentage,
                StartDate = o.StartDate,
                OperadoresCount = operadoresCounts.ContainsKey(o.Id) ? operadoresCounts[o.Id] : 0
            }).ToList();
        }

        private string FormatLocation(string? city, string? state)
        {
            if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(state))
                return $"{city}, {state}";
            if (!string.IsNullOrEmpty(city))
                return city;
            if (!string.IsNullOrEmpty(state))
                return state;
            return string.Empty;
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                1 => "Em andamento",
                2 => "Concluída",
                3 => "Pausada",
                _ => "Inativa"
            };
        }
    }

    public interface IObrasRepository : IGenericRepository<Obras>
    {
        Task<Obras?> GetObraById(int id);
        Task<PagedResult<Obras>> GetAllObrasPaged(FiltersObrasDTO filtersDTO);
        Task<List<ObraSimpleDTO>> GetObrasSimple();
        Task<List<ObraCardDTO>> GetObrasCardsByEmpresaId(int empresaId);
        Task<List<ObraCardDTO>> GetObrasCardsByOperadorId(int operadorId);
    }
}