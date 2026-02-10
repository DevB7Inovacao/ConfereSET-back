using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class SupportTicketsRepository : GenericRepository<SupportTicket>, ISupportTicketsRepository
    {
        public SupportTicketsRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<SupportTicket?> GetById(int id)
        {
            return await _dbContext.Set<SupportTicket>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<SupportTicket>> GetAllPaged(FiltersSupportTicketsDTO filtersDTO)
        {
            var query = _dbContext.Set<SupportTicket>().AsQueryable();

            if (filtersDTO.EmpresaId.HasValue)
                query = query.Where(x => x.EmpresaId == filtersDTO.EmpresaId.Value);

            if (filtersDTO.Subject.HasValue)
                query = query.Where(x => x.Subject == filtersDTO.Subject.Value);

            if (filtersDTO.Status.HasValue)
                query = query.Where(x => x.Status == filtersDTO.Status.Value);

            if (!string.IsNullOrWhiteSpace(filtersDTO.Title))
                query = query.Where(x => EF.Functions.Like(x.Title.ToLower(), $"%{filtersDTO.Title.ToLower()}%"));

            if (filtersDTO.CreatedFrom.HasValue)
                query = query.Where(x => x.CreatedDate >= filtersDTO.CreatedFrom.Value);

            if (filtersDTO.CreatedTo.HasValue)
                query = query.Where(x => x.CreatedDate <= filtersDTO.CreatedTo.Value);

            query = query.OrderByDescending(x => x.CreatedDate);

            return await query.GetPagedAsync<SupportTicket>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }

        public async Task<List<SupportTicketSimpleDTO>> GetSimple(int empresaId)
        {
            return await _dbContext.Set<SupportTicket>()
                .Where(x => x.EmpresaId == empresaId)
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new SupportTicketSimpleDTO
                {
                    Id = x.Id,
                    Title = x.Title
                })
                .ToListAsync();
        }
    }

    public interface ISupportTicketsRepository : IGenericRepository<SupportTicket>
    {
        Task<SupportTicket?> GetById(int id);
        Task<PagedResult<SupportTicket>> GetAllPaged(FiltersSupportTicketsDTO filtersDTO);
        Task<List<SupportTicketSimpleDTO>> GetSimple(int empresaId);
    }
}