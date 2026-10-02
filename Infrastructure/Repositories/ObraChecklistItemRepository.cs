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
                .Include(x => x.Fotos)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<ObraChecklistItem>> GetByObraChecklist(int obraChecklistId)
        {
            return await _dbContext.Set<ObraChecklistItem>()
                .Include(x => x.ChecklistItem)
                .Include(x => x.Fotos)
                .Where(x => x.ObraChecklistId == obraChecklistId)
                .OrderBy(x => x.ChecklistItem!.Ordem)
                .ToListAsync();
        }

        public async Task<List<ObraChecklistItem>> GetByChecklistItem(int checklistItemId)
        {
            return await _dbContext.Set<ObraChecklistItem>()
                .Include(x => x.Fotos)
                .Where(x => x.ChecklistItemId == checklistItemId)
                .ToListAsync();
        }

        // ---------------------------------------------------------------------
        // [Conferelist v2] Fotos dos itens
        // ---------------------------------------------------------------------

        public async Task AddFoto(ObraChecklistItemFoto foto)
        {
            await _dbContext.Set<ObraChecklistItemFoto>().AddAsync(foto);
        }

        public async Task<ObraChecklistItemFoto?> GetFotoById(int fotoId)
        {
            return await _dbContext.Set<ObraChecklistItemFoto>().FirstOrDefaultAsync(x => x.Id == fotoId);
        }

        public void UpdateFoto(ObraChecklistItemFoto foto)
        {
            _dbContext.Set<ObraChecklistItemFoto>().Update(foto);
        }

        public void DeleteFoto(ObraChecklistItemFoto foto)
        {
            _dbContext.Set<ObraChecklistItemFoto>().Remove(foto);
        }

        public async Task<int> CountFotos(int obraChecklistItemId)
        {
            return await _dbContext.Set<ObraChecklistItemFoto>().CountAsync(x => x.ObraChecklistItemId == obraChecklistItemId);
        }
    }

    public interface IObraChecklistItemRepository : IGenericRepository<ObraChecklistItem>
    {
        Task<ObraChecklistItem?> GetById(int id);
        Task<List<ObraChecklistItem>> GetByObraChecklist(int obraChecklistId);
        Task<List<ObraChecklistItem>> GetByChecklistItem(int checklistItemId);
        Task AddFoto(ObraChecklistItemFoto foto);
        Task<ObraChecklistItemFoto?> GetFotoById(int fotoId);
        void UpdateFoto(ObraChecklistItemFoto foto);
        void DeleteFoto(ObraChecklistItemFoto foto);
        Task<int> CountFotos(int obraChecklistItemId);
    }
}
