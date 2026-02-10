using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class SupportTicketsService : ISupportTicketsService
    {
        public IUnitOfWork _unitOfWork;

        public SupportTicketsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SupportTicket> Create(SupportTicket ticket)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));
            await _unitOfWork.SupportTickets.Add(ticket);
            _unitOfWork.Save();
            return ticket;
        }

        public async Task<bool> Update(SupportTicket ticket, int id)
        {
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var ticket = await _unitOfWork.SupportTickets.GetById(id);
            if (ticket == null) throw new Exception("Chamado não encontrado.");

            _unitOfWork.SupportTickets.Delete(ticket);
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> ToggleStatus(int id)
        {
            var ticket = await _unitOfWork.SupportTickets.GetById(id);
            if (ticket == null) throw new Exception("Chamado não encontrado.");

            ticket.Status = ticket.Status == 1 ? 0 : 1;
            _unitOfWork.SupportTickets.Update(ticket);
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<SupportTicket?> GetById(int id)
        {
            return await _unitOfWork.SupportTickets.GetById(id);
        }

        public async Task<SupportTicketPagedDTO> GetPaged(FiltersSupportTicketsDTO filtersDTO)
        {
            var paged = await _unitOfWork.SupportTickets.GetAllPaged(filtersDTO);

            var dto = paged.Results.Select(x => new SupportTicketDTO
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
            }).ToList();

            return new SupportTicketPagedDTO { Result = dto, PageCount = paged.PageCount };
        }

        public async Task<List<SupportTicketSimpleDTO>> GetSimple(int empresaId)
        {
            return await _unitOfWork.SupportTickets.GetSimple(empresaId);
        }
    }

    public interface ISupportTicketsService
    {
        Task<SupportTicket> Create(SupportTicket ticket);
        Task<bool> Update(SupportTicket ticket, int id);
        Task<bool> Delete(int id);
        Task<bool> ToggleStatus(int id);
        Task<SupportTicket?> GetById(int id);
        Task<SupportTicketPagedDTO> GetPaged(FiltersSupportTicketsDTO filtersDTO);
        Task<List<SupportTicketSimpleDTO>> GetSimple(int empresaId);
    }
}