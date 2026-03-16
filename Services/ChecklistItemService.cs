using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class ChecklistItemService : IChecklistItemService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChecklistItemService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChecklistItemDTO> Create(CreateChecklistItemRequest req)
        {
            var checklist = await _unitOfWork.Checklists.GetById(req.ChecklistId);
            if (checklist == null) throw new Exception("Checklist não encontrado.");
            if (checklist.EmpresaId != req.EmpresaId) throw new Exception("Checklist não pertence à empresa informada.");

            var model = new ChecklistItem
            {
                EmpresaId = req.EmpresaId,
                ChecklistId = req.ChecklistId,
                Descricao = req.Descricao.Trim(),
                Ordem = req.Ordem,
                Status = 1
            };

            await _unitOfWork.ChecklistItems.Add(model);
            _unitOfWork.Save();

            return MapToDTO(model);
        }

        public async Task<List<ChecklistItemDTO>> GetByChecklist(int checklistId)
        {
            var itens = await _unitOfWork.ChecklistItems.GetByChecklist(checklistId);
            return itens.Select(MapToDTO).ToList();
        }

        public async Task<ChecklistItemDTO?> GetById(int id)
        {
            var item = await _unitOfWork.ChecklistItems.GetById(id);
            return item == null ? null : MapToDTO(item);
        }

        public async Task<bool> Update(int id, UpdateChecklistItemRequest req)
        {
            var existing = await _unitOfWork.ChecklistItems.GetById(id);
            if (existing == null) throw new Exception("Item não encontrado.");

            if (!string.IsNullOrWhiteSpace(req.Descricao))
                existing.Descricao = req.Descricao.Trim();

            if (req.Ordem.HasValue)
                existing.Ordem = req.Ordem.Value;

            if (req.Status.HasValue)
                existing.Status = req.Status.Value;

            _unitOfWork.ChecklistItems.Update(existing);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var existing = await _unitOfWork.ChecklistItems.GetById(id);
            if (existing == null) throw new Exception("Item não encontrado.");

            _unitOfWork.ChecklistItems.Delete(existing);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> ToggleStatus(int id)
        {
            var existing = await _unitOfWork.ChecklistItems.GetById(id);
            if (existing == null) throw new Exception("Item não encontrado.");

            existing.Status = existing.Status == 1 ? 0 : 1;
            _unitOfWork.ChecklistItems.Update(existing);
            return _unitOfWork.Save() > 0;
        }

        private static ChecklistItemDTO MapToDTO(ChecklistItem x) => new()
        {
            Id = x.Id,
            EmpresaId = x.EmpresaId,
            ChecklistId = x.ChecklistId,
            Descricao = x.Descricao,
            Ordem = x.Ordem,
            Status = x.Status
        };
    }

    public interface IChecklistItemService
    {
        Task<ChecklistItemDTO> Create(CreateChecklistItemRequest req);
        Task<List<ChecklistItemDTO>> GetByChecklist(int checklistId);
        Task<ChecklistItemDTO?> GetById(int id);
        Task<bool> Update(int id, UpdateChecklistItemRequest req);
        Task<bool> Delete(int id);
        Task<bool> ToggleStatus(int id);
    }
}