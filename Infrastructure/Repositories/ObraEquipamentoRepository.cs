using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObraEquipamentoRepository : GenericRepository<ObraEquipamento>, IObraEquipamentoRepository
    {
        public ObraEquipamentoRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<bool> AddEquipamentoToObra(int obraId, int equipamentoId)
        {
            var exists = await _dbContext.Set<ObraEquipamento>()
                .AnyAsync(x => x.ObraId == obraId && x.EquipamentoId == equipamentoId);

            if (exists)
                return false;

            var obraEquipamento = new ObraEquipamento
            {
                ObraId = obraId,
                EquipamentoId = equipamentoId
            };

            await _dbContext.Set<ObraEquipamento>().AddAsync(obraEquipamento);
            return true;
        }

        public async Task<bool> RemoveEquipamentoFromObra(int obraId, int equipamentoId)
        {
            var relation = await _dbContext.Set<ObraEquipamento>()
                .FirstOrDefaultAsync(x => x.ObraId == obraId && x.EquipamentoId == equipamentoId);

            if (relation == null)
                return false;

            _dbContext.Set<ObraEquipamento>().Remove(relation);
            return true;
        }

        public async Task<List<ObraEquipamentoDTO>> GetEquipamentosByObraId(int obraId)
        {
            return await _dbContext.Set<ObraEquipamento>()
                .AsNoTracking()
                .Where(x => x.ObraId == obraId)
                .Include(x => x.Equipamento)
                .Select(x => new ObraEquipamentoDTO
                {
                    Id = x.Equipamento!.Id,
                    Nome = x.Equipamento.Nome,
                    Descricao = x.Equipamento.Descricao
                })
                .ToListAsync();
        }
    }

    public interface IObraEquipamentoRepository : IGenericRepository<ObraEquipamento>
    {
        Task<bool> AddEquipamentoToObra(int obraId, int equipamentoId);
        Task<bool> RemoveEquipamentoFromObra(int obraId, int equipamentoId);
        Task<List<ObraEquipamentoDTO>> GetEquipamentosByObraId(int obraId);
    }
}