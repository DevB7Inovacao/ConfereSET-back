using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class EquipamentosService : IEquipamentosService
    {
        public IUnitOfWork _unitOfWork;

        public EquipamentosService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Equipamentos> CreateEquipamento(Equipamentos equipamento)
        {
            try
            {
                if (equipamento == null)
                    throw new ArgumentNullException(nameof(equipamento));

                await _unitOfWork.Equipamentos.Add(equipamento);
                _unitOfWork.Save();
                return equipamento;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> UpdateEquipamento(Equipamentos equipamento, int id)
        {
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> DeleteEquipamento(int id)
        {
            try
            {
                var item = await _unitOfWork.Equipamentos.GetEquipamentoById(id);
                if (item == null)
                    throw new Exception("Equipamento não encontrado.");

                _unitOfWork.Equipamentos.Delete(item);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao excluir o equipamento: " + ex.Message);
            }
        }

        public async Task<bool> ToggleEquipamentoStatus(int id)
        {
            try
            {
                var item = await _unitOfWork.Equipamentos.GetEquipamentoById(id);
                if (item == null)
                    throw new Exception("Equipamento não encontrado.");

                item.Status = item.Status == 1 ? 0 : 1;

                _unitOfWork.Equipamentos.Update(item);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao alterar o status do equipamento: " + ex.Message);
            }
        }

        public async Task<Equipamentos> GetEquipamentoById(int id)
        {
            return await _unitOfWork.Equipamentos.GetEquipamentoById(id);
        }

        public async Task<EquipamentosPagedDTO> GetEquipamentosPaged(FiltersEquipamentosDTO filtersDTO)
        {
            try
            {
                var paged = await _unitOfWork.Equipamentos.GetAllEquipamentosPaged(filtersDTO);

                if (paged == null || paged.Results == null || !paged.Results.Any())
                    throw new Exception("Nenhum dado foi encontrado.");

                var dto = paged.Results.Select(x => new EquipamentosDTO
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = x.Descricao,
                    Status = x.Status
                }).ToList();

                return new EquipamentosPagedDTO
                {
                    Result = dto,
                    PageCount = paged.PageCount
                };
            }
            catch (Exception ex)
            {
        return new EquipamentosPagedDTO();

                //throw new Exception(ex.Message);
            }
        }

        public async Task<List<EquipamentoSimpleDTO>> GetEquipamentosSimple(int empresaId)
        {
            return await _unitOfWork.Equipamentos.GetEquipamentosSimple(empresaId);
        }
    }

    public interface IEquipamentosService
    {
        Task<Equipamentos> CreateEquipamento(Equipamentos equipamento);
        Task<bool> UpdateEquipamento(Equipamentos equipamento, int id);
        Task<bool> DeleteEquipamento(int id);
        Task<bool> ToggleEquipamentoStatus(int id);
        Task<Equipamentos> GetEquipamentoById(int id);
        Task<EquipamentosPagedDTO?> GetEquipamentosPaged(FiltersEquipamentosDTO filtersDTO);
        Task<List<EquipamentoSimpleDTO>> GetEquipamentosSimple(int empresaId);
    }
}