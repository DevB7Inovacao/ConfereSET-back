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
                Status = 1,
                Descricao = TrimOrNull(req.Descricao, MaxDescricao),
                Categoria = TrimOrNull(req.Categoria, MaxCategoria),
            };

            await _unitOfWork.Checklists.Add(model);
            _unitOfWork.Save();

            return model;
        }

        private const int MaxDescricao = 2000;
        private const int MaxCategoria = 120;

        private static string? TrimOrNull(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim();
            return t.Length > max ? t[..max] : t;
        }

        /// <summary>
        /// [v2] Duplica o modelo com todos os itens ATIVOS (mesma ordem e configuração).
        /// Nome padrão "Cópia de X"; se já existir, acrescenta " (2)", " (3)"…
        /// </summary>
        public async Task<ChecklistDTO> Duplicar(int id, string? nome)
        {
            var original = await _unitOfWork.Checklists.GetById(id);
            if (original == null) throw new Exception("Checklist não encontrado.");

            var baseNome = string.IsNullOrWhiteSpace(nome) ? $"Cópia de {original.Nome}" : nome.Trim();
            var novoNome = baseNome;
            for (var n = 2; await _unitOfWork.Checklists.ExistsByNome(original.EmpresaId, novoNome); n++)
            {
                if (n > 100) throw new Exception("Já existe um checklist com esse nome.");
                novoNome = $"{baseNome} ({n})";
            }

            var copia = new Checklist
            {
                EmpresaId = original.EmpresaId,
                Nome = novoNome,
                Status = 1,
                Descricao = original.Descricao,
                Categoria = original.Categoria,
            };
            await _unitOfWork.Checklists.Add(copia);
            _unitOfWork.Save();

            var itens = (await _unitOfWork.ChecklistItems.GetByChecklist(id)).Where(i => i.Status == 1).ToList();
            var ordem = 0;
            foreach (var i in itens)
            {
                await _unitOfWork.ChecklistItems.Add(new ChecklistItem
                {
                    EmpresaId = copia.EmpresaId,
                    ChecklistId = copia.Id,
                    Descricao = i.Descricao,
                    Ordem = ordem++,
                    Status = 1,
                    Tipo = i.Tipo,
                    Grupo = i.Grupo,
                    Obrigatorio = i.Obrigatorio,
                    ExigirFotoNaoConforme = i.ExigirFotoNaoConforme,
                    ExigirObservacaoNaoConforme = i.ExigirObservacaoNaoConforme,
                    Ajuda = i.Ajuda,
                    Opcoes = i.Opcoes,
                });
            }
            if (itens.Count > 0) _unitOfWork.Save();

            return new ChecklistDTO
            {
                Id = copia.Id,
                EmpresaId = copia.EmpresaId,
                Nome = copia.Nome,
                Status = copia.Status,
                Descricao = copia.Descricao,
                Categoria = copia.Categoria,
                TotalItens = itens.Count,
            };
        }

        public async Task<Checklist?> GetById(int id)
        {
            return await _unitOfWork.Checklists.GetById(id);
        }

        public async Task<ChecklistPagedDTO> GetPaged(FiltersChecklistDTO filters)
        {
            var paged = await _unitOfWork.Checklists.GetPaged(filters);
            var totais = await _unitOfWork.ChecklistItems.CountAtivosByChecklistIds(paged.Results.Select(x => x.Id).ToList());

            var dto = paged.Results.Select(x => new ChecklistDTO
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                Nome = x.Nome,
                Status = x.Status,
                Descricao = x.Descricao,
                Categoria = x.Categoria,
                TotalItens = totais.TryGetValue(x.Id, out var total) ? total : 0
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

            // [v2] Ausente no JSON = mantém; presente (null/"") = limpa.
            if (req.DescricaoInformada)
                existing.Descricao = TrimOrNull(req.Descricao, MaxDescricao);
            if (req.CategoriaInformada)
                existing.Categoria = TrimOrNull(req.Categoria, MaxCategoria);

            _unitOfWork.Checklists.Update(existing);
            return _unitOfWork.Save() > 0;
        }

        /// <summary>
        /// Exclui o checklist. Se algum vínculo com obra já tiver respostas, apenas desativa
        /// e retorna uma mensagem; caso contrário remove vínculos e itens antes (FK Restrict em ObraChecklistItem).
        /// </summary>
        public async Task<(bool Ok, string? Mensagem)> Delete(int id)
        {
            var existing = await _unitOfWork.Checklists.GetById(id);
            if (existing == null) throw new Exception("Checklist não encontrado.");

            var obraChecklists = await _unitOfWork.ObraChecklists.GetByChecklistId(id);
            if (obraChecklists.Any(oc => oc.Itens.Any(ChecklistItemService.PossuiResposta)))
            {
                existing.Status = 0;
                _unitOfWork.Checklists.Update(existing);
                _unitOfWork.Save();
                return (true, "Checklist desativado (não excluído) porque já possui respostas em obras.");
            }

            foreach (var oc in obraChecklists)
            {
                foreach (var item in oc.Itens.ToList())
                    _unitOfWork.ObraChecklistItems.Delete(item);
                _unitOfWork.ObraChecklists.Delete(oc);
            }

            var itens = await _unitOfWork.ChecklistItems.GetByChecklist(id);
            foreach (var item in itens)
                _unitOfWork.ChecklistItems.Delete(item);

            _unitOfWork.Checklists.Delete(existing);
            return (_unitOfWork.Save() > 0, null);
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
        Task<(bool Ok, string? Mensagem)> Delete(int id);
        Task<bool> ToggleStatus(int id);
        Task<ChecklistDTO> Duplicar(int id, string? nome);
    }
}