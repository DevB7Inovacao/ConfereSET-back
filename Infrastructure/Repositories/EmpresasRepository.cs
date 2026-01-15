using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class EmpresasRepository : GenericRepository<Empresas>, IEmpresasRepository
    {
        public EmpresasRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<Empresas> GetEmpresaById(int id)
        {
            return await _dbContext.Set<Empresas>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Empresas> GetEmpresasByName(string name)
        {
            return await _dbContext.Set<Empresas>().FirstOrDefaultAsync(x => x.Name == name);
        }

        public async Task<Empresas> GetEmpresasByCNPJ(string cnpj)
        {
            return await _dbContext.Set<Empresas>().FirstOrDefaultAsync(x => x.CNPJ == cnpj);
        }

        public async Task<PagedResult<Empresas>> GetAllEmpresasPaged(FiltersDTO filtersDTO)
        {
             return await _dbContext.Set<Empresas>()
            .Where(x => string.IsNullOrEmpty(filtersDTO.Name) || EF.Functions.Like(x.Name.ToLower(), $"%{filtersDTO.Name.ToLower()}%"))
            .GetPagedAsync<Empresas>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }
    }

    public interface IEmpresasRepository : IGenericRepository<Empresas>
    {
        public Task<Empresas> GetEmpresaById(int id);
        public Task<Empresas> GetEmpresasByName(string name);
        public Task<Empresas> GetEmpresasByCNPJ(string cnpj);
        public Task<PagedResult<Empresas>> GetAllEmpresasPaged(FiltersDTO filtersDTO);
    }
}
