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
    public class MaoDeObraService : IMaoDeObraService
    {
        public IUnitOfWork _unitOfWork;

        public MaoDeObraService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<MaoDeObra> CreateMaoDeObra(MaoDeObra maoDeObra)
        {
            try
            {
                if (maoDeObra == null)
                    throw new ArgumentNullException(nameof(maoDeObra));

                await _unitOfWork.MaoDeObra.Add(maoDeObra);
                _unitOfWork.Save();
                return maoDeObra;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> UpdateMaoDeObra(MaoDeObra maoDeObra, int id)
        {
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> DeleteMaoDeObra(int id)
        {
            try
            {
                var item = await _unitOfWork.MaoDeObra.GetMaoDeObraById(id);
                if (item == null)
                    throw new Exception("Função de mão de obra não encontrada.");

                _unitOfWork.MaoDeObra.Delete(item);
                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao excluir a mão de obra: " + ex.Message);
            }
        }

        public async Task<bool> ToggleMaoDeObraStatus(int id)
        {
            try
            {
                var item = await _unitOfWork.MaoDeObra.GetMaoDeObraById(id);
                if (item == null)
                    throw new Exception("Função de mão de obra não encontrada.");

                item.Status = item.Status == 1 ? 0 : 1;

                _unitOfWork.MaoDeObra.Update(item);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao alterar o status da mão de obra: " + ex.Message);
            }
        }

        public async Task<MaoDeObra> GetMaoDeObraById(int id)
        {
            return await _unitOfWork.MaoDeObra.GetMaoDeObraById(id);
        }

        public async Task<MaoDeObraPagedDTO> GetMaoDeObraPaged(FiltersMaoDeObraDTO filtersDTO)
        {
            try
            {
                var paged = await _unitOfWork.MaoDeObra.GetAllMaoDeObraPaged(filtersDTO);

                if (paged == null || paged.Results == null || !paged.Results.Any())
                    throw new Exception("Nenhum dado foi encontrado.");

                var dto = paged.Results.Select(x => new MaoDeObraDTO
                {
                    Id = x.Id,
                    Funcao = x.Funcao,
                    Descricao = x.Descricao,
                    Status = x.Status
                }).ToList();

                return new MaoDeObraPagedDTO
                {
                    Result = dto,
                    PageCount = paged.PageCount
                };
            }
            catch (Exception ex)
            {
        return new MaoDeObraPagedDTO();
                //throw new Exception(ex.Message);
            }
        }

        public async Task<List<MaoDeObraSimpleDTO>> GetMaoDeObraSimple()
        {
            return await _unitOfWork.MaoDeObra.GetMaoDeObraSimple();
        }
    }

    public interface IMaoDeObraService
    {
        Task<MaoDeObra> CreateMaoDeObra(MaoDeObra maoDeObra);
        Task<bool> UpdateMaoDeObra(MaoDeObra maoDeObra, int id);
        Task<bool> DeleteMaoDeObra(int id);
        Task<bool> ToggleMaoDeObraStatus(int id);
        Task<MaoDeObra> GetMaoDeObraById(int id);
        Task<MaoDeObraPagedDTO?> GetMaoDeObraPaged(FiltersMaoDeObraDTO filtersDTO);
        Task<List<MaoDeObraSimpleDTO>> GetMaoDeObraSimple();
    }
}