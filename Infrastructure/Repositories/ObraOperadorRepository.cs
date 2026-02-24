using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ObraOperadorRepository : GenericRepository<ObraOperador>, IObraOperadorRepository
    {
        public ObraOperadorRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<bool> AddOperadorToObra(int obraId, int operadorId)
        {
            var exists = await _dbContext.Set<ObraOperador>()
                .AnyAsync(x => x.ObraId == obraId && x.OperadorId == operadorId);

            if (exists)
                return false;

            var obraOperador = new ObraOperador
            {
                ObraId = obraId,
                OperadorId = operadorId
            };

            await _dbContext.Set<ObraOperador>().AddAsync(obraOperador);
            return true;
        }

        public async Task<bool> RemoveOperadorFromObra(int obraId, int operadorId)
        {
            var relation = await _dbContext.Set<ObraOperador>()
                .FirstOrDefaultAsync(x => x.ObraId == obraId && x.OperadorId == operadorId);

            if (relation == null)
                return false;

            _dbContext.Set<ObraOperador>().Remove(relation);
            return true;
        }

        public async Task<List<ObraOperadorDTO>> GetOperadoresByObraId(int obraId)
        {
            return await _dbContext.Set<ObraOperador>()
                .AsNoTracking()
                .Where(x => x.ObraId == obraId)
                .Include(x => x.Operador)
                .Select(x => new ObraOperadorDTO
                {
                    Id = x.Operador!.Id,
                    Name = x.Operador.Name
                })
                .ToListAsync();
        }

        public async Task<List<ObrasDTO>> GetObrasByOperadorId(int operadorId)
        {
            return await _dbContext.Set<ObraOperador>()
                .AsNoTracking()
                .Where(x => x.OperadorId == operadorId)
                .Include(x => x.Obra)
                .Select(x => new ObrasDTO
                {
                    Id = x.Obra!.Id,
                    Name = x.Obra.Name,
                    Status = x.Obra.Status,
                    StreetAddress = x.Obra.StreetAddress,
                    Number = x.Obra.Number,
                    AddressLine2 = x.Obra.AddressLine2,
                    Neighborhood = x.Obra.Neighborhood,
                    City = x.Obra.City,
                    State = x.Obra.State,
                    PostalCode = x.Obra.PostalCode,
                    Country = x.Obra.Country,
                    ClientName = x.Obra.ClientName,
                    ClientEmail = x.Obra.ClientEmail,
                    ClientPhone = x.Obra.ClientPhone,
                    ClientDocument = x.Obra.ClientDocument
                })
                .ToListAsync();
        }

        public async Task<ObraWithOperadoresDTO?> GetObraWithOperadores(int obraId)
        {
            var obra = await _dbContext.Set<Obras>()
                .AsNoTracking()
                .Where(x => x.Id == obraId)
                .Select(x => new ObraWithOperadoresDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Status = x.Status,
                    StreetAddress = x.StreetAddress,
                    Number = x.Number,
                    AddressLine2 = x.AddressLine2,
                    Neighborhood = x.Neighborhood,
                    City = x.City,
                    State = x.State,
                    PostalCode = x.PostalCode,
                    Country = x.Country,
                    ClientName = x.ClientName,
                    ClientEmail = x.ClientEmail,
                    ClientPhone = x.ClientPhone,
                    ClientDocument = x.ClientDocument
                })
                .FirstOrDefaultAsync();

            if (obra == null)
                return null;

            obra.Operadores = await GetOperadoresByObraId(obraId);
            return obra;
        }
    }

    public interface IObraOperadorRepository : IGenericRepository<ObraOperador>
    {
        Task<bool> AddOperadorToObra(int obraId, int operadorId);
        Task<bool> RemoveOperadorFromObra(int obraId, int operadorId);
        Task<List<ObraOperadorDTO>> GetOperadoresByObraId(int obraId);
        Task<List<ObrasDTO>> GetObrasByOperadorId(int operadorId);
        Task<ObraWithOperadoresDTO?> GetObraWithOperadores(int obraId);
    }
}