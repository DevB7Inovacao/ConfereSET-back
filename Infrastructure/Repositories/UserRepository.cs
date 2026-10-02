using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
	public class UserRepository : GenericRepository<User>, IUserRepository
	{
		public UserRepository(DbContextClass dbContext) : base(dbContext) { }

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
					.Where(x => (string.IsNullOrEmpty(filtersDTO.Name)
					|| EF.Functions.Like(x.Name.ToLower(), $"%{filtersDTO.Name.ToLower()}%")
					|| EF.Functions.Like(x.Email.ToLower(), $"%{filtersDTO.Name.ToLower()}%"))
					&& x.EmpresaId == filtersDTO.EmpresaId
					// O master da plataforma nunca aparece para a empresa, mesmo cadastrado nela.
					&& x.Type != TypeUser.admin
					)
					.OrderBy(x => x.Name).ThenBy(x => x.Id)
					.GetPagedAsync<User>(filtersDTO.pageNumber, filtersDTO.pageSize);
		}

		public async Task<PagedResult<User>> GetAllPaged(FiltersDTO filtersDTO)
		{
			// Visão do admin master: todas as empresas, com filtro opcional por empresa
			// e busca por nome OU e-mail.
			var termo = (filtersDTO.Name ?? "").Trim().ToLower();
			return await _dbContext.Set<User>()
						.Include(x => x.Empresa)
						.Where(x => (termo == ""
							|| EF.Functions.Like(x.Name.ToLower(), $"%{termo}%")
							|| EF.Functions.Like(x.Email.ToLower(), $"%{termo}%"))
							&& (filtersDTO.EmpresaId <= 0 || x.EmpresaId == filtersDTO.EmpresaId))
						.OrderBy(x => x.Name)
						.GetPagedAsync<User>(filtersDTO.pageNumber, filtersDTO.pageSize);
		}

		public async Task<int> CountUsersByEmpresaId(int empresaId)
		{
			return await _dbContext.Set<User>()
					.AsNoTracking()
					.Where(u => u.Empresa != null && u.Empresa.Id == empresaId && u.Type != TypeUser.admin)
					.CountAsync();
		}

		public async Task<int> CountUsersByEmpresaIdAndType(int empresaId, int type)
		{
			return await _dbContext.Set<User>()
					.AsNoTracking()
					.Where(u => u.Empresa != null && u.Empresa.Id == empresaId && u.Type == (TypeUser)type)
					.CountAsync();
		}

		/// <summary>[v2] Id → Nome (projeção leve, sem senha) para exibir autoria em DTOs.</summary>
		public async Task<Dictionary<int, string>> GetNamesByIds(IEnumerable<int> ids)
		{
			var lista = ids?.Distinct().ToList() ?? new List<int>();
			if (lista.Count == 0) return new Dictionary<int, string>();
			return await _dbContext.Set<User>()
					.AsNoTracking()
					.Where(u => lista.Contains(u.Id))
					.Select(u => new { u.Id, u.Name })
					.ToDictionaryAsync(u => u.Id, u => u.Name);
		}
	}

	public interface IUserRepository : IGenericRepository<User>
	{
		Task<User?> GetUserByEmail(string email);
		Task<User?> GetUserById(int id);
		Task<UserSafeDTO?> GetUserSafeById(int userId);
		Task<PagedResult<User>> GetAllUsersPaged(FiltersDTO filtersDTO);
		Task<int> CountUsersByEmpresaId(int empresaId);
		Task<int> CountUsersByEmpresaIdAndType(int empresaId, int type);
		Task<PagedResult<User>> GetAllPaged(FiltersDTO filtersDTO);
		Task<Dictionary<int, string>> GetNamesByIds(IEnumerable<int> ids);

	}
}