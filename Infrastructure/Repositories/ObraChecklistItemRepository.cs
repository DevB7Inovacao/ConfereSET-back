using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObraChecklistItemRepository : GenericRepository<ObraChecklistItem>, IObraChecklistItemRepository
    {
        public ObraChecklistItemRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<ObraChecklistItem?> GetById(int id)
        {
            return await _dbContext.Set<ObraChecklistItem>()
                .Include(x => x.ChecklistItem)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<ObraChecklistItem>> GetByObraChecklist(int obraChecklistId)
        {
            return await _dbContext.Set<ObraChecklistItem>()
                .Include(x => x.ChecklistItem)
                .Where(x => x.ObraChecklistId == obraChecklistId)
                .OrderBy(x => x.ChecklistItem!.Ordem)
                .ToListAsync();
        }
    }

    public interface IObraChecklistItemRepository : IGenericRepository<ObraChecklistItem>
    {
        Task<ObraChecklistItem?> GetById(int id);
        Task<List<ObraChecklistItem>> GetByObraChecklist(int obraChecklistId);
    }
}