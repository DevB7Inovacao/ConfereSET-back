using Core.DTO;
using Core.Enums;
using Core.Models;
using HtmlAgilityPack;
using Infrastructure.Repositories;
using System.Text.Json;

namespace Services
{
    public class RelatorioService : IRelatorioService
    {
        private readonly IUnitOfWork _unitOfWork;

        private static readonly Dictionary<string, TipoSecao> DataSecaoMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["local"] = TipoSecao.Local,
                ["mao-de-obra"] = TipoSecao.MaoDeObra,
                ["equipamentos"] = TipoSecao.Equipamentos,
                ["texto-livre"] = TipoSecao.TextoLivre,
                ["fotos"] = TipoSecao.Fotos,
                ["comentarios"] = TipoSecao.Comentarios,
                ["ocorrencias"] = TipoSecao.Ocorrencias,
            };

        public RelatorioService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Relatorio> Create(CreateRelatorioRequest req)
        {
            var modelo = await _unitOfWork.ModeloTextos.GetById(req.ModeloTextoId);
            if (modelo == null) throw new Exception("Modelo de texto não encontrado.");

            var obra = await _unitOfWork.Obras.GetObraById(req.ObraId);
            if (obra == null) throw new Exception("Obra não encontrada.");

            var secoes = await ParseSecoesDoHtml(modelo.Texto, obra);

            var relatorio = new Relatorio
            {
                ModeloTextoId = req.ModeloTextoId,
                ObraId = req.ObraId,
                CriadoPorUserId = req.CriadoPorUserId,
                Titulo = req.Titulo.Trim(),
                Status = StatusRelatorio.Rascunho,
                DataRelatorio = req.DataRelatorio ?? DateTime.Now,
                HtmlSnapshot = modelo.Texto,
                Secoes = secoes
            };

            await _unitOfWork.Relatorios.Add(relatorio);
            _unitOfWork.Save();
            return relatorio;
        }

        public async Task<RelatorioDTO?> GetById(int id)
        {
            var relatorio = await _unitOfWork.Relatorios.GetById(id);
            if (relatorio == null) return null;
            return MapToDTO(relatorio);
        }

        public async Task<RelatorioPagedDTO> GetPaged(FiltersRelatorioDTO filters)
        {
            var paged = await _unitOfWork.Relatorios.GetPaged(filters);
            return new RelatorioPagedDTO
            {
                PageCount = paged.PageCount,
                Result = paged.Results.Select(MapToDTO).ToList()
            };
        }

        public async Task<bool> UpdateStatus(int id, StatusRelatorio status)
        {
            var relatorio = await _unitOfWork.Relatorios.GetById(id);
            if (relatorio == null) throw new Exception("Relatório não encontrado.");

            relatorio.Status = status;
            _unitOfWork.Relatorios.Update(relatorio);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var relatorio = await _unitOfWork.Relatorios.GetById(id);
            if (relatorio == null) throw new Exception("Relatório não encontrado.");

            _unitOfWork.Relatorios.Delete(relatorio);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> UpdateItem(int itemId, UpdateRelatorioSecaoItemRequest req)
        {
            var item = await _unitOfWork.Relatorios.GetItemById(itemId);
            if (item == null) throw new Exception("Item não encontrado.");

            if (req.ReferenciaId.HasValue)
                item.ReferenciaId = req.ReferenciaId.Value;

            if (req.Descricao != null)
                item.Descricao = req.Descricao;

            _unitOfWork.Relatorios.UpdateItem(item);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> AddFotoToItem(int itemId, AddFotoToItemRequest req)
        {
            var item = await _unitOfWork.Relatorios.GetItemById(itemId);
            if (item == null) throw new Exception("Item não encontrado.");

            var foto = new RelatorioItemFoto
            {
                RelatorioSecaoItemId = itemId,
                ImagemBytes = Convert.FromBase64String(req.ImagemBase64),
                ContentType = req.ContentType,
                NomeArquivo = req.NomeArquivo
            };

            await _unitOfWork.Relatorios.AddFoto(foto);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> DeleteFoto(int fotoId)
        {
            var foto = await _unitOfWork.Relatorios.GetFotoById(fotoId);
            if (foto == null) throw new Exception("Foto não encontrada.");

            _unitOfWork.Relatorios.DeleteFoto(foto);
            return _unitOfWork.Save() > 0;
        }

        private async Task<List<RelatorioSecao>> ParseSecoesDoHtml(string html, Obras obra)
        {
            var secoes = new List<RelatorioSecao>();
            var secoesMap = new Dictionary<TipoSecao, RelatorioSecao>();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//*[@data-secao]");
            if (nodes == null) return secoes;

            int ordem = 0;
            foreach (var node in nodes)
            {
                var dataSecao = node.GetAttributeValue("data-secao", "").Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(dataSecao)) continue;

                if (!DataSecaoMap.TryGetValue(dataSecao, out var tipoSecao))
                    continue;

                if (tipoSecao != TipoSecao.Ocorrencias && HasAncestorWithDataSecao(node))
                    continue;

                if (tipoSecao == TipoSecao.Ocorrencias)
                {
                    if (secoesMap.ContainsKey(tipoSecao))
                        continue;

                    var (secoesOcorrencias, proximaOrdem) = await BuildSecoesOcorrencias(obra, ordem);
                    secoes.AddRange(secoesOcorrencias);
                    ordem = proximaOrdem;
                    secoesMap[tipoSecao] = new RelatorioSecao { DataSecao = dataSecao, TipoSecao = tipoSecao };
                    continue;
                }

                if (secoesMap.ContainsKey(tipoSecao))
                    continue;

                var secao = new RelatorioSecao
                {
                    DataSecao = dataSecao,
                    TipoSecao = tipoSecao,
                    Ordem = ordem++,
                    Itens = new List<RelatorioSecaoItem>()
                };

                switch (tipoSecao)
                {
                    case TipoSecao.Local:
                        secao.ConteudoJson = JsonSerializer.Serialize(new
                        {
                            obra.Name,
                            obra.StreetAddress,
                            obra.Number,
                            obra.AddressLine2,
                            obra.Neighborhood,
                            obra.City,
                            obra.State,
                            obra.PostalCode,
                            obra.Country,
                            obra.ClientName
                        });
                        break;

                    case TipoSecao.MaoDeObra:
                        var maos = await _unitOfWork.ObraMaoDeObra.GetMaoDeObraByObraId(obra.Id);
                        secao.Itens = maos.Select(m => new RelatorioSecaoItem
                        {
                            ReferenciaId = m.Id,
                            Nome = m.Funcao,
                            Descricao = null
                        }).ToList();
                        break;

                    case TipoSecao.Equipamentos:
                        var equips = await _unitOfWork.ObraEquipamentos.GetEquipamentosByObraId(obra.Id);
                        secao.Itens = equips.Select(e => new RelatorioSecaoItem
                        {
                            ReferenciaId = e.Id,
                            Nome = e.Nome,
                            Descricao = null
                        }).ToList();
                        break;

                    case TipoSecao.TextoLivre:
                        secao.Itens = new List<RelatorioSecaoItem>
                        {
                            new RelatorioSecaoItem { Nome = null, Descricao = null }
                        };
                        break;

                    case TipoSecao.Fotos:
                        secao.Itens = new List<RelatorioSecaoItem>
                        {
                            new RelatorioSecaoItem { Nome = null, Descricao = null }
                        };
                        break;

                    case TipoSecao.Comentarios:
                        break;
                }

                secoesMap[tipoSecao] = secao;
                secoes.Add(secao);
            }

            return secoes;
        }

        private async Task<(List<RelatorioSecao> Secoes, int ProximaOrdem)> BuildSecoesOcorrencias(Obras obra, int ordemInicial)
        {
            var ocorrencias = await _unitOfWork.Ocorrencias.GetByObraId(obra.Id);

            if (!ocorrencias.Any())
                return (new List<RelatorioSecao>(), ordemInicial);

            var ordem = ordemInicial;
            var secoes = new List<RelatorioSecao>();

            foreach (var grupo in ocorrencias.GroupBy(o => new { o.TipoOcorrenciaId, o.TipoOcorrenciaNome }))
            {
                secoes.Add(new RelatorioSecao
                {
                    DataSecao = grupo.Key.TipoOcorrenciaNome ?? "ocorrencias",
                    TipoSecao = TipoSecao.Ocorrencias,
                    TipoOcorrenciaId = grupo.Key.TipoOcorrenciaId,
                    ConteudoJson = grupo.Key.TipoOcorrenciaNome,
                    Ordem = ordem++,
                    Itens = grupo.Select(o => new RelatorioSecaoItem
                    {
                        ReferenciaId = o.Id,
                        Nome = o.Titulo,
                        Descricao = o.Descricao
                    }).ToList()
                });
            }

            return (secoes, ordem);
        }

        private static bool HasAncestorWithDataSecao(HtmlNode node)
        {
            var parent = node.ParentNode;
            while (parent != null && parent.NodeType == HtmlNodeType.Element)
            {
                if (parent.GetAttributeValue("data-secao", null) != null)
                    return true;
                parent = parent.ParentNode;
            }
            return false;
        }

        private static RelatorioDTO MapToDTO(Relatorio r) => new()
        {
            Id = r.Id,
            ModeloTextoId = r.ModeloTextoId,
            ModeloTextoNome = r.ModeloTexto?.Nome,
            ObraId = r.ObraId,
            ObraNome = r.Obra?.Name,
            ObraStreetAddress = r.Obra?.StreetAddress,
            ObraNumber = r.Obra?.Number,
            ObraAddressLine2 = r.Obra?.AddressLine2,
            ObraNeighborhood = r.Obra?.Neighborhood,
            ObraCity = r.Obra?.City,
            ObraState = r.Obra?.State,
            ObraPostalCode = r.Obra?.PostalCode,
            ObraCountry = r.Obra?.Country,
            ObraClientName = r.Obra?.ClientName,
            ObraClientEmail = r.Obra?.ClientEmail,
            ObraClientPhone = r.Obra?.ClientPhone,
            CriadoPorUserId = r.CriadoPorUserId,
            CriadoPorNome = r.CriadoPor?.Name,
            Titulo = r.Titulo,
            Status = r.Status,
            DataRelatorio = r.DataRelatorio,
            HtmlSnapshot = r.HtmlSnapshot,
            Secoes = r.Secoes?.Select(s => new RelatorioSecaoDTO
            {
                Id = s.Id,
                RelatorioId = s.RelatorioId,
                DataSecao = s.DataSecao,
                TipoSecao = s.TipoSecao,
                Ordem = s.Ordem,
                ConteudoJson = s.ConteudoJson,
                TipoOcorrenciaId = s.TipoOcorrenciaId,
                TipoOcorrenciaNome = s.TipoOcorrencia?.Nome,
                Itens = s.Itens?.Select(i => new RelatorioSecaoItemDTO
                {
                    Id = i.Id,
                    RelatorioSecaoId = i.RelatorioSecaoId,
                    ReferenciaId = i.ReferenciaId,
                    Nome = i.Nome,
                    Descricao = i.Descricao,
                    Fotos = i.Fotos?.Select(f => new RelatorioItemFotoDTO
                    {
                        Id = f.Id,
                        RelatorioSecaoItemId = f.RelatorioSecaoItemId,
                        ContentType = f.ContentType,
                        NomeArquivo = f.NomeArquivo,
                        ImagemBase64 = Convert.ToBase64String(f.ImagemBytes)
                    }).ToList() ?? new()
                }).ToList() ?? new()
            }).ToList() ?? new()
        };
    }

    public interface IRelatorioService
    {
        Task<Relatorio> Create(CreateRelatorioRequest req);
        Task<RelatorioDTO?> GetById(int id);
        Task<RelatorioPagedDTO> GetPaged(FiltersRelatorioDTO filters);
        Task<bool> UpdateStatus(int id, StatusRelatorio status);
        Task<bool> Delete(int id);
        Task<bool> UpdateItem(int itemId, UpdateRelatorioSecaoItemRequest req);
        Task<bool> AddFotoToItem(int itemId, AddFotoToItemRequest req);
        Task<bool> DeleteFoto(int fotoId);
    }
}