using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class GrupoDeObrasService : IGrupoDeObrasService
    {
        public IUnitOfWork _unitOfWork;

        public GrupoDeObrasService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrupoDeObras> CreateGrupo(GrupoDeObras grupo)
        {
            if (grupo == null) throw new ArgumentNullException(nameof(grupo));

            await _unitOfWork.GrupoDeObras.Add(grupo);
            _unitOfWork.Save();
            return grupo;
        }

        public async Task<bool> UpdateGrupo(int groupId, UpdateGrupoDeObrasRequest req)
        {
            var existing = await _unitOfWork.GrupoDeObras.GetGrupoById(groupId);
            if (existing == null) throw new Exception("Grupo não encontrado.");

            if (!string.IsNullOrWhiteSpace(req.Name))
                existing.Name = req.Name;

            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> DeleteGrupo(int grupoId)
        {
            var grupo = await _unitOfWork.GrupoDeObras.GetGrupoById(grupoId);
            if (grupo == null) throw new Exception("Grupo não encontrado.");

            _unitOfWork.GrupoDeObras.Delete(grupo);
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> ToggleGrupoStatus(int grupoId)
        {
            var grupo = await _unitOfWork.GrupoDeObras.GetGrupoById(grupoId);
            if (grupo == null) throw new Exception("Grupo não encontrado.");

            grupo.Status = grupo.Status == 1 ? 0 : 1;

            _unitOfWork.GrupoDeObras.Update(grupo);
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<GrupoDeObras?> GetGrupoById(int id)
        {
            return await _unitOfWork.GrupoDeObras.GetGrupoById(id);
        }

        public async Task<GrupoDeObrasPagedDTO> GetGrupoPaged(FiltersGrupoDeObrasDTO filtersDTO)
        {
            var grupos = await _unitOfWork.GrupoDeObras.GetAllGrupoPaged(filtersDTO);

            if (grupos == null || grupos.Results == null || !grupos.Results.Any())
                throw new Exception("Nenhum dado foi encontrado.");

            var dtos = grupos.Results.Select(g => new GrupoDeObrasDTO
            {
                Id = g.Id,
                Name = g.Name,
                Status = g.Status,
                ObrasIds = g.Obras?.Select(x => x.ObraId).ToList() ?? new()
            }).ToList();

            return new GrupoDeObrasPagedDTO
            {
                Result = dtos,
                PageCount = grupos.PageCount
            };
        }

        public async Task<bool> AddObraToGrupo(int groupId, int obraId)
        {
            var grupo = await _unitOfWork.GrupoDeObras.GetGrupoById(groupId);
            if (grupo == null) throw new Exception("Grupo não encontrado.");

            var obra = await _unitOfWork.Obras.GetObraById(obraId);
            if (obra == null) throw new Exception("Obra não encontrada.");

            var existing = await _unitOfWork.GrupoDeObras.GetRelacao(groupId, obraId);
            if (existing != null) return true;

            await _unitOfWork.GrupoDeObras.AddRelacao(new RelacaoGrupoObras
            {
                GroupId = groupId,
                ObraId = obraId
            });

            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> RemoveObraFromGrupo(int groupId, int obraId)
        {
            var grupo = await _unitOfWork.GrupoDeObras.GetGrupoById(groupId);
            if (grupo == null) throw new Exception("Grupo não encontrado.");

            var relacao = await _unitOfWork.GrupoDeObras.GetRelacao(groupId, obraId);
            if (relacao == null) return true;

            _unitOfWork.GrupoDeObras.RemoveRelacao(relacao);
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<int[]> GetObrasIdsByGrupo(int groupId)
        {
            var grupo = await _unitOfWork.GrupoDeObras.GetGrupoById(groupId);
            if (grupo == null) throw new Exception("Grupo não encontrado.");

            var relacoes = await _unitOfWork.GrupoDeObras.GetRelacoesByGroupId(groupId);
            return relacoes.Select(x => x.ObraId).Distinct().ToArray();
        }
    }

    public interface IGrupoDeObrasService
    {
        Task<GrupoDeObras> CreateGrupo(GrupoDeObras grupo);
        Task<bool> UpdateGrupo(int groupId, UpdateGrupoDeObrasRequest req);
        Task<bool> DeleteGrupo(int grupoId);
        Task<bool> ToggleGrupoStatus(int grupoId);
        Task<GrupoDeObras?> GetGrupoById(int id);
        Task<GrupoDeObrasPagedDTO> GetGrupoPaged(FiltersGrupoDeObrasDTO filtersDTO);

        Task<bool> AddObraToGrupo(int groupId, int obraId);
        Task<bool> RemoveObraFromGrupo(int groupId, int obraId);
        Task<int[]> GetObrasIdsByGrupo(int groupId);
    }
}