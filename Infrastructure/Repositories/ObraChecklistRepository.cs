using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObraChecklistRepository : GenericRepository<ObraChecklist>, IObraChecklistRepository
    {
        public ObraChecklistRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<bool> Exists(int obraId, int checklistId)
        {
            return await _dbContext.Set<ObraChecklist>()
                .AnyAsync(x => x.ObraId == obraId && x.ChecklistId == checklistId);
        }

        public async Task<ObraChecklist?> GetById(int id)
        {
            return await _dbContext.Set<ObraChecklist>()
                .Include(x => x.Checklist)
                .Include(x => x.Itens)
                    .ThenInclude(i => i.ChecklistItem)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<ObraChecklist>> GetByObra(int obraId)
        {
            return await _dbContext.Set<ObraChecklist>()
                .Include(x => x.Checklist)
                .Include(x => x.Itens)
                    .ThenInclude(i => i.ChecklistItem)
                .Where(x => x.ObraId == obraId)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }
    }

    public interface IObraChecklistRepository : IGenericRepository<ObraChecklist>
    {
        Task<bool> Exists(int obraId, int checklistId);
        Task<ObraChecklist?> GetById(int id);
        Task<List<ObraChecklist>> GetByObra(int obraId);
    }
}