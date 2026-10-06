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

        public async Task<PagedResult<SupportTicketDTO>> GetAllPaged(FiltersSupportTicketsDTO filtersDTO)
        {
            var query = _dbContext.Set<SupportTicket>().AsNoTracking();

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

            // Projeção: o anexo (bytes) nunca sai do banco na listagem — antes cada chamado
            // trazia o arquivo inteiro só para saber se havia anexo.
            return await query
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new SupportTicketDTO
                {
                    Id = x.Id,
                    EmpresaId = x.EmpresaId,
                    Subject = x.Subject,
                    Title = x.Title,
                    Description = x.Description,
                    Status = x.Status,
                    HasAttachment = x.AttachmentBytes != null && x.AttachmentBytes.Length > 0,
                    AttachmentFileName = x.AttachmentFileName,
                    AttachmentContentType = x.AttachmentContentType,
                    CreatedDate = x.CreatedDate,
                    UpdatedDate = x.UpdatedDate
                })
                .GetPagedAsync(filtersDTO.pageNumber, filtersDTO.pageSize);
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
        Task<PagedResult<SupportTicketDTO>> GetAllPaged(FiltersSupportTicketsDTO filtersDTO);
        Task<List<SupportTicketSimpleDTO>> GetSimple(int empresaId);
    }
}