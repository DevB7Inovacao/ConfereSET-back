using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObraTipoOcorrenciaRepository : GenericRepository<ObraTipoOcorrencia>, IObraTipoOcorrenciaRepository
    {
        public ObraTipoOcorrenciaRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<bool> AddTipoOcorrenciaToObra(int obraId, int tipoOcorrenciaId)
        {
            var exists = await _dbContext.Set<ObraTipoOcorrencia>()
                .AnyAsync(x => x.ObraId == obraId && x.TipoOcorrenciaId == tipoOcorrenciaId);

            if (exists)
                return false;

            var obraTipoOcorrencia = new ObraTipoOcorrencia
            {
                ObraId = obraId,
                TipoOcorrenciaId = tipoOcorrenciaId
            };

            await _dbContext.Set<ObraTipoOcorrencia>().AddAsync(obraTipoOcorrencia);
            return true;
        }

        public async Task<bool> RemoveTipoOcorrenciaFromObra(int obraId, int tipoOcorrenciaId)
        {
            var relation = await _dbContext.Set<ObraTipoOcorrencia>()
                .FirstOrDefaultAsync(x => x.ObraId == obraId && x.TipoOcorrenciaId == tipoOcorrenciaId);

            if (relation == null)
                return false;

            _dbContext.Set<ObraTipoOcorrencia>().Remove(relation);
            return true;
        }

        public async Task<List<ObraTipoOcorrenciaDTO>> GetTiposOcorrenciaByObraId(int obraId)
        {
            return await _dbContext.Set<ObraTipoOcorrencia>()
                .AsNoTracking()
                .Where(x => x.ObraId == obraId)
                .Include(x => x.TipoOcorrencia)
                .Select(x => new ObraTipoOcorrenciaDTO
                {
                    Id = x.TipoOcorrencia!.Id,
                    Nome = x.TipoOcorrencia.Nome,
                    Descricao = x.TipoOcorrencia.Descricao,
                    Gravidade = x.TipoOcorrencia.Gravidade
                })
                .ToListAsync();
        }
    }

    public interface IObraTipoOcorrenciaRepository : IGenericRepository<ObraTipoOcorrencia>
    {
        Task<bool> AddTipoOcorrenciaToObra(int obraId, int tipoOcorrenciaId);
        Task<bool> RemoveTipoOcorrenciaFromObra(int obraId, int tipoOcorrenciaId);
        Task<List<ObraTipoOcorrenciaDTO>> GetTiposOcorrenciaByObraId(int obraId);
    }
}