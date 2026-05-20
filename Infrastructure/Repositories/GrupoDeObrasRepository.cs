using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class GrupoDeObrasRepository : GenericRepository<GrupoDeObras>, IGrupoDeObrasRepository
    {
        public GrupoDeObrasRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<GrupoDeObras?> GetGrupoById(int id)
        {
            return await _dbContext.Set<GrupoDeObras>()
                .Include(x => x.Obras)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<GrupoDeObras>> GetAllGrupoPaged(FiltersGrupoDeObrasDTO filtersDTO)
        {
            var q = _dbContext.Set<GrupoDeObras>()
                .Include(x => x.Obras)
                .AsQueryable();

            if (filtersDTO.EmpresaId.HasValue && filtersDTO.EmpresaId.Value > 0)
                q = q.Where(x => x.EmpresaId == filtersDTO.EmpresaId.Value);

            if (!string.IsNullOrEmpty(filtersDTO.Name))
                q = q.Where(x => EF.Functions.Like(x.Name.ToLower(), $"%{filtersDTO.Name.ToLower()}%"));

            if (filtersDTO.Status.HasValue)
                q = q.Where(x => x.Status == filtersDTO.Status.Value);

            return await q.GetPagedAsync<GrupoDeObras>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }

        public async Task<RelacaoGrupoObras?> GetRelacao(int groupId, int obraId)
        {
            return await _dbContext.Set<RelacaoGrupoObras>()
                .FirstOrDefaultAsync(x => x.GroupId == groupId && x.ObraId == obraId);
        }

        public async Task AddRelacao(RelacaoGrupoObras relacao)
        {
            await _dbContext.Set<RelacaoGrupoObras>().AddAsync(relacao);
        }

        public void RemoveRelacao(RelacaoGrupoObras relacao)
        {
            _dbContext.Set<RelacaoGrupoObras>().Remove(relacao);
        }

        public async Task<List<RelacaoGrupoObras>> GetRelacoesByGroupId(int groupId)
        {
            return await _dbContext.Set<RelacaoGrupoObras>()
                .Where(x => x.GroupId == groupId)
                .ToListAsync();
        }
    }

    public interface IGrupoDeObrasRepository : IGenericRepository<GrupoDeObras>
    {
        Task<GrupoDeObras?> GetGrupoById(int id);
        Task<PagedResult<GrupoDeObras>> GetAllGrupoPaged(FiltersGrupoDeObrasDTO filtersDTO);

        Task<RelacaoGrupoObras?> GetRelacao(int groupId, int obraId);
        Task AddRelacao(RelacaoGrupoObras relacao);
        void RemoveRelacao(RelacaoGrupoObras relacao);
        Task<List<RelacaoGrupoObras>> GetRelacoesByGroupId(int groupId);
    }
}