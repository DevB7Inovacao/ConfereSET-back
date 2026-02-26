using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObraMaoDeObraRepository : GenericRepository<ObraMaoDeObra>, IObraMaoDeObraRepository
    {
        public ObraMaoDeObraRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<bool> AddMaoDeObraToObra(int obraId, int maoDeObraId)
        {
            var exists = await _dbContext.Set<ObraMaoDeObra>()
                .AnyAsync(x => x.ObraId == obraId && x.MaoDeObraId == maoDeObraId);

            if (exists)
                return false;

            var obraMaoDeObra = new ObraMaoDeObra
            {
                ObraId = obraId,
                MaoDeObraId = maoDeObraId
            };

            await _dbContext.Set<ObraMaoDeObra>().AddAsync(obraMaoDeObra);
            return true;
        }

        public async Task<bool> RemoveMaoDeObraFromObra(int obraId, int maoDeObraId)
        {
            var relation = await _dbContext.Set<ObraMaoDeObra>()
                .FirstOrDefaultAsync(x => x.ObraId == obraId && x.MaoDeObraId == maoDeObraId);

            if (relation == null)
                return false;

            _dbContext.Set<ObraMaoDeObra>().Remove(relation);
            return true;
        }

        public async Task<List<ObraMaoDeObraDTO>> GetMaoDeObraByObraId(int obraId)
        {
            return await _dbContext.Set<ObraMaoDeObra>()
                .AsNoTracking()
                .Where(x => x.ObraId == obraId)
                .Include(x => x.MaoDeObra)
                .Select(x => new ObraMaoDeObraDTO
                {
                    Id = x.MaoDeObra!.Id,
                    Funcao = x.MaoDeObra.Funcao,
                    Descricao = x.MaoDeObra.Descricao
                })
                .ToListAsync();
        }
    }

    public interface IObraMaoDeObraRepository : IGenericRepository<ObraMaoDeObra>
    {
        Task<bool> AddMaoDeObraToObra(int obraId, int maoDeObraId);
        Task<bool> RemoveMaoDeObraFromObra(int obraId, int maoDeObraId);
        Task<List<ObraMaoDeObraDTO>> GetMaoDeObraByObraId(int obraId);
    }
}