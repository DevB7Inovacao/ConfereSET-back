using Core.DTO;
using Core.Enums;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Saller.Infrastructure.ServiceExtension;

namespace Infrastructure.Repositories
{
    public class RelatorioRepository : IRelatorioRepository
    {
        private readonly DbContextClass _dbContext;

        public RelatorioRepository(DbContextClass dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(Relatorio relatorio)
        {
            await _dbContext.Relatorios.AddAsync(relatorio);
        }

        public void Update(Relatorio relatorio)
        {
            _dbContext.Relatorios.Update(relatorio);
        }

        public void Delete(Relatorio relatorio)
        {
            _dbContext.Relatorios.Remove(relatorio);
        }

        public async Task<Relatorio?> GetById(int id)
        {
            return await _dbContext.Relatorios
                .Include(x => x.ModeloTexto)
                .Include(x => x.Obra).ThenInclude(x=>x.Empresa)
                .Include(x => x.CriadoPor)
                .Include(x => x.Secoes.OrderBy(s => s.Ordem))
                    .ThenInclude(s => s.TipoOcorrencia)
                .Include(x => x.Secoes.OrderBy(s => s.Ordem))
                    .ThenInclude(s => s.Itens)
                        .ThenInclude(i => i.Fotos)
                .Include(x => x.Secoes.OrderBy(s => s.Ordem))
                    .ThenInclude(s => s.Comentarios)
                        .ThenInclude(c => c.Autor)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<Relatorio>> GetPaged(FiltersRelatorioDTO filters)
        {
            var query = _dbContext.Relatorios
                .Include(x => x.ModeloTexto)
                .Include(x => x.Obra).ThenInclude(x=>x.Empresa)
                .Include(x => x.CriadoPor)
                .AsQueryable();

            if (filters.ObraId.HasValue)
                query = query.Where(x => x.ObraId == filters.ObraId.Value);

            if (filters.EmpresaId.HasValue)
                query = query.Where(x => x.Obra != null && x.Obra.EmpresaId == filters.EmpresaId.Value);

            if (filters.CriadoPorUserId.HasValue)
                query = query.Where(x => x.CriadoPorUserId == filters.CriadoPorUserId.Value);

            if (filters.Status.HasValue)
                query = query.Where(x => x.Status == filters.Status.Value);

            var total = await query.CountAsync();
            var pageSize = filters.PageSize > 0 ? filters.PageSize : 10;
            var pageNumber = filters.PageNumber > 0 ? filters.PageNumber : 1;
            var pageCount = (int)Math.Ceiling(total / (double)pageSize);

            var results = await query
                .OrderByDescending(x => x.DataRelatorio)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Relatorio> { Results = results, PageCount = pageCount };
        }

        public async Task<RelatorioSecaoItem?> GetItemById(int itemId)
        {
            return await _dbContext.RelatorioSecaoItens
                .Include(x => x.Fotos)
                .FirstOrDefaultAsync(x => x.Id == itemId);
        }

        public async Task AddItem(RelatorioSecaoItem item)
        {
            await _dbContext.RelatorioSecaoItens.AddAsync(item);
        }

        public void UpdateItem(RelatorioSecaoItem item)
        {
            _dbContext.RelatorioSecaoItens.Update(item);
        }

        public void DeleteItem(RelatorioSecaoItem item)
        {
            _dbContext.RelatorioSecaoItens.Remove(item);
        }

        public async Task AddFoto(RelatorioItemFoto foto)
        {
            await _dbContext.RelatorioItemFotos.AddAsync(foto);
        }

        public async Task<RelatorioItemFoto?> GetFotoById(int fotoId)
        {
            return await _dbContext.RelatorioItemFotos.FirstOrDefaultAsync(x => x.Id == fotoId);
        }

        public void DeleteFoto(RelatorioItemFoto foto)
        {
            _dbContext.RelatorioItemFotos.Remove(foto);
        }

        public async Task<RelatorioComentario?> GetComentarioById(int comentarioId)
        {
            return await _dbContext.RelatorioComentarios
                .Include(x => x.Autor)
                .FirstOrDefaultAsync(x => x.Id == comentarioId);
        }

        public async Task AddComentario(RelatorioComentario comentario)
        {
            await _dbContext.RelatorioComentarios.AddAsync(comentario);
        }

        public void UpdateComentario(RelatorioComentario comentario)
        {
            _dbContext.RelatorioComentarios.Update(comentario);
        }

        public void DeleteComentario(RelatorioComentario comentario)
        {
            _dbContext.RelatorioComentarios.Remove(comentario);
        }

        public async Task<RelatorioSecao?> GetSecaoById(int secaoId)
        {
            return await _dbContext.RelatorioSecoes
                .Include(x => x.Comentarios)
                    .ThenInclude(c => c.Autor)
                .FirstOrDefaultAsync(x => x.Id == secaoId);
        }
    }

    public interface IRelatorioRepository
    {
        Task Add(Relatorio relatorio);
        void Update(Relatorio relatorio);
        void Delete(Relatorio relatorio);
        Task<Relatorio?> GetById(int id);
        Task<PagedResult<Relatorio>> GetPaged(FiltersRelatorioDTO filters);
        Task<RelatorioSecaoItem?> GetItemById(int itemId);
        Task AddItem(RelatorioSecaoItem item);
        void UpdateItem(RelatorioSecaoItem item);
        void DeleteItem(RelatorioSecaoItem item);
        Task AddFoto(RelatorioItemFoto foto);
        Task<RelatorioItemFoto?> GetFotoById(int fotoId);
        void DeleteFoto(RelatorioItemFoto foto);
        Task<RelatorioComentario?> GetComentarioById(int comentarioId);
        Task AddComentario(RelatorioComentario comentario);
        void UpdateComentario(RelatorioComentario comentario);
        void DeleteComentario(RelatorioComentario comentario);
        Task<RelatorioSecao?> GetSecaoById(int secaoId);
    }
}