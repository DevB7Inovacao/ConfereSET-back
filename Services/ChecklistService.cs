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
                Texto = req.Texto,
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
                Texto = x.Texto,
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

            if (!string.IsNullOrWhiteSpace(req.Texto))
                existing.Texto = req.Texto;

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

        public async Task<SyncChecklistVariavelResponse> Sync(SyncChecklistVariavelRequest req)
        {
            if (req.EmpresaId <= 0) throw new Exception("EmpresaId inválido.");
            if (req.ChecklistId <= 0) throw new Exception("ChecklistId inválido.");

            var checklist = await _unitOfWork.Checklists.GetById(req.ChecklistId);
            if (checklist == null) throw new Exception("Checklist não encontrado.");
            if (checklist.EmpresaId != req.EmpresaId) throw new Exception("Checklist não pertence à empresa informada.");

            var tokens = (req.Tokens ?? new List<string>())
                .Select(t => (t ?? "").Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var t in tokens)
            {
                if (!t.StartsWith("{{") || !t.EndsWith("}}"))
                    throw new Exception($"Token inválido: {t}. Use formato {{'{{TOKEN}}'}}.");
            }

            int createdVars = 0;
            int createdLinks = 0;
            int disabledLinks = 0;
            int enabledLinks = 0;

            var existingVarsPaged = await _unitOfWork.ModeloTextoVariaveis.GetPaged(new FiltersModeloTextoVariavelDTO
            {
                EmpresaId = req.EmpresaId,
                pageNumber = 1,
                pageSize = 5000
            });

            var existingVars = existingVarsPaged.Results.ToList();
            var varsByToken = existingVars.ToDictionary(x => x.Nome, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var token in tokens)
            {
                if (!varsByToken.ContainsKey(token))
                {
                    var inner = token.Substring(2, token.Length - 4).Trim();

                    var v = new ModeloTextoVariavel
                    {
                        EmpresaId = req.EmpresaId,
                        Nome = token,
                        NomeAmigavel = inner.Replace(".", " ").Replace("_", " ").ToUpperInvariant(),
                        NomePropriedade = inner,
                        Categoria = 0,
                        Classe = null,
                        Valor = null,
                        Status = 1
                    };

                    await _unitOfWork.ModeloTextoVariaveis.Add(v);
                    _unitOfWork.Save();

                    createdVars++;
                    varsByToken[token] = v;
                }
            }

            var links = await _unitOfWork.ChecklistsVariavel.GetLinksOnly(req.EmpresaId, req.ChecklistId);
            var linkByVarId = links.ToDictionary(x => x.ModeloTextoVariavelId, x => x);
            var tokenVarIds = tokens.Select(t => varsByToken[t].Id).Distinct().ToHashSet();

            foreach (var varId in tokenVarIds)
            {
                if (!linkByVarId.TryGetValue(varId, out var link))
                {
                    var nl = new ChecklistVariavel
                    {
                        EmpresaId = req.EmpresaId,
                        ChecklistId = req.ChecklistId,
                        ModeloTextoVariavelId = varId,
                        Status = 1
                    };
                    await _unitOfWork.ChecklistsVariavel.Add(nl);
                    _unitOfWork.Save();
                    createdLinks++;
                }
                else
                {
                    if (link.Status != 1)
                    {
                        link.Status = 1;
                        _unitOfWork.ChecklistsVariavel.Update(link);
                        _unitOfWork.Save();
                        enabledLinks++;
                    }
                }
            }

            foreach (var link in links)
            {
                if (link.Status == 1 && !tokenVarIds.Contains(link.ModeloTextoVariavelId))
                {
                    link.Status = 0;
                    _unitOfWork.ChecklistsVariavel.Update(link);
                    _unitOfWork.Save();
                    disabledLinks++;
                }
            }

            return new SyncChecklistVariavelResponse
            {
                ChecklistId = req.ChecklistId,
                CreatedVariables = createdVars,
                CreatedLinks = createdLinks,
                DisabledLinks = disabledLinks,
                EnabledLinks = enabledLinks,
                Tokens = tokens
            };
        }

        public async Task<List<ChecklistVariavelByChecklistDTO>> GetVariaveisByChecklist(int empresaId, int checklistId, bool onlyActiveLinks = true)
        {
            var links = await _unitOfWork.ChecklistsVariavel.GetByChecklist(empresaId, checklistId);

            if (onlyActiveLinks)
                links = links.Where(x => x.Status == 1).ToList();

            return links
                .Where(x => x.ModeloTextoVariavel != null)
                .Select(x => new ChecklistVariavelByChecklistDTO
                {
                    Id = x.ModeloTextoVariavel!.Id,
                    EmpresaId = x.EmpresaId,
                    Nome = x.ModeloTextoVariavel!.Nome,
                    NomeAmigavel = x.ModeloTextoVariavel!.NomeAmigavel,
                    NomePropriedade = x.ModeloTextoVariavel!.NomePropriedade,
                    Categoria = x.ModeloTextoVariavel!.Categoria,
                    Classe = x.ModeloTextoVariavel!.Classe,
                    Valor = x.ModeloTextoVariavel!.Valor,
                    Status = x.ModeloTextoVariavel!.Status,
                    VinculoId = x.Id,
                    VinculoStatus = x.Status,
                    ChecklistId = x.ChecklistId
                })
                .ToList();
        }

        public async Task<RenderChecklistResponse> Render(int empresaId, int checklistId, RenderChecklistRequest req)
        {
            var checklist = await _unitOfWork.Checklists.GetById(checklistId);
            if (checklist == null) throw new Exception("Checklist não encontrado.");
            if (checklist.EmpresaId != empresaId) throw new Exception("Checklist não pertence à empresa informada.");

            var html = checklist.Texto ?? "";
            var values = req.Values ?? new Dictionary<string, string?>();

            foreach (var kv in values)
            {
                var token = (kv.Key ?? "").Trim();
                if (string.IsNullOrWhiteSpace(token)) continue;
                var val = kv.Value ?? "";

                html = html.Replace(token, val, StringComparison.OrdinalIgnoreCase);
            }

            return new RenderChecklistResponse
            {
                ChecklistId = checklistId,
                Html = html
            };
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
        Task<SyncChecklistVariavelResponse> Sync(SyncChecklistVariavelRequest req);
        Task<List<ChecklistVariavelByChecklistDTO>> GetVariaveisByChecklist(int empresaId, int checklistId, bool onlyActiveLinks = true);
        Task<RenderChecklistResponse> Render(int empresaId, int checklistId, RenderChecklistRequest req);
    }
}