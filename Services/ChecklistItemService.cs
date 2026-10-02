using Core.DTO;
using Core.Enums;
using Core.Models;
using Infrastructure.Repositories;
using System.Text.Json;

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
                Status = 1,
                // [v2] Configuração opcional do item.
                Tipo = NormalizarTipo(req.Tipo),
                Grupo = TrimOrNull(req.Grupo, MaxGrupo),
                Obrigatorio = req.Obrigatorio ?? false,
                ExigirFotoNaoConforme = req.ExigirFotoNaoConforme ?? false,
                ExigirObservacaoNaoConforme = req.ExigirObservacaoNaoConforme ?? false,
                Ajuda = TrimOrNull(req.Ajuda, MaxAjuda),
                Opcoes = NormalizarOpcoes(req.Opcoes),
            };

            await _unitOfWork.ChecklistItems.Add(model);
            _unitOfWork.Save();

            return MapToDTO(model);
        }

        public const int MaxBulkItens = 300;
        private const int MaxGrupo = 120;
        private const int MaxAjuda = 2000;
        private const int MaxOpcoes = 100;
        private const int MaxOpcaoChars = 200;

        /// <summary>
        /// [v2] Cria vários itens de uma vez com Ordem sequencial após o último item do checklist.
        /// Itens sem descrição são ignorados. Máx. 300 por chamada.
        /// </summary>
        public async Task<List<ChecklistItemDTO>> BulkCreate(int empresaId, BulkCreateChecklistItensRequest req)
        {
            if (req?.Itens == null || req.Itens.Count == 0) throw new Exception("Nenhum item enviado.");
            if (req.Itens.Count > MaxBulkItens) throw new Exception($"Envie no máximo {MaxBulkItens} itens por vez.");

            var checklist = await _unitOfWork.Checklists.GetById(req.ChecklistId);
            if (checklist == null || checklist.EmpresaId != empresaId) throw new KeyNotFoundException("Checklist não encontrado.");

            var validos = req.Itens.Where(i => i != null && !string.IsNullOrWhiteSpace(i.Descricao)).ToList();
            if (validos.Count == 0) throw new Exception("Nenhum item com descrição foi enviado.");

            var ordem = await _unitOfWork.ChecklistItems.GetMaxOrdem(req.ChecklistId);
            var criados = new List<ChecklistItem>();
            foreach (var i in validos)
            {
                var model = new ChecklistItem
                {
                    EmpresaId = checklist.EmpresaId,
                    ChecklistId = checklist.Id,
                    Descricao = i.Descricao!.Trim(),
                    Ordem = ++ordem,
                    Status = 1,
                    Tipo = NormalizarTipo(i.Tipo),
                    Grupo = TrimOrNull(i.Grupo, MaxGrupo),
                    Obrigatorio = i.Obrigatorio ?? false,
                    ExigirFotoNaoConforme = i.ExigirFotoNaoConforme ?? false,
                    ExigirObservacaoNaoConforme = i.ExigirObservacaoNaoConforme ?? false,
                    Ajuda = TrimOrNull(i.Ajuda, MaxAjuda),
                    Opcoes = NormalizarOpcoes(i.Opcoes),
                };
                await _unitOfWork.ChecklistItems.Add(model);
                criados.Add(model);
            }

            _unitOfWork.Save();
            return criados.Select(MapToDTO).ToList();
        }

        /// <summary>
        /// [v2] Reordena: Ordem = índice em ItemIds (0..n-1). Itens do checklist que não vierem na
        /// lista vão para o final, mantendo a ordem relativa atual.
        /// </summary>
        public async Task<bool> Reorder(int empresaId, ReorderChecklistItensRequest req)
        {
            if (req?.ItemIds == null || req.ItemIds.Count == 0) throw new Exception("Nenhum item informado.");

            var checklist = await _unitOfWork.Checklists.GetById(req.ChecklistId);
            if (checklist == null || checklist.EmpresaId != empresaId) throw new KeyNotFoundException("Checklist não encontrado.");

            var itens = await _unitOfWork.ChecklistItems.GetByChecklist(req.ChecklistId);
            var porId = itens.ToDictionary(i => i.Id);
            var ids = req.ItemIds.Distinct().ToList();
            if (ids.Any(id => !porId.ContainsKey(id))) throw new Exception("Há itens que não pertencem a este checklist.");

            var listados = ids.ToHashSet();
            var ordem = 0;
            foreach (var id in ids)
                porId[id].Ordem = ordem++;
            foreach (var resto in itens.Where(i => !listados.Contains(i.Id)))
                resto.Ordem = ordem++;

            _unitOfWork.Save();
            return true;
        }

        /// <summary>Tipo válido (1..7); ausente → 1 (legado). Inválido → exceção (400).</summary>
        public static int NormalizarTipo(int? tipo)
        {
            if (!tipo.HasValue) return (int)TipoItemChecklist.ConformeNaoConformeNA;
            if (!Enum.IsDefined(typeof(TipoItemChecklist), tipo.Value)) throw new Exception("Tipo de item inválido.");
            return tipo.Value;
        }

        private static string? TrimOrNull(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim();
            return t.Length > max ? t[..max] : t;
        }

        /// <summary>
        /// Normaliza Opcoes para JSON array de strings (trim, sem vazias/duplicadas, máx. 100).
        /// Tolera texto separado por quebra de linha ou ";" quando não for JSON.
        /// </summary>
        public static string? NormalizarOpcoes(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            List<string> lista;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                lista = doc.RootElement.ValueKind == JsonValueKind.Array
                    ? doc.RootElement.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : e.ToString()).ToList()
                    : new List<string>();
            }
            catch (JsonException)
            {
                lista = raw.Split(new[] { '\n', ';' }).ToList();
            }

            var limpas = lista
                .Select(o => o.Trim())
                .Where(o => o.Length > 0)
                .Select(o => o.Length > MaxOpcaoChars ? o[..MaxOpcaoChars] : o)
                .Distinct()
                .Take(MaxOpcoes)
                .ToList();
            return limpas.Count == 0 ? null : JsonSerializer.Serialize(limpas);
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

            // [v2] Configuração do item (campos ausentes no JSON são mantidos).
            if (req.Tipo.HasValue)
                existing.Tipo = NormalizarTipo(req.Tipo);
            if (req.Obrigatorio.HasValue)
                existing.Obrigatorio = req.Obrigatorio.Value;
            if (req.ExigirFotoNaoConforme.HasValue)
                existing.ExigirFotoNaoConforme = req.ExigirFotoNaoConforme.Value;
            if (req.ExigirObservacaoNaoConforme.HasValue)
                existing.ExigirObservacaoNaoConforme = req.ExigirObservacaoNaoConforme.Value;
            if (req.GrupoInformado)
                existing.Grupo = TrimOrNull(req.Grupo, MaxGrupo);
            if (req.AjudaInformada)
                existing.Ajuda = TrimOrNull(req.Ajuda, MaxAjuda);
            if (req.OpcoesInformadas)
                existing.Opcoes = NormalizarOpcoes(req.Opcoes);

            _unitOfWork.ChecklistItems.Update(existing);
            return _unitOfWork.Save() > 0;
        }

        /// <summary>
        /// Exclui o item. Se já houver respostas em obras (FK Restrict), apenas desativa
        /// e retorna uma mensagem explicando; caso contrário, remove os vínculos vazios antes.
        /// </summary>
        public async Task<(bool Ok, string? Mensagem)> Delete(int id)
        {
            var existing = await _unitOfWork.ChecklistItems.GetById(id);
            if (existing == null) throw new Exception("Item não encontrado.");

            var dependentes = await _unitOfWork.ObraChecklistItems.GetByChecklistItem(id);
            if (dependentes.Any(PossuiResposta))
            {
                existing.Status = 0;
                _unitOfWork.ChecklistItems.Update(existing);
                _unitOfWork.Save();
                return (true, "Item desativado (não excluído) porque já possui respostas em obras.");
            }

            foreach (var dep in dependentes)
                _unitOfWork.ObraChecklistItems.Delete(dep);

            _unitOfWork.ChecklistItems.Delete(existing);
            return (_unitOfWork.Save() > 0, null);
        }

        public static bool PossuiResposta(ObraChecklistItem i) =>
            i.Resposta != 0
            || !string.IsNullOrWhiteSpace(i.Valor)
            || (i.Fotos != null && i.Fotos.Count > 0)
            || !string.IsNullOrWhiteSpace(i.Observacao)
            || !string.IsNullOrWhiteSpace(i.Empresa)
            || !string.IsNullOrWhiteSpace(i.DataHora)
            || !string.IsNullOrWhiteSpace(i.Equipamento)
            || !string.IsNullOrWhiteSpace(i.Marca);

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
            Status = x.Status,
            Tipo = x.Tipo,
            Grupo = x.Grupo,
            Obrigatorio = x.Obrigatorio,
            ExigirFotoNaoConforme = x.ExigirFotoNaoConforme,
            ExigirObservacaoNaoConforme = x.ExigirObservacaoNaoConforme,
            Ajuda = x.Ajuda,
            Opcoes = x.Opcoes
        };
    }

    public interface IChecklistItemService
    {
        Task<ChecklistItemDTO> Create(CreateChecklistItemRequest req);
        Task<List<ChecklistItemDTO>> GetByChecklist(int checklistId);
        Task<ChecklistItemDTO?> GetById(int id);
        Task<bool> Update(int id, UpdateChecklistItemRequest req);
        Task<(bool Ok, string? Mensagem)> Delete(int id);
        Task<bool> ToggleStatus(int id);
        Task<List<ChecklistItemDTO>> BulkCreate(int empresaId, BulkCreateChecklistItensRequest req);
        Task<bool> Reorder(int empresaId, ReorderChecklistItensRequest req);
    }
}