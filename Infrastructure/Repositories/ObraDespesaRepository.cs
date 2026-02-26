using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObraDespesaRepository : GenericRepository<ObraDespesa>, IObraDespesaRepository
    {
        public ObraDespesaRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<bool> AddDespesaToObra(int obraId, int despesaId)
        {
            var exists = await _dbContext.Set<ObraDespesa>()
                .AnyAsync(x => x.ObraId == obraId && x.DespesaId == despesaId);

            if (exists)
                return false;

            var obraDespesa = new ObraDespesa
            {
                ObraId = obraId,
                DespesaId = despesaId
            };

            await _dbContext.Set<ObraDespesa>().AddAsync(obraDespesa);
            return true;
        }

        public async Task<bool> RemoveDespesaFromObra(int obraId, int despesaId)
        {
            var relation = await _dbContext.Set<ObraDespesa>()
                .FirstOrDefaultAsync(x => x.ObraId == obraId && x.DespesaId == despesaId);

            if (relation == null)
                return false;

            _dbContext.Set<ObraDespesa>().Remove(relation);
            return true;
        }

        public async Task<List<ObraDespesaDTO>> GetDespesasByObraId(int obraId)
        {
            return await _dbContext.Set<ObraDespesa>()
                .AsNoTracking()
                .Where(x => x.ObraId == obraId)
                .Include(x => x.Despesa)
                .Select(x => new ObraDespesaDTO
                {
                    Id = x.Despesa!.Id,
                    Name = x.Despesa.Name,
                    Amount = x.Despesa.Amount,
                    Date = x.Despesa.Date,
                    Category = x.Despesa.Category,
                    Description = x.Despesa.Description,
                    Status = x.Despesa.Status
                })
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }
    }

    public interface IObraDespesaRepository : IGenericRepository<ObraDespesa>
    {
        Task<bool> AddDespesaToObra(int obraId, int despesaId);
        Task<bool> RemoveDespesaFromObra(int obraId, int despesaId);
        Task<List<ObraDespesaDTO>> GetDespesasByObraId(int obraId);
    }
}