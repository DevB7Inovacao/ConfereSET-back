using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ChecklistItemRepository : GenericRepository<ChecklistItem>, IChecklistItemRepository
    {
        public ChecklistItemRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<List<ChecklistItem>> GetByChecklist(int checklistId)
        {
            return await _dbContext.Set<ChecklistItem>()
                .Where(x => x.ChecklistId == checklistId)
                .OrderBy(x => x.Ordem)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<ChecklistItem?> GetById(int id)
        {
            return await _dbContext.Set<ChecklistItem>().FirstOrDefaultAsync(x => x.Id == id);
        }
    }

    public interface IChecklistItemRepository : IGenericRepository<ChecklistItem>
    {
        Task<List<ChecklistItem>> GetByChecklist(int checklistId);
        Task<ChecklistItem?> GetById(int id);
    }
}