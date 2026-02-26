using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObraModeloTextoRepository : GenericRepository<ObraModeloTexto>, IObraModeloTextoRepository
    {
        public ObraModeloTextoRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<bool> AddModeloTextoToObra(int obraId, int modeloTextoId)
        {
            var exists = await _dbContext.Set<ObraModeloTexto>()
                .AnyAsync(x => x.ObraId == obraId && x.ModeloTextoId == modeloTextoId);

            if (exists)
                return false;

            var obraModeloTexto = new ObraModeloTexto
            {
                ObraId = obraId,
                ModeloTextoId = modeloTextoId
            };

            await _dbContext.Set<ObraModeloTexto>().AddAsync(obraModeloTexto);
            return true;
        }

        public async Task<bool> RemoveModeloTextoFromObra(int obraId, int modeloTextoId)
        {
            var relation = await _dbContext.Set<ObraModeloTexto>()
                .FirstOrDefaultAsync(x => x.ObraId == obraId && x.ModeloTextoId == modeloTextoId);

            if (relation == null)
                return false;

            _dbContext.Set<ObraModeloTexto>().Remove(relation);
            return true;
        }

        public async Task<List<ObraModeloTextoDTO>> GetModelosTextoByObraId(int obraId)
        {
            return await _dbContext.Set<ObraModeloTexto>()
                .AsNoTracking()
                .Where(x => x.ObraId == obraId)
                .Include(x => x.ModeloTexto)
                .Select(x => new ObraModeloTextoDTO
                {
                    Id = x.ModeloTexto!.Id,
                    Nome = x.ModeloTexto.Nome,
                    Texto = x.ModeloTexto.Texto,
                    Status = x.ModeloTexto.Status
                })
                .ToListAsync();
        }
    }

    public interface IObraModeloTextoRepository : IGenericRepository<ObraModeloTexto>
    {
        Task<bool> AddModeloTextoToObra(int obraId, int modeloTextoId);
        Task<bool> RemoveModeloTextoFromObra(int obraId, int modeloTextoId);
        Task<List<ObraModeloTextoDTO>> GetModelosTextoByObraId(int obraId);
    }
}