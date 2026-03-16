using Core.DTO;
using Core.Enums;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class OcorrenciaRepository : GenericRepository<Ocorrencia>, IOcorrenciaRepository
    {
        public OcorrenciaRepository(DbContextClass dbContext) : base(dbContext) { }

        public async Task<Ocorrencia?> GetOcorrenciaById(int id)
        {
            return await _dbContext.Set<Ocorrencia>()
                .Include(x => x.Obra)
                .Include(x => x.TipoOcorrencia)
                .Include(x => x.CriadoPor)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<Ocorrencia>> GetPaged(FiltersOcorrenciaDTO filters)
        {
            var query = _dbContext.Set<Ocorrencia>()
                .Include(x => x.Obra)
                .Include(x => x.TipoOcorrencia)
                .Include(x => x.CriadoPor)
                .AsQueryable();

            if (filters.OperadorId.HasValue)
            {
                var obrasDoOperador = await _dbContext.Set<ObraOperador>()
                    .Where(oo => oo.OperadorId == filters.OperadorId.Value)
                    .Select(oo => oo.ObraId)
                    .ToListAsync();

                if (!obrasDoOperador.Any())
                    return new PagedResult<Ocorrencia> { Results = new List<Ocorrencia>(), PageCount = 0 };

                query = query.Where(x => obrasDoOperador.Contains(x.ObraId));
            }

            if (filters.ObraId.HasValue)
                query = query.Where(x => x.ObraId == filters.ObraId.Value);

            if (filters.EmpresaId.HasValue)
                query = query.Where(x => x.Obra != null && x.Obra.EmpresaId == filters.EmpresaId.Value);

            if (filters.CriadoPorUserId.HasValue)
                query = query.Where(x => x.CriadoPorUserId == filters.CriadoPorUserId.Value);

            if (filters.Status.HasValue)
                query = query.Where(x => x.Status == filters.Status.Value);

            if (filters.TipoOcorrenciaId.HasValue)
                query = query.Where(x => x.TipoOcorrenciaId == filters.TipoOcorrenciaId.Value);

            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                var s = filters.Search.ToLower();
                query = query.Where(x =>
                    EF.Functions.Like((x.Titulo ?? string.Empty).ToLower(), $"%{s}%") ||
                    EF.Functions.Like((x.Descricao ?? string.Empty).ToLower(), $"%{s}%") ||
                    EF.Functions.Like((x.Localizacao ?? string.Empty).ToLower(), $"%{s}%")
                );
            }

            query = query.OrderByDescending(x => x.DataOcorrencia);

            return await query.GetPagedAsync<Ocorrencia>(filters.PageNumber, filters.PageSize);
        }

        public async Task<List<OcorrenciaDTO>> GetByObraId(int obraId)
        {
            return await _dbContext.Set<Ocorrencia>()
                .AsNoTracking()
                .Include(x => x.TipoOcorrencia)
                .Include(x => x.CriadoPor)
                .Where(x => x.ObraId == obraId)
                .OrderByDescending(x => x.DataOcorrencia)
                .Select(x => new OcorrenciaDTO
                {
                    Id = x.Id,
                    ObraId = x.ObraId,
                    ObraNome = x.Obra != null ? x.Obra.Name : null,
                    TipoOcorrenciaId = x.TipoOcorrenciaId,
                    TipoOcorrenciaNome = x.TipoOcorrencia != null ? x.TipoOcorrencia.Nome : null,
                    TipoOcorrenciaGravidade = x.TipoOcorrencia != null ? x.TipoOcorrencia.Gravidade : 0,
                    Titulo = x.Titulo,
                    Descricao = x.Descricao,
                    Localizacao = x.Localizacao,
                    Status = x.Status,
                    DataOcorrencia = x.DataOcorrencia,
                    CriadoPorUserId = x.CriadoPorUserId,
                    CriadoPorNome = x.CriadoPor != null ? x.CriadoPor.Name : null,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync();
        }
    }

    public interface IOcorrenciaRepository : IGenericRepository<Ocorrencia>
    {
        Task<Ocorrencia?> GetOcorrenciaById(int id);
        Task<PagedResult<Ocorrencia>> GetPaged(FiltersOcorrenciaDTO filters);
        Task<List<OcorrenciaDTO>> GetByObraId(int obraId);
    }
}