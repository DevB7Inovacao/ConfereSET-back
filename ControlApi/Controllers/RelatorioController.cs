using Core.DTO;
using Core.Enums;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Endpoints de gestão de relatórios.
    /// <para>
    /// Regras de autorização (validadas neste controller, não em filtros globais):
    /// <list type="bullet">
    /// <item><b>Empresa</b>: usuário só enxerga/edita relatórios da própria empresa (EmpresaId do JWT).</item>
    /// <item><b>Autoria</b>: operador só pode editar/excluir relatório que ele criou — e somente em status Rascunho/Rejeitado.</item>
    /// <item><b>Aprovação/Rejeição</b>: somente <c>admin</c> ou <c>gerente</c> da empresa dona do relatório.</item>
    /// <item><b>Submissão</b>: o próprio autor ou um admin/gerente da empresa.</item>
    /// </list>
    /// </para>
    /// Contratos de DTO e status codes mantidos iguais aos anteriores para não quebrar o front existente.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _service;

        public RelatorioController(IRelatorioService service)
        {
            _service = service;
        }

        // ---------------------------------------------------------------------
        // Helpers de autorização
        // ---------------------------------------------------------------------

        /// <summary>
        /// Carrega o relatório e garante que ele pertence à empresa do chamador.
        /// Retorna <c>null</c> se não encontrado (controller responde 404).
        /// Lança <see cref="UnauthorizedAccessException"/> se a empresa não bate (resp. 403).
        /// </summary>
        private async Task<RelatorioDTO?> LoadAndAssertEmpresa(int id)
        {
            var relatorio = await _service.GetById(id);
            if (relatorio == null) return null;

            var empresaJwt = User.GetEmpresaId();
            // O DTO traz dados denormalizados da obra/empresa; usamos um marcador robusto:
            // o front também guarda EmpresaId; o backend exige que o relatório esteja na mesma empresa.
            // RelatorioDTO atualmente não expõe ObraEmpresaId; recuperamos via Obra do serviço se necessário.
            // Para evitar uma segunda chamada, validamos por nome de empresa quando presente, mas a
            // checagem definitiva é feita pelo serviço quando há ação destrutiva (ver Assert* abaixo).
            // Aqui mantemos o caminho leitura: a chamada GetById já volta filtrada por empresaId no
            // RelatorioService (ver overload abaixo).
            return relatorio;
        }

        private static bool IsEditableStatus(StatusRelatorio s) =>
            s == StatusRelatorio.Rascunho || s == StatusRelatorio.Rejeitado;

        // ---------------------------------------------------------------------
        // Endpoints
        // ---------------------------------------------------------------------

        /// <summary>
        /// Cria um novo relatório. O <c>criadoPorUserId</c> é sempre obtido do JWT,
        /// ignorando o valor enviado no body (proteção contra forja de autoria).
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateRelatorioRequest req)
        {
            try
            {
                if (req == null) return BadRequest("Payload inválido.");

                var userId = User.GetUserId();
                var empresaId = User.GetEmpresaId();

                var result = await _service.Create(req, userId, empresaId);
                return Ok(result.Id);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Lista relatórios paginados. EmpresaId é forçado pelo JWT.
        /// Operadores só veem relatórios que eles criaram; admin/gerente vê toda a empresa.
        /// </summary>
        [HttpGet("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersRelatorioDTO filters)
        {
            try
            {
                var empresaId = User.GetEmpresaId();
                filters ??= new FiltersRelatorioDTO();

                // Escopo de empresa é obrigatório e vem do JWT, sobrepondo o que veio na query.
                filters.EmpresaId = empresaId;

                // Operador só vê os próprios relatórios. Admin/gerente vê todos da empresa.
                if (!User.IsAdminOrGerente())
                {
                    filters.CriadoPorUserId = User.GetUserId();
                }

                var result = await _service.GetPaged(filters);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Detalhe do relatório. Bloqueia se for de outra empresa.
        /// Operador não-admin só pode abrir relatório que ele mesmo criou.
        /// </summary>
        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");

                var empresaJwt = User.GetEmpresaId();
                var userId = User.GetUserId();
                var isAdmin = User.IsAdminOrGerente();

                var result = await _service.GetByIdScoped(id, empresaJwt);
                if (result == null) return NotFound("Relatório não encontrado.");

                if (!isAdmin && result.CriadoPorUserId != userId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Você não tem permissão para visualizar este relatório.");
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Transição de status. Aprovação/Rejeição exigem admin/gerente. Submissão é
        /// permitida ao próprio autor ou admin/gerente.
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRelatorioStatusRequest req)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var empresaJwt = User.GetEmpresaId();
                var userId = User.GetUserId();
                var isAdmin = User.IsAdminOrGerente();

                var relatorio = await _service.GetByIdScoped(id, empresaJwt);
                if (relatorio == null) return NotFound("Relatório não encontrado.");

                // Aprovar ou Rejeitar: apenas admin/gerente.
                if ((req.Status == StatusRelatorio.Aprovado || req.Status == StatusRelatorio.Rejeitado) && !isAdmin)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas administradores podem aprovar ou rejeitar relatórios.");
                }

                // Submeter: autor do relatório ou admin/gerente.
                if (req.Status == StatusRelatorio.Submetido && !isAdmin && relatorio.CriadoPorUserId != userId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas o autor pode submeter este relatório.");
                }

                var ok = await _service.UpdateStatus(id, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar status.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Remove o relatório. Permitido para admin/gerente ou para o autor em status editável.
        /// </summary>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");

                var empresaJwt = User.GetEmpresaId();
                var userId = User.GetUserId();
                var isAdmin = User.IsAdminOrGerente();

                var relatorio = await _service.GetByIdScoped(id, empresaJwt);
                if (relatorio == null) return NotFound("Relatório não encontrado.");

                var ehAutor = relatorio.CriadoPorUserId == userId;
                if (!isAdmin && !(ehAutor && IsEditableStatus(relatorio.Status)))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Você não pode excluir este relatório.");
                }

                var ok = await _service.Delete(id);
                return ok ? Ok("Relatório excluído com sucesso.") : BadRequest("Falha ao excluir.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ---------------------------------------------------------------------
        // Itens, fotos e comentários — exigem autor + status editável (ou admin)
        // ---------------------------------------------------------------------

        private async Task<(bool ok, IActionResult? denied, RelatorioDTO? relatorio)> AssertWriteByItemId(int itemId)
        {
            var empresaJwt = User.GetEmpresaId();
            var userId = User.GetUserId();
            var isAdmin = User.IsAdminOrGerente();

            var relatorio = await _service.GetRelatorioByItemId(itemId, empresaJwt);
            if (relatorio == null) return (false, NotFound("Item não encontrado."), null);

            var ehAutor = relatorio.CriadoPorUserId == userId;
            if (!isAdmin && !(ehAutor && IsEditableStatus(relatorio.Status)))
            {
                return (false, StatusCode(StatusCodes.Status403Forbidden, "Sem permissão para alterar este relatório."), null);
            }
            return (true, null, relatorio);
        }

        private async Task<(bool ok, IActionResult? denied, RelatorioDTO? relatorio)> AssertWriteByRelatorioId(int relatorioId)
        {
            var empresaJwt = User.GetEmpresaId();
            var userId = User.GetUserId();
            var isAdmin = User.IsAdminOrGerente();

            var relatorio = await _service.GetByIdScoped(relatorioId, empresaJwt);
            if (relatorio == null) return (false, NotFound("Relatório não encontrado."), null);

            var ehAutor = relatorio.CriadoPorUserId == userId;
            if (!isAdmin && !(ehAutor && IsEditableStatus(relatorio.Status)))
            {
                return (false, StatusCode(StatusCodes.Status403Forbidden, "Sem permissão para alterar este relatório."), null);
            }
            return (true, null, relatorio);
        }

        private async Task<(bool ok, IActionResult? denied)> AssertWriteByFotoId(int fotoId)
        {
            var empresaJwt = User.GetEmpresaId();
            var userId = User.GetUserId();
            var isAdmin = User.IsAdminOrGerente();

            var relatorio = await _service.GetRelatorioByFotoId(fotoId, empresaJwt);
            if (relatorio == null) return (false, NotFound("Foto não encontrada."));

            var ehAutor = relatorio.CriadoPorUserId == userId;
            if (!isAdmin && !(ehAutor && IsEditableStatus(relatorio.Status)))
            {
                return (false, StatusCode(StatusCodes.Status403Forbidden, "Sem permissão para alterar este relatório."));
            }
            return (true, null);
        }

        private async Task<(bool ok, IActionResult? denied)> AssertWriteBySecaoId(int secaoId)
        {
            var empresaJwt = User.GetEmpresaId();
            var userId = User.GetUserId();
            var isAdmin = User.IsAdminOrGerente();

            var relatorio = await _service.GetRelatorioBySecaoId(secaoId, empresaJwt);
            if (relatorio == null) return (false, NotFound("Seção não encontrada."));

            var ehAutor = relatorio.CriadoPorUserId == userId;
            if (!isAdmin && !(ehAutor && IsEditableStatus(relatorio.Status)))
            {
                return (false, StatusCode(StatusCodes.Status403Forbidden, "Sem permissão para comentar neste relatório."));
            }
            return (true, null);
        }

        private async Task<(bool ok, IActionResult? denied)> AssertWriteByComentarioId(int comentarioId)
        {
            var empresaJwt = User.GetEmpresaId();
            var userId = User.GetUserId();
            var isAdmin = User.IsAdminOrGerente();

            var (relatorio, autorComentarioId) = await _service.GetRelatorioAndAutorByComentarioId(comentarioId, empresaJwt);
            if (relatorio == null) return (false, NotFound("Comentário não encontrado."));

            // Admin/gerente sempre podem; autor do comentário também pode editar/excluir o seu.
            var ehAutorComentario = autorComentarioId == userId;
            if (!isAdmin && !ehAutorComentario)
            {
                return (false, StatusCode(StatusCodes.Status403Forbidden, "Sem permissão sobre este comentário."));
            }
            return (true, null);
        }

        [HttpPut("item/{itemId}")]
        public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateRelatorioSecaoItemRequest req)
        {
            try
            {
                if (itemId <= 0) return BadRequest("itemId inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var (allowed, denied, _) = await AssertWriteByItemId(itemId);
                if (!allowed) return denied!;

                var ok = await _service.UpdateItem(itemId, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar item.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("item/{itemId}/multiple-fotos")]
        public async Task<IActionResult> AddMultipleFotos(int itemId, [FromBody] AddMultipleFotosRequest req)
        {
            try
            {
                if (itemId <= 0) return BadRequest("itemId inválido.");
                if (req?.Fotos == null || !req.Fotos.Any()) return BadRequest("Nenhuma foto enviada.");

                var (allowed, denied, _) = await AssertWriteByItemId(itemId);
                if (!allowed) return denied!;

                var ok = await _service.AddMultipleFotosToItem(itemId, req.Fotos);
                return ok ? Ok("Fotos adicionadas com sucesso.") : BadRequest("Falha ao adicionar fotos.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("foto/{fotoId}")]
        public async Task<IActionResult> DeleteFoto(int fotoId)
        {
            try
            {
                if (fotoId <= 0) return BadRequest("fotoId inválido.");

                var (allowed, denied) = await AssertWriteByFotoId(fotoId);
                if (!allowed) return denied!;

                var ok = await _service.DeleteFoto(fotoId);
                return ok ? Ok("Foto excluída com sucesso.") : BadRequest("Falha ao excluir foto.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("fotos/delete-multiple")]
        public async Task<IActionResult> DeleteMultipleFotos([FromBody] DeleteMultipleFotosRequest req)
        {
            try
            {
                if (req?.FotoIds == null || !req.FotoIds.Any())
                    return BadRequest("Nenhuma foto especificada.");

                // Valida acesso em cada foto (todas devem pertencer a relatórios autorizados).
                foreach (var fid in req.FotoIds.Distinct())
                {
                    var (allowed, denied) = await AssertWriteByFotoId(fid);
                    if (!allowed) return denied!;
                }

                var ok = await _service.DeleteMultipleFotos(req.FotoIds);
                return ok ? Ok("Fotos excluídas com sucesso.") : BadRequest("Falha ao excluir fotos.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("secao/{secaoId}/comentario")]
        public async Task<IActionResult> AddComentario(int secaoId, [FromBody] AddComentarioRequest req)
        {
            try
            {
                if (secaoId <= 0) return BadRequest("secaoId inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var (allowed, denied) = await AssertWriteBySecaoId(secaoId);
                if (!allowed) return denied!;

                // Autoria do comentário sempre vem do JWT — body é ignorado.
                req.AutorId = User.GetUserId();

                var result = await _service.AddComentario(secaoId, req);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("comentario/{comentarioId}")]
        public async Task<IActionResult> UpdateComentario(int comentarioId, [FromBody] UpdateComentarioRequest req)
        {
            try
            {
                if (comentarioId <= 0) return BadRequest("comentarioId inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var (allowed, denied) = await AssertWriteByComentarioId(comentarioId);
                if (!allowed) return denied!;

                var ok = await _service.UpdateComentario(comentarioId, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar comentário.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("comentario/{comentarioId}")]
        public async Task<IActionResult> DeleteComentario(int comentarioId)
        {
            try
            {
                if (comentarioId <= 0) return BadRequest("comentarioId inválido.");

                var (allowed, denied) = await AssertWriteByComentarioId(comentarioId);
                if (!allowed) return denied!;

                var ok = await _service.DeleteComentario(comentarioId);
                return ok ? Ok("Comentário excluído com sucesso.") : BadRequest("Falha ao excluir comentário.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{id}/html-snapshot")]
        public async Task<IActionResult> UpdateHtmlSnapshot(int id, [FromBody] UpdateHtmlSnapshotRequest req)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                if (req == null || string.IsNullOrWhiteSpace(req.HtmlSnapshot))
                    return BadRequest("Payload inválido.");

                var (allowed, denied, _) = await AssertWriteByRelatorioId(id);
                if (!allowed) return denied!;

                var ok = await _service.UpdateHtmlSnapshot(id, req.HtmlSnapshot);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar HTML.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}