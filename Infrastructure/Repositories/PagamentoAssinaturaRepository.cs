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
    }

    public interface IPagamentoAssinaturaRepository : IGenericRepository<PagamentoAssinatura>
    {
        Task<bool> ExistsByMPPaymentId(string mpPaymentId);
        Task<List<PagamentoAssinatura>> GetByAssinaturaId(int assinaturaId);
    }
}