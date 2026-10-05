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
		// Bloco "local" do ConteudoJson: o front lê em camelCase.
		private static readonly JsonSerializerOptions LocalJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

		private static string SerializeLocal(object conteudo) => JsonSerializer.Serialize(conteudo, LocalJsonOptions);

		private readonly IUnitOfWork _unitOfWork;
		private readonly IAtividadeRecenteService _atividadeService;
		private readonly IS3Service _s3Service;

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
					// [v2] Blocos configuráveis
					["clima"] = TipoSecao.Clima,
					["assinatura"] = TipoSecao.Assinatura,
					["formulario"] = TipoSecao.Formulario,
					["checklist"] = TipoSecao.Checklist,
					["despesas"] = TipoSecao.Despesas,
					// "observacao" é um texto livre com título "Observações".
					["observacao"] = TipoSecao.TextoLivre,
				};

		public RelatorioService(IUnitOfWork unitOfWork, IAtividadeRecenteService atividadeService, IS3Service s3Service)
		{
			_unitOfWork = unitOfWork;
			_atividadeService = atividadeService;
			_s3Service = s3Service;
		}

		public async Task<Relatorio> Create(CreateRelatorioRequest req)
		{
			// Compat: mantém assinatura antiga; usa CriadoPorUserId do request e não valida empresa.
			return await CreateInternal(req, req.CriadoPorUserId, null);
		}

		/// <summary>
		/// Cria o relatório atribuindo a autoria ao <paramref name="criadoPorUserIdJwt"/> (vindo do JWT,
		/// ignorando o valor presente no request) e valida que a obra pertence à empresa do chamador.
		/// </summary>
		public async Task<Relatorio> Create(CreateRelatorioRequest req, int criadoPorUserIdJwt, int empresaIdJwt)
		{
			return await CreateInternal(req, criadoPorUserIdJwt, empresaIdJwt);
		}

		private async Task<Relatorio> CreateInternal(CreateRelatorioRequest req, int criadoPorUserId, int? empresaIdJwt)
		{
			var modelo = await _unitOfWork.ModeloTextos.GetById(req.ModeloTextoId);
			if (modelo == null) throw new Exception("Modelo de texto não encontrado.");

			var obra = await _unitOfWork.Obras.GetObraById(req.ObraId);
			if (obra == null) throw new Exception("Obra não encontrada.");

			// Escopo de empresa: obra deve pertencer à empresa do chamador (quando contexto disponível).
			if (empresaIdJwt.HasValue && obra.EmpresaId != empresaIdJwt.Value)
				throw new UnauthorizedAccessException("Obra não pertence à sua empresa.");

			// Igualmente, modelo de texto deve pertencer à mesma empresa.
			if (empresaIdJwt.HasValue && modelo.EmpresaId != empresaIdJwt.Value)
				throw new UnauthorizedAccessException("Modelo de texto não pertence à sua empresa.");

			var html = DesaninharSecoesHtml(modelo.Texto, out _) ?? modelo.Texto;
			var secoes = await ParseSecoesDoHtml(html, obra);

			var relatorio = new Relatorio
			{
				ModeloTextoId = req.ModeloTextoId,
				ObraId = req.ObraId,
				CriadoPorUserId = criadoPorUserId,
				Titulo = req.Titulo.Trim(),
				Status = StatusRelatorio.Rascunho,
				DataRelatorio = req.DataRelatorio ?? DateTime.Now,
				HtmlSnapshot = html,
				Secoes = secoes
			};

			await _unitOfWork.Relatorios.Add(relatorio);
			_unitOfWork.Save();
			return relatorio;
		}

		/// <summary>
		/// Variante de <see cref="GetById"/> que valida o escopo de empresa.
		/// Retorna <c>null</c> tanto para inexistente quanto para "fora da empresa" — o caller
		/// devolve 404 nos dois casos (não vaza informação de existência cruzada).
		/// </summary>
		public async Task<RelatorioDTO?> GetByIdScoped(int id, int empresaIdJwt)
		{
			var relatorio = await _unitOfWork.Relatorios.GetById(id);
			if (relatorio == null) return null;
			if (relatorio.Obra?.EmpresaId != empresaIdJwt) return null;
			await EnsureSecoesAninhadas(relatorio);
			// [v12] Lazy: garante seção Comentários + item raiz em Fotos
			await EnsureComentariosSection(relatorio);
			await EnsureFotosItemRaiz(relatorio);
			return MapToDTO(relatorio);
		}

		/// <summary>
		/// Retorna um DTO leve do relatório a partir do id de um item, validando escopo de empresa.
		/// </summary>
		public async Task<RelatorioDTO?> GetRelatorioByItemId(int itemId, int empresaIdJwt)
		{
			var item = await _unitOfWork.Relatorios.GetItemById(itemId);
			if (item == null) return null;
			var secao = await _unitOfWork.Relatorios.GetSecaoById(item.RelatorioSecaoId);
			if (secao == null) return null;
			return await GetByIdScoped(secao.RelatorioId, empresaIdJwt);
		}

		public async Task<RelatorioDTO?> GetRelatorioByFotoId(int fotoId, int empresaIdJwt)
		{
			var foto = await _unitOfWork.Relatorios.GetFotoById(fotoId);
			if (foto == null) return null;
			var item = await _unitOfWork.Relatorios.GetItemById(foto.RelatorioSecaoItemId);
			if (item == null) return null;
			var secao = await _unitOfWork.Relatorios.GetSecaoById(item.RelatorioSecaoId);
			if (secao == null) return null;
			return await GetByIdScoped(secao.RelatorioId, empresaIdJwt);
		}

		public async Task<RelatorioDTO?> GetRelatorioBySecaoId(int secaoId, int empresaIdJwt)
		{
			var secao = await _unitOfWork.Relatorios.GetSecaoById(secaoId);
			if (secao == null) return null;
			return await GetByIdScoped(secao.RelatorioId, empresaIdJwt);
		}

		public async Task<(RelatorioDTO? relatorio, int? autorComentarioId)> GetRelatorioAndAutorByComentarioId(int comentarioId, int empresaIdJwt)
		{
			var comentario = await _unitOfWork.Relatorios.GetComentarioById(comentarioId);
			if (comentario == null) return (null, null);
			var secao = await _unitOfWork.Relatorios.GetSecaoById(comentario.RelatorioSecaoId);
			if (secao == null) return (null, null);
			var dto = await GetByIdScoped(secao.RelatorioId, empresaIdJwt);
			return (dto, comentario.AutorId);
		}

		public async Task<RelatorioDTO?> GetById(int id)
		{
			var relatorio = await _unitOfWork.Relatorios.GetById(id);
			if (relatorio == null) return null;

			await EnsureSecoesAninhadas(relatorio);

			// Lazy: garante a seção de Comentários para relatórios antigos criados sem ela.
			// Sem isso, o operador não consegue comentar e o admin não tem onde ler.
			await EnsureComentariosSection(relatorio);

			// [v12] Lazy: garante item raiz em cada seção de Fotos pra o upload funcionar.
			await EnsureFotosItemRaiz(relatorio);

			return MapToDTO(relatorio);
		}

		// [v12] Endpoint explícito: garante item raiz de UMA seção específica e devolve o ID.
		// Usado pelo front imediatamente antes do upload pra ter certeza que o item existe.
		public async Task<int?> EnsureFotoItemRaiz(int secaoId)
		{
			var secao = await _unitOfWork.Relatorios.GetSecaoById(secaoId);
			if (secao == null) return null;
			if (secao.TipoSecao != TipoSecao.Fotos) return null;

			if (secao.Itens != null && secao.Itens.Count > 0)
				return secao.Itens[0].Id;

			var item = new RelatorioSecaoItem
			{
				RelatorioSecaoId = secaoId,
				Nome = "Fotos",
				Descricao = null,
			};
			await _unitOfWork.Relatorios.AddItem(item);
			_unitOfWork.Save();
			return item.Id;
		}

		// [v12] Garante que cada seção de Fotos tem ao menos 1 item raiz pra ancorar
		// as imagens. Necessário pra relatórios antigos criados antes do fix de UpdateV2.
		private async Task EnsureFotosItemRaiz(Relatorio relatorio)
		{
			if (relatorio.Secoes == null) return;
			var changed = false;
			foreach (var s in relatorio.Secoes.Where(s => s.TipoSecao == TipoSecao.Fotos))
			{
				if (s.Itens == null || s.Itens.Count == 0)
				{
					var itemRaiz = new RelatorioSecaoItem
					{
						RelatorioSecaoId = s.Id,
						Nome = "Fotos",
						Descricao = null,
					};
					await _unitOfWork.Relatorios.AddItem(itemRaiz);
					s.Itens ??= new List<RelatorioSecaoItem>();
					s.Itens.Add(itemRaiz);
					changed = true;
				}
			}
			if (changed) _unitOfWork.Save();
		}

		/// <summary>
		/// Relatórios criados de um modelo com seção DENTRO de outra (ex.: bloco Checklist "aberto"
		/// no editor engoliu Observações e Assinatura) nasceram sem essas seções — o operário não
		/// tinha onde escrever. Ao abrir um relatório ainda não aprovado, cria as seções que faltam
		/// logo depois do bloco que as continha e grava o snapshot já corrigido (roda uma vez só).
		/// </summary>
		private async Task EnsureSecoesAninhadas(Relatorio relatorio)
		{
			if (relatorio.Status == StatusRelatorio.Aprovado || relatorio.Obra == null || relatorio.Secoes == null) return;
			var html = DesaninharSecoesHtml(relatorio.HtmlSnapshot, out var movidas);
			if (html == null) return;

			var modelo = await ParseSecoesDoHtml(html, relatorio.Obra);
			var ordenadas = relatorio.Secoes.OrderBy(s => s.Ordem).ThenBy(s => s.Id).ToList();
			bool Existe(RelatorioSecao nova) => ordenadas.Any(s =>
				string.Equals(s.DataSecao, nova.DataSecao, StringComparison.OrdinalIgnoreCase)
				|| (!RelatorioSecaoConfig.IsConfiguravel(nova.TipoSecao) && s.TipoSecao == nova.TipoSecao));

			var novas = new List<RelatorioSecao>();
			for (var i = 0; i < modelo.Count; i++)
			{
				var nova = modelo[i];
				if (nova.TipoSecao == TipoSecao.Comentarios || nova.TipoSecao == TipoSecao.Ocorrencias) continue;
				var baseSecao = (nova.DataSecao ?? "").Split(':')[0];
				if (!movidas.Contains(baseSecao) || Existe(nova)) continue;

				// Âncora: a seção anterior a ela no modelo que já existe no relatório.
				var pos = 0;
				for (var j = i - 1; j >= 0; j--)
				{
					var anterior = ordenadas.FindIndex(s => string.Equals(s.DataSecao, modelo[j].DataSecao, StringComparison.OrdinalIgnoreCase));
					if (anterior >= 0) { pos = anterior + 1; break; }
				}
				nova.RelatorioId = relatorio.Id;
				ordenadas.Insert(pos, nova);
				novas.Add(nova);
			}

			for (var i = 0; i < ordenadas.Count; i++) ordenadas[i].Ordem = i;
			foreach (var nova in novas)
			{
				await _unitOfWork.Relatorios.AddSecao(nova);
				relatorio.Secoes.Add(nova);
			}
			relatorio.HtmlSnapshot = html;
			_unitOfWork.Save();
		}

		/// <summary>
		/// Move cada seção que está dentro de OUTRA seção para logo depois do bloco que a continha
		/// (na mesma ordem). Seção repetida dentro dela mesma (assinatura em assinatura) fica.
		/// Devolve o HTML corrigido, ou <c>null</c> quando não havia nada aninhado.
		/// Mesma regra do front (lib/secoes-html.ts).
		/// </summary>
		internal static string? DesaninharSecoesHtml(string? html, out HashSet<string> movidas)
		{
			movidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(html) || html.IndexOf("data-secao", StringComparison.OrdinalIgnoreCase) < 0) return null;

			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var nodes = doc.DocumentNode.SelectNodes("//*[@data-secao]");
			if (nodes == null) return null;

			static string Base(HtmlNode n) => n.GetAttributeValue("data-secao", "").Trim().ToLowerInvariant().Split(':')[0];
			var ultimoInserido = new Dictionary<HtmlNode, HtmlNode>();
			foreach (var node in nodes.ToList())
			{
				var b = Base(node);
				if (b.Length == 0) continue;
				var ancestrais = new List<HtmlNode>();
				for (var p = node.ParentNode; p != null && p.NodeType == HtmlNodeType.Element; p = p.ParentNode)
					if (p.GetAttributeValue("data-secao", null) != null) ancestrais.Add(p);
				if (ancestrais.Count == 0 || ancestrais.Any(a => Base(a) == b)) continue;

				var topo = ancestrais[^1];
				if (topo.ParentNode == null) continue;
				var referencia = ultimoInserido.TryGetValue(topo, out var u) ? u : topo;
				node.Remove();
				topo.ParentNode.InsertAfter(node, referencia);
				ultimoInserido[topo] = node;
				movidas.Add(b);
			}
			return movidas.Count > 0 ? doc.DocumentNode.OuterHtml : null;
		}

		private async Task EnsureComentariosSection(Relatorio relatorio)
		{
			if (relatorio.Secoes?.Any(s => s.TipoSecao == TipoSecao.Comentarios) == true)
				return;

			var ordemMax = relatorio.Secoes?.Max(s => (int?)s.Ordem) ?? -1;
			var nova = new RelatorioSecao
			{
				RelatorioId = relatorio.Id,
				DataSecao = "comentarios",
				TipoSecao = TipoSecao.Comentarios,
				Ordem = ordemMax + 1,
				Itens = new List<RelatorioSecaoItem>()
			};
			await _unitOfWork.Relatorios.AddSecao(nova);
			_unitOfWork.Save();
			relatorio.Secoes ??= new List<RelatorioSecao>();
			relatorio.Secoes.Add(nova);
		}

		public async Task<RelatorioPagedDTO> GetPaged(FiltersRelatorioDTO filters)
		{
			var paged = await _unitOfWork.Relatorios.GetPaged(filters);
			return new RelatorioPagedDTO
			{
				PageCount = paged.PageCount,
				// Listagem: não trafega HTML nem logo (pesados); ficam só no GetById.
				Result = paged.Results.Select(r =>
				{
					var dto = MapToDTO(r);
					dto.HtmlSnapshot = null;
					dto.EmpresaLogoBase64 = null;
					return dto;
				}).ToList()
			};
		}

		public async Task<bool> UpdateStatus(int id, UpdateRelatorioStatusRequest req)
		{
			var relatorio = await _unitOfWork.Relatorios.GetById(id);
			if (relatorio == null) throw new Exception("Relatório não encontrado.");

			ValidarTransicaoStatus(relatorio.Status, req.Status);

			// [v2] Campos obrigatórios do modelo precisam estar preenchidos para enviar.
			if (req.Status == StatusRelatorio.Submetido)
			{
				var pendencias = CalcularPendencias(relatorio);
				if (pendencias.Count > 0)
					throw new PendenciasException("Preencha os campos obrigatórios antes de enviar.", pendencias);
			}

			if (req.Status == StatusRelatorio.Rejeitado)
			{
				if (string.IsNullOrWhiteSpace(req.ObservacaoRejeicao))
					throw new Exception("É obrigatório informar uma observação ao reprovar o relatório.");

				relatorio.ObservacaoRejeicao = req.ObservacaoRejeicao.Trim();
			}
			else
			{
				relatorio.ObservacaoRejeicao = null;
			}

			relatorio.Status = req.Status;
			_unitOfWork.Relatorios.Update(relatorio);
			var saved = _unitOfWork.Save() > 0;

			if (saved)
			{
				var obraNome = relatorio.Obra?.Name ?? $"obra #{relatorio.ObraId}";
				var (tipo, descricao) = req.Status switch
				{
					StatusRelatorio.Submetido => (TipoAtividade.RelatorioSubmetido,
							$"Relatório '{relatorio.Titulo}' submetido para aprovação na {obraNome}."),
					StatusRelatorio.Aprovado => (TipoAtividade.RelatorioAprovado,
							$"Relatório '{relatorio.Titulo}' foi aprovado na {obraNome}."),
					StatusRelatorio.Rejeitado => (TipoAtividade.RelatorioRejeitado,
							$"Relatório '{relatorio.Titulo}' foi rejeitado na {obraNome}."),
					_ => ((TipoAtividade?)null, (string?)null)
				};

				if (tipo.HasValue)
					await _atividadeService.Registrar(
							relatorio.CriadoPorUserId,
							tipo.Value,
							descricao!,
							relatorio.ObraId,
							relatorio.Id);
			}

			return saved;
		}

		public async Task<bool> Delete(int id)
		{
			var relatorio = await _unitOfWork.Relatorios.GetById(id);
			if (relatorio == null) throw new Exception("Relatório não encontrado.");

			var operadorId = relatorio.CriadoPorUserId;
			var obraId = relatorio.ObraId;
			var titulo = relatorio.Titulo;
			var obraNome = relatorio.Obra?.Name ?? $"obra #{obraId}";

			_unitOfWork.Relatorios.Delete(relatorio);
			var saved = _unitOfWork.Save() > 0;

			if (saved)
				await _atividadeService.Registrar(
						operadorId,
						TipoAtividade.RelatorioExcluido,
						$"Relatório '{titulo}' foi excluído da {obraNome}.",
						obraId);

			return saved;
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

			var imagem = ValidarImagem(req);

			var foto = new RelatorioItemFoto
			{
				RelatorioSecaoItemId = itemId,
				ImagemBytes = imagem.Bytes,
				ContentType = imagem.ContentType,
				NomeArquivo = imagem.NomeArquivo
			};

			await _unitOfWork.Relatorios.AddFoto(foto);
			return _unitOfWork.Save() > 0;
		}
		// ---------------------------------------------------------------------
		// Validação de imagens (upload de fotos) — lógica compartilhada em ImageValidation.
		// ---------------------------------------------------------------------

		private static ImagemValidada ValidarImagem(AddFotoToItemRequest f) =>
			ImageValidation.Validar(f?.ImagemBase64, f?.NomeArquivo);

		/// <summary>
		/// Upload em lote "tudo ou nada": valida TODAS as fotos antes de enviar qualquer uma; se um
		/// upload no S3 (ou o Save no banco) falhar no meio do lote, os objetos já enviados são
		/// removidos do S3 (sem órfãos) e nada é gravado no banco. O front pode reenviar o lote.
		/// </summary>
		public async Task<bool> AddMultipleFotosToItem(int itemId, List<AddFotoToItemRequest> fotos)
		{
			var item = await _unitOfWork.Relatorios.GetItemById(itemId);
			if (item == null) throw new Exception("Item não encontrado.");

			// 1) Valida tudo antes de qualquer upload (base64, tamanho, magic bytes).
			var validadas = fotos.Select(ValidarImagem).ToList();

			// 2) Upload sequencial com compensação em caso de falha.
			var enviadas = new List<string>();
			try
			{
				// [v2] Novas fotos entram no fim da ordem atual do item.
				var proximaOrdem = item.Fotos.Count == 0 ? 0 : item.Fotos.Max(f => f.Ordem) + 1;
				for (var idx = 0; idx < validadas.Count; idx++)
				{
					var img = validadas[idx];
					// ContentType do objeto no S3 vem do tipo detectado, nunca do cliente.
					var s3Url = await _s3Service.UploadImageAsync(img.Bytes, img.NomeArquivo, img.ContentType);
					enviadas.Add(s3Url);

					await _unitOfWork.Relatorios.AddFoto(new RelatorioItemFoto
					{
						RelatorioSecaoItemId = itemId,
						S3Url = s3Url,
						ContentType = img.ContentType,
						NomeArquivo = img.NomeArquivo,
						Legenda = LimitarTexto(fotos[idx]?.Legenda, MaxLegendaFoto),
						Ordem = proximaOrdem++,
					});
				}

				return _unitOfWork.Save() > 0;
			}
			catch (Exception ex)
			{
				foreach (var url in enviadas)
					await _s3Service.DeleteImageAsync(url); // best-effort; o S3Service já loga a falha

				throw new Exception($"Falha ao enviar as fotos ({enviadas.Count} de {validadas.Count} enviadas antes do erro); nenhuma foto foi salva. Tente novamente. Detalhe: {ex.Message}", ex);
			}
		}

		/// <summary>
		/// Exclui várias fotos: remove do banco primeiro (um único Save) e depois apaga os objetos no
		/// S3 (best-effort). Se o S3 falhar, sobra no máximo um objeto órfão — nunca um registro
		/// apontando para imagem inexistente. A autorização é feita uma única vez no controller
		/// (via <see cref="GetFotoEscopos"/>), sem recarregar o relatório por foto.
		/// </summary>
		public async Task<bool> DeleteMultipleFotos(List<int> fotoIds)
		{
			var urls = new List<string>();
			foreach (var id in fotoIds.Distinct())
			{
				var foto = await _unitOfWork.Relatorios.GetFotoById(id);
				if (foto == null) continue;

				if (!string.IsNullOrWhiteSpace(foto.S3Url)) urls.Add(foto.S3Url);
				_unitOfWork.Relatorios.DeleteFoto(foto);
			}

			var saved = _unitOfWork.Save() > 0;
			if (saved)
			{
				foreach (var url in urls)
					await _s3Service.DeleteImageAsync(url); // best-effort; o S3Service já loga a falha
			}
			return saved;
		}

		public async Task<bool> DeleteFoto(int fotoId)
		{
			var foto = await _unitOfWork.Relatorios.GetFotoById(fotoId);
			if (foto == null) throw new Exception("Foto não encontrada.");

			var s3Url = foto.S3Url;
			_unitOfWork.Relatorios.DeleteFoto(foto);
			var saved = _unitOfWork.Save() > 0;

			// Fotos antigas guardavam os bytes no banco (S3Url vazio) — nada a apagar no S3.
			if (saved && !string.IsNullOrWhiteSpace(s3Url))
				await _s3Service.DeleteImageAsync(s3Url);

			return saved;
		}

		/// <summary>
		/// Escopo leve (projeção, sem carregar o grafo do relatório) para autorizar operações sobre fotos.
		/// </summary>
		public async Task<List<RelatorioFotoEscopoDTO>> GetFotoEscopos(List<int> fotoIds)
		{
			return await _unitOfWork.Relatorios.GetFotoEscopos(fotoIds.Distinct().ToList());
		}


		public async Task<RelatorioComentarioDTO> AddComentario(int secaoId, AddComentarioRequest req)
		{
			// [v10] Comentário defensivo com diagnóstico completo
			try
			{
				if (secaoId <= 0) throw new Exception("secaoId inválido.");
				if (req == null) throw new Exception("Payload inválido.");
				if (req.AutorId <= 0) throw new Exception("Autor inválido (token sem UserId). Faça login novamente.");
				if (string.IsNullOrWhiteSpace(req.Texto)) throw new Exception("Texto do comentário é obrigatório.");

				var secao = await _unitOfWork.Relatorios.GetSecaoById(secaoId);
				if (secao == null) throw new Exception($"Seção {secaoId} não encontrada.");

				var autor = await _unitOfWork.Users.GetUserSafeById(req.AutorId);
				if (autor == null) throw new Exception($"Usuário autor (id={req.AutorId}) não encontrado.");

				var comentario = new RelatorioComentario
				{
					RelatorioSecaoId = secaoId,
					AutorId = req.AutorId,
					Texto = req.Texto.Trim()
				};

				try
				{
					await _unitOfWork.Relatorios.AddComentario(comentario);
					_unitOfWork.Save();
				}
				catch (Exception saveEx)
				{
					var inner = saveEx.InnerException?.Message ?? saveEx.Message;
					throw new Exception($"[v10] Falha ao gravar comentário: {inner}");
				}

				var saved = await _unitOfWork.Relatorios.GetComentarioById(comentario.Id);

				try
				{
					var relatorioDoComentario = await _unitOfWork.Relatorios.GetById(secao.RelatorioId);
					var obraIdReal = relatorioDoComentario?.ObraId;
					await _atividadeService.Registrar(
							req.AutorId,
							TipoAtividade.ComentarioAdicionado,
							"Você adicionou um comentário em um relatório.",
							obraIdReal,
							secaoId);
				}
				catch { /* atividade é best-effort */ }

				return MapComentarioToDTO(saved!);
			}
			catch (Exception ex)
			{
				var prefix = ex.Message.StartsWith("[v10]") ? "" : "[v10] ";
				throw new Exception(prefix + ex.Message, ex);
			}
		}

		public async Task<bool> UpdateComentario(int comentarioId, UpdateComentarioRequest req)
		{
			var comentario = await _unitOfWork.Relatorios.GetComentarioById(comentarioId);
			if (comentario == null) throw new Exception("Comentário não encontrado.");

			comentario.Texto = req.Texto.Trim();
			_unitOfWork.Relatorios.UpdateComentario(comentario);
			return _unitOfWork.Save() > 0;
		}

		public async Task<bool> DeleteComentario(int comentarioId)
		{
			var comentario = await _unitOfWork.Relatorios.GetComentarioById(comentarioId);
			if (comentario == null) throw new Exception("Comentário não encontrado.");

			_unitOfWork.Relatorios.DeleteComentario(comentario);
			return _unitOfWork.Save() > 0;
		}

		private static void ValidarTransicaoStatus(StatusRelatorio atual, StatusRelatorio novo)
		{
			var transicoesPermitidas = new Dictionary<StatusRelatorio, HashSet<StatusRelatorio>>
			{
				[StatusRelatorio.Rascunho] = [StatusRelatorio.Submetido],
				[StatusRelatorio.Submetido] = [StatusRelatorio.Aprovado, StatusRelatorio.Rejeitado],
				[StatusRelatorio.Rejeitado] = [StatusRelatorio.Submetido],
				[StatusRelatorio.Aprovado] = [],
			};

			if (!transicoesPermitidas.TryGetValue(atual, out var permitidos) || !permitidos.Contains(novo))
				throw new Exception($"Transição de status inválida: {atual} → {novo}.");
		}

		private async Task<List<RelatorioSecao>> ParseSecoesDoHtml(string html, Obras obra)
		{
			var secoes = new List<RelatorioSecao>();
			// Tipos únicos (Local, MaoDeObra, Equipamentos, Comentarios, Ocorrencias) deduplicam por tipo;
			// os configuráveis (4,5,8,9,10,11) deduplicam pelo DataSecao ("<tipo>" ou "<tipo>:<campo>").
			var tiposUnicosVistos = new HashSet<TipoSecao>();
			var dataSecoesVistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var doc = new HtmlDocument();
			doc.LoadHtml(html ?? string.Empty);

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
					if (!tiposUnicosVistos.Add(tipoSecao))
						continue;

					var (secoesOcorrencias, proximaOrdem) = await BuildSecoesOcorrencias(obra, ordem);
					secoes.AddRange(secoesOcorrencias);
					ordem = proximaOrdem;
					continue;
				}

				var configuravel = RelatorioSecaoConfig.IsConfiguravel(tipoSecao);
				string chaveDataSecao = dataSecao;
				if (configuravel)
				{
					var campo = RelatorioSecaoConfig.NormalizarCampo(node.GetAttributeValue("data-campo", null));
					if (campo != null) chaveDataSecao = $"{dataSecao}:{campo}";
					if (!dataSecoesVistas.Add(chaveDataSecao))
						continue;
				}
				else if (!tiposUnicosVistos.Add(tipoSecao))
				{
					continue;
				}

				var titulo = LerAtributoTexto(node, "data-titulo", 200);
				if (titulo == null && dataSecao == "observacao") titulo = "Observações";

				var secao = new RelatorioSecao
				{
					DataSecao = chaveDataSecao,
					TipoSecao = tipoSecao,
					Ordem = ordem++,
					Titulo = titulo,
					Itens = new List<RelatorioSecaoItem>()
				};

				if (configuravel)
				{
					// data-config chega HTML-escaped no atributo; JSON inválido/grande vira config vazia.
					var rawConfig = node.GetAttributeValue("data-config", null);
					if (rawConfig != null) rawConfig = HtmlEntity.DeEntitize(rawConfig);
					secao.ConteudoJson = RelatorioSecaoConfig.Normalizar(tipoSecao, rawConfig);

					var cfg = RelatorioSecaoConfig.Parse(secao.ConteudoJson);
					if (tipoSecao == TipoSecao.Checklist)
						secao.ConteudoJson = await BuildChecklistConteudo(obra.Id, cfg);
					else
						secao.Itens = RelatorioSecaoConfig.ItensPadrao(tipoSecao, cfg);
				}

				switch (tipoSecao)
				{
					case TipoSecao.Local:
						secao.ConteudoJson = SerializeLocal(new
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

					case TipoSecao.Comentarios:
						break;
				}

				secoes.Add(secao);
			}

			// Garante que TODA relatório tenha uma seção de Comentários no final, mesmo que
			// o modelo HTML não declare data-secao="comentarios". É o local oficial onde admin
			// e gerente trocam observações sobre o relatório durante a aprovação.
			if (!tiposUnicosVistos.Contains(TipoSecao.Comentarios))
			{
				secoes.Add(new RelatorioSecao
				{
					DataSecao = "comentarios",
					TipoSecao = TipoSecao.Comentarios,
					Ordem = ordem++,
					Itens = new List<RelatorioSecaoItem>()
				});
			}

			return secoes;
		}

		/// <summary>Atributo de texto do modelo (HTML-decoded, trim, limitado). <c>null</c> se vazio.</summary>
		private static string? LerAtributoTexto(HtmlNode node, string atributo, int max)
		{
			var v = node.GetAttributeValue(atributo, null);
			if (v == null) return null;
			v = HtmlEntity.DeEntitize(v).Trim();
			if (v.Length == 0) return null;
			return v.Length > max ? v[..max] : v;
		}

		/// <summary>
		/// [v2] Config da seção Checklist: mantém a config do modelo e acrescenta <c>obraChecklistIds</c>
		/// com as execuções existentes na obra (filtradas por checklistId quando informado).
		/// </summary>
		private async Task<string?> BuildChecklistConteudo(int obraId, System.Text.Json.Nodes.JsonObject? cfg)
		{
			var obj = cfg ?? new System.Text.Json.Nodes.JsonObject();
			try
			{
				var checklistId = RelatorioSecaoConfig.ChecklistId(cfg);
				var ids = await _unitOfWork.ObraChecklists.GetIdsByObra(obraId, checklistId);
				if (ids.Count > 0)
				{
					var arr = new System.Text.Json.Nodes.JsonArray();
					foreach (var id in ids) arr.Add(id);
					obj["obraChecklistIds"] = arr;
				}
			}
			catch
			{
				// best-effort: a lista é opcional (o front busca as execuções da obra).
			}
			return obj.Count == 0 ? null : obj.ToJsonString();
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

		private static RelatorioComentarioDTO MapComentarioToDTO(RelatorioComentario c) => new()
		{
			Id = c.Id,
			RelatorioSecaoId = c.RelatorioSecaoId,
			AutorId = c.AutorId,
			AutorNome = c.Autor?.Name,
			Texto = c.Texto,
			CreatedDate = c.CreatedDate
		};

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
			HtmlSnapshot = r.HtmlSnapshot??r.ModeloTexto.Texto,
			ObservacaoRejeicao = r.ObservacaoRejeicao,
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
				// [v2] título por seção
				Titulo = s.Titulo,
				Itens = s.Itens?.Select(i => new RelatorioSecaoItemDTO
				{
					Id = i.Id,
					RelatorioSecaoId = i.RelatorioSecaoId,
					ReferenciaId = i.ReferenciaId,
					Nome = i.Nome,
					Descricao = i.Descricao,
					Fotos = i.Fotos?.OrderBy(f => f.Ordem).ThenBy(f => f.Id).Select(f => new RelatorioItemFotoDTO
					{
						Id = f.Id,
						RelatorioSecaoItemId = f.RelatorioSecaoItemId,
						ContentType = f.ContentType,
						NomeArquivo = f.NomeArquivo,
						ImagemBase64 = f.ImagemBytes!=null? Convert.ToBase64String(f.ImagemBytes) : null,
						S3Url=f.S3Url,
						Legenda = f.Legenda,
						Ordem = f.Ordem,
						CreatedDate = f.CreatedDate,
					}).ToList() ?? new()
				}).ToList() ?? new(),
				Comentarios = s.Comentarios?.Select(MapComentarioToDTO).ToList() ?? new()
			}).ToList() ?? new(),

			EmpresaNome = r.Obra?.Empresa?.Name,
			EmpresaTelefone = r.Obra?.Empresa?.Phone,
			EmpresaEmail = r.Obra?.Empresa?.ContactEmail,
			EmpresaLogoBase64 = r.Obra?.Empresa?.LogoBase64,
			EmpresaLogoContentType = r.Obra?.Empresa?.LogoContentType
		};
		public async Task<bool> UpdateHtmlSnapshot(int id, string htmlSnapshot)
		{
			try
			{
				var relatorio = await _unitOfWork.Relatorios.GetById(id);//_context.Relatorios.FindAsync(id);
				if (relatorio == null) return false;

				relatorio.HtmlSnapshot = htmlSnapshot;
				relatorio.UpdatedDate = DateTime.UtcNow;
				_unitOfWork.Relatorios.Update(relatorio);
				_unitOfWork.Save();
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		// =====================================================================
		// [v2] Bulk update — Big Bang Relatórios
		// =====================================================================
		// Atualiza título do relatório + metadados das seções (Titulo, Ordem,
		// ConteudoJson, TipoOcorrenciaId) numa única transação. Seções com Id
		// existente são atualizadas; sem Id são criadas. Seções existentes no
		// banco que não vierem no payload são MANTIDAS (sem delete implícito).
		// Para deletar use o endpoint granular existente (delete/{id}) ou
		// adicione um marcador "deleted: true" no payload futuramente.
		// =====================================================================
		// [v2] Fotos: legenda e ordem
		// =====================================================================

		private const int MaxLegendaFoto = 300;

		private static string? LimitarTexto(string? s, int max)
		{
			if (string.IsNullOrWhiteSpace(s)) return null;
			var t = s.Trim();
			return t.Length > max ? t[..max] : t;
		}

		public async Task<bool> UpdateFoto(int fotoId, UpdateRelatorioFotoRequest req)
		{
			var foto = await _unitOfWork.Relatorios.GetFotoById(fotoId);
			if (foto == null) throw new KeyNotFoundException("Foto não encontrada.");
			if (req.LegendaInformada) foto.Legenda = LimitarTexto(req.Legenda, MaxLegendaFoto);
			if (req.Ordem.HasValue) foto.Ordem = Math.Max(0, req.Ordem.Value);
			foto.UpdatedDate = DateTime.UtcNow;
			_unitOfWork.Relatorios.UpdateFoto(foto);
			return _unitOfWork.Save() >= 0;
		}

		/// <summary>Ordem = posição em <paramref name="fotoIds"/>; fotos não listadas vão para o fim.</summary>
		public async Task<bool> ReorderFotos(int itemId, List<int> fotoIds)
		{
			var item = await _unitOfWork.Relatorios.GetItemById(itemId);
			if (item == null) throw new KeyNotFoundException("Item não encontrado.");
			var porId = item.Fotos.ToDictionary(f => f.Id);
			var ids = (fotoIds ?? new List<int>()).Distinct().ToList();
			if (ids.Any(id => !porId.ContainsKey(id))) throw new Exception("Há fotos que não pertencem a este item.");

			var ordem = 0;
			foreach (var id in ids) porId[id].Ordem = ordem++;
			foreach (var resto in item.Fotos.Where(f => !ids.Contains(f.Id)).OrderBy(f => f.Ordem).ThenBy(f => f.Id))
				resto.Ordem = ordem++;
			_unitOfWork.Save();
			return true;
		}

		// =====================================================================
		// [v2] Duplicar relatório (ex.: RDO do dia seguinte)
		// =====================================================================

		private static readonly HashSet<TipoSecao> TiposCopiaveis = new()
		{
			TipoSecao.TextoLivre, TipoSecao.Formulario, TipoSecao.Clima, TipoSecao.MaoDeObra, TipoSecao.Equipamentos,
		};

		public async Task<int> Duplicar(int id, DuplicarRelatorioRequest req, int criadoPorUserId, int empresaIdJwt)
		{
			var original = await _unitOfWork.Relatorios.GetById(id);
			if (original == null || original.Obra?.EmpresaId != empresaIdJwt) throw new KeyNotFoundException("Relatório não encontrado.");

			var titulo = string.IsNullOrWhiteSpace(req?.Titulo) ? $"{original.Titulo} (cópia)" : req!.Titulo!.Trim();
			if (titulo.Length > 300) titulo = titulo[..300];

			var novo = await CreateInternal(new CreateRelatorioRequest
			{
				ModeloTextoId = original.ModeloTextoId,
				ObraId = original.ObraId,
				CriadoPorUserId = criadoPorUserId,
				Titulo = titulo,
				DataRelatorio = req?.DataRelatorio,
			}, criadoPorUserId, empresaIdJwt);

			// Copia os valores de texto por DataSecao + item (ReferenciaId para mão de obra/equipamentos,
			// senão Nome; por último a posição). Fotos, assinaturas e comentários não são copiados.
			var origemPorChave = original.Secoes
				.Where(sec => TiposCopiaveis.Contains(sec.TipoSecao))
				.GroupBy(sec => sec.DataSecao ?? string.Empty, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

			var alterou = false;
			foreach (var destino in novo.Secoes.Where(sec => TiposCopiaveis.Contains(sec.TipoSecao)))
			{
				if (!origemPorChave.TryGetValue(destino.DataSecao ?? string.Empty, out var origem)) continue;
				var itensOrigem = origem.Itens.OrderBy(i => i.Id).ToList();
				var usados = new HashSet<int>();
				var destinos = destino.Itens.ToList();
				for (var idx = 0; idx < destinos.Count; idx++)
				{
					var d = destinos[idx];
					RelatorioSecaoItem? o = null;
					if (d.ReferenciaId.HasValue)
						o = itensOrigem.FirstOrDefault(x => x.ReferenciaId == d.ReferenciaId && !usados.Contains(x.Id));
					o ??= itensOrigem.FirstOrDefault(x => !usados.Contains(x.Id) && string.Equals(x.Nome ?? "", d.Nome ?? "", StringComparison.OrdinalIgnoreCase) && !d.ReferenciaId.HasValue && !x.ReferenciaId.HasValue);
					if (o == null && !d.ReferenciaId.HasValue && idx < itensOrigem.Count && !usados.Contains(itensOrigem[idx].Id) && !itensOrigem[idx].ReferenciaId.HasValue)
						o = itensOrigem[idx];
					if (o == null || string.IsNullOrEmpty(o.Descricao)) continue;
					usados.Add(o.Id);
					d.Descricao = o.Descricao;
					alterou = true;
				}
			}

			if (alterou) _unitOfWork.Save();


			return novo.Id;
		}

		// =====================================================================
		// [v2] Pendências de preenchimento (obrigatórios do modelo)
		// =====================================================================

		private static List<PendenciaRelatorioDTO> CalcularPendencias(Relatorio relatorio)
		{
			var pendencias = new List<PendenciaRelatorioDTO>();
			foreach (var secao in (relatorio.Secoes ?? new List<RelatorioSecao>()).OrderBy(x => x.Ordem))
			{
				if (!RelatorioSecaoConfig.IsConfiguravel(secao.TipoSecao)) continue;
				var cfg = RelatorioSecaoConfig.Parse(secao.ConteudoJson);
				var titulo = string.IsNullOrWhiteSpace(secao.Titulo) ? RelatorioSecaoConfig.TituloPadrao(secao.TipoSecao) : secao.Titulo!;
				void Add(string motivo) => pendencias.Add(new PendenciaRelatorioDTO { SecaoId = secao.Id, Titulo = titulo, Motivo = motivo });
				var itens = secao.Itens.OrderBy(i => i.Id).ToList();

				switch (secao.TipoSecao)
				{
					case TipoSecao.TextoLivre:
						if (RelatorioSecaoConfig.Obrigatorio(cfg) && itens.All(i => RelatorioSecaoConfig.TextoVazio(i.Descricao)))
							Add("Preencha este campo.");
						break;

					case TipoSecao.Formulario:
						var campos = RelatorioSecaoConfig.Campos(cfg);
						for (var idx = 0; idx < campos.Count; idx++)
						{
							if (!campos[idx].Obrigatorio) continue;
							var campoItem = idx < itens.Count && string.Equals(itens[idx].Nome ?? "", campos[idx].Label, StringComparison.OrdinalIgnoreCase)
								? itens[idx]
								: itens.FirstOrDefault(i => string.Equals(i.Nome ?? "", campos[idx].Label, StringComparison.OrdinalIgnoreCase));
							if (campoItem == null || string.IsNullOrWhiteSpace(campoItem.Descricao))
								Add($"Preencha '{campos[idx].Label}'.");
						}
						break;

					case TipoSecao.Clima:
						if (!RelatorioSecaoConfig.Obrigatorio(cfg)) break;
						foreach (var periodo in itens)
							if (string.IsNullOrWhiteSpace(RelatorioSecaoConfig.LerCampoValor(periodo.Descricao, "tempo")))
								Add($"Informe o tempo de '{periodo.Nome ?? "período"}'.");
						break;

					case TipoSecao.Assinatura:
						if (!RelatorioSecaoConfig.Obrigatorio(cfg)) break;
						foreach (var assinante in itens)
							if (assinante.Fotos.Count == 0)
								Add($"Falta a assinatura de '{assinante.Nome ?? "responsável"}'.");
						break;

					case TipoSecao.Fotos:
						var totalFotos = itens.Sum(i => i.Fotos.Count);
						var min = RelatorioSecaoConfig.MinFotos(cfg);
						if (min.HasValue && min.Value > 0 && totalFotos < min.Value)
							Add(min.Value == 1 ? "Adicione pelo menos 1 foto." : $"Adicione pelo menos {min.Value} fotos.");
						if (RelatorioSecaoConfig.GetBool(cfg, "exigirLegenda") && itens.SelectMany(i => i.Fotos).Any(f => string.IsNullOrWhiteSpace(f.Legenda)))
							Add("Todas as fotos precisam de legenda.");
						break;
				}
			}
			return pendencias;
		}

		public async Task<bool> UpdateV2(int id, UpdateRelatorioV2Request req)
		{
			try
			{
				if (id <= 0 || req == null) return false;

				var relatorio = await _unitOfWork.Relatorios.GetById(id);
				if (relatorio == null) return false;

				// 1) Título do relatório (só atualiza se vier preenchido — null/empty mantém o atual)
				if (!string.IsNullOrWhiteSpace(req.Titulo))
				{
					relatorio.Titulo = req.Titulo.Trim();
				}
				relatorio.UpdatedDate = DateTime.UtcNow;
				_unitOfWork.Relatorios.Update(relatorio);

				// 2) Seções: update por Id, ou create se Id=null/0
				if (req.Secoes != null)
				{
					var existentesPorId = relatorio.Secoes.ToDictionary(s => s.Id);

					// Multi-tenant: tipo de ocorrência só pode ser da empresa dona do relatório
					// (senão o nome de um tipo de outra empresa apareceria no documento).
					var empresaDoRelatorio = relatorio.Obra?.EmpresaId;
					var tiposValidos = new Dictionary<int, bool>();
					foreach (var tipoId in req.Secoes.Where(x => x.TipoOcorrenciaId.HasValue).Select(x => x.TipoOcorrenciaId!.Value).Distinct())
					{
						var tipo = await _unitOfWork.TiposOcorrencia.GetTipoById(tipoId);
						tiposValidos[tipoId] = tipo != null && empresaDoRelatorio.HasValue && tipo.EmpresaId == empresaDoRelatorio.Value;
					}
					foreach (var sReq in req.Secoes)
					{
						if (sReq.TipoOcorrenciaId.HasValue && !tiposValidos.GetValueOrDefault(sReq.TipoOcorrenciaId.Value))
							sReq.TipoOcorrenciaId = null;
					}

					foreach (var sReq in req.Secoes)
					{
						if (sReq.Id.HasValue && sReq.Id.Value > 0 && existentesPorId.TryGetValue(sReq.Id.Value, out var existente))
						{
							// UPDATE
							if (RelatorioSecaoConfig.ExcedeLimite(sReq.ConteudoJson))
								throw new Exception("Configuração da seção muito grande.");
							existente.Titulo = sReq.Titulo;
							existente.Ordem = sReq.Ordem;
							existente.ConteudoJson = sReq.ConteudoJson;
							existente.TipoOcorrenciaId = sReq.TipoOcorrenciaId;
							if (!string.IsNullOrWhiteSpace(sReq.DataSecao))
								existente.DataSecao = sReq.DataSecao;
							existente.UpdatedDate = DateTime.UtcNow;
						}
						else
						{
							// CREATE
							var nova = new RelatorioSecao
							{
								RelatorioId = relatorio.Id,
								DataSecao = string.IsNullOrWhiteSpace(sReq.DataSecao)
									? sReq.TipoSecao.ToString().ToLower()
									: sReq.DataSecao,
								TipoSecao = sReq.TipoSecao,
								Ordem = sReq.Ordem,
								Titulo = sReq.Titulo,
								ConteudoJson = sReq.ConteudoJson,
								TipoOcorrenciaId = sReq.TipoOcorrenciaId,
							};
							await _unitOfWork.Relatorios.AddSecao(nova);

							// [v12] Seções de Fotos precisam de um item raiz pra ancorar as fotos.
							// [v2] Demais configuráveis (texto, clima, assinatura, formulário) recebem os
							// itens definidos pela config (períodos, assinantes, campos).
							if (RelatorioSecaoConfig.IsConfiguravel(sReq.TipoSecao) && sReq.TipoSecao != TipoSecao.Checklist)
							{
								nova.ConteudoJson = RelatorioSecaoConfig.Normalizar(sReq.TipoSecao, sReq.ConteudoJson) ?? sReq.ConteudoJson;
								// Salva primeiro pra ter o ID da seção
								_unitOfWork.Save();
								var cfgNova = RelatorioSecaoConfig.Parse(nova.ConteudoJson);
								foreach (var itemPadrao in RelatorioSecaoConfig.ItensPadrao(sReq.TipoSecao, cfgNova))
								{
									itemPadrao.RelatorioSecaoId = nova.Id;
									await _unitOfWork.Relatorios.AddItem(itemPadrao);
								}
							}
						}
					}
				}

				_unitOfWork.Save();
				return true;
			}
			catch (Exception ex)
			{
				// Log via console — em produção o middleware já captura.
				System.Console.Error.WriteLine($"[RelatorioService.UpdateV2] erro: {ex.Message}");
				return false;
			}
		}
	}

	public interface IRelatorioService
	{
		Task<Relatorio> Create(CreateRelatorioRequest req);
		Task<Relatorio> Create(CreateRelatorioRequest req, int criadoPorUserIdJwt, int empresaIdJwt);
		Task<RelatorioDTO?> GetById(int id);
		Task<RelatorioDTO?> GetByIdScoped(int id, int empresaIdJwt);
		Task<RelatorioDTO?> GetRelatorioByItemId(int itemId, int empresaIdJwt);
		Task<RelatorioDTO?> GetRelatorioByFotoId(int fotoId, int empresaIdJwt);
		Task<RelatorioDTO?> GetRelatorioBySecaoId(int secaoId, int empresaIdJwt);
		Task<(RelatorioDTO? relatorio, int? autorComentarioId)> GetRelatorioAndAutorByComentarioId(int comentarioId, int empresaIdJwt);
		Task<RelatorioPagedDTO> GetPaged(FiltersRelatorioDTO filters);
		Task<bool> UpdateStatus(int id, UpdateRelatorioStatusRequest req);
		Task<bool> Delete(int id);
		Task<bool> UpdateItem(int itemId, UpdateRelatorioSecaoItemRequest req);
		Task<bool> UpdateFoto(int fotoId, UpdateRelatorioFotoRequest req);
		Task<bool> ReorderFotos(int itemId, List<int> fotoIds);
		Task<int> Duplicar(int id, DuplicarRelatorioRequest req, int criadoPorUserId, int empresaIdJwt);
		Task<bool> AddFotoToItem(int itemId, AddFotoToItemRequest req);
		Task<bool> DeleteFoto(int fotoId);
		Task<RelatorioComentarioDTO> AddComentario(int secaoId, AddComentarioRequest req);
		Task<bool> UpdateComentario(int comentarioId, UpdateComentarioRequest req);
		Task<bool> DeleteComentario(int comentarioId);
		Task<bool> AddMultipleFotosToItem(int itemId, List<AddFotoToItemRequest> fotos);
		Task<bool> DeleteMultipleFotos(List<int> fotoIds);
		Task<List<RelatorioFotoEscopoDTO>> GetFotoEscopos(List<int> fotoIds);
		Task<bool> UpdateHtmlSnapshot(int id, string htmlSnapshot);
		// [v2] Bulk update — título + seções num único PUT
		Task<bool> UpdateV2(int id, UpdateRelatorioV2Request req);
		// [v12] Garante item raiz numa seção de Fotos e devolve o ID
		Task<int?> EnsureFotoItemRaiz(int secaoId);
	}
}