using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class ModeloTextoService : IModeloTextoService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ModeloTextoService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ModeloTexto> Create(CreateModeloTextoRequest req)
        {
            if (await _unitOfWork.ModeloTextos.ExistsByNome(req.EmpresaId, req.Nome))
                throw new Exception("Já existe um modelo de texto com esse nome.");

            var model = new ModeloTexto
            {
                EmpresaId = req.EmpresaId,
                Nome = req.Nome.Trim(),
                Texto = req.Texto,
                Status = 1
            };

            await _unitOfWork.ModeloTextos.Add(model);
            _unitOfWork.Save();

            return model;
        }

        public async Task<ModeloTexto?> GetById(int id)
        {
            return await _unitOfWork.ModeloTextos.GetById(id);
        }

        public async Task<ModeloTextoPagedDTO> GetPaged(FiltersModeloTextoDTO filters)
        {
            var paged = await _unitOfWork.ModeloTextos.GetPaged(filters);

            var dto = paged.Results.Select(x => new ModeloTextoDTO
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Nome = x.Nome,
                Texto = x.Texto,
                Status = x.Status
            }).ToList();

            return new ModeloTextoPagedDTO
            {
                PageCount = paged.PageCount,
                Result = dto
            };
        }

        public async Task<bool> Update(int id, UpdateModeloTextoRequest req)
        {
            var existing = await _unitOfWork.ModeloTextos.GetById(id);
            if (existing == null) throw new Exception("Modelo não encontrado.");

            if (!string.IsNullOrWhiteSpace(req.Nome))
            {
                var newNome = req.Nome.Trim();
                if (await _unitOfWork.ModeloTextos.ExistsByNome(existing.EmpresaId, newNome, ignoreId: id))
                    throw new Exception("Já existe um modelo de texto com esse nome.");

                existing.Nome = newNome;
            }

            if (!string.IsNullOrWhiteSpace(req.Texto))
                existing.Texto = req.Texto;

            if (req.Status.HasValue)
                existing.Status = req.Status.Value;

            _unitOfWork.ModeloTextos.Update(existing);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var existing = await _unitOfWork.ModeloTextos.GetById(id);
            if (existing == null) throw new Exception("Modelo não encontrado.");

            _unitOfWork.ModeloTextos.Delete(existing);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> ToggleStatus(int id)
        {
            var existing = await _unitOfWork.ModeloTextos.GetById(id);
            if (existing == null) throw new Exception("Modelo não encontrado.");

            existing.Status = existing.Status == 1 ? 0 : 1;
            _unitOfWork.ModeloTextos.Update(existing);
            return _unitOfWork.Save() > 0;
        }
    }

    public interface IModeloTextoService
    {
        Task<ModeloTexto> Create(CreateModeloTextoRequest req);
        Task<ModeloTexto?> GetById(int id);
        Task<ModeloTextoPagedDTO> GetPaged(FiltersModeloTextoDTO filters);
        Task<bool> Update(int id, UpdateModeloTextoRequest req);
        Task<bool> Delete(int id);
        Task<bool> ToggleStatus(int id);
    }
}