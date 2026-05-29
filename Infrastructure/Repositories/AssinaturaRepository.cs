using Core.Enums;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
	public class AssinaturaRepository : GenericRepository<Assinatura>, IAssinaturaRepository
	{
		public AssinaturaRepository(DbContextClass dbContext) : base(dbContext) { }

		public async Task<Assinatura?> GetAssinaturaById(int id)
		{
			return await _dbContext.Set<Assinatura>()
					.Include(x => x.Empresa)
					.Include(x => x.Plano)
					.Include(x => x.Pagamentos)
					.FirstOrDefaultAsync(x => x.Id == id);
		}

		public async Task<Assinatura?> GetAssinaturaAtivaByEmpresaId(int empresaId)
		{
			return await _dbContext.Set<Assinatura>()
					.Include(x => x.Plano)
					.Where(x => x.EmpresaId == empresaId && x.Status == Core.Enums.StatusAssinatura.Ativa)
					.FirstOrDefaultAsync();
		}

		public async Task<Assinatura?> GetByMPSubscriptionId(string mpSubscriptionId)
		{
			return await _dbContext.Set<Assinatura>()
					.Include(x => x.Plano)
					.FirstOrDefaultAsync(x => x.MPSubscriptionId == mpSubscriptionId);
		}

		public async Task<List<Assinatura>> GetAllPaged(int page, int pageSize, int empresaId)
		{
			// empresaId <= 0 → sem filtro de empresa (visão do dono da plataforma).
			var query = _dbContext.Set<Assinatura>()
					.AsNoTracking()
					.Include(x => x.Empresa)
					.Include(x => x.Plano)
					.AsQueryable();

			if (empresaId > 0)
				query = query.Where(x => x.EmpresaId == empresaId);

			return await query
					.OrderByDescending(x => x.CreatedDate)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync();
		}

		public async Task<int> CountAll(int empresaId = 0)
		{
			var query = _dbContext.Set<Assinatura>().AsQueryable();
			if (empresaId > 0)
				query = query.Where(x => x.EmpresaId == empresaId);
			return await query.CountAsync();
		}

		public async Task<int> CountByPlanoId(int planoId)
		{
			return await _dbContext.Set<Assinatura>().CountAsync(x => x.PlanoId == planoId);
		}
		public async Task<Assinatura?> GetByExternalReference(string externaReference)
		{
			return await _dbContext.Set<Assinatura>()
						.Include(x => x.Plano)
						.FirstOrDefaultAsync(x => x.ExternalReference == externaReference);
		}
		public async Task<Assinatura?> GetPendingByPlanIdAndNoMPId(string preapprovalPlanId)
		{
			// Legado — mantido para compatibilidade de interface. Não usar em novos fluxos.
			return await _dbContext.Set<Assinatura>()
					.Where(a => a.Plano.MPPreapprovalPlanId == preapprovalPlanId
											&& a.Status == StatusAssinatura.Pendente
											&& a.MPSubscriptionId == null)
					.OrderByDescending(a => a.DataInicio)
					.FirstOrDefaultAsync();
		}

		public async Task<Assinatura?> GetPendingEmpresaSemMPId(int empresaId)
		{
			return await _dbContext.Set<Assinatura>()
					.Where(a => a.EmpresaId == empresaId
											&& a.Status == StatusAssinatura.Pendente
											&& a.MPSubscriptionId == null)
					.OrderByDescending(a => a.DataInicio)
					.FirstOrDefaultAsync();
		}
	}

	public interface IAssinaturaRepository : IGenericRepository<Assinatura>
	{
		Task<Assinatura?> GetAssinaturaById(int id);
		Task<Assinatura?> GetAssinaturaAtivaByEmpresaId(int empresaId);
		Task<Assinatura?> GetByMPSubscriptionId(string mpSubscriptionId);
		Task<List<Assinatura>> GetAllPaged(int page, int pageSize, int empresaId);
		Task<int> CountAll(int empresaId = 0);
		Task<int> CountByPlanoId(int planoId);
		Task<Assinatura?> GetByExternalReference(string externaReference);
		Task<Assinatura?> GetPendingByPlanIdAndNoMPId(string preapprovalPlanId);
		Task<Assinatura?> GetPendingEmpresaSemMPId(int empresaId);
	}
}