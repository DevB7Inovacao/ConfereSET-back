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

        public async Task<SyncModeloTextoVariavelResponse> Sync(SyncModeloTextoVariavelRequest req)
        {
            if (req.EmpresaId <= 0) throw new Exception("EmpresaId inválido.");
            if (req.ModeloTextoId <= 0) throw new Exception("ModeloTextoId inválido.");

            var modelo = await _unitOfWork.ModeloTextos.GetById(req.ModeloTextoId);
            if (modelo == null) throw new Exception("ModeloTexto não encontrado.");
            if (modelo.EmpresaId != req.EmpresaId) throw new Exception("ModeloTexto não pertence à empresa informada.");

            // normaliza tokens e valida formato {{TOKEN}}
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

            // 1) garante variáveis no catálogo
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
                    var key = token.Trim().TrimStart('{').TrimEnd('}').Trim(); // bruto
                    // melhor: remove "{{" e "}}"
                    var inner = token.Substring(2, token.Length - 4).Trim();

                    var v = new ModeloTextoVariavel
                    {
                        EmpresaId = req.EmpresaId,
                        Nome = token,
                        NomeAmigavel = inner.Replace(".", " ").Replace("_", " ").ToUpperInvariant(),
                        NomePropriedade = inner, // default: o mesmo (você ajusta depois no front)
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

            // 2) sincroniza vínculos
            var links = await _unitOfWork.ModeloTextoVariavelVinculos.GetLinksOnly(req.EmpresaId, req.ModeloTextoId);
            var linkByVarId = links.ToDictionary(x => x.ModeloTextoVariavelId, x => x);

            var tokenVarIds = tokens.Select(t => varsByToken[t].Id).Distinct().ToHashSet();

            // cria/reativa vínculos faltantes
            foreach (var varId in tokenVarIds)
            {
                if (!linkByVarId.TryGetValue(varId, out var link))
                {
                    var nl = new ModeloTextoVariavelVinculo
                    {
                        EmpresaId = req.EmpresaId,
                        ModeloTextoId = req.ModeloTextoId,
                        ModeloTextoVariavelId = varId,
                        Status = 1
                    };
                    await _unitOfWork.ModeloTextoVariavelVinculos.Add(nl);
                    _unitOfWork.Save();
                    createdLinks++;
                }
                else
                {
                    if (link.Status != 1)
                    {
                        link.Status = 1;
                        _unitOfWork.ModeloTextoVariavelVinculos.Update(link);
                        _unitOfWork.Save();
                        enabledLinks++;
                    }
                }
            }

            // desativa vínculos que saíram do HTML
            foreach (var link in links)
            {
                if (link.Status == 1 && !tokenVarIds.Contains(link.ModeloTextoVariavelId))
                {
                    link.Status = 0;
                    _unitOfWork.ModeloTextoVariavelVinculos.Update(link);
                    _unitOfWork.Save();
                    disabledLinks++;
                }
            }

            return new SyncModeloTextoVariavelResponse
            {
                ModeloTextoId = req.ModeloTextoId,
                CreatedVariables = createdVars,
                CreatedLinks = createdLinks,
                DisabledLinks = disabledLinks,
                EnabledLinks = enabledLinks,
                Tokens = tokens
            };
        }

        public async Task<List<ModeloTextoVariavelByModeloDTO>> GetByModelo(int empresaId, int modeloTextoId, bool onlyActiveLinks = true)
        {
            var links = await _unitOfWork.ModeloTextoVariavelVinculos.GetByModelo(empresaId, modeloTextoId);

            if (onlyActiveLinks)
                links = links.Where(x => x.Status == 1).ToList();

            return links
                .Where(x => x.ModeloTextoVariavel != null)
                .Select(x => new ModeloTextoVariavelByModeloDTO
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
                    ModeloTextoId = x.ModeloTextoId
                })
                .ToList();
        }

        public async Task<RenderModeloTextoResponse> Render(int empresaId, int modeloTextoId, RenderModeloTextoRequest req)
        {
            var modelo = await _unitOfWork.ModeloTextos.GetById(modeloTextoId);
            if (modelo == null) throw new Exception("Modelo não encontrado.");
            if (modelo.EmpresaId != empresaId) throw new Exception("Modelo não pertence à empresa informada.");

            var html = modelo.Texto ?? "";

            var values = req.Values ?? new Dictionary<string, string?>();

            // substitui tokens: match {{ ... }}
            // Obs: não remove tokens não encontrados, só substitui os encontrados
            foreach (var kv in values)
            {
                var token = (kv.Key ?? "").Trim();
                if (string.IsNullOrWhiteSpace(token)) continue;
                var val = kv.Value ?? "";

                html = html.Replace(token, val, StringComparison.OrdinalIgnoreCase);
            }

            return new RenderModeloTextoResponse
            {
                ModeloTextoId = modeloTextoId,
                Html = html
            };
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
        Task<SyncModeloTextoVariavelResponse> Sync(SyncModeloTextoVariavelRequest req);
        Task<List<ModeloTextoVariavelByModeloDTO>> GetByModelo(int empresaId, int modeloTextoId, bool onlyActiveLinks = true);
        Task<RenderModeloTextoResponse> Render(int empresaId, int modeloTextoId, RenderModeloTextoRequest req);
    }
}