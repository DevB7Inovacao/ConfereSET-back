using Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ModeloTextoVariavelVinculoRepository : GenericRepository<ModeloTextoVariavelVinculo>, IModeloTextoVariavelVinculoRepository
    {
        public ModeloTextoVariavelVinculoRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<List<ModeloTextoVariavelVinculo>> GetByModelo(int empresaId, int modeloTextoId)
        {
            return await _dbContext.Set<ModeloTextoVariavelVinculo>()
                .Include(x => x.ModeloTextoVariavel)
                .Where(x => x.EmpresaId == empresaId && x.ModeloTextoId == modeloTextoId)
                .OrderByDescending(x => x.Status)
                .ThenBy(x => x.ModeloTextoVariavel!.Categoria)
                .ThenBy(x => x.ModeloTextoVariavel!.NomeAmigavel)
                .ToListAsync();
        }

        public async Task<List<ModeloTextoVariavelVinculo>> GetLinksOnly(int empresaId, int modeloTextoId)
        {
            return await _dbContext.Set<ModeloTextoVariavelVinculo>()
                .Where(x => x.EmpresaId == empresaId && x.ModeloTextoId == modeloTextoId)
                .ToListAsync();
        }

        public async Task<ModeloTextoVariavelVinculo?> GetByKey(int empresaId, int modeloTextoId, int variavelId)
        {
            return await _dbContext.Set<ModeloTextoVariavelVinculo>()
                .FirstOrDefaultAsync(x =>
                    x.EmpresaId == empresaId &&
                    x.ModeloTextoId == modeloTextoId &&
                    x.ModeloTextoVariavelId == variavelId);
        }
    }

    public interface IModeloTextoVariavelVinculoRepository : IGenericRepository<ModeloTextoVariavelVinculo>
    {
        Task<List<ModeloTextoVariavelVinculo>> GetByModelo(int empresaId, int modeloTextoId);
        Task<List<ModeloTextoVariavelVinculo>> GetLinksOnly(int empresaId, int modeloTextoId);
        Task<ModeloTextoVariavelVinculo?> GetByKey(int empresaId, int modeloTextoId, int variavelId);
    }
}