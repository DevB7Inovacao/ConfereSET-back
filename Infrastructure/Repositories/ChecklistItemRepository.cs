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

        /// <summary>[v2] Maior Ordem do checklist (-1 se vazio).</summary>
        public async Task<int> GetMaxOrdem(int checklistId)
        {
            return await _dbContext.Set<ChecklistItem>()
                .Where(x => x.ChecklistId == checklistId)
                .MaxAsync(x => (int?)x.Ordem) ?? -1;
        }

        /// <summary>[v2] Quantidade de itens ativos por checklist.</summary>
        public async Task<Dictionary<int, int>> CountAtivosByChecklistIds(List<int> checklistIds)
        {
            if (checklistIds == null || checklistIds.Count == 0) return new Dictionary<int, int>();
            return await _dbContext.Set<ChecklistItem>()
                .AsNoTracking()
                .Where(x => checklistIds.Contains(x.ChecklistId) && x.Status == 1)
                .GroupBy(x => x.ChecklistId)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Total);
        }
    }

    public interface IChecklistItemRepository : IGenericRepository<ChecklistItem>
    {
        Task<List<ChecklistItem>> GetByChecklist(int checklistId);
        Task<ChecklistItem?> GetById(int id);
        Task<int> GetMaxOrdem(int checklistId);
        Task<Dictionary<int, int>> CountAtivosByChecklistIds(List<int> checklistIds);
    }
}