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

			var secoes = await ParseSecoesDoHtml(modelo.Texto, obra);

			var relatorio = new Relatorio
			{
				ModeloTextoId = req.ModeloTextoId,
				ObraId = req.ObraId,
				CriadoPorUserId = criadoPorUserId,
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
				Result = paged.Results.Select(MapToDTO).ToList()
			};
		}

		public async Task<bool> UpdateStatus(int id, UpdateRelatorioStatusRequest req)
		{
			var relatorio = await _unitOfWork.Relatorios.GetById(id);
			if (relatorio == null) throw new Exception("Relatório não encontrado.");

			ValidarTransicaoStatus(relatorio.Status, req.Status);

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
		public async Task<bool> AddMultipleFotosToItem(int itemId, List<AddFotoToItemRequest> fotos)
		{
			var item = await _unitOfWork.Relatorios.GetItemById(itemId);
			if (item == null) throw new Exception("Item não encontrado.");

			foreach (var f in fotos)
			{
				try
				{
					// Converter Base64 para bytes
					var imageBytes = Convert.FromBase64String(f.ImagemBase64);

					// Validar tamanho da imagem (ex: máximo 10MB)
					//if (imageBytes.Length > 10 * 1024 * 1024)
					//{
					//	_logger.LogWarning("Imagem muito grande: {NomeArquivo}, Tamanho: {Tamanho} bytes",
					//			f.NomeArquivo, imageBytes.Length);
					//	continue; // Pular esta imagem
					//}

					// Upload para S3
					var s3Url = await _s3Service.UploadImageAsync(imageBytes, f.NomeArquivo, f.ContentType);

					var foto = new RelatorioItemFoto
					{
						RelatorioSecaoItemId = itemId,
						S3Url = s3Url,
						ContentType = f.ContentType,
						NomeArquivo = f.NomeArquivo,
					};

					await _unitOfWork.Relatorios.AddFoto(foto);
				}
				catch (Exception ex)
				{
				
					throw;
				}
			}

			return  _unitOfWork.Save() > 0;
		}

		public async Task<bool> DeleteMultipleFotos(List<int> fotoIds)
		{
			foreach (var id in fotoIds)
			{
				var foto = await _unitOfWork.Relatorios.GetFotoById(id);
				if (foto == null)
				{
					//_logger.LogWarning("Foto não encontrada: {FotoId}", id);
					continue;
				}

				try
				{
					// Deletar do S3
					await _s3Service.DeleteImageAsync(foto.S3Url);

					// Deletar do banco
					_unitOfWork.Relatorios.DeleteFoto(foto);
				}
				catch (Exception ex)
				{
					//_logger.LogError(ex, "Erro ao deletar foto {FotoId} do S3", id);
					throw;
				}
			}

			return  _unitOfWork.Save() > 0;
		}
		public async Task<bool> DeleteFoto(int fotoId)
		{
			var foto = await _unitOfWork.Relatorios.GetFotoById(fotoId);
			if (foto == null) throw new Exception("Foto não encontrada.");

			_unitOfWork.Relatorios.DeleteFoto(foto);
			return _unitOfWork.Save() > 0;
		}
		//public async Task<bool> DeleteMultipleFotos(List<int> fotoIds)
		//{
		//	foreach (var id in fotoIds)
		//	{
		//		var foto = await _unitOfWork.Relatorios.GetFotoById(id);
		//		if (foto == null) throw new Exception("Foto não encontrada.");

		//		_unitOfWork.Relatorios.DeleteFoto(foto);
		//	}
		//	return _unitOfWork.Save() > 0;
		//}


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
						secao.Itens = [new RelatorioSecaoItem { Nome = null, Descricao = null }];
						break;

					case TipoSecao.Fotos:
						secao.Itens = [new RelatorioSecaoItem { Nome = null, Descricao = null }];
						break;

					case TipoSecao.Comentarios:
						break;
				}

				secoesMap[tipoSecao] = secao;
				secoes.Add(secao);
			}

			// Garante que TODA relatório tenha uma seção de Comentários no final, mesmo que
			// o modelo HTML não declare data-secao="comentarios". É o local oficial onde admin
			// e gerente trocam observações sobre o relatório durante a aprovação.
			if (!secoesMap.ContainsKey(TipoSecao.Comentarios))
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
					Fotos = i.Fotos?.Select(f => new RelatorioItemFotoDTO
					{
						Id = f.Id,
						RelatorioSecaoItemId = f.RelatorioSecaoItemId,
						ContentType = f.ContentType,
						NomeArquivo = f.NomeArquivo,
						ImagemBase64 = f.ImagemBytes!=null? Convert.ToBase64String(f.ImagemBytes) : null,
						S3Url=f.S3Url,
					}).ToList() ?? new()
				}).ToList() ?? new(),
				Comentarios = s.Comentarios?.Select(MapComentarioToDTO).ToList() ?? new()
			}).ToList() ?? new(),

			EmpresaNome = r.Obra?.Empresa?.Name,
			EmpresaTelefone = r.Obra?.Empresa?.Phone,
			EmpresaEmail = r.Obra?.Empresa?.ContactEmail
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

					foreach (var sReq in req.Secoes)
					{
						if (sReq.Id.HasValue && sReq.Id.Value > 0 && existentesPorId.TryGetValue(sReq.Id.Value, out var existente))
						{
							// UPDATE
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

							// [v12] Seções de Fotos precisam de um item raiz pra ancorar
							// as fotos. Sem isso, secao.itens[0] fica vazio e o operador
							// não consegue fazer upload.
							if (sReq.TipoSecao == TipoSecao.Fotos)
							{
								// Salva primeiro pra ter o ID da seção
								_unitOfWork.Save();
								var itemRaiz = new RelatorioSecaoItem
								{
									RelatorioSecaoId = nova.Id,
									Nome = "Fotos",
									Descricao = null,
								};
								await _unitOfWork.Relatorios.AddItem(itemRaiz);
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
		Task<bool> AddFotoToItem(int itemId, AddFotoToItemRequest req);
		Task<bool> DeleteFoto(int fotoId);
		Task<RelatorioComentarioDTO> AddComentario(int secaoId, AddComentarioRequest req);
		Task<bool> UpdateComentario(int comentarioId, UpdateComentarioRequest req);
		Task<bool> DeleteComentario(int comentarioId);
		Task<bool> AddMultipleFotosToItem(int itemId, List<AddFotoToItemRequest> fotos);
		Task<bool> DeleteMultipleFotos(List<int> fotoIds);
		Task<bool> UpdateHtmlSnapshot(int id, string htmlSnapshot);
		// [v2] Bulk update — título + seções num único PUT
		Task<bool> UpdateV2(int id, UpdateRelatorioV2Request req);
		// [v12] Garante item raiz numa seção de Fotos e devolve o ID
		Task<int?> EnsureFotoItemRaiz(int secaoId);
	}
}