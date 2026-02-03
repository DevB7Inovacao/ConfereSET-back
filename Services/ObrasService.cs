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
                    ClientDocument = obra.ClientDocument
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
    }

    public interface IObrasService
    {
        public Task<Obras> CreateObra(Obras obras);
        public Task<bool> UpdateObra(Obras obras, int idObra);
        public Task<bool> DeleteObra(int obraId);
        public Task<bool> ToggleObraStatus(int obraId);
        public Task<Obras> GetObraById(int id);
        public Task<ObrasPagedDTO?> GetObrasPaged(FiltersObrasDTO filtersDTO);
        public Task<List<ObraSimpleDTO>> GetObrasSimple();
    }
}
