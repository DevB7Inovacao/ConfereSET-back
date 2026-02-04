using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<DespesaSimpleDTO>> GetDespesasSimple(int? obraId)
        {
            var list = await _unitOfWork.Despesas.GetDespesasSimple(obraId);
            return list;
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
    }
}