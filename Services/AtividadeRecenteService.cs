using Core.DTO;
using Core.Enums;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class AtividadeRecenteService : IAtividadeRecenteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AtividadeRecenteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Registrar(int operadorId, TipoAtividade tipo, string descricao, int? obraId = null, int? referenciaId = null)
        {
            var atividade = new AtividadeRecente
            {
                OperadorId = operadorId,
                Tipo = tipo,
                Descricao = descricao,
                ObraId = obraId,
                ReferenciaId = referenciaId
            };

            await _unitOfWork.AtividadesRecentes.Add(atividade);
            _unitOfWork.Save();
        }

        public async Task<AtividadeRecentePagedDTO> GetPagedByOperadorId(int operadorId, FiltersAtividadeRecenteDTO filters)
        {
            var paged = await _unitOfWork.AtividadesRecentes.GetPagedByOperadorId(operadorId, filters);
            return MapToPagedDTO(paged);
        }

        public async Task<AtividadeRecentePagedDTO> GetPagedByEmpresaId(int empresaId, FiltersAtividadeRecenteDTO filters)
        {
            var paged = await _unitOfWork.AtividadesRecentes.GetPagedByEmpresaId(empresaId, filters);
            return MapToPagedDTO(paged);
        }

        private static AtividadeRecentePagedDTO MapToPagedDTO(Saller.Infrastructure.ServiceExtension.PagedResult<AtividadeRecente> paged)
        {
            return new AtividadeRecentePagedDTO
            {
                PageCount = paged.PageCount,
                Result = paged.Results.Select(x => new AtividadeRecenteDTO
                {
                    Id = x.Id,
                    OperadorId = x.OperadorId,
                    OperadorNome = x.Operador?.Name,
                    Tipo = x.Tipo,
                    Descricao = x.Descricao,
                    ObraId = x.ObraId,
                    ObraNome = x.Obra?.Name,
                    ReferenciaId = x.ReferenciaId,
                    CreatedDate = x.CreatedDate
                }).ToList()
            };
        }
    }

    public interface IAtividadeRecenteService
    {
        Task Registrar(int operadorId, TipoAtividade tipo, string descricao, int? obraId = null, int? referenciaId = null);
        Task<AtividadeRecentePagedDTO> GetPagedByOperadorId(int operadorId, FiltersAtividadeRecenteDTO filters);
        Task<AtividadeRecentePagedDTO> GetPagedByEmpresaId(int empresaId, FiltersAtividadeRecenteDTO filters);
    }
}