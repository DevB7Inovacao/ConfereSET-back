using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PagamentoAssinaturaRepository : GenericRepository<PagamentoAssinatura>, IPagamentoAssinaturaRepository
    {
        public PagamentoAssinaturaRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<bool> ExistsByMPPaymentId(string mpPaymentId)
        {
            return await _dbContext.Set<PagamentoAssinatura>()
                .AnyAsync(x => x.MPPaymentId == mpPaymentId);
        }

        public async Task<List<PagamentoAssinatura>> GetByAssinaturaId(int assinaturaId)
        {
            return await _dbContext.Set<PagamentoAssinatura>()
                .AsNoTracking()
                .Where(x => x.AssinaturaId == assinaturaId)
                .OrderByDescending(x => x.DataPagamento)
                .ToListAsync();
        }

        /// <summary>Todos os pagamentos (visão do admin master), com empresa e plano.</summary>
        public async Task<List<PagamentoAssinatura>> GetAllComDetalhes(DateTime? de, DateTime? ate)
        {
            var query = _dbContext.Set<PagamentoAssinatura>()
                .AsNoTracking()
                .Include(x => x.Assinatura).ThenInclude(a => a!.Empresa)
                .Include(x => x.Assinatura).ThenInclude(a => a!.Plano)
                .AsQueryable();

            if (de.HasValue) query = query.Where(x => x.DataPagamento >= de.Value);
            if (ate.HasValue) query = query.Where(x => x.DataPagamento <= ate.Value);

            return await query.OrderByDescending(x => x.DataPagamento).ToListAsync();
        }
    }

    public interface IPagamentoAssinaturaRepository : IGenericRepository<PagamentoAssinatura>
    {
        Task<bool> ExistsByMPPaymentId(string mpPaymentId);
        Task<List<PagamentoAssinatura>> GetByAssinaturaId(int assinaturaId);
        Task<List<PagamentoAssinatura>> GetAllComDetalhes(DateTime? de, DateTime? ate);
    }
}