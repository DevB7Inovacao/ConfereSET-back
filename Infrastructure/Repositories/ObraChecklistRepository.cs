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

		/// <summary>[v2] Há execução NÃO concluída deste checklist na obra?</summary>
		public async Task<bool> ExistsEmAndamento(int obraId, int checklistId)
		{
			return await _dbContext.Set<ObraChecklist>()
					.AnyAsync(x => x.ObraId == obraId && x.ChecklistId == checklistId && x.ConcluidoEm == null);
		}

		/// <summary>[v2] Ids das execuções da obra (opcionalmente de um checklist), mais recentes primeiro.</summary>
		public async Task<List<int>> GetIdsByObra(int obraId, int? checklistId = null)
		{
			var q = _dbContext.Set<ObraChecklist>().AsNoTracking().Where(x => x.ObraId == obraId);
			if (checklistId.HasValue) q = q.Where(x => x.ChecklistId == checklistId.Value);
			return await q.OrderByDescending(x => x.Id).Select(x => x.Id).ToListAsync();
		}

		/// <summary>[v2] Escopo leve (obra + concluído) sem carregar itens.</summary>
		public async Task<ObraChecklist?> GetHeaderById(int id)
		{
			return await _dbContext.Set<ObraChecklist>()
					.Include(x => x.Checklist)
					.FirstOrDefaultAsync(x => x.Id == id);
		}

		public async Task<ObraChecklist?> GetById(int id)
		{
			return await _dbContext.Set<ObraChecklist>()
					.Include(x => x.Obra)
					.Include(x => x.Checklist)
					.Include(x => x.Itens)
							.ThenInclude(i => i.ChecklistItem)
					.Include(x => x.Itens)
							.ThenInclude(i => i.Fotos)
					.AsSplitQuery()
					.FirstOrDefaultAsync(x => x.Id == id);
		}

		public async Task<List<ObraChecklist>> GetByObra(int obraId)
		{
			return await _dbContext.Set<ObraChecklist>()
					.Include(x => x.Obra)
					.Include(x => x.Checklist)
					.Include(x => x.Itens)
							.ThenInclude(i => i.ChecklistItem)
					.Include(x => x.Itens)
							.ThenInclude(i => i.Fotos)
					.Where(x => x.ObraId == obraId)
					.OrderByDescending(x => x.Id)
					.AsSplitQuery()
					.ToListAsync();
		}
		public async Task<List<ObraChecklist>> GetByObraEmpresa(int empresaId)
		{
			return await _dbContext.Set<ObraChecklist>()
				.Include(x => x.Obra)
					.Include(x => x.Checklist)
					.Include(x => x.Itens)

							.ThenInclude(i => i.ChecklistItem)
					.Include(x => x.Itens)
							.ThenInclude(i => i.Fotos)
					.Where(x => x.Obra.EmpresaId == empresaId)
					.OrderByDescending(x => x.Id)
					.AsSplitQuery()
					.ToListAsync();
		}

		public async Task<List<ObraChecklist>> GetByChecklistId(int checklistId)
		{
			return await _dbContext.Set<ObraChecklist>()
					.Include(x => x.Obra)
					.Include(x => x.Itens)
							.ThenInclude(i => i.ChecklistItem)
					.Include(x => x.Itens)
							.ThenInclude(i => i.Fotos)
					.Where(x => x.ChecklistId == checklistId)
					.AsSplitQuery()
					.ToListAsync();
		}
	}

	public interface IObraChecklistRepository : IGenericRepository<ObraChecklist>
	{
		Task<bool> Exists(int obraId, int checklistId);
		Task<bool> ExistsEmAndamento(int obraId, int checklistId);
		Task<List<int>> GetIdsByObra(int obraId, int? checklistId = null);
		Task<ObraChecklist?> GetHeaderById(int id);
		Task<ObraChecklist?> GetById(int id);
		Task<List<ObraChecklist>> GetByObra(int obraId);
		Task<List<ObraChecklist>> GetByChecklistId(int checklistId);
		Task<List<ObraChecklist>> GetByObraEmpresa(int empresaId);

		}
}
