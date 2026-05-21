using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;
using Infrastructure.Security;

namespace Services
{
    public class UserService : IUserService
    {
        public IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> CreateUser(User user)
        {
            if (user != null)
            {
                user.Password = Encrypt.HashPassword((user.Password ?? "").Trim());
                user.Email = (user.Email ?? "").Trim();
                user.Name = (user.Name ?? "").Trim();
                await _unitOfWork.Users.Add(user);

                var result = _unitOfWork.Save();

                if (result > 0)
                    return true;
                else
                    return false;
            }
            return false;
        }

        /// <summary>
        /// Valida a senha do usuário considerando o algoritmo de hash em uso (BCrypt) e o legado
        /// (AES reversível). Se a senha bate via AES, regrava com BCrypt — assim a migração é
        /// transparente: usuário não precisa redefinir senha, mas o banco vai sendo atualizado.
        /// </summary>
        public async Task<bool> VerifyPasswordAndUpgrade(User user, string plainPassword)
        {
            if (user == null || string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(user.Password))
                return false;

            var (ok, needsUpgrade) = Encrypt.VerifyPassword(plainPassword, user.Password);

            if (!ok) return false;

            if (needsUpgrade)
            {
                user.Password = Encrypt.HashPassword(plainPassword);
                _unitOfWork.Users.Update(user);
                _unitOfWork.Save();
            }

            return await Task.FromResult(true);
        }

        public async Task<UsersPagedDTO> GetUsersPaged(FiltersDTO filtersDTO)
        {
            try
            {
                var users = await _unitOfWork.Users.GetAllUsersPaged(filtersDTO);

                //if (users == null || users.Results == null || !users.Results.Any())
                //    throw new Exception("Nenhum dado foi encontrado.");

                var usersDTO = users.Results.Select(users => new UsersDTO
                {
                    Id = users.Id,
                    Name = users.Name,
                    Email = users.Email,
                    Type = users.Type,
                    Status = users.Status,
                    Empresas = new EmpresasDTO
                    {
                        Id = users.Empresa != null ? users.Empresa.Id : 0,
                        Name = users.Empresa != null ? users.Empresa.Name : string.Empty,
                        CNPJ = users.Empresa != null ? users.Empresa.CNPJ : string.Empty,
                        Status = users.Empresa != null ? users.Empresa.Status : null
                    }
                }).ToList();

                return new UsersPagedDTO() { Result = usersDTO, PageCount = users.PageCount };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> DeleteUser(int userId)
        {
            if (userId > 0)
            {
                var userDetail = await _unitOfWork.Users.GetById(userId);
                if (userDetail != null)
                {
                    _unitOfWork.Users.Delete(userDetail);
                    var result = _unitOfWork.Save();

                    if (result > 0)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            var userDetailList = await _unitOfWork.Users.GetAll();
            return userDetailList;
        }

        public async Task<UserSafeDTO?> GetUserById(int userId)
        {
            if (userId > 0)
            {
                var userDetail = await _unitOfWork.Users.GetUserSafeById(userId);
                return userDetail;
            }
            return null;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                var userDetail = await _unitOfWork.Users.GetUserByEmail(email);
                if (userDetail != null)
                    return userDetail;
            }
            return null;
        }

        public async Task<bool> UpdateUser(CreateUserRequest userParam, int userId)
        {
            if (userParam != null)
            {
                var user = await _unitOfWork.Users.GetById(userId);
                if (user != null)
                {
                    var senhaTrim = (userParam.Password ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(senhaTrim))
                        user.Password = Encrypt.HashPassword(senhaTrim);
                    user.Email = (userParam.Email ?? "").Trim();
                    user.Name = (userParam.Name ?? "").Trim();
                    user.Status = userParam.Status;
                    user.Type = userParam.Type;

                    _unitOfWork.Users.Update(user);

                    var result = _unitOfWork.Save();

                    if (result > 0)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }

        public async Task<int> CountUsersByEmpresaId(int empresaId)
        {
            if (empresaId <= 0) return 0;
            return await _unitOfWork.Users.CountUsersByEmpresaId(empresaId);
        }

        public async Task<int> CountUsersByEmpresaIdAndType(int empresaId, int type)
        {
            if (empresaId <= 0) return 0;
            return await _unitOfWork.Users.CountUsersByEmpresaIdAndType(empresaId, type);
        }
    public async Task<UsersPagedDTO> GetUsers(FiltersDTO filtersDTO)
    {
			try
			{
				var users = await _unitOfWork.Users.GetAllPaged(filtersDTO);

				//if (users == null || users.Results == null || !users.Results.Any())
				//    throw new Exception("Nenhum dado foi encontrado.");

				var usersDTO = users.Results.Select(users => new UsersDTO
				{
					Id = users.Id,
					Name = users.Name,
					Email = users.Email,
					Type = users.Type,
					Status = users.Status,
					Empresas = new EmpresasDTO
					{
						Id = users.Empresa != null ? users.Empresa.Id : 0,
						Name = users.Empresa != null ? users.Empresa.Name : string.Empty,
						CNPJ = users.Empresa != null ? users.Empresa.CNPJ : string.Empty,
						Status = users.Empresa != null ? users.Empresa.Status : null
					}
				}).ToList();

				return new UsersPagedDTO() { Result = usersDTO, PageCount = users.PageCount };
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		}

    public interface IUserService
    {
        Task<bool> CreateUser(User user);
        Task<bool> VerifyPasswordAndUpgrade(User user, string plainPassword);
        Task<UsersPagedDTO> GetUsersPaged(FiltersDTO filtersDTO);
        Task<IEnumerable<User>> GetAllUsers();
        Task<UserSafeDTO?> GetUserById(int userId);
        Task<User?> GetUserByEmail(string email);
        Task<bool> UpdateUser(CreateUserRequest userParam, int userId);
        Task<bool> DeleteUser(int userId);
        Task<int> CountUsersByEmpresaId(int empresaId);
        Task<int> CountUsersByEmpresaIdAndType(int empresaId, int type);
		Task<UsersPagedDTO> GetUsers(FiltersDTO filtersDTO);
	}
}