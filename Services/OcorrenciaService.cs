using Core.DTO;
using Core.Enums;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class OcorrenciaService : IOcorrenciaService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAtividadeRecenteService _atividadeService;

        public OcorrenciaService(IUnitOfWork unitOfWork, IAtividadeRecenteService atividadeService)
        {
            _unitOfWork = unitOfWork;
            _atividadeService = atividadeService;
        }

        public async Task<Ocorrencia> Create(CreateOcorrenciaRequest req, int empresaId)
        {
            var obra = await _unitOfWork.Obras.GetObraById(req.ObraId);
            if (obra == null || obra.EmpresaId != empresaId) throw new Exception("Obra não encontrada para a empresa logada.");

            var tipoOcorrencia = await _unitOfWork.TiposOcorrencia.GetTipoById(req.TipoOcorrenciaId);
            if (tipoOcorrencia == null || tipoOcorrencia.EmpresaId != empresaId) throw new Exception("Tipo de ocorrência não encontrado para a empresa logada.");

            var ocorrencia = new Ocorrencia
            {
                ObraId = req.ObraId,
                TipoOcorrenciaId = req.TipoOcorrenciaId,
                Titulo = req.Titulo.Trim(),
                Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim(),
                Localizacao = string.IsNullOrWhiteSpace(req.Localizacao) ? null : req.Localizacao.Trim(),
                Status = StatusOcorrencia.Aberta,
                DataOcorrencia = req.DataOcorrencia ?? DateTime.Now,
                CriadoPorUserId = req.CriadoPorUserId
            };

            await _unitOfWork.Ocorrencias.Add(ocorrencia);
            _unitOfWork.Save();

            if (req.CriadoPorUserId.HasValue)
                await _atividadeService.Registrar(
                    req.CriadoPorUserId.Value,
                    TipoAtividade.OcorrenciaRegistrada,
                    $"Ocorrência '{ocorrencia.Titulo}' registrada na obra '{obra.Name}'.",
                    req.ObraId,
                    ocorrencia.Id);

            return ocorrencia;
        }

        public async Task<OcorrenciaDTO?> GetById(int id, int empresaId)
        {
            var ocorrencia = await _unitOfWork.Ocorrencias.GetOcorrenciaById(id);
            if (ocorrencia == null || ocorrencia.Obra?.EmpresaId != empresaId) return null;
            return MapToDTO(ocorrencia);
        }

        public async Task<OcorrenciaPagedDTO> GetPaged(FiltersOcorrenciaDTO filters)
        {
            var paged = await _unitOfWork.Ocorrencias.GetPaged(filters);
            return new OcorrenciaPagedDTO
            {
                PageCount = paged.PageCount,
                Result = paged.Results.Select(MapToDTO).ToList()
            };
        }

        public async Task<List<OcorrenciaDTO>> GetByObraId(int obraId, int empresaId)
        {
            var obra = await _unitOfWork.Obras.GetObraById(obraId);
            if (obra == null || obra.EmpresaId != empresaId) return new List<OcorrenciaDTO>();

            return await _unitOfWork.Ocorrencias.GetByObraId(obraId);
        }

        public async Task<bool> Update(int id, UpdateOcorrenciaRequest req, int empresaId)
        {
            var ocorrencia = await _unitOfWork.Ocorrencias.GetOcorrenciaById(id);
            if (ocorrencia == null || ocorrencia.Obra?.EmpresaId != empresaId) throw new Exception("Ocorrência não encontrada para a empresa logada.");

            if (req.TipoOcorrenciaId.HasValue)
            {
                var tipo = await _unitOfWork.TiposOcorrencia.GetTipoById(req.TipoOcorrenciaId.Value);
                if (tipo == null || tipo.EmpresaId != empresaId) throw new Exception("Tipo de ocorrência não encontrado para a empresa logada.");
                ocorrencia.TipoOcorrenciaId = req.TipoOcorrenciaId.Value;
            }

            if (!string.IsNullOrWhiteSpace(req.Titulo))
                ocorrencia.Titulo = req.Titulo.Trim();

            if (req.Descricao != null)
                ocorrencia.Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim();

            if (req.Localizacao != null)
                ocorrencia.Localizacao = string.IsNullOrWhiteSpace(req.Localizacao) ? null : req.Localizacao.Trim();

            if (req.Status.HasValue)
                ocorrencia.Status = req.Status.Value;

            if (req.DataOcorrencia.HasValue)
                ocorrencia.DataOcorrencia = req.DataOcorrencia.Value;

            _unitOfWork.Ocorrencias.Update(ocorrencia);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> UpdateStatus(int id, StatusOcorrencia status, int empresaId)
        {
            var ocorrencia = await _unitOfWork.Ocorrencias.GetOcorrenciaById(id);
            if (ocorrencia == null || ocorrencia.Obra?.EmpresaId != empresaId) throw new Exception("Ocorrência não encontrada para a empresa logada.");

            ocorrencia.Status = status;
            _unitOfWork.Ocorrencias.Update(ocorrencia);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> Delete(int id, int empresaId)
        {
            var ocorrencia = await _unitOfWork.Ocorrencias.GetOcorrenciaById(id);
            if (ocorrencia == null || ocorrencia.Obra?.EmpresaId != empresaId) throw new Exception("Ocorrência não encontrada para a empresa logada.");

            _unitOfWork.Ocorrencias.Delete(ocorrencia);
            return _unitOfWork.Save() > 0;
        }

        private static OcorrenciaDTO MapToDTO(Ocorrencia o) => new()
        {
            Id = o.Id,
            ObraId = o.ObraId,
            ObraNome = o.Obra?.Name,
            TipoOcorrenciaId = o.TipoOcorrenciaId,
            TipoOcorrenciaNome = o.TipoOcorrencia?.Nome,
            TipoOcorrenciaGravidade = o.TipoOcorrencia?.Gravidade ?? 0,
            Titulo = o.Titulo,
            Descricao = o.Descricao,
            Localizacao = o.Localizacao,
            Status = o.Status,
            DataOcorrencia = o.DataOcorrencia,
            CriadoPorUserId = o.CriadoPorUserId,
            CriadoPorNome = o.CriadoPor?.Name,
            CreatedDate = o.CreatedDate
        };
    }

    public interface IOcorrenciaService
    {
        Task<Ocorrencia> Create(CreateOcorrenciaRequest req, int empresaId);
        Task<OcorrenciaDTO?> GetById(int id, int empresaId);
        Task<OcorrenciaPagedDTO> GetPaged(FiltersOcorrenciaDTO filters);
        Task<List<OcorrenciaDTO>> GetByObraId(int obraId, int empresaId);
        Task<bool> Update(int id, UpdateOcorrenciaRequest req, int empresaId);
        Task<bool> UpdateStatus(int id, StatusOcorrencia status, int empresaId);
        Task<bool> Delete(int id, int empresaId);
    }
}