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
                ["tipos-ocorrencia"] = TipoSecao.TiposOcorrencia,
                ["texto-livre"] = TipoSecao.TextoLivre,
                ["fotos"] = TipoSecao.Fotos,
                ["paralisacao"] = TipoSecao.Paralisacao,
                ["acidentes"] = TipoSecao.Acidentes,
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

        // ---------------------------------------------------------------
        // Parse do HTML via HtmlAgilityPack
        // Lê todos os elementos com atributo data-secao e monta as seções
        // ---------------------------------------------------------------
        private async Task<List<RelatorioSecao>> ParseSecoesDoHtml(string html, Obras obra)
        {
            var secoes = new List<RelatorioSecao>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode
                .SelectNodes("//*[@data-secao]");

            if (nodes == null) return secoes;

            int ordem = 0;
            foreach (var node in nodes)
            {
                var dataSecao = node.GetAttributeValue("data-secao", "").Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(dataSecao)) continue;

                if (!DataSecaoMap.TryGetValue(dataSecao, out var tipoSecao))
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

                    case TipoSecao.TiposOcorrencia:
                        var tipos = await _unitOfWork.ObraTiposOcorrencia.GetTiposOcorrenciaByObraId(obra.Id);
                        secao.Itens = tipos.Select(t => new RelatorioSecaoItem
                        {
                            ReferenciaId = t.Id,
                            Nome = t.Nome,
                            Descricao = null
                        }).ToList();
                        break;
                }

                secoes.Add(secao);
            }

            return secoes;
        }

        private static RelatorioDTO MapToDTO(Relatorio r) => new()
        {
            Id = r.Id,
            ModeloTextoId = r.ModeloTextoId,
            ModeloTextoNome = r.ModeloTexto?.Nome,
            ObraId = r.ObraId,
            ObraNome = r.Obra?.Name,
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

        public async Task<string> GetRenderedHtml(int relatorioId)
        {
            var relatorio = await _unitOfWork.Relatorios.GetById(relatorioId);
            if (relatorio == null) throw new Exception("Relatório não encontrado.");

            var html = relatorio.HtmlSnapshot ?? "";
            if (string.IsNullOrWhiteSpace(html)) return html;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            foreach (var secao in relatorio.Secoes ?? new List<RelatorioSecao>())
            {
                var node = doc.DocumentNode
                    .SelectSingleNode($"//*[@data-secao='{secao.DataSecao}']");

                if (node == null) continue;

                switch (secao.TipoSecao)
                {
                    case TipoSecao.MaoDeObra:
                    case TipoSecao.Equipamentos:
                    case TipoSecao.TiposOcorrencia:
                        InjetarItensNaTabela(node, secao.Itens ?? new List<RelatorioSecaoItem>());
                        break;
                    case TipoSecao.Paralisacao:
                    case TipoSecao.Acidentes:
                        secao.Itens = new List<RelatorioSecaoItem>();
                        break;
                    case TipoSecao.Fotos:
                        InjetarFotosNaSecao(node, secao.Itens ?? new List<RelatorioSecaoItem>());
                        break;
                    case TipoSecao.Local:
                        if (!string.IsNullOrWhiteSpace(secao.ConteudoJson))
                        {
                            var localData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(secao.ConteudoJson);
                            if (localData != null)
                            {
                                var innerHtml = node.InnerHtml;
                                foreach (var kv in localData)
                                    innerHtml = innerHtml.Replace($"{{{{{kv.Key.ToLowerInvariant()}}}}}", kv.Value.ToString(), StringComparison.OrdinalIgnoreCase);
                                node.InnerHtml = innerHtml;
                            }
                        }
                        break;
                }
            }

            var finalHtml = doc.DocumentNode.OuterHtml;
            finalHtml = finalHtml.Replace("{{data_relatorio}}", relatorio.DataRelatorio.ToString("dd/MM/yyyy"), StringComparison.OrdinalIgnoreCase);
            finalHtml = finalHtml.Replace("{{autor_nome}}", relatorio.CriadoPor?.Name ?? "", StringComparison.OrdinalIgnoreCase);
            finalHtml = finalHtml.Replace("{{cliente_nome}}", relatorio.Obra?.ClientName ?? "", StringComparison.OrdinalIgnoreCase);
            finalHtml = finalHtml.Replace("{{local_nome}}", relatorio.Obra?.Name ?? "", StringComparison.OrdinalIgnoreCase);
            finalHtml = finalHtml.Replace("{{local_endereco}}",
                $"{relatorio.Obra?.StreetAddress}, {relatorio.Obra?.Number} - {relatorio.Obra?.City}/{relatorio.Obra?.State}",
                StringComparison.OrdinalIgnoreCase);

            const string charsetMeta = "<meta charset=\"utf-8\">";
            if (!finalHtml.Contains("charset", StringComparison.OrdinalIgnoreCase))
            {
                finalHtml = finalHtml.Contains("<head>", StringComparison.OrdinalIgnoreCase)
                    ? finalHtml.Replace("<head>", $"<head>{charsetMeta}", StringComparison.OrdinalIgnoreCase)
                    : charsetMeta + finalHtml;
            }

            return finalHtml;
        }

        private static void InjetarItensNaTabela(HtmlNode secaoNode, IList<RelatorioSecaoItem> itens)
        {
            var tituloNode = secaoNode.SelectSingleNode(".//td[@colspan] | .//th[@colspan]");
            var titulo = tituloNode?.InnerText?.Trim() ?? secaoNode.GetAttributeValue("data-secao", "");

            var tabelaTitulo =
                "<table style=\"border-collapse: collapse; width: 100%;\" border=\"1\">" +
                "<colgroup><col style=\"width: 100%;\"></colgroup>" +
                "<tbody>" +
                $"<tr><td style=\"padding: 8px;\"><strong>{titulo}</strong></td></tr>" +
                "</tbody>" +
                "</table>";

            var sbLinhas = new System.Text.StringBuilder();
            foreach (var item in itens)
            {
                var nome = item.Nome ?? "";
                var descricao = item.Descricao ?? "";
                sbLinhas.Append(
                    "<tr>" +
                    $"<td style=\"width: 50%; padding: 8px;\">{nome}</td>" +
                    $"<td style=\"width: 50%; padding: 8px;\">{descricao}</td>" +
                    "</tr>");
            }

            var tabelaDados = itens.Any()
                ? "<table style=\"border-collapse: collapse; width: 100%;\" border=\"1\">" +
                  "<colgroup><col style=\"width: 50%;\"><col style=\"width: 50%;\"></colgroup>" +
                  "<tbody>" +
                  sbLinhas.ToString() +
                  "</tbody>" +
                  "</table>"
                : "";

            secaoNode.InnerHtml = tabelaTitulo + tabelaDados;
        }

        private static void InjetarFotosNaSecao(HtmlNode secaoNode, IList<RelatorioSecaoItem> itens)
        {
            var tituloNode = secaoNode.SelectSingleNode(".//td[@colspan] | .//th[@colspan]");
            var titulo = tituloNode?.InnerText?.Trim() ?? "Registro Fotográfico";

            var tabelaTitulo =
                "<table style=\"border-collapse: collapse; width: 100%;\" border=\"1\">" +
                "<colgroup><col style=\"width: 100%;\"></colgroup>" +
                "<tbody>" +
                $"<tr><td style=\"padding: 8px;\"><strong>{titulo}</strong></td></tr>" +
                "</tbody>" +
                "</table>";

            if (!itens.Any())
            {
                secaoNode.InnerHtml = tabelaTitulo;
                return;
            }

            var todasFotos = itens
                .SelectMany(i => i.Fotos ?? new List<RelatorioItemFoto>(),
                    (item, foto) => new { item.Nome, foto.ImagemBytes, foto.ContentType, foto.NomeArquivo })
                .ToList();

            var sbLinhas = new System.Text.StringBuilder();
            sbLinhas.Append("<table style=\"border-collapse: collapse; width: 100%;\" border=\"1\">");
            sbLinhas.Append("<colgroup><col style=\"width: 50%;\"><col style=\"width: 50%;\"></colgroup>");
            sbLinhas.Append("<tbody>");

            for (int i = 0; i < todasFotos.Count; i += 2)
            {
                sbLinhas.Append("<tr>");

                var f1 = todasFotos[i];
                var base64_1 = Convert.ToBase64String(f1.ImagemBytes);
                sbLinhas.Append(
                    "<td style=\"padding: 8px; text-align: center; vertical-align: top;\">" +
                    $"<img src=\"data:{f1.ContentType};base64,{base64_1}\" style=\"max-width:100%;max-height:300px;\" />" +
                    $"<br/><small>{f1.Nome ?? f1.NomeArquivo ?? ""}</small>" +
                    "</td>");

                if (i + 1 < todasFotos.Count)
                {
                    var f2 = todasFotos[i + 1];
                    var base64_2 = Convert.ToBase64String(f2.ImagemBytes);
                    sbLinhas.Append(
                        "<td style=\"padding: 8px; text-align: center; vertical-align: top;\">" +
                        $"<img src=\"data:{f2.ContentType};base64,{base64_2}\" style=\"max-width:100%;max-height:300px;\" />" +
                        $"<br/><small>{f2.Nome ?? f2.NomeArquivo ?? ""}</small>" +
                        "</td>");
                }
                else
                {
                    sbLinhas.Append("<td style=\"padding: 8px;\"></td>");
                }

                sbLinhas.Append("</tr>");
            }

            sbLinhas.Append("</tbody></table>");

            secaoNode.InnerHtml = tabelaTitulo + sbLinhas.ToString();
        }
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
        Task<string> GetRenderedHtml(int relatorioId);
    }
}