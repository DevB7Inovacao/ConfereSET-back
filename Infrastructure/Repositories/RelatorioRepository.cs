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
                    .ThenInclude(s => s.Comentarios.OrderBy(c => c.CreatedDate).ThenBy(c => c.Id))
                        .ThenInclude(c => c.Autor)
                // Split query: evita o produto cartesiano Secoes x Itens x Fotos x Comentarios
                // (single query multiplicava as linhas — e os bytes das fotos — por comentário).
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedResult<Relatorio>> GetPaged(FiltersRelatorioDTO filters)
        {
            var query = _dbContext.Relatorios
                .AsNoTracking()
                .AsQueryable();

            if (filters.ObraId.HasValue)
                query = query.Where(x => x.ObraId == filters.ObraId.Value);

            if (filters.EmpresaId.HasValue)
                query = query.Where(x => x.Obra != null && x.Obra.EmpresaId == filters.EmpresaId.Value);

            if (filters.CriadoPorUserId.HasValue)
                query = query.Where(x => x.CriadoPorUserId == filters.CriadoPorUserId.Value);

            if (filters.Status.HasValue)
                query = query.Where(x => x.Status == filters.Status.Value);

            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                var term = $"%{filters.Search.Trim().ToLower()}%";
                query = query.Where(x =>
                    EF.Functions.Like(x.Titulo.ToLower(), term)
                    || (x.Obra != null && EF.Functions.Like(x.Obra.Name.ToLower(), term))
                    || (x.CriadoPor != null && EF.Functions.Like(x.CriadoPor.Name.ToLower(), term)));
            }

            if (filters.DataDe.HasValue)
            {
                var de = filters.DataDe.Value.Date;
                query = query.Where(x => x.DataRelatorio >= de);
            }

            if (filters.DataAte.HasValue)
            {
                var ateExclusivo = filters.DataAte.Value.Date.AddDays(1);
                query = query.Where(x => x.DataRelatorio < ateExclusivo);
            }

            var total = await query.CountAsync();
            var pageSize = filters.PageSize > 0 ? filters.PageSize : 10;
            var pageNumber = filters.PageNumber > 0 ? filters.PageNumber : 1;
            var pageCount = (int)Math.Ceiling(total / (double)pageSize);

            // Lista: só as colunas que a listagem mostra. Antes vinham o HTML do relatório, o HTML
            // do modelo e a logo da empresa (base64) em cada linha — e eram descartados no serviço.
            var results = await query
                .OrderByDescending(x => x.DataRelatorio)
                .ThenByDescending(x => x.CreatedDate)
                .ThenBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new Relatorio
                {
                    Id = x.Id,
                    ModeloTextoId = x.ModeloTextoId,
                    ObraId = x.ObraId,
                    CriadoPorUserId = x.CriadoPorUserId,
                    Titulo = x.Titulo,
                    Status = x.Status,
                    DataRelatorio = x.DataRelatorio,
                    CreatedDate = x.CreatedDate,
                    ObservacaoRejeicao = x.ObservacaoRejeicao,
                    ModeloTexto = new ModeloTexto { Id = x.ModeloTexto!.Id, EmpresaId = x.ModeloTexto.EmpresaId, Nome = x.ModeloTexto.Nome, Texto = null! },
                    Obra = x.Obra == null ? null : new Obras
                    {
                        Id = x.Obra.Id,
                        EmpresaId = x.Obra.EmpresaId,
                        Name = x.Obra.Name,
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
                        Empresa = x.Obra.Empresa == null ? null : new Empresas
                        {
                            Id = x.Obra.Empresa.Id,
                            Name = x.Obra.Empresa.Name,
                            Phone = x.Obra.Empresa.Phone,
                            ContactEmail = x.Obra.Empresa.ContactEmail,
                        },
                    },
                    CriadoPor = x.CriadoPor == null ? null : new User
                    {
                        Id = x.CriadoPor.Id,
                        Name = x.CriadoPor.Name,
                        Email = "",
                        Password = "",
                        Type = x.CriadoPor.Type,
                        Status = x.CriadoPor.Status,
                        Empresa = null!,
                    },
                })
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

        public void UpdateFoto(RelatorioItemFoto foto)
        {
            _dbContext.RelatorioItemFotos.Update(foto);
        }

        /// <summary>Escopo mínimo (autor, status, empresa) para autorizar escrita — uma consulta leve.</summary>
        public async Task<RelatorioFotoEscopoDTO?> GetEscopo(int relatorioId)
        {
            return await _dbContext.Relatorios
                .AsNoTracking()
                .Where(r => r.Id == relatorioId)
                .Select(r => new RelatorioFotoEscopoDTO
                {
                    RelatorioId = r.Id,
                    CriadoPorUserId = r.CriadoPorUserId,
                    Status = r.Status,
                    EmpresaId = r.Obra!.EmpresaId
                })
                .FirstOrDefaultAsync();
        }

        public async Task<int?> GetRelatorioIdByItemId(int itemId) =>
            await _dbContext.RelatorioSecaoItens.AsNoTracking()
                .Where(i => i.Id == itemId).Select(i => (int?)i.RelatorioSecao!.RelatorioId).FirstOrDefaultAsync();

        public async Task<int?> GetRelatorioIdBySecaoId(int secaoId) =>
            await _dbContext.RelatorioSecoes.AsNoTracking()
                .Where(s => s.Id == secaoId).Select(s => (int?)s.RelatorioId).FirstOrDefaultAsync();

        public async Task<(int RelatorioId, int AutorId)?> GetRelatorioEAutorByComentarioId(int comentarioId)
        {
            var r = await _dbContext.RelatorioComentarios.AsNoTracking()
                .Where(c => c.Id == comentarioId)
                .Select(c => new { c.RelatorioSecao!.RelatorioId, c.AutorId })
                .FirstOrDefaultAsync();
            return r == null ? null : (r.RelatorioId, r.AutorId);
        }

        public async Task<List<RelatorioFotoEscopoDTO>> GetFotoEscopos(List<int> fotoIds)
        {
            if (fotoIds == null || fotoIds.Count == 0) return new List<RelatorioFotoEscopoDTO>();

            return await _dbContext.RelatorioItemFotos
                .AsNoTracking()
                .Where(f => fotoIds.Contains(f.Id))
                .Select(f => new RelatorioFotoEscopoDTO
                {
                    FotoId = f.Id,
                    RelatorioId = f.RelatorioSecaoItem!.RelatorioSecao!.RelatorioId,
                    CriadoPorUserId = f.RelatorioSecaoItem.RelatorioSecao.Relatorio!.CriadoPorUserId,
                    Status = f.RelatorioSecaoItem.RelatorioSecao.Relatorio.Status,
                    EmpresaId = f.RelatorioSecaoItem.RelatorioSecao.Relatorio.Obra!.EmpresaId
                })
                .ToListAsync();
        }

        public async Task<RelatorioComentario?> GetComentarioById(int comentarioId)
        {
            return await _dbContext.RelatorioComentarios
                .Include(x => x.Autor)
                .FirstOrDefaultAsync(x => x.Id == comentarioId);
        }

        public async Task AddSecao(RelatorioSecao secao)
        {
            await _dbContext.RelatorioSecoes.AddAsync(secao);
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
        void UpdateFoto(RelatorioItemFoto foto);
        Task<List<RelatorioFotoEscopoDTO>> GetFotoEscopos(List<int> fotoIds);
        Task<RelatorioFotoEscopoDTO?> GetEscopo(int relatorioId);
        Task<int?> GetRelatorioIdByItemId(int itemId);
        Task<int?> GetRelatorioIdBySecaoId(int secaoId);
        Task<(int RelatorioId, int AutorId)?> GetRelatorioEAutorByComentarioId(int comentarioId);
        Task<RelatorioComentario?> GetComentarioById(int comentarioId);
        Task AddSecao(RelatorioSecao secao);
        Task AddComentario(RelatorioComentario comentario);
        void UpdateComentario(RelatorioComentario comentario);
        void DeleteComentario(RelatorioComentario comentario);
        Task<RelatorioSecao?> GetSecaoById(int secaoId);
    }
}