using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class EmpresasService : IEmpresasService
    {
        public IUnitOfWork _unitOfWork;
        public IUserService _userService;

        public EmpresasService(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        public async Task<Empresas> CreateEmpresa(Empresas empresas)
        {
            try
            {
                if (empresas == null)
                    throw new ArgumentNullException(nameof(empresas));

                var existingCNPJ = await _unitOfWork.Empresas.GetEmpresasByCNPJ(empresas.CNPJ);
                if (existingCNPJ != null || empresas == null || !ValidarCNPJ(empresas.CNPJ))
                {
                    throw new Exception("CNPJ inválido.");
                }

                await _unitOfWork.Empresas.Add(empresas);
                _unitOfWork.Save();
                return empresas;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static bool ValidarCNPJ(string cnpj)
        {
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            cnpj = cnpj.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
                return false;

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
        }


        public async Task<bool> UpdateEmpresa(EmpresasDTO empresasParam, int idEmpresa)
        {
            if (empresasParam != null)
            {
                var empresa = await _unitOfWork.Empresas.GetEmpresaById(idEmpresa);

                if (empresa != null)
                {
                    empresa.Name = empresasParam.Name ?? empresa.Name;
                    empresa.Status = empresasParam.Status ?? empresa.Status;
                    empresa.CNPJ = empresasParam.CNPJ ?? empresa.CNPJ;

                    _unitOfWork.Empresas.Update(empresa);

                    var result = _unitOfWork.Save();

                    if (result > 0)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }

        public async Task<bool> DeleteEmpresa(int empresaId)
        {
            try
            {
                var empresa = await _unitOfWork.Empresas.GetEmpresaById(empresaId);
                if (empresa == null)
                {
                    throw new Exception("Empresa não encontrada.");
                }

                _unitOfWork.Empresas.Delete(empresa);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao excluir a empresa: " + ex.Message);
            }
        }

        public async Task<bool> ToggleEmpresaStatus(int empresaId)
        {
            try
            {
                var empresa = await _unitOfWork.Empresas.GetEmpresaById(empresaId);
                if (empresa == null)
                {
                    throw new Exception("Empresa não encontrada.");
                }

                empresa.Status = empresa.Status == true ? false : true;

                _unitOfWork.Empresas.Update(empresa);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao alterar o status da empresa: " + ex.Message);
            }
        }

        public async Task<Empresas> GetEmpresasByName(string name)
        {
            try
            {
                var empresa = await _unitOfWork.Empresas.GetEmpresasByName(name);

                if (empresa == null)
                {
                    throw new Exception("Nenhum dado foi encontrado.");
                }

                return empresa;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<Empresas> GetEmpresaById(int id)
        {
            return await _unitOfWork.Empresas.GetEmpresaById(id);
        }

        public async Task<EmpresasPagedDTO> GetEmpresasPaged(FiltersDTO filtersDTO)
        {
            try
            {
                var empresas = await _unitOfWork.Empresas.GetAllEmpresasPaged(filtersDTO);

                if (empresas == null || empresas.Results == null || !empresas.Results.Any())
                {
                    throw new Exception("Nenhum dado foi encontrado.");
                }

                var empresaDTO = empresas.Results.Select(empresa => new EmpresasDTO
                {
                    Id = empresa.Id,
                    Name = empresa.Name,
                    CNPJ = empresa.CNPJ,
                    Status = empresa.Status,
                }).ToList();

                return new EmpresasPagedDTO() { Result = empresaDTO, PageCount = empresas.PageCount };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }

    public interface IEmpresasService
    {
        public Task<Empresas> GetEmpresasByName(string name);
        public Task<Empresas> CreateEmpresa(Empresas empresas);
        public Task<bool> UpdateEmpresa(EmpresasDTO empresas, int idEmpresa);
        public Task<bool> DeleteEmpresa(int empresaId);
        public Task<bool> ToggleEmpresaStatus(int empresaId);
        public Task<Empresas> GetEmpresaById(int id);
        public Task<EmpresasPagedDTO?> GetEmpresasPaged(FiltersDTO filtersDTO);
    }
}
