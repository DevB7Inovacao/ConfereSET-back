using System.Globalization;
using Core.DTO;
using Core.Enums;
using Core.Models;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Services
{
	public class ObraChecklistService : IObraChecklistService
	{
		private const int MaxValorChars = 2000;
		private const int MaxLegendaChars = 300;
		private const int MaxFotosPorChamada = 10;
		private const int MaxFotosPorItem = 30;
		private const int MaxTextoGeral = 4000;

		private readonly IUnitOfWork _unitOfWork;
		private readonly IS3Service _s3Service;
		private readonly ILogger<ObraChecklistService>? _logger;

		public ObraChecklistService(IUnitOfWork unitOfWork, IS3Service s3Service, ILogger<ObraChecklistService>? logger = null)
		{
			_unitOfWork = unitOfWork;
			_s3Service = s3Service;
			_logger = logger;
		}

		// =====================================================================
		// Vínculo / rodadas
		// =====================================================================

		public async Task<ObraChecklistDTO> AddChecklistToObra(AddChecklistToObraRequest req)
		{
			var obra = await _unitOfWork.Obras.GetObraById(req.ObraId);
			if (obra == null) throw new Exception("Obra não encontrada.");

			var checklist = await _unitOfWork.Checklists.GetById(req.ChecklistId);
			if (checklist == null) throw new Exception("Checklist não encontrado.");

			// [v2] Nova rodada só quando todas as execuções anteriores estiverem concluídas.
			if (await _unitOfWork.ObraChecklists.ExistsEmAndamento(req.ObraId, req.ChecklistId))
				throw new Exception("Já existe uma execução em andamento deste checklist nesta obra.");

			var jaTeveExecucao = await _unitOfWork.ObraChecklists.Exists(req.ObraId, req.ChecklistId);
			var titulo = string.IsNullOrWhiteSpace(req.Titulo)
				? (jaTeveExecucao ? $"{checklist.Nome} – {DateTime.Now:dd/MM/yyyy}" : null)
				: Limitar(req.Titulo, 200);

			var obraChecklist = new ObraChecklist
			{
				ObraId = req.ObraId,
				ChecklistId = req.ChecklistId,
				Status = 1,
				Titulo = titulo,
			};

			await _unitOfWork.ObraChecklists.Add(obraChecklist);
			_unitOfWork.Save();

			var itens = await _unitOfWork.ChecklistItems.GetByChecklist(req.ChecklistId);

			foreach (var item in itens.Where(i => i.Status == 1))
			{
				await _unitOfWork.ObraChecklistItems.Add(new ObraChecklistItem
				{
					ObraChecklistId = obraChecklist.Id,
					ChecklistItemId = item.Id,
					Resposta = 0
				});
			}

			_unitOfWork.Save();

			return await GetById(obraChecklist.Id);
		}

		/// <summary>[v2] O checklist já foi aplicado (alguma rodada) nesta obra?</summary>
		public Task<bool> JaAplicadoNaObra(int obraId, int checklistId)
			=> _unitOfWork.ObraChecklists.Exists(obraId, checklistId);

		public async Task<ObraChecklistDTO> GetById(int id)
		{
			var obraChecklist = await _unitOfWork.ObraChecklists.GetById(id);
			if (obraChecklist == null) throw new Exception("Vínculo de checklist não encontrado.");

			return (await MapManyAsync(new List<ObraChecklist> { obraChecklist })).First();
		}

		public async Task<List<ObraChecklistDTO>> GetByObra(int obraId)
		{
			var list = await _unitOfWork.ObraChecklists.GetByObra(obraId);
			return await MapManyAsync(list);
		}

		public async Task<List<ObraChecklistDTO>> GetByObra(int obraId, int empresaIdJwt)
		{
			// Multi-tenant: confere se a obra pertence à empresa do JWT antes de devolver.
			var obra = await _unitOfWork.Obras.GetObraById(obraId);
			if (obra == null || obra.EmpresaId != empresaIdJwt) return new List<ObraChecklistDTO>();
			var list = await _unitOfWork.ObraChecklists.GetByObra(obraId);
			return await MapManyAsync(list);
		}

		// =====================================================================
		// Respostas
		// =====================================================================

		public Task<bool> ResponderItem(int obraChecklistItemId, ResponderChecklistItemRequest req)
			=> ResponderItem(obraChecklistItemId, req, null);

		/// <summary>
		/// Grava a resposta conforme o tipo do item (ver <see cref="TipoItemChecklist"/>).
		/// Somente Resposta/Valor — os metadados vão por <see cref="ResponderItensAdicionais"/>.
		/// </summary>
		public async Task<bool> ResponderItem(int obraChecklistItemId, ResponderChecklistItemRequest req, int? userId)
		{
			var item = await _unitOfWork.ObraChecklistItems.GetById(obraChecklistItemId);
			if (item == null) throw new Exception("Item não encontrado.");
			if (req == null) throw new Exception("Payload inválido.");

			var tipo = (TipoItemChecklist)(item.ChecklistItem?.Tipo ?? (int)TipoItemChecklist.ConformeNaoConformeNA);

			if (req.Resposta.HasValue && (req.Resposta.Value < 0 || req.Resposta.Value > 3))
				throw new Exception("Resposta inválida.");

			switch (tipo)
			{
				case TipoItemChecklist.ConformeNaoConformeNA:
				case TipoItemChecklist.SimNao:
					if (!req.Resposta.HasValue) throw new Exception("Informe a resposta.");
					item.Resposta = req.Resposta.Value;
					break;

				case TipoItemChecklist.Foto:
					// Respondido = ter foto. Só aceita marcar/desmarcar N/A manualmente.
					if (req.Resposta == 3) item.Resposta = 3;
					else item.Resposta = item.Fotos.Count > 0 ? 1 : 0;
					break;

				default:
					if (req.Resposta == 3)
					{
						item.Resposta = 3;
						item.Valor = null;
					}
					else if (req.Valor != null)
					{
						item.Valor = NormalizarValor(tipo, req.Valor, item.ChecklistItem?.Opcoes);
						item.Resposta = string.IsNullOrEmpty(item.Valor) ? 0 : 1;
					}
					else if (req.Resposta == 0)
					{
						item.Resposta = 0;
						item.Valor = null;
					}
					break;
			}

			MarcarRespondido(item, userId);
			_unitOfWork.ObraChecklistItems.Update(item);
			return _unitOfWork.Save() >= 0;
		}

		public async Task<bool> ResponderItensAdicionais(int obraChecklistItemId, ResponderChecklistItemRequest req)
		{
			var item = await _unitOfWork.ObraChecklistItems.GetById(obraChecklistItemId);
			if (item == null) throw new Exception("Item não encontrado.");

			// Metadados apenas — a Resposta é alterada exclusivamente pelo endpoint responder/{id}.
			item.Observacao = req.Observacao;
			item.Empresa = req.Empresa;
			item.DataHora = req.DataHora;
			item.Equipamento = req.Equipamento;
			item.Marca = req.Marca;
			_unitOfWork.ObraChecklistItems.Update(item);
			return _unitOfWork.Save() >= 0;
		}

		private static void MarcarRespondido(ObraChecklistItem item, int? userId)
		{
			if (item.Resposta == 0)
			{
				item.RespondidoEm = null;
				item.RespondidoPorUserId = null;
			}
			else
			{
				item.RespondidoEm = DateTime.UtcNow;
				if (userId.HasValue) item.RespondidoPorUserId = userId;
			}
		}

		private static string? NormalizarValor(TipoItemChecklist tipo, string valor, string? opcoesJson)
		{
			var v = valor.Trim();
			if (v.Length == 0) return null;
			if (v.Length > MaxValorChars) v = v[..MaxValorChars];

			switch (tipo)
			{
				case TipoItemChecklist.Numero:
					var normalizado = v.Replace(" ", "");
					// Aceita "12,5" (pt-BR) e "12.5".
					if (normalizado.Contains(',') && !normalizado.Contains('.')) normalizado = normalizado.Replace(',', '.');
					if (!double.TryParse(normalizado, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) || double.IsNaN(n) || double.IsInfinity(n))
						throw new Exception("Informe um número válido.");
					return n.ToString(CultureInfo.InvariantCulture);

				case TipoItemChecklist.Data:
					if (!DateTime.TryParseExact(v.Length >= 10 ? v[..10] : v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
						throw new Exception("Informe uma data válida.");
					return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

				case TipoItemChecklist.Selecao:
					var opcoes = ParseOpcoes(opcoesJson);
					if (opcoes.Count > 0 && !opcoes.Contains(v))
						throw new Exception("Opção inválida para este item.");
					return v;

				default:
					return v;
			}
		}

		private static List<string> ParseOpcoes(string? json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new List<string>();
			try
			{
				return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
			}
			catch
			{
				return new List<string>();
			}
		}

		// =====================================================================
		// Fotos dos itens
		// =====================================================================

		public async Task<List<ObraChecklistItemFotoDTO>> AddFotos(int obraChecklistItemId, List<AddChecklistItemFotoRequest> fotos, int? userId)
		{
			if (fotos == null || fotos.Count == 0) throw new Exception("Nenhuma foto enviada.");
			if (fotos.Count > MaxFotosPorChamada) throw new Exception($"Envie no máximo {MaxFotosPorChamada} fotos por vez.");

			var item = await _unitOfWork.ObraChecklistItems.GetById(obraChecklistItemId);
			if (item == null) throw new Exception("Item não encontrado.");
			if (item.Fotos.Count + fotos.Count > MaxFotosPorItem)
				throw new Exception($"Cada item aceita no máximo {MaxFotosPorItem} fotos.");

			// Valida tudo antes de enviar qualquer coisa ao S3.
			var validadas = fotos.Select(f => (Imagem: ImageValidation.Validar(f.ImagemBase64, f.NomeArquivo), f.Legenda)).ToList();

			var enviadas = new List<string>();
			var criadas = new List<ObraChecklistItemFoto>();
			try
			{
				foreach (var (img, legenda) in validadas)
				{
					var nome = $"checklist/{item.ObraChecklistId}/{item.Id}/{Guid.NewGuid():N}_{img.NomeArquivo}";
					var url = await _s3Service.UploadImageAsync(img.Bytes, nome, img.ContentType);
					enviadas.Add(url);

					var foto = new ObraChecklistItemFoto
					{
						ObraChecklistItemId = item.Id,
						S3Url = url,
						ContentType = img.ContentType,
						NomeArquivo = img.NomeArquivo,
						Legenda = Limitar(legenda, MaxLegendaChars),
						CriadoPorUserId = userId,
						CreatedDate = DateTime.UtcNow,
						UpdatedDate = DateTime.UtcNow,
					};
					await _unitOfWork.ObraChecklistItems.AddFoto(foto);
					criadas.Add(foto);
				}

				if ((item.ChecklistItem?.Tipo ?? 1) == (int)TipoItemChecklist.Foto && item.Resposta != 3)
				{
					item.Resposta = 1;
					MarcarRespondido(item, userId);
					_unitOfWork.ObraChecklistItems.Update(item);
				}

				_unitOfWork.Save();
			}
			catch (Exception ex)
			{
				// Tudo ou nada: remove do S3 o que já subiu.
				foreach (var url in enviadas)
				{
					try { await _s3Service.DeleteImageAsync(url); }
					catch (Exception delEx) { _logger?.LogWarning(delEx, "Falha ao remover foto órfã do S3: {Url}", url); }
				}
				_logger?.LogError(ex, "Falha ao salvar fotos do item {ItemId}", obraChecklistItemId);
				throw new Exception("Não foi possível salvar as fotos. Nenhuma foto foi gravada — tente enviar novamente.");
			}

			return criadas.Select(MapFoto).ToList();
		}

		public async Task<bool> UpdateFotoLegenda(int fotoId, string? legenda)
		{
			var foto = await _unitOfWork.ObraChecklistItems.GetFotoById(fotoId);
			if (foto == null) throw new KeyNotFoundException("Foto não encontrada.");
			foto.Legenda = Limitar(legenda, MaxLegendaChars);
			foto.UpdatedDate = DateTime.UtcNow;
			_unitOfWork.ObraChecklistItems.UpdateFoto(foto);
			return _unitOfWork.Save() >= 0;
		}

		public async Task<bool> DeleteFoto(int fotoId)
		{
			var foto = await _unitOfWork.ObraChecklistItems.GetFotoById(fotoId);
			if (foto == null) throw new KeyNotFoundException("Foto não encontrada.");

			var item = await _unitOfWork.ObraChecklistItems.GetById(foto.ObraChecklistItemId);
			var url = foto.S3Url;

			_unitOfWork.ObraChecklistItems.DeleteFoto(foto);
			if (item != null && (item.ChecklistItem?.Tipo ?? 1) == (int)TipoItemChecklist.Foto && item.Resposta == 1)
			{
				var restantes = item.Fotos.Count(f => f.Id != fotoId);
				if (restantes == 0)
				{
					item.Resposta = 0;
					MarcarRespondido(item, null);
					_unitOfWork.ObraChecklistItems.Update(item);
				}
			}
			_unitOfWork.Save();

			// Banco primeiro; S3 best-effort (pior caso: objeto órfão, nunca imagem quebrada).
			await RemoverDoS3(url);
			return true;
		}

		// =====================================================================
		// Concluir / reabrir
		// =====================================================================

		public async Task<ObraChecklistDTO> Concluir(int obraChecklistId, ConcluirObraChecklistRequest req, int userId)
		{
			var exec = await _unitOfWork.ObraChecklists.GetById(obraChecklistId);
			if (exec == null) throw new KeyNotFoundException("Checklist não encontrado.");
			if (exec.ConcluidoEm != null) throw new Exception("Este checklist já foi concluído.");

			var pendencias = new List<PendenciaChecklistDTO>();
			foreach (var i in exec.Itens.Where(i => i.ChecklistItem != null).OrderBy(i => i.ChecklistItem!.Ordem))
			{
				var cfg = i.ChecklistItem!;
				var tipo = (TipoItemChecklist)cfg.Tipo;
				var respondido = tipo switch
				{
					TipoItemChecklist.Foto => i.Fotos.Count > 0 || i.Resposta == 3,
					TipoItemChecklist.Texto or TipoItemChecklist.Numero or TipoItemChecklist.Data or TipoItemChecklist.Selecao
						=> !string.IsNullOrWhiteSpace(i.Valor) || i.Resposta == 3,
					_ => i.Resposta > 0,
				};
				if (cfg.Obrigatorio && !respondido)
					pendencias.Add(new PendenciaChecklistDTO { ItemId = i.Id, Descricao = cfg.Descricao, Motivo = "Resposta obrigatória" });

				var naoConforme = (tipo == TipoItemChecklist.ConformeNaoConformeNA || tipo == TipoItemChecklist.SimNao) && i.Resposta == 2;
				if (naoConforme && cfg.ExigirObservacaoNaoConforme && string.IsNullOrWhiteSpace(i.Observacao))
					pendencias.Add(new PendenciaChecklistDTO { ItemId = i.Id, Descricao = cfg.Descricao, Motivo = "Observação obrigatória quando não conforme" });
				if (naoConforme && cfg.ExigirFotoNaoConforme && i.Fotos.Count == 0)
					pendencias.Add(new PendenciaChecklistDTO { ItemId = i.Id, Descricao = cfg.Descricao, Motivo = "Foto obrigatória quando não conforme" });
			}

			if (pendencias.Count > 0)
				throw new PendenciasException("Há itens pendentes para concluir.", pendencias);

			string? assinaturaUrl = null;
			if (!string.IsNullOrWhiteSpace(req?.AssinaturaBase64))
			{
				var img = ImageValidation.Validar(req!.AssinaturaBase64, "assinatura.png", "assinatura");
				assinaturaUrl = await _s3Service.UploadImageAsync(img.Bytes, $"checklist/{exec.Id}/assinatura_{Guid.NewGuid():N}{Path.GetExtension(img.NomeArquivo)}", img.ContentType);
			}

			exec.ConcluidoEm = DateTime.UtcNow;
			exec.ConcluidoPorUserId = userId;
			exec.ResponsavelNome = Limitar(req?.ResponsavelNome, 200);
			exec.ObservacaoGeral = Limitar(req?.ObservacaoGeral, MaxTextoGeral);
			var assinaturaAntiga = exec.AssinaturaUrl;
			exec.AssinaturaUrl = assinaturaUrl;
			exec.UpdatedDate = DateTime.UtcNow;
			_unitOfWork.ObraChecklists.Update(exec);

			try
			{
				_unitOfWork.Save();
			}
			catch
			{
				if (assinaturaUrl != null) await RemoverDoS3(assinaturaUrl);
				throw;
			}

			if (!string.IsNullOrEmpty(assinaturaAntiga) && assinaturaAntiga != assinaturaUrl) await RemoverDoS3(assinaturaAntiga);
			return await GetById(exec.Id);
		}

		public async Task<ObraChecklistDTO> Reabrir(int obraChecklistId)
		{
			var exec = await _unitOfWork.ObraChecklists.GetHeaderById(obraChecklistId);
			if (exec == null) throw new KeyNotFoundException("Checklist não encontrado.");
			if (exec.ConcluidoEm == null) throw new Exception("Este checklist não está concluído.");

			// Não pode haver duas execuções em andamento do mesmo checklist na obra.
			if (await _unitOfWork.ObraChecklists.ExistsEmAndamento(exec.ObraId, exec.ChecklistId))
				throw new Exception("Já existe outra execução em andamento deste checklist nesta obra. Conclua-a ou remova-a antes de reabrir esta.");

			var assinatura = exec.AssinaturaUrl;
			exec.ConcluidoEm = null;
			exec.ConcluidoPorUserId = null;
			exec.AssinaturaUrl = null;
			exec.UpdatedDate = DateTime.UtcNow;
			_unitOfWork.ObraChecklists.Update(exec);
			_unitOfWork.Save();

			if (!string.IsNullOrEmpty(assinatura)) await RemoverDoS3(assinatura);
			return await GetById(exec.Id);
		}

		// =====================================================================
		// Remoção / sincronização
		// =====================================================================

		public async Task<bool> RemoveChecklistFromObra(int obraChecklistId)
		{
			var obraChecklist = await _unitOfWork.ObraChecklists.GetById(obraChecklistId);
			if (obraChecklist == null) throw new Exception("Vínculo não encontrado.");

			var urls = obraChecklist.Itens.SelectMany(i => i.Fotos).Select(f => f.S3Url).ToList();
			if (!string.IsNullOrEmpty(obraChecklist.AssinaturaUrl)) urls.Add(obraChecklist.AssinaturaUrl);

			var itens = await _unitOfWork.ObraChecklistItems.GetByObraChecklist(obraChecklistId);
			foreach (var item in itens)
				_unitOfWork.ObraChecklistItems.Delete(item);

			_unitOfWork.ObraChecklists.Delete(obraChecklist);
			var ok = _unitOfWork.Save() > 0;

			foreach (var url in urls) await RemoverDoS3(url);
			return ok;
		}

		public async Task<bool> SincronizarChecklist(int checklistId)
		{
			var checklist = await _unitOfWork.Checklists.GetById(checklistId);
			if (checklist == null) throw new Exception("Checklist não encontrado.");

			var itensAtivos = await _unitOfWork.ChecklistItems.GetByChecklist(checklistId);
			var idsAtivos = itensAtivos.Where(i => i.Status == 1).Select(i => i.Id).ToHashSet();
			var idsTodos = itensAtivos.Select(i => i.Id).ToHashSet();

			// [v2] Execuções concluídas ficam como foram entregues.
			var obraChecklists = (await _unitOfWork.ObraChecklists.GetByChecklistId(checklistId))
				.Where(x => x.ConcluidoEm == null)
				.ToList();

			var urlsRemovidas = new List<string>();
			foreach (var obraChecklist in obraChecklists)
			{
				var itensExistentes = await _unitOfWork.ObraChecklistItems.GetByObraChecklist(obraChecklist.Id);
				var idsExistentes = itensExistentes.Select(i => i.ChecklistItemId).ToHashSet();

				foreach (var itemId in idsAtivos.Except(idsExistentes))
				{
					await _unitOfWork.ObraChecklistItems.Add(new ObraChecklistItem
					{
						ObraChecklistId = obraChecklist.Id,
						ChecklistItemId = itemId,
						Resposta = 0
					});
				}

				foreach (var item in itensExistentes.Where(i => !idsTodos.Contains(i.ChecklistItemId)))
				{
					urlsRemovidas.AddRange(item.Fotos.Select(f => f.S3Url));
					_unitOfWork.ObraChecklistItems.Delete(item);
				}
			}

			var ok = _unitOfWork.Save() >= 0;
			foreach (var url in urlsRemovidas) await RemoverDoS3(url);
			return ok;
		}

		// =====================================================================
		// Listagem por empresa
		// =====================================================================

		public async Task<List<ObraChecklistEmpresaDTO>> GetByObraEmpresa(int empresaId, int? operadorId = null)
		{
			var list = await _unitOfWork.ObraChecklists.GetByObraEmpresa(empresaId, operadorId);
			var dtos = await MapManyAsync(list);
			var porId = list.ToDictionary(x => x.Id);
			return dtos.Select(b =>
			{
				var x = porId[b.Id];
				return new ObraChecklistEmpresaDTO
				{
					Id = b.Id,
					ObraId = b.ObraId,
					ChecklistId = b.ChecklistId,
					ChecklistNome = b.ChecklistNome,
					Status = b.Status,
					Itens = b.Itens,
					Titulo = b.Titulo,
					ConcluidoEm = b.ConcluidoEm,
					ConcluidoPorUserId = b.ConcluidoPorUserId,
					ConcluidoPorNome = b.ConcluidoPorNome,
					ResponsavelNome = b.ResponsavelNome,
					AssinaturaUrl = b.AssinaturaUrl,
					ObservacaoGeral = b.ObservacaoGeral,
					ChecklistDescricao = b.ChecklistDescricao,
					ChecklistCategoria = b.ChecklistCategoria,
					ObraNome = b.ObraNome,
					CreatedDate = b.CreatedDate,
					Obra = x.Obra == null ? null : new ObraChecklistObraResumoDTO
					{
						Id = x.Obra.Id,
						Name = x.Obra.Name,
						Status = x.Obra.Status
					}
				};
			}).ToList();
		}

		// =====================================================================
		// Escopo (autorização no controller)
		// =====================================================================

		/// <summary>EmpresaId do checklist (template) para validações de escopo; <c>null</c> se não existir.</summary>
		public async Task<int?> GetChecklistEmpresaId(int checklistId)
		{
			var checklist = await _unitOfWork.Checklists.GetById(checklistId);
			return checklist?.EmpresaId;
		}

		/// <summary>
		/// Resolve <c>ObraChecklistItemId → ObraId</c> para validações de escopo no controller.
		/// Retorna <c>null</c> se o item não existir.
		/// </summary>
		public async Task<int?> GetObraIdByItemId(int obraChecklistItemId)
			=> (await GetEscopoByItemId(obraChecklistItemId))?.ObraId;

		/// <summary>
		/// Resolve <c>ObraChecklistId → ObraId</c> para validações de escopo no controller.
		/// </summary>
		public async Task<int?> GetObraIdByObraChecklistId(int obraChecklistId)
			=> (await GetEscopo(obraChecklistId))?.ObraId;

		public async Task<ObraChecklistEscopoDTO?> GetEscopo(int obraChecklistId)
		{
			var exec = await _unitOfWork.ObraChecklists.GetHeaderById(obraChecklistId);
			return exec == null ? null : new ObraChecklistEscopoDTO { ObraChecklistId = exec.Id, ObraId = exec.ObraId, Concluido = exec.ConcluidoEm != null };
		}

		public async Task<ObraChecklistEscopoDTO?> GetEscopoByItemId(int obraChecklistItemId)
		{
			var item = await _unitOfWork.ObraChecklistItems.GetById(obraChecklistItemId);
			return item == null ? null : await GetEscopo(item.ObraChecklistId);
		}

		public async Task<ObraChecklistEscopoDTO?> GetEscopoByFotoId(int fotoId)
		{
			var foto = await _unitOfWork.ObraChecklistItems.GetFotoById(fotoId);
			return foto == null ? null : await GetEscopoByItemId(foto.ObraChecklistItemId);
		}

		// =====================================================================
		// Mapeamento
		// =====================================================================

		private async Task<List<ObraChecklistDTO>> MapManyAsync(List<ObraChecklist> list)
		{
			var userIds = list.Where(x => x.ConcluidoPorUserId.HasValue).Select(x => x.ConcluidoPorUserId!.Value)
				.Concat(list.SelectMany(x => x.Itens).Where(i => i.RespondidoPorUserId.HasValue).Select(i => i.RespondidoPorUserId!.Value))
				.Distinct()
				.ToList();
			var nomes = userIds.Count > 0 ? await _unitOfWork.Users.GetNamesByIds(userIds) : new Dictionary<int, string>();
			return list.Select(x => MapToDTO(x, nomes)).ToList();
		}

		private static string? Nome(Dictionary<int, string> nomes, int? id)
			=> id.HasValue && nomes.TryGetValue(id.Value, out var n) ? n : null;

		private static ObraChecklistDTO MapToDTO(ObraChecklist x, Dictionary<int, string> nomes) => new()
		{
			Id = x.Id,
			ObraId = x.ObraId,
			ChecklistId = x.ChecklistId,
			ChecklistNome = x.Checklist?.Nome,
			Status = x.Status,
			Titulo = x.Titulo,
			ConcluidoEm = x.ConcluidoEm,
			ConcluidoPorUserId = x.ConcluidoPorUserId,
			ConcluidoPorNome = Nome(nomes, x.ConcluidoPorUserId),
			ResponsavelNome = x.ResponsavelNome,
			AssinaturaUrl = x.AssinaturaUrl,
			ObservacaoGeral = x.ObservacaoGeral,
			ChecklistDescricao = x.Checklist?.Descricao,
			ChecklistCategoria = x.Checklist?.Categoria,
			ObraNome = x.Obra?.Name,
			CreatedDate = x.CreatedDate,
			Itens = x.Itens.Select(i => new ObraChecklistItemDTO
			{
				Id = i.Id,
				ObraChecklistId = i.ObraChecklistId,
				ChecklistItemId = i.ChecklistItemId,
				Descricao = i.ChecklistItem?.Descricao,
				Ordem = i.ChecklistItem?.Ordem ?? 0,
				Resposta = i.Resposta,
				Observacao = i.Observacao,
				Empresa = i.Empresa,
				DataHora = i.DataHora,
				Equipamento = i.Equipamento,
				Marca = i.Marca,
				Tipo = i.ChecklistItem?.Tipo ?? 1,
				Grupo = i.ChecklistItem?.Grupo,
				Obrigatorio = i.ChecklistItem?.Obrigatorio ?? false,
				ExigirFotoNaoConforme = i.ChecklistItem?.ExigirFotoNaoConforme ?? false,
				ExigirObservacaoNaoConforme = i.ChecklistItem?.ExigirObservacaoNaoConforme ?? false,
				Ajuda = i.ChecklistItem?.Ajuda,
				Opcoes = i.ChecklistItem?.Opcoes,
				Valor = i.Valor,
				RespondidoPorUserId = i.RespondidoPorUserId,
				RespondidoPorNome = Nome(nomes, i.RespondidoPorUserId),
				RespondidoEm = i.RespondidoEm,
				Fotos = i.Fotos.OrderBy(f => f.Id).Select(MapFoto).ToList(),
			}).OrderBy(i => i.Ordem).ThenBy(i => i.Id).ToList()
		};

		private static ObraChecklistItemFotoDTO MapFoto(ObraChecklistItemFoto f) => new()
		{
			Id = f.Id,
			ObraChecklistItemId = f.ObraChecklistItemId,
			S3Url = f.S3Url,
			ContentType = f.ContentType,
			NomeArquivo = f.NomeArquivo,
			Legenda = f.Legenda,
			CreatedDate = f.CreatedDate,
		};

		private static string? Limitar(string? s, int max)
		{
			if (string.IsNullOrWhiteSpace(s)) return null;
			var t = s.Trim();
			return t.Length > max ? t[..max] : t;
		}

		private async Task RemoverDoS3(string? url)
		{
			if (string.IsNullOrWhiteSpace(url)) return;
			try { await _s3Service.DeleteImageAsync(url); }
			catch (Exception ex) { _logger?.LogWarning(ex, "Falha ao remover objeto do S3: {Url}", url); }
		}
	}

	public interface IObraChecklistService
	{
		Task<ObraChecklistDTO> AddChecklistToObra(AddChecklistToObraRequest req);
		Task<ObraChecklistDTO> GetById(int id);
		Task<List<ObraChecklistDTO>> GetByObra(int obraId);
		Task<List<ObraChecklistDTO>> GetByObra(int obraId, int empresaIdJwt);
		Task<bool> ResponderItem(int obraChecklistItemId, ResponderChecklistItemRequest req);
		Task<bool> ResponderItem(int obraChecklistItemId, ResponderChecklistItemRequest req, int? userId);
		Task<bool> RemoveChecklistFromObra(int obraChecklistId);
		Task<bool> SincronizarChecklist(int checklistId);
		Task<bool> ResponderItensAdicionais(int obraChecklistItemId, ResponderChecklistItemRequest req);
		Task<List<ObraChecklistEmpresaDTO>> GetByObraEmpresa(int empresaId, int? operadorId = null);
		Task<int?> GetChecklistEmpresaId(int checklistId);
		Task<int?> GetObraIdByItemId(int obraChecklistItemId);
		Task<int?> GetObraIdByObraChecklistId(int obraChecklistId);
		Task<ObraChecklistEscopoDTO?> GetEscopo(int obraChecklistId);
		Task<ObraChecklistEscopoDTO?> GetEscopoByItemId(int obraChecklistItemId);
		Task<ObraChecklistEscopoDTO?> GetEscopoByFotoId(int fotoId);
		Task<List<ObraChecklistItemFotoDTO>> AddFotos(int obraChecklistItemId, List<AddChecklistItemFotoRequest> fotos, int? userId);
		Task<bool> UpdateFotoLegenda(int fotoId, string? legenda);
		Task<bool> DeleteFoto(int fotoId);
		Task<ObraChecklistDTO> Concluir(int obraChecklistId, ConcluirObraChecklistRequest req, int userId);
		Task<ObraChecklistDTO> Reabrir(int obraChecklistId);
		Task<bool> JaAplicadoNaObra(int obraId, int checklistId);
	}
}
