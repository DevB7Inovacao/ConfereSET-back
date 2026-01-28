using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class ModeloTextoVariavelService : IModeloTextoVariavelService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ModeloTextoVariavelService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ModeloTextoVariavel> Create(CreateModeloTextoVariavelRequest req)
        {
            if (await _unitOfWork.ModeloTextoVariaveis.ExistsByNome(req.EmpresaId, req.Nome))
                throw new Exception("Já existe uma variável com esse token.");

            // validação simples do token {{...}}
            var token = req.Nome.Trim();
            if (!token.StartsWith("{{") || !token.EndsWith("}}"))
                throw new Exception("O campo Nome deve estar no formato {{TOKEN}}.");

            var v = new ModeloTextoVariavel
            {
                EmpresaId = req.EmpresaId,
                Nome = token,
                NomeAmigavel = req.NomeAmigavel.Trim(),
                NomePropriedade = req.NomePropriedade.Trim(),
                Categoria = req.Categoria,
                Classe = string.IsNullOrWhiteSpace(req.Classe) ? null : req.Classe.Trim(),
                Valor = req.Valor,
                Status = 1
            };

            await _unitOfWork.ModeloTextoVariaveis.Add(v);
            _unitOfWork.Save();

            return v;
        }

        public async Task<ModeloTextoVariavel?> GetById(int id)
        {
            return await _unitOfWork.ModeloTextoVariaveis.GetById(id);
        }

        public async Task<ModeloTextoVariavelPagedDTO> GetPaged(FiltersModeloTextoVariavelDTO filters)
        {
            var paged = await _unitOfWork.ModeloTextoVariaveis.GetPaged(filters);

            var dto = paged.Results.Select(x => new ModeloTextoVariavelDTO
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Nome = x.Nome,
                NomeAmigavel = x.NomeAmigavel,
                NomePropriedade = x.NomePropriedade,
                Categoria = x.Categoria,
                Classe = x.Classe,
                Valor = x.Valor,
                Status = x.Status
            }).ToList();

            return new ModeloTextoVariavelPagedDTO
            {
                PageCount = paged.PageCount,
                Result = dto
            };
        }

        public async Task<bool> Update(int id, UpdateModeloTextoVariavelRequest req)
        {
            var existing = await _unitOfWork.ModeloTextoVariaveis.GetById(id);
            if (existing == null) throw new Exception("Variável não encontrada.");

            if (!string.IsNullOrWhiteSpace(req.Nome))
            {
                var token = req.Nome.Trim();
                if (!token.StartsWith("{{") || !token.EndsWith("}}"))
                    throw new Exception("O campo Nome deve estar no formato {{TOKEN}}.");

                if (await _unitOfWork.ModeloTextoVariaveis.ExistsByNome(existing.EmpresaId, token, ignoreId: id))
                    throw new Exception("Já existe uma variável com esse token.");

                existing.Nome = token;
            }

            if (!string.IsNullOrWhiteSpace(req.NomeAmigavel))
                existing.NomeAmigavel = req.NomeAmigavel.Trim();

            if (!string.IsNullOrWhiteSpace(req.NomePropriedade))
                existing.NomePropriedade = req.NomePropriedade.Trim();

            if (req.Categoria.HasValue)
                existing.Categoria = req.Categoria.Value;

            if (req.Classe != null)
                existing.Classe = string.IsNullOrWhiteSpace(req.Classe) ? null : req.Classe.Trim();

            if (req.Valor != null)
                existing.Valor = req.Valor;

            if (req.Status.HasValue)
                existing.Status = req.Status.Value;

            _unitOfWork.ModeloTextoVariaveis.Update(existing);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var existing = await _unitOfWork.ModeloTextoVariaveis.GetById(id);
            if (existing == null) throw new Exception("Variável não encontrada.");

            _unitOfWork.ModeloTextoVariaveis.Delete(existing);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> ToggleStatus(int id)
        {
            var existing = await _unitOfWork.ModeloTextoVariaveis.GetById(id);
            if (existing == null) throw new Exception("Variável não encontrada.");

            existing.Status = existing.Status == 1 ? 0 : 1;
            _unitOfWork.ModeloTextoVariaveis.Update(existing);
            return _unitOfWork.Save() > 0;
        }
    }

    public interface IModeloTextoVariavelService
    {
        Task<ModeloTextoVariavel> Create(CreateModeloTextoVariavelRequest req);
        Task<ModeloTextoVariavel?> GetById(int id);
        Task<ModeloTextoVariavelPagedDTO> GetPaged(FiltersModeloTextoVariavelDTO filters);
        Task<bool> Update(int id, UpdateModeloTextoVariavelRequest req);
        Task<bool> Delete(int id);
        Task<bool> ToggleStatus(int id);
    }
}