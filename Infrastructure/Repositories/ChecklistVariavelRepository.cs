using Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ChecklistVariavelRepository : GenericRepository<ChecklistVariavel>, IChecklistVariavelRepository
    {
        public ChecklistVariavelRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<List<ChecklistVariavel>> GetByChecklist(int empresaId, int checklistId)
        {
            return await _dbContext.Set<ChecklistVariavel>()
                .Include(x => x.ModeloTextoVariavel)
                .Where(x => x.EmpresaId == empresaId && x.ChecklistId == checklistId)
                .OrderByDescending(x => x.Status)
                .ThenBy(x => x.ModeloTextoVariavel!.Categoria)
                .ThenBy(x => x.ModeloTextoVariavel!.NomeAmigavel)
                .ToListAsync();
        }

        public async Task<List<ChecklistVariavel>> GetLinksOnly(int empresaId, int checklistId)
        {
            return await _dbContext.Set<ChecklistVariavel>()
                .Where(x => x.EmpresaId == empresaId && x.ChecklistId == checklistId)
                .ToListAsync();
        }

        public async Task<ChecklistVariavel?> GetByKey(int empresaId, int checklistId, int variavelId)
        {
            return await _dbContext.Set<ChecklistVariavel>()
                .FirstOrDefaultAsync(x =>
                    x.EmpresaId == empresaId &&
                    x.ChecklistId == checklistId &&
                    x.ModeloTextoVariavelId == variavelId);
        }
    }

    public interface IChecklistVariavelRepository : IGenericRepository<ChecklistVariavel>
    {
        Task<List<ChecklistVariavel>> GetByChecklist(int empresaId, int checklistId);
        Task<List<ChecklistVariavel>> GetLinksOnly(int empresaId, int checklistId);
        Task<ChecklistVariavel?> GetByKey(int empresaId, int checklistId, int variavelId);
    }
}