using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Services
{
	public class DespesasService : IDespesasService
	{
		/// <summary>Comprovantes por despesa (cupom + nota + recibo costuma bastar).</summary>
		public const int MaxComprovantes = 5;

		public IUnitOfWork _unitOfWork;
		private readonly IS3Service _s3Service;
		private readonly ILogger<DespesasService>? _logger;

		public DespesasService(IUnitOfWork unitOfWork, IS3Service s3Service, ILogger<DespesasService>? logger = null)
		{
			_unitOfWork = unitOfWork;
			_s3Service = s3Service;
			_logger = logger;
		}

		public static DespesaDTO MapDTO(Despesas x) => new DespesaDTO
		{
			Id = x.Id,
			Name = x.Name,
			Amount = x.Amount,
			Date = x.Date,
			Category = x.Category,
			Description = x.Description,
			ObraId = x.ObraId,
			Status = x.Status,
			Comprovantes = (x.Comprovantes ?? new List<DespesaComprovante>())
				.OrderBy(c => c.Id).Select(DespesaComprovanteDTO.De).ToList(),
		};

		public async Task<List<DespesaComprovanteDTO>> AddComprovantes(Despesas despesa, List<AddDespesaComprovanteRequest> fotos, int? userId)
		{
			if (fotos == null || fotos.Count == 0) throw new Exception("Nenhuma foto enviada.");
			var atuais = despesa.Comprovantes?.Count ?? 0;
			if (atuais + fotos.Count > MaxComprovantes)
				throw new Exception($"Cada despesa aceita no máximo {MaxComprovantes} fotos de comprovante.");

			// Valida tudo antes de enviar qualquer coisa ao S3.
			var validadas = fotos.Select(f => ImageValidation.Validar(f.ImagemBase64, f.NomeArquivo, "comprovante")).ToList();

			var enviadas = new List<string>();
			var criados = new List<DespesaComprovante>();
			try
			{
				foreach (var img in validadas)
				{
					var nome = $"despesa_{despesa.Id}_{Guid.NewGuid():N}_{img.NomeArquivo}";
					var url = await _s3Service.UploadImageAsync(img.Bytes, nome, img.ContentType);
					enviadas.Add(url);

					var comprovante = new DespesaComprovante
					{
						DespesaId = despesa.Id,
						S3Url = url,
						ContentType = img.ContentType,
						NomeArquivo = img.NomeArquivo,
						CriadoPorUserId = userId,
						CreatedDate = DateTime.UtcNow,
						UpdatedDate = DateTime.UtcNow,
					};
					await _unitOfWork.Despesas.AddComprovante(comprovante);
					criados.Add(comprovante);
				}
				_unitOfWork.Save();
			}
			catch (Exception ex)
			{
				// Tudo ou nada: remove do S3 o que já subiu.
				foreach (var url in enviadas)
				{
					try { await _s3Service.DeleteImageAsync(url); }
					catch (Exception delEx) { _logger?.LogWarning(delEx, "Falha ao remover comprovante órfão do S3: {Url}", url); }
				}
				_logger?.LogError(ex, "Falha ao salvar comprovantes da despesa {DespesaId}", despesa.Id);
				throw new Exception("Não foi possível salvar o comprovante. Nada foi gravado — tente enviar novamente.");
			}

			return criados.Select(DespesaComprovanteDTO.De).ToList();
		}

		public async Task<DespesaComprovante?> GetComprovanteById(int comprovanteId)
		{
			return await _unitOfWork.Despesas.GetComprovanteById(comprovanteId);
		}

		public async Task<bool> DeleteComprovante(DespesaComprovante comprovante)
		{
			var url = comprovante.S3Url;
			_unitOfWork.Despesas.DeleteComprovante(comprovante);
			var ok = _unitOfWork.Save() > 0;
			if (ok) await ApagarDoS3(new[] { url });
			return ok;
		}

		/// <summary>Remove arquivos do S3 sem falhar a operação (o registro já saiu do banco).</summary>
		private async Task ApagarDoS3(IEnumerable<string> urls)
		{
			foreach (var url in urls.Where(u => !string.IsNullOrWhiteSpace(u)))
			{
				try { await _s3Service.DeleteImageAsync(url); }
				catch (Exception ex) { _logger?.LogWarning(ex, "Falha ao remover comprovante do S3: {Url}", url); }
			}
		}

		public async Task<Despesas> CreateDespesa(Despesas despesa)
		{
			try
			{
				if (despesa == null)
					throw new ArgumentNullException(nameof(despesa));

				await _unitOfWork.Despesas.Add(despesa);
				_unitOfWork.Save();
				return despesa;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		public async Task<bool> UpdateDespesa(Despesas despesa, int idDespesa)
		{
			// Salvar sem nenhuma mudança (ex.: só trocou a foto do comprovante) grava 0 linhas e
			// não é erro — antes aparecia "Falha ao atualizar despesa".
			var result = _unitOfWork.Save();
			return result >= 0;
		}

		public async Task<bool> DeleteDespesa(int despesaId)
		{
			try
			{
				var despesa = await _unitOfWork.Despesas.GetDespesaById(despesaId);
				if (despesa == null)
					throw new Exception("Despesa não encontrada.");

				// Os comprovantes saem do banco em cascata; os arquivos, do S3 logo depois.
				var arquivos = despesa.Comprovantes?.Select(c => c.S3Url).ToList() ?? new List<string>();
				_unitOfWork.Despesas.Delete(despesa);
				var result = _unitOfWork.Save();
				if (result > 0) await ApagarDoS3(arquivos);

				return result > 0;
			}
			catch (Exception ex)
			{
				throw new Exception("Falha ao excluir a despesa: " + ex.Message);
			}
		}

		public async Task<bool> ToggleDespesaStatus(int despesaId)
		{
			try
			{
				var despesa = await _unitOfWork.Despesas.GetDespesaById(despesaId);
				if (despesa == null)
					throw new Exception("Despesa não encontrada.");

				despesa.Status = despesa.Status == 1 ? 0 : 1;

				_unitOfWork.Despesas.Update(despesa);
				var result = _unitOfWork.Save();

				return result > 0;
			}
			catch (Exception ex)
			{
				throw new Exception("Falha ao alterar o status da despesa: " + ex.Message);
			}
		}

		public async Task<Despesas> GetDespesaById(int id)
		{
			return await _unitOfWork.Despesas.GetDespesaById(id);
		}

		public async Task<DespesasPagedDTO> GetDespesasPaged(FiltersDespesasDTO filtersDTO)
		{
			try
			{
				var despesas = await _unitOfWork.Despesas.GetAllDespesasPaged(filtersDTO);

				if (despesas == null || despesas.Results == null || !despesas.Results.Any())
					throw new Exception("Nenhum dado foi encontrado.");

				var dto = despesas.Results.Select(MapDTO).ToList();

				return new DespesasPagedDTO { Result = dto, PageCount = despesas.PageCount };
			}
			catch (Exception ex)
			{
				return new DespesasPagedDTO { Result = null, PageCount = 0 };
			}
		}

		public async Task<List<DespesaSimpleDTO>> GetDespesasSimple(int? obraId, int empresaId)
		{
			var list = await _unitOfWork.Despesas.GetDespesasSimple(obraId, empresaId);
			return list;
		}

		public async Task<RelatorioResumoDTO> GetRelatorioResumo(FiltrosRelatorioDTO filtros)
		{
			try
			{
				var despesas = await _unitOfWork.Despesas.GetDespesasParaRelatorio(filtros);
				var obras = await _unitOfWork.Obras.GetObrasSimple();

				if (!despesas.Any())
					throw new Exception("Nenhuma despesa encontrada para o período informado.");

				var totalGeral = despesas.Sum(x => x.Amount);

				var resumoPorObra = despesas
						.GroupBy(x => x.ObraId)
						.Select(g => new ResumoPorObraDTO
						{
							ObraId = g.Key,
							ObraNome = obras.FirstOrDefault(o => o.Id == g.Key)?.Name ?? "Obra não encontrada",
							TotalObra = g.Sum(x => x.Amount),
							QuantidadeDespesas = g.Count(),
							PercentualDoTotal = totalGeral > 0 ? (g.Sum(x => x.Amount) / totalGeral) * 100 : 0
						})
						.OrderByDescending(x => x.TotalObra)
						.ToList();

				var resumoPorCategoria = despesas
						.GroupBy(x => string.IsNullOrEmpty(x.Category) ? "Sem Categoria" : x.Category)
						.Select(g => new ResumoPorCategoriaDTO
						{
							Categoria = g.Key,
							TotalCategoria = g.Sum(x => x.Amount),
							QuantidadeDespesas = g.Count(),
							PercentualDoTotal = totalGeral > 0 ? (g.Sum(x => x.Amount) / totalGeral) * 100 : 0
						})
						.OrderByDescending(x => x.TotalCategoria)
						.ToList();

				return new RelatorioResumoDTO
				{
					TotalGeral = totalGeral,
					QuantidadeDespesas = despesas.Count,
					MediaPorDespesa = despesas.Count > 0 ? totalGeral / despesas.Count : 0,
					ResumosPorObra = resumoPorObra,
					ResumosPorCategoria = resumoPorCategoria,
					PeriodoInicio = filtros.DataInicio,
					PeriodoFim = filtros.DataFim
				};
			}
			catch (Exception ex)
			{
				throw new Exception("Erro ao gerar relatório resumido: " + ex.Message);
			}
		}

		public async Task<RelatorioDetalhadoDTO> GetRelatorioDetalhado(FiltrosRelatorioDTO filtros)
		{
			try
			{
				var despesas = await _unitOfWork.Despesas.GetDespesasParaRelatorio(filtros);
				var obras = await _unitOfWork.Obras.GetObrasSimple();

				var despesasDetalhadas = despesas.Select(d => new DespesaRelatorioDTO
				{
					Id = d.Id,
					Name = d.Name,
					Amount = d.Amount,
					Date = d.Date,
					Category = d.Category,
					Description = d.Description,
					ObraId = d.ObraId,
					ObraNome = obras.FirstOrDefault(o => o.Id == d.ObraId)?.Name ?? "Obra não encontrada",
					Status = d.Status,
					Comprovantes = (d.Comprovantes ?? new List<DespesaComprovante>())
						.OrderBy(c => c.Id).Select(DespesaComprovanteDTO.De).ToList(),
				}).ToList();

				string? obraNomeFiltro = null;
				if (filtros.ObraId.HasValue)
				{
					obraNomeFiltro = obras.FirstOrDefault(o => o.Id == filtros.ObraId.Value)?.Name;
				}

				return new RelatorioDetalhadoDTO
				{
					Despesas = despesasDetalhadas,
					TotalGeral = despesas.Sum(x => x.Amount),
					QuantidadeTotal = despesas.Count,
					PeriodoInicio = filtros.DataInicio,
					PeriodoFim = filtros.DataFim,
					ObraIdFiltro = filtros.ObraId,
					ObraNomeFiltro = obraNomeFiltro
				};
			}
			catch (Exception ex)
			{
				throw new Exception("Erro ao gerar relatório detalhado: " + ex.Message);
			}
		}
	}

	public interface IDespesasService
	{
		public Task<Despesas> CreateDespesa(Despesas despesa);
		public Task<bool> UpdateDespesa(Despesas despesa, int idDespesa);
		public Task<bool> DeleteDespesa(int despesaId);
		public Task<bool> ToggleDespesaStatus(int despesaId);
		public Task<Despesas> GetDespesaById(int id);
		public Task<DespesasPagedDTO?> GetDespesasPaged(FiltersDespesasDTO filtersDTO);
		public Task<List<DespesaSimpleDTO>> GetDespesasSimple(int? obraId, int empresaId);
		public Task<RelatorioResumoDTO> GetRelatorioResumo(FiltrosRelatorioDTO filtros);
		public Task<RelatorioDetalhadoDTO> GetRelatorioDetalhado(FiltrosRelatorioDTO filtros);
		public Task<List<DespesaComprovanteDTO>> AddComprovantes(Despesas despesa, List<AddDespesaComprovanteRequest> fotos, int? userId);
		public Task<DespesaComprovante?> GetComprovanteById(int comprovanteId);
		public Task<bool> DeleteComprovante(DespesaComprovante comprovante);
	}
}