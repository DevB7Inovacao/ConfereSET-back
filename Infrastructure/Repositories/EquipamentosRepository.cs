using Core.DTO;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class EquipamentosRepository : GenericRepository<Equipamentos>, IEquipamentosRepository
    {
        public EquipamentosRepository(DbContextClass dbContext) : base(dbContext)
        {
        }

        public async Task<Equipamentos> GetEquipamentoById(int id)
        {
            return await _dbContext.Set<Equipamentos>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<Equipamentos>> GetAllEquipamentosPaged(FiltersEquipamentosDTO filtersDTO)
        {
            var query = _dbContext.Set<Equipamentos>().AsQueryable();

            if (filtersDTO.EmpresaId.HasValue && filtersDTO.EmpresaId.Value > 0)
                query = query.Where(x => x.EmpresaId == filtersDTO.EmpresaId.Value);

            if (!string.IsNullOrWhiteSpace(filtersDTO.Search))
            {
                var s = filtersDTO.Search.ToLower();
                query = query.Where(x =>
                    EF.Functions.Like((x.Nome ?? string.Empty).ToLower(), $"%{s}%") ||
                    EF.Functions.Like((x.Descricao ?? string.Empty).ToLower(), $"%{s}%")
                );
            }

            if (filtersDTO.Status.HasValue)
            {
                query = query.Where(x => x.Status == filtersDTO.Status.Value);
            }

            query = query.OrderBy(x => x.Nome);

            return await query.GetPagedAsync<Equipamentos>(filtersDTO.pageNumber, filtersDTO.pageSize);
        }

        public async Task<List<EquipamentoSimpleDTO>> GetEquipamentosSimple(int empresaId)
        {
            return await _dbContext.Set<Equipamentos>()
                .Where(x => x.EmpresaId == empresaId && x.Status == 1)
                .OrderBy(x => x.Nome)
                .Select(x => new EquipamentoSimpleDTO
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = x.Descricao
                })
                .ToListAsync();
        }
    }

    public interface IEquipamentosRepository : IGenericRepository<Equipamentos>
    {
        Task<Equipamentos> GetEquipamentoById(int id);
        Task<PagedResult<Equipamentos>> GetAllEquipamentosPaged(FiltersEquipamentosDTO filtersDTO);
        Task<List<EquipamentoSimpleDTO>> GetEquipamentosSimple(int empresaId);
    }
}