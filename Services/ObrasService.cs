using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class ObrasService : IObrasService
    {
        public IUnitOfWork _unitOfWork;

        public ObrasService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Obras> CreateObra(Obras obras)
        {
            try
            {
                if (obras == null)
                    throw new ArgumentNullException(nameof(obras));
                await _unitOfWork.Obras.Add(obras);
                _unitOfWork.Save();
                return obras;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> UpdateObra(Obras obra, int idObra)
        {
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> DeleteObra(int obraId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                {
                    throw new Exception("Obra não encontrada.");
                }
                _unitOfWork.Obras.Delete(obra);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao excluir a obra: " + ex.Message);
            }
        }

        public async Task<bool> ToggleObraStatus(int obraId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                {
                    throw new Exception("Obra não encontrada.");
                }

                obra.Status = obra.Status == 1 ? 0 : 1;

                _unitOfWork.Obras.Update(obra);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao alterar o status da obra: " + ex.Message);
            }
        }

        public async Task<Obras> GetObraById(int id)
        {
            return await _unitOfWork.Obras.GetObraById(id);
        }

        public async Task<ObrasPagedDTO> GetObrasPaged(FiltersObrasDTO filtersDTO)
        {
            try
            {
                var obras = await _unitOfWork.Obras.GetAllObrasPaged(filtersDTO);

                if (obras == null || obras.Results == null || !obras.Results.Any())
                {
                    throw new Exception("Nenhum dado foi encontrado.");
                }

                var obraIds = obras.Results.Select(o => o.Id).ToList();
                var operadoresCounts = await GetOperadoresCountsByObraIds(obraIds);

                var obraDTO = obras.Results.Select(obra => new ObrasDTO
                {
                    Id = obra.Id,
                    Name = obra.Name,
                    Status = obra.Status,
                    StreetAddress = obra.StreetAddress,
                    Number = obra.Number,
                    AddressLine2 = obra.AddressLine2,
                    Neighborhood = obra.Neighborhood,
                    City = obra.City,
                    State = obra.State,
                    PostalCode = obra.PostalCode,
                    Country = obra.Country,
                    ClientName = obra.ClientName,
                    ClientEmail = obra.ClientEmail,
                    ClientPhone = obra.ClientPhone,
                    ClientDocument = obra.ClientDocument,
                    EmpresaId = obra.EmpresaId,
                    StartDate = obra.StartDate,
                    ProgressPercentage = obra.ProgressPercentage,
                    OperadoresCount = operadoresCounts.ContainsKey(obra.Id) ? operadoresCounts[obra.Id] : 0
                }).ToList();

                return new ObrasPagedDTO() { Result = obraDTO, PageCount = obras.PageCount };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ObraSimpleDTO>> GetObrasSimple()
        {
            var list = await _unitOfWork.Obras.GetObrasSimple();
            return list;
        }

        public async Task<bool> AddOperadorToObra(int obraId, int operadorId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                    throw new Exception("Obra não encontrada.");

                var operador = await _unitOfWork.Users.GetById(operadorId);
                if (operador == null)
                    throw new Exception("Operador não encontrado.");

                if (operador.Type != 3)
                    throw new Exception("Usuário não é um operador.");

                var added = await _unitOfWork.ObraOperadores.AddOperadorToObra(obraId, operadorId);
                if (!added)
                    throw new Exception("Operador já está vinculado a esta obra.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveOperadorFromObra(int obraId, int operadorId)
        {
            try
            {
                var removed = await _unitOfWork.ObraOperadores.RemoveOperadorFromObra(obraId, operadorId);
                if (!removed)
                    throw new Exception("Relação não encontrada.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ObraOperadorDTO>> GetOperadoresByObraId(int obraId)
        {
            return await _unitOfWork.ObraOperadores.GetOperadoresByObraId(obraId);
        }

        public async Task<List<ObrasDTO>> GetObrasByOperadorId(int operadorId)
        {
            return await _unitOfWork.ObraOperadores.GetObrasByOperadorId(operadorId);
        }

        public async Task<ObraWithOperadoresDTO?> GetObraWithOperadores(int obraId)
        {
            return await _unitOfWork.ObraOperadores.GetObraWithOperadores(obraId);
        }

        public async Task<List<ObraCardDTO>> GetObrasCardsByEmpresaId(int empresaId)
        {
            return await _unitOfWork.Obras.GetObrasCardsByEmpresaId(empresaId);
        }

        public async Task<List<ObraCardDTO>> GetObrasCardsByOperadorId(int operadorId)
        {
            return await _unitOfWork.Obras.GetObrasCardsByOperadorId(operadorId);
        }

        private async Task<Dictionary<int, int>> GetOperadoresCountsByObraIds(List<int> obraIds)
        {
            var counts = new Dictionary<int, int>();
            foreach (var obraId in obraIds)
            {
                var operadores = await _unitOfWork.ObraOperadores.GetOperadoresByObraId(obraId);
                counts[obraId] = operadores.Count;
            }
            return counts;
        }
    }

    public interface IObrasService
    {
        Task<Obras> CreateObra(Obras obras);
        Task<bool> UpdateObra(Obras obras, int idObra);
        Task<bool> DeleteObra(int obraId);
        Task<bool> ToggleObraStatus(int obraId);
        Task<Obras> GetObraById(int id);
        Task<ObrasPagedDTO?> GetObrasPaged(FiltersObrasDTO filtersDTO);
        Task<List<ObraSimpleDTO>> GetObrasSimple();
        Task<bool> AddOperadorToObra(int obraId, int operadorId);
        Task<bool> RemoveOperadorFromObra(int obraId, int operadorId);
        Task<List<ObraOperadorDTO>> GetOperadoresByObraId(int obraId);
        Task<List<ObrasDTO>> GetObrasByOperadorId(int operadorId);
        Task<ObraWithOperadoresDTO?> GetObraWithOperadores(int obraId);
        Task<List<ObraCardDTO>> GetObrasCardsByEmpresaId(int empresaId);
        Task<List<ObraCardDTO>> GetObrasCardsByOperadorId(int operadorId);
    }
}