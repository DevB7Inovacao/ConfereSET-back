using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
	public class DespesasService : IDespesasService
	{
		public IUnitOfWork _unitOfWork;

		public DespesasService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
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
			var result = _unitOfWork.Save();
			return result > 0;
		}

		public async Task<bool> DeleteDespesa(int despesaId)
		{
			try
			{
				var despesa = await _unitOfWork.Despesas.GetDespesaById(despesaId);
				if (despesa == null)
					throw new Exception("Despesa não encontrada.");

				_unitOfWork.Despesas.Delete(despesa);
				var result = _unitOfWork.Save();

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

				var dto = despesas.Results.Select(x => new DespesaDTO
				{
					Id = x.Id,
					Name = x.Name,
					Amount = x.Amount,
					Date = x.Date,
					Category = x.Category,
					Description = x.Description,
					ObraId = x.ObraId,
					Status = x.Status
				}).ToList();

				return new DespesasPagedDTO { Result = dto, PageCount = despesas.PageCount };
			}
			catch (Exception ex)
			{
				return new DespesasPagedDTO { Result = null, PageCount = 0 };
			}
		}

		public async Task<List<DespesaSimpleDTO>> GetDespesasSimple(int? obraId)
		{
			var list = await _unitOfWork.Despesas.GetDespesasSimple(obraId);
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
					Status = d.Status
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
		public Task<List<DespesaSimpleDTO>> GetDespesasSimple(int? obraId);
		public Task<RelatorioResumoDTO> GetRelatorioResumo(FiltrosRelatorioDTO filtros);
		public Task<RelatorioDetalhadoDTO> GetRelatorioDetalhado(FiltrosRelatorioDTO filtros);
	}
}