using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
	public class UserRepository : GenericRepository<User>, IUserRepository
	{

		public UserRepository(DbContextClass dbContext) : base(dbContext)
		{

		}
		public async Task<User?> GetUserByEmail(string email)
		{
			return await _dbContext.Set<User>()
				.Include(x => x.Empresa)
				.FirstOrDefaultAsync(x => x.Email == email);
		}

		public async Task<User?> GetUserById(int id)
		{
            return await _dbContext.Set<User>()
                .Include(x => x.Empresa)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<UserSafeDTO?> GetUserSafeById(int userId)
        {
            return await _dbContext.Set<User>()
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserSafeDTO
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Type = u.Type,
                    Status = u.Status,
                    EmpresaId = u.Empresa.Id
                    
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResult<User>> GetAllUsersPaged(FiltersDTO filtersDTO)
        {
            return await _dbContext.Set<User>()
				.Include(x => x.Empresa)
				.Where(x => string.IsNullOrEmpty(filtersDTO.Name) || EF.Functions.Like(x.Name.ToLower(), $"%{filtersDTO.Name.ToLower()}%"))
				.GetPagedAsync<User>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }

        public async Task<int> CountUsersByEmpresaId(int empresaId)
        {
            return await _dbContext.Set<User>()
                .AsNoTracking()
                .Where(u => u.Empresa != null && u.Empresa.Id == empresaId)
                .CountAsync();
        }
    }
    public interface IUserRepository : IGenericRepository<User>
	{
		public Task<User?> GetUserByEmail(string email);
		public Task<User?> GetUserById(int id);
        public Task<UserSafeDTO?> GetUserSafeById(int userId);
        public Task<PagedResult<User>> GetAllUsersPaged(FiltersDTO filtersDTO);
        public Task<int> CountUsersByEmpresaId(int empresaId);
    }
}
