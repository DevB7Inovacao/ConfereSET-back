using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class ChecklistService : IChecklistService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChecklistService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Checklist> Create(CreateChecklistRequest req)
        {
            if (await _unitOfWork.Checklists.ExistsByNome(req.EmpresaId, req.Nome))
                throw new Exception("Já existe um checklist com esse nome.");

            var model = new Checklist
            {
                EmpresaId = req.EmpresaId,
                Nome = req.Nome.Trim(),
                Status = 1
            };

            await _unitOfWork.Checklists.Add(model);
            _unitOfWork.Save();

            return model;
        }

        public async Task<Checklist?> GetById(int id)
        {
            return await _unitOfWork.Checklists.GetById(id);
        }

        public async Task<ChecklistPagedDTO> GetPaged(FiltersChecklistDTO filters)
        {
            var paged = await _unitOfWork.Checklists.GetPaged(filters);

            var dto = paged.Results.Select(x => new ChecklistDTO
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Nome = x.Nome,
                Status = x.Status
            }).ToList();

            return new ChecklistPagedDTO
            {
                PageCount = paged.PageCount,
                Result = dto
            };
        }

        public async Task<bool> Update(int id, UpdateChecklistRequest req)
        {
            var existing = await _unitOfWork.Checklists.GetById(id);
            if (existing == null) throw new Exception("Checklist não encontrado.");

            if (!string.IsNullOrWhiteSpace(req.Nome))
            {
                var newNome = req.Nome.Trim();
                if (await _unitOfWork.Checklists.ExistsByNome(existing.EmpresaId, newNome, ignoreId: id))
                    throw new Exception("Já existe um checklist com esse nome.");

                existing.Nome = newNome;
            }

            if (req.Status.HasValue)
                existing.Status = req.Status.Value;

            _unitOfWork.Checklists.Update(existing);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var existing = await _unitOfWork.Checklists.GetById(id);
            if (existing == null) throw new Exception("Checklist não encontrado.");

            _unitOfWork.Checklists.Delete(existing);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> ToggleStatus(int id)
        {
            var existing = await _unitOfWork.Checklists.GetById(id);
            if (existing == null) throw new Exception("Checklist não encontrado.");

            existing.Status = existing.Status == 1 ? 0 : 1;
            _unitOfWork.Checklists.Update(existing);
            return _unitOfWork.Save() > 0;
        }
    }

    public interface IChecklistService
    {
        Task<Checklist> Create(CreateChecklistRequest req);
        Task<Checklist?> GetById(int id);
        Task<ChecklistPagedDTO> GetPaged(FiltersChecklistDTO filters);
        Task<bool> Update(int id, UpdateChecklistRequest req);
        Task<bool> Delete(int id);
        Task<bool> ToggleStatus(int id);
    }
}