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

		public async Task<List<Assinatura>> GetAllPaged(int page, int pageSize)
		{
			return await _dbContext.Set<Assinatura>()
					.AsNoTracking()
					.Include(x => x.Empresa)
					.Include(x => x.Plano)
					.OrderByDescending(x => x.CreatedDate)
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync();
		}

		public async Task<int> CountAll()
		{
			return await _dbContext.Set<Assinatura>().CountAsync();
		}
		public async Task<Assinatura?> GetByExternalReference(string externaReference)
		{
			return await _dbContext.Set<Assinatura>()
						.Include(x => x.Plano)
						.FirstOrDefaultAsync(x => x.ExternalReference == externaReference);
		}
		public async Task<Assinatura?> GetPendingByPlanIdAndNoMPId(string preapprovalPlanId)
		{
			// Busca a assinatura pendente mais recente para este plano
			// que ainda não tem MPPreapprovalId associado
			return await _dbContext.Set<Assinatura>()
					.Where(a => a.Plano.MPPreapprovalPlanId == preapprovalPlanId
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
		Task<List<Assinatura>> GetAllPaged(int page, int pageSize);
		Task<int> CountAll();
		Task<Assinatura?> GetByExternalReference(string externaReference);
		Task<Assinatura?> GetPendingByPlanIdAndNoMPId(string preapprovalPlanId);
	}
}