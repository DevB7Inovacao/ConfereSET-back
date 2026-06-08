using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class TiposOcorrenciaService : ITiposOcorrenciaService
    {
        public IUnitOfWork _unitOfWork;

        public TiposOcorrenciaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TiposOcorrencia> Create(TiposOcorrencia tipo)
        {
            try
            {
                if (tipo == null)
                    throw new ArgumentNullException(nameof(tipo));

                await _unitOfWork.TiposOcorrencia.Add(tipo);
                _unitOfWork.Save();
                return tipo;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> Update(TiposOcorrencia tipo, int id)
        {
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> Delete(int id)
        {
            try
            {
                var item = await _unitOfWork.TiposOcorrencia.GetTipoById(id);
                if (item == null)
                    throw new Exception("Tipo de ocorrência não encontrado.");

                _unitOfWork.TiposOcorrencia.Delete(item);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao excluir o tipo de ocorrência: " + ex.Message);
            }
        }

        public async Task<bool> ToggleStatus(int id)
        {
            try
            {
                var item = await _unitOfWork.TiposOcorrencia.GetTipoById(id);
                if (item == null)
                    throw new Exception("Tipo de ocorrência não encontrado.");

                item.Status = item.Status == 1 ? 0 : 1;

                _unitOfWork.TiposOcorrencia.Update(item);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao alterar o status do tipo de ocorrência: " + ex.Message);
            }
        }

        public async Task<TiposOcorrencia> GetById(int id)
        {
            return await _unitOfWork.TiposOcorrencia.GetTipoById(id);
        }

        public async Task<TiposOcorrenciaPagedDTO> GetPaged(FiltersTiposOcorrenciaDTO filtersDTO)
        {
            try
            {
                var paged = await _unitOfWork.TiposOcorrencia.GetAllPaged(filtersDTO);

                if (paged == null || paged.Results == null || !paged.Results.Any())
                    throw new Exception("Nenhum dado foi encontrado.");

                var dto = paged.Results.Select(x => new TiposOcorrenciaDTO
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = x.Descricao,
                    Gravidade = x.Gravidade,
                    Requisitos = x.Requisitos,
                    Status = x.Status
                }).ToList();

                return new TiposOcorrenciaPagedDTO
                {
                    Result = dto,
                    PageCount = paged.PageCount
                };
            }
            catch (Exception ex)
            {
				return new TiposOcorrenciaPagedDTO
				{
					Result = null,
					PageCount = 0
				};
			}
        }

        public async Task<List<TipoOcorrenciaSimpleDTO>> GetSimple(int empresaId)
        {
            return await _unitOfWork.TiposOcorrencia.GetSimple(empresaId);
        }
    }

    public interface ITiposOcorrenciaService
    {
        Task<TiposOcorrencia> Create(TiposOcorrencia tipo);
        Task<bool> Update(TiposOcorrencia tipo, int id);
        Task<bool> Delete(int id);
        Task<bool> ToggleStatus(int id);
        Task<TiposOcorrencia> GetById(int id);
        Task<TiposOcorrenciaPagedDTO?> GetPaged(FiltersTiposOcorrenciaDTO filtersDTO);
        Task<List<TipoOcorrenciaSimpleDTO>> GetSimple(int empresaId);
    }
}