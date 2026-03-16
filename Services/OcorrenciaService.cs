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

        public async Task<Ocorrencia> Create(CreateOcorrenciaRequest req)
        {
            var obra = await _unitOfWork.Obras.GetObraById(req.ObraId);
            if (obra == null) throw new Exception("Obra não encontrada.");

            var tipoOcorrencia = await _unitOfWork.TiposOcorrencia.GetTipoById(req.TipoOcorrenciaId);
            if (tipoOcorrencia == null) throw new Exception("Tipo de ocorrência não encontrado.");

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

        public async Task<OcorrenciaDTO?> GetById(int id)
        {
            var ocorrencia = await _unitOfWork.Ocorrencias.GetOcorrenciaById(id);
            if (ocorrencia == null) return null;
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

        public async Task<List<OcorrenciaDTO>> GetByObraId(int obraId)
        {
            return await _unitOfWork.Ocorrencias.GetByObraId(obraId);
        }

        public async Task<bool> Update(int id, UpdateOcorrenciaRequest req)
        {
            var ocorrencia = await _unitOfWork.Ocorrencias.GetOcorrenciaById(id);
            if (ocorrencia == null) throw new Exception("Ocorrência não encontrada.");

            if (req.TipoOcorrenciaId.HasValue)
            {
                var tipo = await _unitOfWork.TiposOcorrencia.GetTipoById(req.TipoOcorrenciaId.Value);
                if (tipo == null) throw new Exception("Tipo de ocorrência não encontrado.");
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

        public async Task<bool> UpdateStatus(int id, StatusOcorrencia status)
        {
            var ocorrencia = await _unitOfWork.Ocorrencias.GetOcorrenciaById(id);
            if (ocorrencia == null) throw new Exception("Ocorrência não encontrada.");

            ocorrencia.Status = status;
            _unitOfWork.Ocorrencias.Update(ocorrencia);
            return _unitOfWork.Save() > 0;
        }

        public async Task<bool> Delete(int id)
        {
            var ocorrencia = await _unitOfWork.Ocorrencias.GetOcorrenciaById(id);
            if (ocorrencia == null) throw new Exception("Ocorrência não encontrada.");

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
        Task<Ocorrencia> Create(CreateOcorrenciaRequest req);
        Task<OcorrenciaDTO?> GetById(int id);
        Task<OcorrenciaPagedDTO> GetPaged(FiltersOcorrenciaDTO filters);
        Task<List<OcorrenciaDTO>> GetByObraId(int obraId);
        Task<bool> Update(int id, UpdateOcorrenciaRequest req);
        Task<bool> UpdateStatus(int id, StatusOcorrencia status);
        Task<bool> Delete(int id);
    }
}